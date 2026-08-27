using UnityEngine;

namespace Hezarfen.Gis
{
    /// <summary>
    /// GeoJSON'dan sahneye alınmış tek bir coğrafi öğe (kıyı halkası, düzeltme alanı).
    ///
    /// Noktalar **yerel metre** cinsindendir; orijin Galata Kulesi tabanı, y = deniz
    /// seviyesi (plan Faz 1 madde 4). Yani bir noktanın konumu doğrudan oyun dünyası
    /// koordinatıdır — dönüşüm gerekmez.
    ///
    /// Görselleştirme gizmo iledir, LineRenderer ile değil: bunlar oyuncunun göreceği
    /// geometri değil, ÜRETİM REFERANSIDIR. LineRenderer HDRP'de malzeme ister, build'e
    /// sızar ve yanlışlıkla oynanışa karışabilir; gizmo yalnızca Editor'de yaşar.
    /// </summary>
    [AddComponentMenu("Hezarfen/GIS Feature")]
    public class GisFeature : MonoBehaviour
    {
        [Tooltip("modern_shoreline | correction_zone")]
        public string layer = "";

        public string featureId = "";
        public string featureName = "";

        [Tooltip("remove_fill | convert_to_gardens | review_only")]
        public string action = "";

        [TextArea(2, 6)]
        public string note = "";

        [Tooltip("Kapalı halka mı (alan) yoksa açık çizgi mi (kıyı)?")]
        public bool closed;

        [Tooltip("Yerel metre; orijin Galata Kulesi, y = deniz seviyesi.")]
        public Vector3[] points = new Vector3[0];

        [Tooltip("Landmark işaretçisinin yüksekliği (m) — sahnede uzaktan görünsün diye.")]
        public float markerHeight = 120f;

        public Color GizmoColor
        {
            get
            {
                if (layer == "landmark")
                {
                    // S-kademe ucus ekseni, A-kademe suriçi (plan Bolum 8)
                    return action == "S" ? new Color(1f, 0.80f, 0.25f)
                                         : new Color(0.75f, 0.60f, 1f);
                }
                if (layer == "wall_gate") return new Color(1f, 0.55f, 0.15f);
                if (layer == "district") return new Color(0.35f, 0.95f, 0.85f);
                if (layer.StartsWith("wall_"))
                {
                    // Kara suru BUGUN AYAKTA (izlendi); deniz surlari taslak kiyimizdan
                    // TURETILDI; Galata KABA taslaktir. Uc ayri kanit sinifi, uc ayri renk —
                    // sahnede hangisine ne kadar guvenilecegi bakisla anlasilmali.
                    if (layer == "wall_land") return new Color(0.95f, 0.90f, 0.80f);
                    if (layer == "wall_galata") return new Color(1f, 0.45f, 0.55f);
                    return new Color(0.85f, 0.70f, 0.45f);
                }
                switch (action)
                {
                    case "remove_fill": return new Color(1f, 0.35f, 0.30f);        // dolgu geri alinacak
                    case "convert_to_gardens": return new Color(0.45f, 0.90f, 0.40f); // bostan olacak
                    case "review_only": return new Color(1f, 0.85f, 0.30f);        // yalnizca inceleme
                    default: return new Color(0.45f, 0.85f, 1f);                   // kiyi cizgisi
                }
            }
        }

        /// <summary>Tek noktalı öğe mi (landmark) yoksa çizgi/alan mı?</summary>
        public bool IsMarker => points != null && points.Length == 1;

        /// <summary>Öğenin toplam uzunluğu (m) — inceleme paketlerinde raporlanır.</summary>
        public float LengthMeters
        {
            get
            {
                if (points == null || points.Length < 2) return 0f;
                float total = 0f;
                for (int i = 1; i < points.Length; i++)
                    total += Vector3.Distance(points[i - 1], points[i]);
                if (closed) total += Vector3.Distance(points[points.Length - 1], points[0]);
                return total;
            }
        }

        private void OnDrawGizmos()
        {
            if (points == null || points.Length == 0) return;

            Gizmos.color = GizmoColor;
            Gizmos.matrix = transform.localToWorldMatrix;

            if (IsMarker)
            {
                // Dikey cubuk + kure: 15 km'lik bir dunyada bir landmark'i
                // Sahne goruntusunde bulmanin tek pratik yolu.
                Vector3 baseP = points[0];
                Vector3 topP = baseP + Vector3.up * markerHeight;
                Gizmos.DrawLine(baseP, topP);
                Gizmos.DrawWireSphere(topP, markerHeight * 0.06f);
                return;
            }

            for (int i = 1; i < points.Length; i++)
                Gizmos.DrawLine(points[i - 1], points[i]);

            if (closed)
                Gizmos.DrawLine(points[points.Length - 1], points[0]);
        }
    }
}
