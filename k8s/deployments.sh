#!/bin/bash

# Minikube üzerinde Full Monitoring Stack kurulum scripti
# Prometheus, Grafana, Loki, Tempo, OpenTelemetry Collector

set -e

echo "🚀 Minikube Full Monitoring Stack Kurulumu"
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

# Monitoring namespace oluştur
echo ""
echo "📦 Monitoring namespace oluşturuluyor..."
kubectl create namespace monitoring --dry-run=client -o yaml | kubectl apply -f -

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
if helm list -n monitoring | grep -q "monitoring"; then
    echo "   → Prometheus Stack zaten kurulu, atlanıyor..."
else
    helm install monitoring prometheus-community/kube-prometheus-stack \
        --namespace monitoring \
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
if helm list -n monitoring | grep -q "loki"; then
    echo "   → Loki Stack zaten kurulu, atlanıyor..."
else
    helm install loki grafana/loki-stack \
        --namespace monitoring \
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

# 4. Uygulama (PostgreSQL + Stock-Mgmt) kurulumu
echo ""
echo "🚀 4/4 - Stock-Mgmt uygulaması kuruluyor..."

# Manifesto dosyalarının varlığını kontrol et
APP_MANIFESTS=("postgres.yaml" "deployment.yaml")
for manifest in "${APP_MANIFESTS[@]}"; do
    if [ ! -f "k8s/$manifest" ]; then
        echo "❌ k8s/$manifest bulunamadı"
        exit 1
    fi
done

kubectl apply -f k8s/postgres.yaml

# Migration varsa uygula
if [ -f "k8s/migration.yaml" ]; then
    echo "   → Eski migration job'u temizleniyor..."
    kubectl delete job stock-mgmt-migration -n default --ignore-not-found
    kubectl apply -f k8s/migration.yaml
    echo "   → Migration job uygulandı"
    sleep 5
fi

kubectl apply -f k8s/deployment.yaml

echo "✓ Uygulama kuruldu"

# Pod'ların hazır olmasını bekle
echo ""
echo "⏳ Pod'ların başlaması bekleniyor (60 saniye)..."
sleep 60

# Pod durumunu kontrol et
echo ""
echo "📊 Pod Durumu:"
echo ""
echo "=== Monitoring Namespace ==="
kubectl get pods -n monitoring
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
echo "📊 Prometheus:             kubectl port-forward -n monitoring svc/monitoring-kube-prometheus-prometheus 9090"
echo "🚨 Alertmanager:           kubectl port-forward -n monitoring svc/monitoring-kube-prometheus-alertmanager 9093"
echo "📝 Loki:                   kubectl port-forward -n monitoring svc/loki 3100"
echo "🔎 Tempo (Grafana datasouce): URL: http://tempo:3200 (cluster-içi)"
echo "   Tempo Query (Jaeger UI): kubectl port-forward -n monitoring svc/tempo-query 16686:16686 → http://localhost:16686"
echo "📝 Stock-Mgmt API:         http://$MINIKUBE_IP (LoadBalancer service)"
echo "🔌 OTLP gRPC:              $MINIKUBE_IP:30317"
echo "🔌 OTLP HTTP:              $MINIKUBE_IP:30318"
echo ""
echo "💡 Kubernetes Dashboard:"
echo "   minikube dashboard"
echo ""
echo "📖 Logs görüntüle:"
echo "   kubectl logs -f deployment/tempo -n monitoring"
echo "   kubectl logs -f deployment/otel-collector -n monitoring"
echo "   kubectl logs -f deployment/stock-mgmt -n default"
echo "   kubectl logs -f statefulset/prometheus-monitoring-kube-prometheus-prometheus -n monitoring"
echo ""
echo "📊 Service'leri kontrol et:"
echo "   kubectl get svc -n monitoring"
echo "   kubectl get svc -n default"
echo ""
echo "🔧 Grafana'da Loki Datasource Ekle:"
echo "   URL: http://loki:3100"
echo "🔧 Grafana'da Tempo Datasource Ekle:"
echo "   URL: http://tempo:3200"
echo ""
echo "🧹 Temizlik için:"
echo "   helm uninstall monitoring loki -n monitoring"
echo "   kubectl delete namespace monitoring"
echo "   kubectl delete deployment,service,configmap,pvc --all -n default"
