using Hezarfen.Sehir;
using Hezarfen.Zaman;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hezarfen.Arayuz
{
    /// <summary>
    /// <b>Oyun içi HUD ve duraklat menüsü.</b>
    ///
    /// ## Neden IMGUI
    ///
    /// Bu ekran oyuncuya bilgi vermek ve kaydı çağırmak için var, sanat
    /// için değil. Canvas kurmak yerine <c>OnGUI</c> kullanmak onu tek
    /// dosyada tutuyor ve kalıcı arayüz kararı Faz 8'in sonrasına
    /// kalabiliyor. <b>Geçici olduğu burada yazıyor</b> — yapının kendisi
    /// söylemeli, yorum değil.
    ///
    /// ## Saat EZANÎ gösterilir
    ///
    /// RESEARCH §6: Osmanlı günü <b>gün batımında başlar</b> ve akşam
    /// ezanında saat 12:00'ye kurulur. Oyuncunun "gece yarısı" dediği an
    /// ezanî saatte 5-6 civarındadır ve <b>bu yabancılık kasıtlıdır</b>.
    /// Modern saati göstermek, üç fazdır vakitleri güneşten hesaplamanın
    /// anlamını silerdi.
    /// </summary>
    public class OyunHud : MonoBehaviour
    {
        public ZamanSistemi zaman;
        public KayitBaglayici kayit;
        public AranmaSistemi aranma;

        [Tooltip("Duraklat tuşu.")]
        public Key duraklatTusu = Key.Escape;

        [Tooltip("Hızlı kaydet / hızlı yükle.")]
        public Key kaydetTusu = Key.F5;
        public Key yukleTusu = Key.F9;

        /// <summary>Oyun duraklatıldı mı — test okur.</summary>
        public bool Duraklatildi { get; private set; }

        private string _mesaj = "";
        private float _mesajSonu;
        private GUIStyle _kutu, _yazi;

        private void Awake()
        {
            if (zaman == null) zaman = FindAnyObjectByType<ZamanSistemi>();
            if (kayit == null) kayit = FindAnyObjectByType<KayitBaglayici>();
            if (aranma == null) aranma = FindAnyObjectByType<AranmaSistemi>();
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb[duraklatTusu].wasPressedThisFrame) Duraklat(!Duraklatildi);
            if (kb[kaydetTusu].wasPressedThisFrame) Kaydet();
            if (kb[yukleTusu].wasPressedThisFrame) Yukle();
        }

        /// <summary>Duraklatır ya da devam eder.</summary>
        public void Duraklat(bool dur)
        {
            Duraklatildi = dur;
            Time.timeScale = dur ? 0f : 1f;
            Cursor.lockState = dur ? CursorLockMode.None
                                   : CursorLockMode.Locked;
            Cursor.visible = dur;
        }

        public void Kaydet()
        {
            bool ok = kayit != null && kayit.Kaydet();
            Bildir(ok ? "Kaydedildi." : "KAYDEDILEMEDI.");
        }

        public void Yukle()
        {
            if (kayit == null) { Bildir("Kayit sistemi yok."); return; }
            if (!Kayit.Var) { Bildir("Kayit bulunamadi."); return; }
            Bildir(kayit.Yukle() ? "Yuklendi." : "YUKLENEMEDI.");
        }

        private void Bildir(string m)
        {
            _mesaj = m;
            _mesajSonu = Time.unscaledTime + 3f;
        }

        private void OnGUI()
        {
            _kutu ??= new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, padding = new RectOffset(10, 10, 8, 8) };
            _yazi ??= new GUIStyle(GUI.skin.label) { fontSize = 14 };

            // --- ust serit: tarih, ezani saat, vakit ---
            if (zaman != null)
            {
                string tarih = Kronoloji.Tarih(zaman.yil, zaman.yilinGunu);
                string gun = Kronoloji.HaftaGunu(zaman.yil,
                                                 zaman.yilinGunu).ToString();
                string ezani = zaman.EzaniYazi;
                GUI.Box(new Rect(10, 10, 330, 62), "", _kutu);
                GUI.Label(new Rect(20, 16, 320, 22),
                          $"{tarih}  ·  {gun}", _yazi);
                GUI.Label(new Rect(20, 38, 320, 22),
                          $"ezanî {ezani}  ·  {zaman.Vakit}", _yazi);
            }

            if (aranma != null && aranma.Seviye > 0.01f)
            {
                GUI.Box(new Rect(10, 78, 330, 30), "", _kutu);
                GUI.Label(new Rect(20, 83, 320, 22),
                          $"aranma: %{aranma.Seviye * 100f:F0} "
                          + $"({aranma.SuAn})", _yazi);
            }

            if (Time.unscaledTime < _mesajSonu)
                GUI.Box(new Rect(10, Screen.height - 46, 260, 32),
                        _mesaj, _kutu);

            // --- duraklat ---
            if (!Duraklatildi)
            {
                GUI.Label(new Rect(Screen.width - 250, 10, 240, 20),
                          "ESC duraklat · F5 kaydet · F9 yükle", _yazi);
                return;
            }

            float g = 320f, y = 240f;
            var r = new Rect((Screen.width - g) * 0.5f,
                             (Screen.height - y) * 0.5f, g, y);
            GUI.Box(r, "DURAKLATILDI", _kutu);
            if (GUI.Button(new Rect(r.x + 30, r.y + 50, g - 60, 36),
                           "Devam et")) Duraklat(false);
            if (GUI.Button(new Rect(r.x + 30, r.y + 96, g - 60, 36),
                           "Kaydet")) Kaydet();
            if (GUI.Button(new Rect(r.x + 30, r.y + 142, g - 60, 36),
                           "Yükle")) Yukle();
            if (GUI.Button(new Rect(r.x + 30, r.y + 188, g - 60, 36),
                           "Ana menü"))
            {
                Time.timeScale = 1f;
                UnityEngine.SceneManagement.SceneManager.LoadScene("Acilis");
            }
        }
    }
}
