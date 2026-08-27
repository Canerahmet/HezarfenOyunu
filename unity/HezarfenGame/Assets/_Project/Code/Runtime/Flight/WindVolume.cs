using UnityEngine;

namespace Hezarfen.Flight
{
    /// <summary>
    /// Elle yerleştirilen rüzgâr hacmi: yamaç yükselticisi, termik veya çökelme bölgesi.
    /// Sahne görünümünde gizmo ile çizilir — plan Görev 6'nın "gizmo'yla yerleştirilebilir"
    /// şartı budur.
    ///
    /// Tasarım ilkesi (plan Bölüm 2): rüzgâr **okunabilir** olmalı. Bu yüzden hacimler
    /// keskin sınırlı değil, kenara doğru sönümlenen alanlardır — oyuncu kenarda "azaldığını"
    /// hissedip merkeze doğru düzeltme yapabilsin.
    /// </summary>
    [AddComponentMenu("Hezarfen/Wind Volume")]
    public class WindVolume : MonoBehaviour
    {
        public enum VolumeShape
        {
            /// <summary>Küresel, merkezde en güçlü. Yerel/nokta etkiler için.</summary>
            Sphere,

            /// <summary>Yamaç yükselticisi için: sırt boyunca uzanan kutu.</summary>
            Box,

            /// <summary>
            /// Termik sütunu: yatayda dairesel, düşeyde uzun.
            /// Gerçek termikler sütundur — küre olarak modellenirse aygıt biraz
            /// alçalınca termikten çıkar ve içinde dönerek yükselmek imkânsızlaşır.
            /// (Bu tam olarak yaşandı; simülasyonla bulundu.)
            /// </summary>
            Column
        }

        public enum WindKind
        {
            /// <summary>Termik veya yamaç: yalnızca dikey bileşen.</summary>
            Lift,

            /// <summary>Serbest yön: yerel eksende verilen vektör.</summary>
            Directional
        }

        [Header("Şekil")]
        public VolumeShape shape = VolumeShape.Sphere;

        [Tooltip("Sphere/Column için yarıçap (m)")]
        public float radius = 120f;

        [Tooltip("Column için toplam yükseklik (m). Merkez transform konumundadır.")]
        public float columnHeight = 400f;

        [Tooltip("Box için tam boyut (m)")]
        public Vector3 boxSize = new Vector3(300f, 150f, 80f);

        [Header("Rüzgâr")]
        public WindKind kind = WindKind.Lift;

        [Tooltip("Lift: merkezdeki dikey hız (m/s). Pozitif = yükselen, negatif = çökelen.")]
        public float liftSpeed = 3f;

        [Tooltip("Directional: yerel eksende rüzgâr vektörü (m/s)")]
        public Vector3 localWind = Vector3.forward * 5f;

        [Header("Sönümleme")]
        [Tooltip("Merkezden kenara doğru şiddet. Sol=merkez, sağ=kenar.")]
        public AnimationCurve falloff = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

        [Header("Görsel")]
        public Color gizmoColor = new Color(0.3f, 0.8f, 1f, 0.25f);

        /// <summary>
        /// Verilen dünya noktasındaki rüzgâr katkısı. Hacim dışındaysa sıfır.
        /// </summary>
        public Vector3 SampleWind(Vector3 worldPos)
        {
            float t = NormalizedDistance(worldPos);
            if (t >= 1f) return Vector3.zero;

            float strength = falloff.Evaluate(Mathf.Clamp01(t));

            return kind == WindKind.Lift
                ? Vector3.up * (liftSpeed * strength)
                : transform.TransformDirection(localWind) * strength;
        }

