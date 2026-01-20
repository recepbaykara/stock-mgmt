#!/bin/bash

# Minikube üzerinde Full observability Stack kurulum scripti
# Prometheus, Grafana, Loki, Tempo, OpenTelemetry Collector

set -e

echo "🚀 Minikube Full observability Stack Kurulumu"
echo "==========================================="

# Minikube kontrolü
if ! command -v minikube &> /dev/null; then
    echo "❌ Minikube yüklü değil. Lütfen minikube'u yükleyin."
    exit 1
fi

# Minikube'ün çalışıp çalışmadığını kontrol et
if ! minikube status &> /dev/null; then
    echo "📦 Minikube başlatılıyor..."
    minikube start --driver=docker --memory=4096 --cpus=2
fi

echo "✓ Minikube çalışıyor"

# Helm kontrolü
if ! command -v helm &> /dev/null; then
    echo "❌ Helm yüklü değil. Lütfen Helm'i yükleyin: https://helm.sh/docs/intro/install/"
    exit 1
fi

echo "✓ Helm hazır"

# Docker env ayarla (local images için)
echo "🐳 Docker environment ayarlanıyor..."
eval $(minikube docker-env)

# Kubectl kontrol et
if ! command -v kubectl &> /dev/null; then
    echo "❌ kubectl yüklü değil."
    exit 1
fi

echo "✓ kubectl hazır"

# observability namespace oluştur
echo ""
echo "📦 observability namespace oluşturuluyor..."
kubectl create namespace observability --dry-run=client -o yaml | kubectl apply -f -

# database namespace oluştur
echo ""
echo "📦 database namespace oluşturuluyor..."
kubectl create namespace database --dry-run=client -o yaml | kubectl apply -f -

# app namespace oluştur
echo ""
echo "📦 app namespace oluşturuluyor..."
kubectl create namespace app --dry-run=client -o yaml | kubectl apply -f -

# Helm repositories ekle
echo ""
echo "📚 Helm repositories ekleniyor..."
helm repo add prometheus-community https://prometheus-community.github.io/helm-charts
helm repo add grafana https://grafana.github.io/helm-charts
helm repo update

echo "✓ Helm repositories güncellendi"

# 1. Kube-Prometheus-Stack (Prometheus + Grafana + Alertmanager) kurulumu
echo ""
echo "📊 1/4 - Prometheus Stack kuruluyor..."
if helm list -n observability | grep -q "observability"; then
    echo "   → Prometheus Stack zaten kurulu, atlanıyor..."
else
    helm install observability prometheus-community/kube-prometheus-stack \
        --namespace observability \
        --set prometheus.prometheusSpec.retention=7d \
        --set prometheus.prometheusSpec.resources.requests.memory=512Mi \
        --set grafana.enabled=true \
        --set grafana.adminPassword=admin \
        --set grafana.service.type=NodePort \
        --set grafana.service.nodePort=30080 \
        --wait --timeout 5m
    echo "✓ Prometheus Stack kuruldu"
fi

# 2. Loki Stack (Loki + Promtail) kurulumu
echo ""
echo "📝 2/4 - Loki Stack kuruluyor..."
if helm list -n observability | grep -q "loki"; then
    echo "   → Loki Stack zaten kurulu, atlanıyor..."
else
    helm install loki grafana/loki-stack \
        --namespace observability \
        --set loki.persistence.enabled=false \
        --set promtail.enabled=true \
        --wait --timeout 5m
    echo "✓ Loki Stack kuruldu"
fi

# 3. Tempo + OpenTelemetry Collector manifest'leri uygula
echo ""
echo "🔍 3/4 - Tempo ve OpenTelemetry Collector kuruluyor..."

# Manifesto dosyalarının varlığını kontrol et
MANIFESTS=("otel-configmap.yaml" "tempo.yaml" "otel-collector.yaml")
for manifest in "${MANIFESTS[@]}"; do
    if [ ! -f "k8s/$manifest" ]; then
        echo "❌ k8s/$manifest bulunamadı"
        exit 1
    fi
done

kubectl apply -f k8s/otel-configmap.yaml
kubectl apply -f k8s/tempo.yaml
kubectl apply -f k8s/otel-collector.yaml

# (Opsiyonel) ServiceMonitor mevcutsa uygula
if [ -f "k8s/otel-servicemonitor.yaml" ]; then
    kubectl apply -f k8s/otel-servicemonitor.yaml
fi

echo "✓ Tempo ve OpenTelemetry Collector kuruldu"

# 4. PostgreSQL kurulumu (database namespace)
echo ""
echo "🗄️  4/5 - PostgreSQL kuruluyor (database namespace)..."

