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

            // 1920x1080 SANAL UZAY — OyunHud ile ayni sozlesme.
            //
            // Bu HUD sabit piksel dikdortgenlerine ciziyordu; yani
            // 4K'da yuruyus HUD'i normal, ucus HUD'i yari boyda
            // goruluyordu. Ayni oyunun iki arayuzu ayni olcegi
            // kullanmali.
            var eskiMatris = GUI.matrix;
            float k = Mathf.Min(Screen.width / 1920f, Screen.height / 1080f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(k, k, 1f));
            float _en = Screen.width / k, _boy = Screen.height / k;

            var rb = glider.GetComponent<Rigidbody>();
            float altitude = glider.transform.position.y;
            float hamDikey = rb != null ? rb.linearVelocity.y : 0f;

            // NETTO VARIO: HAVAYA GORE, YERE GORE DEGIL.
            //
            // ADR 0083 tam bu hatayi otomatik pilotta bulup duzeltti —
            // dalisdan alinan hizin irtifaya cevrilmesi "termik" diye
            // okunuyor ve bir dal-zoom-don limit cevrimi doguruyordu.
            // Ama **oyuncunun baktigi alet** duzeltilmemisti: o hala
            // yer eksenli dikey hizi gosteriyordu, yani otomatik
            // pilotu yaniltan sinyalin aynisini.
            //
            // Termik +1,87 m/s ve 33 derece yatista batis 2,12 m/s
            // olan bir modelde oyuncunun dogru karar verme sansi,
            // aletin dogru sayiyi gostermesine bagli.
            float ruzgarY = glider.WindAtCraft.y;
            float vertical = hamDikey - ruzgarY;

            float distance = 0f;
            string hedefYon = "", yetisme = "";
            if (target != null)
            {
                Vector3 d = target.position - glider.transform.position;
                distance = new Vector2(d.x, d.z).magnitude;
                hedefYon = YonAdi(d);

                // SUZULME KONISI: OYUNUN TEK GERCEK SORUSU.
                //
                // HUD "HEDEFE 3312 m" yaziyordu — bir bilgi degil bir
                // sayi: oyuncu ne yone gidecegini de, yetisip
                // yetismeyecegini de bilmiyordu. Oysa cevabin butun
                // terimleri elde: irtifa farki ve anlik L/D.
                //
                // Bugun bunu ancak yere cakildiginda ogreniyor. Bir
                // suzulus oyununda "yetisiyor mu" sorusunun cevabi
                // UCUS BOYUNCA gorunmeli, ucus bitince degil.
                float ld = glider.CurrentDrag > 1e-3f
                           ? glider.CurrentLift / glider.CurrentDrag : 0f;
                float ulasilir = Mathf.Max(0f, altitude - target.position.y) * ld;
                yetisme = ulasilir >= distance
                    ? "YETİŞİYOR"
                    : $"{distance - ulasilir:F0} m EKSİK";
            }

            string vario = vertical >= 0f ? $"+{vertical:F1}" : $"{vertical:F1}";
            string text =
                $"HAVA HIZI   {glider.AirspeedMps:F1} m/s   ({glider.AirspeedMps * 3.6f:F0} km/h)\n" +
                $"İRTİFA      {altitude:F0} m\n" +
                $"DİKEY       {vario} m/s\n" +
                $"HÜCUM AÇISI {glider.AngleOfAttackDeg:F1}°\n" +
                $"YATIŞ       {glider.BankAngleDeg:F0}°\n" +
                $"HEDEFE      {distance:F0} m  {hedefYon}  {yetisme}\n" +
                $"RÜZGÂR      {glider.WindAtCraft.magnitude:F1} m/s "
                + $"{YonAdi(glider.WindAtCraft)} (dikey {ruzgarY:+0.0;-0.0;0.0})";

            GUI.Label(new Rect(14, 14, 340, 165), text, box);

            DrawVario(vertical);

            if (glider.IsStalled)
            {
                var prev = GUI.color;
                GUI.color = new Color(1f, 0.35f, 0.25f, 0.95f);
                GUI.Label(new Rect(_en * 0.5f - 110f, 40f, 220f, 40f), "STALL — BURNU İNDİR", warn);
                GUI.color = prev;
            }

            GUI.Label(new Rect(14, _boy - 46f, 480f, 32f), Tuslar(), box);
            GUI.matrix = eskiMatris;
        }

        /// <summary>
        /// Kumanda satırı — <b>elindeki aygıta göre</b>.
        ///
        /// Ekranda "W/S veya ↑/↓" yazıyordu ve kolla oynayan oyuncunun
        /// elinde o tuşlar yok. Uçuşun kolla oynanabilmesi kodda
        /// düzeltilmişti; arayüzde düzeltilmemişti.
        /// </summary>
        private static string Tuslar() =>
            UnityEngine.InputSystem.Gamepad.current != null
                ? "Sol çubuk: burun ve yatış  •  RB: kanat  •  Start: duraklat"
                : "W/S veya ↑/↓ : burun  •  A/D veya ←/→ : yatış  •  G: kanat";

        /// <summary>
        /// Rüzgârın geldiği yön, okunur adla.
        ///
        /// HUD yalnız <c>magnitude</c> yazıyordu — yani oyuncu rüzgârın
        /// <b>olduğunu</b> biliyor, <b>nereden</b> geldiğini bilmiyordu.
        /// Süzülüşte kaldıracı bulmanın tek ipucu bu ve o ipucu ekranda
        /// yoktu.
        /// </summary>
        private static string YonAdi(Vector3 v)
        {
            var yatay = new Vector2(v.x, v.z);
            if (yatay.magnitude < 0.5f) return "";
            float aci = Mathf.Atan2(yatay.x, yatay.y) * Mathf.Rad2Deg;
            if (aci < 0f) aci += 360f;
            string[] ad = { "K", "KD", "D", "GD", "G", "GB", "B", "KB" };
            return ad[Mathf.RoundToInt(aci / 45f) % 8] + "'ya";
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
