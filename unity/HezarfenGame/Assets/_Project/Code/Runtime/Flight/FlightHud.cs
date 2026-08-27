using UnityEngine;

namespace Hezarfen.Flight
{
    /// <summary>
    /// Prototip göstergesi: hava hızı, irtifa, variometre, hedefe mesafe, stall uyarısı.
    ///
    /// Neden gerekli: Faz 0'ın sorusu "uçuş eğlenceli mi?" — ama göstergesiz bir uçuş
    /// modeli öğrenilemez. Oyuncu neden düştüğünü ya da neden yükseldiğini göremezse
    /// "ustalık" diye bir şey oluşmaz, sadece rastgelelik hissi kalır.
    ///
    /// Bu HUD geçicidir; nihai oyunda diegetik ipuçlarına (kumaş sesi, kanat titreşimi,
    /// ufuk çizgisi) devredilecek. Şimdilik ayar yapabilmek için sayı lazım.
    /// </summary>
    [AddComponentMenu("Hezarfen/Flight HUD")]
    public class FlightHud : MonoBehaviour
    {
        [Tooltip("Boşsa sahnede aranır.")]
        public GlideController glider;

        [Tooltip("Hedef nesne — mesafe bundan ölçülür.")]
        public Transform target;

        [Tooltip("Variometrenin doyum noktası (m/s).")]
        public float varioScale = 4f;

        private GUIStyle box;
        private GUIStyle warn;

        private void Awake()
        {
            if (glider == null) glider = FindAnyObjectByType<GlideController>();
            if (target == null)
            {
                var t = GameObject.Find("GB_Target_Dogancilar");
                if (t != null) target = t.transform;
            }
        }

        private void OnGUI()
        {
            if (glider == null) return;

            box ??= new GUIStyle(GUI.skin.box)
            {
                fontSize = 15,
                alignment = TextAnchor.UpperLeft,
                padding = new RectOffset(12, 12, 10, 10)
            };
            warn ??= new GUIStyle(GUI.skin.box)
            {
                fontSize = 20,
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };

            var rb = glider.GetComponent<Rigidbody>();
            float altitude = glider.transform.position.y;
            float vertical = rb != null ? rb.linearVelocity.y : 0f;

            float distance = 0f;
            if (target != null)
            {
                Vector3 d = target.position - glider.transform.position;
                distance = new Vector2(d.x, d.z).magnitude;
            }

            string vario = vertical >= 0f ? $"+{vertical:F1}" : $"{vertical:F1}";
            string text =
                $"HAVA HIZI   {glider.AirspeedMps:F1} m/s   ({glider.AirspeedMps * 3.6f:F0} km/h)\n" +
                $"İRTİFA      {altitude:F0} m\n" +
                $"DİKEY       {vario} m/s\n" +
                $"HÜCUM AÇISI {glider.AngleOfAttackDeg:F1}°\n" +
                $"YATIŞ       {glider.BankAngleDeg:F0}°\n" +
                $"HEDEFE      {distance:F0} m\n" +
                $"RÜZGÂR      {glider.WindAtCraft.magnitude:F1} m/s  (dikey {glider.WindAtCraft.y:+0.0;-0.0;0.0})";

            GUI.Label(new Rect(14, 14, 320, 150), text, box);

            DrawVario(vertical);

            if (glider.IsStalled)
            {
                var prev = GUI.color;
                GUI.color = new Color(1f, 0.35f, 0.25f, 0.95f);
                GUI.Label(new Rect(Screen.width * 0.5f - 110f, 40f, 220f, 40f), "STALL — BURNU İNDİR", warn);
                GUI.color = prev;
            }

            GUI.Label(new Rect(14, Screen.height - 46f, 460f, 32f),
                "W/S veya ↑/↓ : burun  •  A/D veya ←/→ : yatış  •  gamepad sol çubuk", box);
        }

        /// <summary>Dikey hız çubuğu — yükseliyor mu batıyor mu, bir bakışta.</summary>
        private void DrawVario(float vertical)
        {
            const float w = 26f, h = 190f;
            float x = 350f, y = 14f;

            GUI.Box(new Rect(x, y, w, h), GUIContent.none);

            float t = Mathf.Clamp(vertical / varioScale, -1f, 1f);
            float mid = y + h * 0.5f;
            float barH = Mathf.Abs(t) * (h * 0.5f - 2f);

            var prev = GUI.color;
            GUI.color = vertical >= 0f
                ? new Color(0.35f, 0.85f, 1f, 0.85f)
                : new Color(1f, 0.45f, 0.35f, 0.85f);

            Rect bar = vertical >= 0f
                ? new Rect(x + 3f, mid - barH, w - 6f, barH)
                : new Rect(x + 3f, mid, w - 6f, barH);

            GUI.DrawTexture(bar, Texture2D.whiteTexture);
            GUI.color = prev;

            // Sifir cizgisi
            GUI.DrawTexture(new Rect(x, mid - 1f, w, 2f), Texture2D.whiteTexture);
        }
    }
}
