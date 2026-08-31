using Hezarfen.Player;
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
        public EtkilesimAlgila etkilesim;
        public Envanter envanter;
        public GorevYonetici gorev;

        [Tooltip("Duraklat tuşu.")]
        public Key duraklatTusu = Key.Escape;

        [Tooltip("Hızlı kaydet / hızlı yükle.")]
        public Key kaydetTusu = Key.F5;
        public Key yukleTusu = Key.F9;

        [Tooltip("Etkileşim tuşu.")]
        public Key etkilesimTusu = Key.E;

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
            if (etkilesim == null)
                etkilesim = FindAnyObjectByType<EtkilesimAlgila>();
            if (envanter == null) envanter = FindAnyObjectByType<Envanter>();
            if (gorev == null) gorev = FindAnyObjectByType<GorevYonetici>();
            if (_oyuncuT == null)
            {
                var go = GameObject.Find("OYUNCU");
                if (go != null) _oyuncuT = go.transform;
            }
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb[duraklatTusu].wasPressedThisFrame) Duraklat(!Duraklatildi);
            if (kb[kaydetTusu].wasPressedThisFrame) Kaydet();
            if (kb[yukleTusu].wasPressedThisFrame) Yukle();

            // DURAKLATILMISKEN ETKILESIM YOK: menu acikken imlec serbest,
            // bakis olu ve "onunde duran sey" artik oyuncunun secimi
            // degil, duraklattigi andan kalma bir kaza.
            if (!Duraklatildi && etkilesim != null
                && kb[etkilesimTusu].wasPressedThisFrame)
            {
                string neydi = etkilesim.Ipucu;
                if (etkilesim.Tetikle()) Bildir(neydi + " · alindi");
                else if (neydi.Length > 0) Bildir("Kese dolu.");
            }
        }

        /// <summary>Duraklatır ya da devam eder.</summary>
        public void Duraklat(bool dur)
        {
            Duraklatildi = dur;
            Time.timeScale = dur ? 0f : 1f;

            // IMLECIN VE BAKISIN TEK SAHIBI BURASI.
            //
            // Once burasi yalniz `Cursor`u yaziyordu; `WalkController` ise
            // kendi `looking` alanini Esc ile ayri ceviriyordu. Iki sahip,
            // iki deger: "Devam et"e tiklayan oyuncunun imleci kilitleniyor
            // ama bakisi olu kaliyordu. Artik ikisi birlikte set ediliyor.
            var yurume = Object.FindAnyObjectByType<Player.WalkController>();
            if (yurume != null) yurume.Capture(!dur);
            else
            {
                Cursor.lockState = dur ? CursorLockMode.None
                                       : CursorLockMode.Locked;
                Cursor.visible = dur;
            }
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

        /// <summary>
        /// Hedefin oyuncuya göre yönü — <b>pusula yerine geçen şey</b>.
        ///
        /// Oyunda harita, pusula ve işaretçi yok ve bu bir tercih
        /// olarak savunulabilir; ama hiçbir yön bilgisi olmaması bir
        /// tercih değil, eksiklik. Dönemin kendi dili kullanılıyor:
        /// bir yer "kuzeyde" değil, <b>şu semte doğru</b>dur. Şimdilik
        /// sekiz yön; kalıcı çözüm rehber rüzgâr (`WindField` zaten
        /// sahnede) ve ayrı bir iş.
        /// </summary>
        private string YonAdi(Vector3? hedef)
        {
            if (hedef == null || _oyuncuT == null) return "—";
            var d = hedef.Value - _oyuncuT.position;
            float aci = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
            if (aci < 0f) aci += 360f;
            string[] ad = { "kuzey", "kuzeydoğu", "doğu", "güneydoğu",
                            "güney", "güneybatı", "batı", "kuzeybatı" };
            return ad[Mathf.RoundToInt(aci / 45f) % 8];
        }

        private Transform _oyuncuT;

        /// <summary>Envanter kaleminin oyuncuya görünen adı.</summary>
        private static string EsyaAdi(EsyaTuru t) => t switch
        {
            EsyaTuru.Su => "su",
            EsyaTuru.Odun => "odun",
            EsyaTuru.Sebze => "sebze",
            EsyaTuru.Ekmek => "ekmek",
            EsyaTuru.KanatParcasi => "kanat parçası",
            _ => t.ToString(),
        };

        /// <summary>Sanal ekran genişliği — bütün koordinatlar buna göre.</summary>
        private const float SanalEn = 1920f;

        /// <summary>Sanal ekran yüksekliği.</summary>
        private const float SanalBoy = 1080f;

        private float _en, _boy;

        private void OnGUI()
        {
            // HUD SABIT PIKSELDEYDI — MENU DEGILDI.
            //
            // Acilis menusu `ScaleWithScreenSize` ile 1920x1080'e
            // gore olcekleniyor; oyun ici HUD ise sabit piksel
            // kullaniyordu. Sonuc: 330 px'lik bir kutu 1280x800'de
            // ekran genisliginin %26'si, 3840x2160'ta %8,6; 14 punto
            // yazi 4K'da ~7 puntoya duser. Iki arayuz iki ayri
            // dunyada yasiyordu.
            //
            // Tek satirlik cozum: cizimi sanal bir 1920x1080 uzayina
            // al ve matrisle olcekle. Koordinatlar oldugu gibi kalir,
            // yalniz artik bir ORANI ifade ederler.
            float k = Screen.height / SanalBoy;
            var eskiMatris = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(k, k, 1f));
            _boy = SanalBoy;
            _en = Screen.width / k;

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

            // --- GOREV: baslik, hedef, mesafe, kese ---
            //
            // Gorunmeyen bir gorev olmayan bir gorevdir. Bu satirlar
            // eklenene kadar `GorevUretici` uretiyordu, `Gorev` durak
            // sayiyordu, `Kese` akce tutuyordu — ve oyuncu hicbirini
            // bilmiyordu.
            float ust = 78f;
            if (gorev != null && gorev.Simdiki != null)
            {
                var isim = gorev.Simdiki;
                GUI.Box(new Rect(10, ust, 330, 74), "", _kutu);
                GUI.Label(new Rect(20, ust + 6, 320, 22), isim.baslik, _yazi);

                float m = gorev.HedefMesafe;
                string yon = YonAdi(gorev.HedefKonum);
                GUI.Label(new Rect(20, ust + 28, 320, 22),
                          m >= 0f ? $"{yon}  ·  {m:F0} m"
                                  : "hedef bulunamadi", _yazi);
                GUI.Label(new Rect(20, ust + 50, 320, 22),
                          $"durak {isim.siradaki + 1}/{isim.duraklar.Count}"
                          + $"  ·  {isim.akce} akçe", _yazi);
                ust += 82f;
            }

            if (gorev != null)
            {
                GUI.Box(new Rect(10, ust, 330, 30), "", _kutu);
                GUI.Label(new Rect(20, ust + 5, 320, 22),
                          $"kese: {gorev.Kese.akce} akçe"
                          + $"  ·  biten görev: {gorev.Bitirilen}", _yazi);
                ust += 38f;
            }

            if (aranma != null && aranma.Seviye > 0.01f)
            {
                GUI.Box(new Rect(10, ust, 330, 30), "", _kutu);
                GUI.Label(new Rect(20, ust + 5, 320, 22),
                          $"aranma: %{aranma.Seviye * 100f:F0} "
                          + $"({aranma.SuAn})", _yazi);
            }

            // --- ETKILESIM IPUCU: ekranin ortasinin biraz altinda ---
            //
            // Ortada degil, altinda: nisangahin oldugu yere yazi koymak
            // tam bakilan seyi kapatir.
            if (!Duraklatildi && etkilesim != null)
            {
                string ip = etkilesim.Ipucu;
                if (ip.Length > 0)
                {
                    var ir = new Rect(_en * 0.5f - 90f,
                                      _boy * 0.5f + 40f, 180f, 30f);
                    GUI.Box(ir, "", _kutu);
                    var eski = _yazi.alignment;
                    _yazi.alignment = TextAnchor.MiddleCenter;
                    GUI.Label(ir, $"[E]  {ip}", _yazi);
                    _yazi.alignment = eski;
                }
            }

            // --- KESE: yalniz doluyken ---
            //
            // Bos bir envanter kutusu ekranin kosesinde surekli durursa
            // oyuncu ona bakmayi birakir; o zaman doldugunda da bakmaz.
            if (envanter != null && envanter.TurSayisi > 0)
            {
                float ky = 10f;
                var kr = new Rect(_en - 250f, 34f, 240f,
                                  22f * envanter.TurSayisi + 14f);
                GUI.Box(kr, "", _kutu);
                foreach (EsyaTuru t in System.Enum.GetValues(typeof(EsyaTuru)))
                {
                    int n = envanter.Adet(t);
                    if (n == 0) continue;
                    GUI.Label(new Rect(kr.x + 12f, kr.y + ky - 4f, 220f, 22f),
                              $"{EsyaAdi(t)}  ×{n}", _yazi);
                    ky += 22f;
                }
            }

            if (Time.unscaledTime < _mesajSonu)
                GUI.Box(new Rect(10, _boy - 46, 260, 32),
                        _mesaj, _kutu);

            // --- duraklat ---
            if (!Duraklatildi)
            {
                GUI.Label(new Rect(_en - 250, 10, 240, 20),
                          "ESC duraklat · E al · G kanat · V bakış · Shift koş" + "\n"
                          + "F5 kaydet · F9 yükle",
                          _yazi);
                return;
            }

            float g = 320f, y = 240f;
            var r = new Rect((_en - g) * 0.5f,
                             (_boy - y) * 0.5f, g, y);
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
            GUI.matrix = eskiMatris;
        }
    }
}
