#!/bin/bash

# Pod durumlarını kontrol eden script
# Tüm namespace'lerdeki podların Running ve Ready durumda olup olmadığını kontrol eder

set -e

echo "🔍 Pod Durumu Kontrol Ediliyor..."
echo "=================================="
echo ""

# Kontrol edilecek namespace'ler
NAMESPACES=("observability" "database" "app")

ALL_HEALTHY=true
TOTAL_PODS=0
READY_PODS=0
FAILED_PODS=0

# Her namespace için pod durumunu kontrol et
for NS in "${NAMESPACES[@]}"; do
    echo "📦 Namespace: $NS"
    echo "-----------------------------------"
    
    # Namespace'deki pod sayısını al
    POD_COUNT=$(kubectl get pods -n "$NS" --no-headers 2>/dev/null | wc -l | tr -d ' ')
    
    if [ "$POD_COUNT" -eq 0 ]; then
        echo "⚠️  Bu namespace'de pod bulunamadı"
        echo ""
        continue
    fi
    
    TOTAL_PODS=$((TOTAL_PODS + POD_COUNT))
    
    # Her pod'u kontrol et
    while IFS= read -r line; do
        POD_NAME=$(echo "$line" | awk '{print $1}')
        POD_READY=$(echo "$line" | awk '{print $2}')
        POD_STATUS=$(echo "$line" | awk '{print $3}')
        POD_RESTARTS=$(echo "$line" | awk '{print $4}')
        
        # Ready durumunu parse et (örn: "1/1" -> "1" ve "1")
        READY_CURRENT=$(echo "$POD_READY" | cut -d'/' -f1)
        READY_TOTAL=$(echo "$POD_READY" | cut -d'/' -f2)
        
        # Pod durumunu değerlendir
        if [ "$POD_STATUS" = "Running" ] && [ "$READY_CURRENT" = "$READY_TOTAL" ]; then
            echo "✅ $POD_NAME: $POD_STATUS ($POD_READY)"
            READY_PODS=$((READY_PODS + 1))
        elif [ "$POD_STATUS" = "Completed" ]; then
            echo "✅ $POD_NAME: $POD_STATUS (Job)"
            READY_PODS=$((READY_PODS + 1))
        else
            echo "❌ $POD_NAME: $POD_STATUS ($POD_READY) - Restarts: $POD_RESTARTS"
            ALL_HEALTHY=false
            FAILED_PODS=$((FAILED_PODS + 1))
            
            # Hata detaylarını göster
            echo "   📋 Son olaylar:"
            kubectl get events -n "$NS" --field-selector involvedObject.name="$POD_NAME" \
                --sort-by='.lastTimestamp' 2>/dev/null | tail -n 3 | sed 's/^/      /'
        fi
    done < <(kubectl get pods -n "$NS" --no-headers 2>/dev/null)
    
    echo ""
done

# Özet rapor
echo "=================================="
echo "📊 ÖZET RAPOR"
echo "=================================="
echo "Toplam Pod Sayısı:    $TOTAL_PODS"
echo "Hazır Pod Sayısı:     $READY_PODS"
echo "Sorunlu Pod Sayısı:   $FAILED_PODS"
echo ""

if [ "$ALL_HEALTHY" = true ]; then
    echo "✅ TÜM PODLAR SAĞLIKLI VE ÇALIŞIYOR!"
    echo ""
    exit 0
else
    echo "❌ BAZI PODLAR SORUNLU!"
    echo ""
    echo "💡 Sorunlu pod detaylarını görmek için:"
    echo "   kubectl describe pod <pod-name> -n <namespace>"
    echo "   kubectl logs <pod-name> -n <namespace>"
    echo ""
    exit 1
fi