# Manifesto dosyalarının varlığını kontrol et
DB_MANIFESTS=("pv.yaml" "pvc.yaml" "postgres.yaml")
for manifest in "${DB_MANIFESTS[@]}"; do
    if [ ! -f "k8s/$manifest" ]; then
        echo "❌ k8s/$manifest bulunamadı"
        exit 1
    fi
done

kubectl apply -f k8s/pv.yaml -n database
kubectl apply -f k8s/pvc.yaml -n database
kubectl apply -f k8s/postgres.yaml -n database

echo "✓ PostgreSQL kuruldu"

# Migration varsa uygula (database namespace)
if [ -f "k8s/migration.yaml" ]; then
    echo ""
    echo "📦 Migration job kuruluyor (database namespace)..."
    echo "   → Eski migration job'u temizleniyor..."
    kubectl delete job stock-mgmt-migration -n database --ignore-not-found
    kubectl apply -f k8s/migration.yaml -n database
    echo "   → Migration job uygulandı"
    sleep 5
fi

# 5. Stock-Mgmt uygulaması kurulumu (app namespace)
echo ""
echo "🚀 5/5 - Stock-Mgmt uygulaması kuruluyor (app namespace)..."

# Manifesto dosyalarının varlığını kontrol et
APP_MANIFESTS=("deployment.yaml")
for manifest in "${APP_MANIFESTS[@]}"; do
    if [ ! -f "k8s/$manifest" ]; then
        echo "❌ k8s/$manifest bulunamadı"
        exit 1
    fi
done

kubectl apply -f k8s/deployment.yaml -n app

echo "✓ Uygulama kuruldu"

# Pod'ların hazır olmasını bekle
echo ""
echo "⏳ Pod'ların başlaması bekleniyor (60 saniye)..."
sleep 60

# Pod durumunu kontrol et
echo ""
echo "📊 Pod Durumu:"
echo ""
echo "=== observability Namespace ==="
kubectl get pods -n observability
echo ""
echo "=== database Namespace ==="
kubectl get pods -n database
echo ""
echo "=== app Namespace ==="
kubectl get pods -n appment.yaml -n app

echo "✓ Uygulama kuruldu"

# Pod'ların hazır olmasını bekle
echo ""
echo "⏳ Pod'ların başlaması bekleniyor (60 saniye)..."
sleep 60

# Pod durumunu kontrol et
echo ""
echo "📊 Pod Durumu:"
echo ""
echo "=== observability Namespace ==="
kubectl get pods -n observability
echo ""
echo "=== Default Namespace ==="
kubectl get pods -n default

# Minikube IP'sini al
MINIKUBE_IP=$(minikube ip)

echo ""
echo "✅ Kurulum tamamlandı!"
echo ""
echo "🌐 Erişim Adresleri:"
echo "==================="
echo ""
echo "📊 Grafana:                http://$MINIKUBE_IP:30080 (admin/admin)"
echo "📊 Prometheus:             kubectl port-forward -n observability svc/observability-kube-prometheus-prometheus 9090"
echo "🚨 Alertmanager:           kubectl port-forward -n observability svc/observability-kube-prometheus-alertmanager 9093"
echo "📝 Loki:                   kubectl port-forward -n observability svc/loki 3100"
echo "🔎 Tempo (Grafana datasouce): URL: http://tempo:3200 (cluster-içi)"
echo "   Tempo Query (Jaeger UI): kubectl port-forward -n observability svc/tempo-query 16686:16686 → http://localhost:16686"
echo "📝 Stock-Mgmt API:         http://$MINIKUBE_IP (LoadBalancer service)"
echo "🔌 OTLP gRPC:              $MINIKUBE_IP:30317"
echo "🔌 OTLP HTTP:              $MINIKUBE_IP:30318"
echo ""
echo "💡 Kubernetes Dashboard:"
echo "   minikube dashboard"
echo ""
echo "📖 Logs görüntüle:"app"
echo "   kubectl logs -f deployment/postgres -n database"
echo "   kubectl logs -f statefulset/prometheus-observability-kube-prometheus-prometheus -n observability"
echo ""
echo "📊 Service'leri kontrol et:"
echo "   kubectl get svc -n observability"
echo "   kubectl get svc -n database"
echo "   kubectl get svc -n app
echo "📊 Service'leri kontrol et:"
echo "   kubectl get svc -n observability"
echo "   kubectl get svc -n default"
echo ""
echo "🔧 Grafana'da Loki Datasource Ekle:"
echo "   URL: http://loki:3100"
echo "🔧 Grafana'da Tempo Datasource Ekle:" database app
echo ""
echo "🧹 Temizlik için:"
echo "   helm uninstall observability loki -n observability"
echo "   kubectl delete namespace observability"
echo "   kubectl delete deployment,service,configmap,pvc --all -n default"