        /// <summary>0 = merkez, 1 = kenar (ve dışı).</summary>
        private float NormalizedDistance(Vector3 worldPos)
        {
            if (shape == VolumeShape.Sphere)
            {
                if (radius <= Mathf.Epsilon) return 1f;
                return Vector3.Distance(worldPos, transform.position) / radius;
            }

            if (shape == VolumeShape.Column)
            {
                if (radius <= Mathf.Epsilon || columnHeight <= Mathf.Epsilon) return 1f;

                Vector3 d = worldPos - transform.position;

                // Dusey YALNIZCA sinir koyar: sutunun disindaysan kaldirma yok.
                float vertical = Mathf.Abs(d.y) / (columnHeight * 0.5f);
                if (vertical >= 1f) return 1f;

                // Sonumlemeyi yalnizca YATAY uzaklik belirler. Gercek termik
                // sutunu boyunca yaklasik ayni gucte kalir; dusey mesafeyi de
                // sonumlemeye katmak, alcalan aygitin kaldirmayi yitirmesine
                // yol acar ve sutun olmanin anlamini ortadan kaldirir.
                return new Vector2(d.x, d.z).magnitude / radius;
            }

            // Box: yerel uzayda her eksende normalize et, en büyüğünü al (Chebyshev)
            Vector3 local = transform.InverseTransformPoint(worldPos);
            Vector3 half = boxSize * 0.5f;

            if (half.x <= Mathf.Epsilon || half.y <= Mathf.Epsilon || half.z <= Mathf.Epsilon) return 1f;

            return Mathf.Max(
                Mathf.Abs(local.x) / half.x,
                Mathf.Abs(local.y) / half.y,
                Mathf.Abs(local.z) / half.z);
        }

        private void OnDrawGizmosSelected()
        {
            DrawGizmo(true);
        }

        private void OnDrawGizmos()
        {
            DrawGizmo(false);
        }

        private void DrawGizmo(bool selected)
        {
            Color c = gizmoColor;
            if (kind == WindKind.Lift)
            {
                // Yukselen mavi, cokelen kirmizi — bir bakista okunsun.
                c = liftSpeed >= 0f ? new Color(0.3f, 0.8f, 1f, c.a) : new Color(1f, 0.4f, 0.3f, c.a);
            }
            if (selected) c.a = Mathf.Min(1f, c.a * 2.2f);

            Gizmos.color = c;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);

            if (shape == VolumeShape.Sphere)
            {
                Gizmos.DrawWireSphere(Vector3.zero, radius);
            }
            else if (shape == VolumeShape.Column)
            {
                // Sutunu alt/orta/ust cemberler + dikey kenarlarla ciz
                float h = columnHeight * 0.5f;
                for (int ring = -1; ring <= 1; ring++)
                {
                    Vector3 ringCenter = Vector3.up * (h * ring);
                    const int seg = 24;
                    Vector3 prevPt = ringCenter + new Vector3(radius, 0f, 0f);
                    for (int i = 1; i <= seg; i++)
                    {
                        float a = i / (float)seg * Mathf.PI * 2f;
                        Vector3 pt = ringCenter + new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius);
                        Gizmos.DrawLine(prevPt, pt);
                        prevPt = pt;
                    }
                }
                Gizmos.DrawLine(new Vector3(radius, -h, 0f), new Vector3(radius, h, 0f));
                Gizmos.DrawLine(new Vector3(-radius, -h, 0f), new Vector3(-radius, h, 0f));
                Gizmos.DrawLine(new Vector3(0f, -h, radius), new Vector3(0f, h, radius));
                Gizmos.DrawLine(new Vector3(0f, -h, -radius), new Vector3(0f, h, -radius));
            }
            else
            {
                Gizmos.DrawWireCube(Vector3.zero, boxSize);
            }

            // Yon oklari
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.color = new Color(c.r, c.g, c.b, 1f);

            Vector3 dir = kind == WindKind.Lift
                ? Vector3.up * Mathf.Sign(liftSpeed)
                : transform.TransformDirection(localWind).normalized;

            float len = shape == VolumeShape.Sphere ? radius : boxSize.magnitude * 0.3f;
            Vector3 from = transform.position;
            Gizmos.DrawLine(from, from + dir * len);
            Gizmos.DrawLine(from + dir * len, from + dir * len * 0.8f + Vector3.right * len * 0.08f);
            Gizmos.DrawLine(from + dir * len, from + dir * len * 0.8f - Vector3.right * len * 0.08f);
        }
    }
}
