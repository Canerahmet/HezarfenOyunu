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
        public VakitBildirimi vakit;

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
            if (vakit == null) vakit = FindAnyObjectByType<VakitBildirimi>();
        }

        /// <summary>
        /// Dört olaya abone olur.
        ///
        /// ## Neden bu gerekli
        ///
        /// `GorevYonetici.GorevBasladi`, `GorevBitti`,
        /// `UcusDizisi.DurumDegisti` ve `AranmaSistemi.DurumDegisti` —
        /// dördü de yazılmış ve çalışma zamanında <b>tek abonesi
        /// yoktu</b>. Yani 25 dakika yürüyüp bir görev bitiren oyuncu
        /// bunu ancak köşedeki sayaca bakarsa öğreniyordu; kanadı
        /// kuşandığında, ases onu fark ettiğinde ekranda hiçbir şey
        /// olmuyordu.
        ///
        /// Bildirme yeri zaten vardı (<see cref="Bildir"/>) ve yalnız
        /// kaydet/yükle için kullanılıyordu. Eksik olan sekiz satırdı.
        /// </summary>
        private void OnEnable()
        {
            if (gorev != null)
            {
                gorev.GorevBasladi += IsBasladi;
                gorev.GorevBitti += IsBitti;
            }
            var dizi = FindAnyObjectByType<Player.UcusDizisi>();
            if (dizi != null)
            {
                dizi.DurumDegisti += UcusDurumu;
                dizi.SuyaDustu += SuyaDustu;
            }
            if (aranma != null) aranma.DurumDegisti += AranmaDurumu;

            if (perde == null) perde = FindAnyObjectByType<Player.Perde2Dilimi>();
            if (perde != null)
            {
                perde.AsamaDegisti += AsamaDegisti;
                perde.TalimBildirimi += Bildir;
                // Ilk kareden itibaren gorunmeli: oyuncu ne yapacagini
                // ancak bir asama DEGISTIGINDE ogrenirse, hic
                // ogrenmez.
                Bildir(AsamaMetni(perde.Simdiki));
            }
        }

        private void OnDisable()
        {
            if (gorev != null)
            {
                gorev.GorevBasladi -= IsBasladi;
                gorev.GorevBitti -= IsBitti;
            }
            var dizi = FindAnyObjectByType<Player.UcusDizisi>();
            if (dizi != null)
            {
                dizi.DurumDegisti -= UcusDurumu;
                dizi.SuyaDustu -= SuyaDustu;
            }
            if (aranma != null) aranma.DurumDegisti -= AranmaDurumu;
            if (perde != null)
            {
                perde.AsamaDegisti -= AsamaDegisti;
                perde.TalimBildirimi -= Bildir;
            }
        }

        private void IsBasladi(Gorev g) => Bildir($"Yeni iş: {g.baslik}");

        /// <summary>
        /// Suya düşünce — <b>bir şey olduğu söylenmeli</b>.
        ///
        /// Oyuncu suya düşüyor, kıyıya bırakılıyor ve ekranda hiçbir
        /// şey olmuyorsa ışınlandığını sanır. Kayıkçılar Haliç'te 373
        /// tekneyle duruyor; onu çıkaran onlar.
        /// </summary>
        private void SuyaDustu() => Bildir("Kayıkçılar seni kıyıya çıkardı.");

        /// <summary>
        /// Oyunun <b>kendi</b> hikâyesi — ekrana ilk kez ulaşıyor.
        ///
        /// <see cref="Player.Perde2Dilimi"/> sahnede duruyor ve beş
        /// aşamalı bir zincir işletiyor: Okmeydanı'nda talim → kuleye
        /// çıkış → uçuş → Doğancılar'a iniş → tepki. <c>AsamaDegisti</c>
        /// olayının depoda <b>sıfır abonesi</b> vardı.
        ///
        /// Yani oyuncu Galata'da doğuyor, prosedürel bir "kayıp eşya"
        /// işi görüyor ve oyunun adını taşıyan görevin var olduğunu
        /// <b>asla öğrenmiyordu</b>. Okmeydanı kuleden 3,5 km ötede;
        /// kimse oraya kazara gitmez.
        /// </summary>
        private void AsamaDegisti(Player.Perde2Dilimi.Asama a)
            => Bildir(AsamaMetni(a));

        private static string AsamaMetni(Player.Perde2Dilimi.Asama a)
            => a switch
            {
                Player.Perde2Dilimi.Asama.Talim =>
                    "Okmeydanı'nda talim et — kanadı tanıman lazım.",
                Player.Perde2Dilimi.Asama.Kule =>
                    "Talim bitti. Galata Kulesi'ne çık.",
                Player.Perde2Dilimi.Asama.Ucus => "Boğaz'ı geç.",
                Player.Perde2Dilimi.Asama.Inis =>
                    "Doğancılar Meydanı'na in.",
                Player.Perde2Dilimi.Asama.Tepki =>
                    "İncili Köşk'e git — padişah seni görmek istiyor.",
                Player.Perde2Dilimi.Asama.Bitti => "Hikâye bitti.",
                _ => "",
            };

        /// <summary>Kapanış paneli bir kez gösterildi mi.</summary>
        private bool _kodeksGosterildi;

        private void IsBitti(Gorev g) =>
            Bildir($"Teslim edildi · +{g.akce} akçe");

        private void UcusDurumu(Player.UcusDizisi.Durum d)
        {
            switch (d)
            {
                case Player.UcusDizisi.Durum.Hazir:
                    Bildir("Kanat kuşanıldı — Space ile atla"); break;
                case Player.UcusDizisi.Durum.Ucuyor:
                    Bildir("Uçuyorsun"); break;
                case Player.UcusDizisi.Durum.Cakildi:
                    Bildir("Sert indin"); break;
            }
        }

        private void AranmaDurumu(AranmaSistemi.Durum d)
        {
            switch (d)
            {
                case AranmaSistemi.Durum.FarkEdildi:
                    Bildir("Ases seni fark etti"); break;
                case AranmaSistemi.Durum.Uyarildi:
                    Bildir("«Kim var orada?»"); break;
                case AranmaSistemi.Durum.Kovalaniyor:
                    Bildir("Kovalanıyorsun"); break;
                case AranmaSistemi.Durum.Yakalandi:
                    Bildir("Yakalandın"); break;
            }
        }

        /// <summary>
        /// Oyuncunun gövdesi — ilk sorulduğunda bulunur.
        ///
        /// Önce yalnız <c>AranmaDurumu</c> olayında atanıyordu; yani
        /// görev pusulası (<see cref="YonAdi"/>) ases seni <b>fark
        /// edene kadar</b> "—" gösteriyordu. Oyuncunun ilk görevi,
        /// suçsuzken, yönsüzdü.
        /// </summary>
        private Transform OyuncuT
        {
            get
            {
                if (_oyuncuT == null)
                {
                    var go = GameObject.Find("OYUNCU");
                    if (go != null) _oyuncuT = go.transform;
                }
                return _oyuncuT;
            }
        }

        private void Update()
        {
            var kb = Keyboard.current;
            var kol = Gamepad.current;

            // KLAVYE YOKSA DA OYNANIR.
            //
            // Once `if (kb == null) return;` vardi ve klavyesiz bir
            // oyuncu — Steam Deck'te oldugu gibi — oyunu
            // DURAKLATAMIYOR, kaydedemiyor, hicbir seye dokunamiyordu.
            // Kolla menuden girilebiliyor, sonrasi bosluktu.
            if (kb == null && kol == null) return;

            bool Basildi(Key t, UnityEngine.InputSystem.Controls.ButtonControl b)
                => (kb != null && kb[t].wasPressedThisFrame)
                   || (b != null && b.wasPressedThisFrame);

            if (Basildi(duraklatTusu, kol?.startButton))
                Duraklat(!Duraklatildi);

            // Duraklatilmisken kol menuyu gezer.
            if (Duraklatildi)
            {
                if (Basildi(Key.DownArrow, kol?.dpad.down)) _menuSecili++;
                if (Basildi(Key.UpArrow, kol?.dpad.up)) _menuSecili--;
                _menuSecili = (_menuSecili + 4) % 4;
                if (Basildi(Key.Enter, kol?.buttonSouth)) _menuOnay = true;
                return;   // duraklatilmisken kaydet/yukle kisayolu yok
            }
            if (Basildi(kaydetTusu, kol?.dpad.up)) Kaydet();
            if (Basildi(yukleTusu, kol?.dpad.down)) Yukle();

            // DURAKLATILMISKEN ETKILESIM YOK: menu acikken imlec serbest,
            // bakis olu ve "onunde duran sey" artik oyuncunun secimi
            // degil, duraklattigi andan kalma bir kaza.
            if (!Duraklatildi && etkilesim != null
                && Basildi(etkilesimTusu, kol?.buttonWest))
            {
                // BASARISIZLIGIN SEBEBI SOYLENIR.
                //
                // Once her basarisizliga "Kese dolu." deniyordu ve
                // iskelede parasi yetmeyen oyuncu bunu okuyordu:
                // para yetersizligi envanter dolulugu diye
                // anlatiliyordu. Oyuncunun ne yapacagini bilmesi
                // icin neyin olmadigini bilmesi gerekir.
                string neydi = etkilesim.Ipucu;
                var kim = etkilesim.Hedef as Sakin;
                if (etkilesim.Tetikle())
                    // KONUSULAN SEY DUYULMALI.
                    //
                    // Bir insanla konusmanin karsiligi "Konus ·
                    // alindi" olamaz; soylenen sozun kendisi
                    // gorunmeli. Oteki etkilesimler icin eski dil
                    // dogru: kupten su ALINIR.
                    Bildir(kim != null && kim.SonSoz.Length > 0
                           ? kim.SonSoz : neydi + " · alındı");
                else if (neydi.Length > 0) Bildir(Sebep());
            }
        }

        /// <summary>
        /// Elindeki aygıta göre tuş adı.
        ///
        /// Ekrandaki her tuş adı klavyeye sabit yazılmıştı — "E al",
        /// "G kanat", "Shift koş" — oysa kod kolu <b>destekliyor</b>:
        /// kanat <c>rightShoulder</c>, etkileşim <c>buttonWest</c>,
        /// duraklat <c>start</c>. Yani kolla oynayan biri kule tepesine
        /// çıkıyor, ekranda "G kanat" okuyor ve elinde G tuşu yok.
        /// "Kolla oynayan biri uçamıyordu" kusuru kodda kapatılmış,
        /// arayüzde açık bırakılmıştı.
        /// </summary>
        public static string Glif(string klavye, string kol) =>
            Gamepad.current != null ? kol : klavye;

        /// <summary>Alt köşedeki kumanda satırı.</summary>
        private static string Tuslar() =>
            Gamepad.current != null
                ? "Start duraklat · X al · RB kanat · R3 bakış\n"
                  + "D-pad yukarı kaydet · D-pad aşağı yükle"
                : "ESC duraklat · E al · G kanat · V bakış · Shift koş\n"
                  + "F5 kaydet · F9 yükle";

        /// <summary>Duraklat menüsünde seçili satır.</summary>
        private int _menuSecili;
        private bool _menuOnay;

        /// <summary>Duraklat menüsünde bir satırı uygular.</summary>
        private void MenuSec(int i)
        {
            switch (i)
            {
                case 0: Duraklat(false); break;
                case 1: Kaydet(); break;
                case 2: Yukle(); break;
                case 3:
                    Time.timeScale = 1f;
                    UnityEngine.SceneManagement.SceneManager
                        .LoadScene("Acilis");
                    break;
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

        /// <summary>
        /// Etkileşim neden olmadı — tahmin değil, <b>hedefe sorulan</b>
        /// cevap.
        /// </summary>
        private string Sebep()
        {
            if (etkilesim != null && etkilesim.Hedef is Perme p)
            {
                int kese = gorev != null ? gorev.Kese.akce : 0;
                int eksik = p.Ucret - kese;
                return eksik > 0 ? $"{eksik} akçe eksik."
                                 : "Kayıkçı şimdi geçmiyor.";
            }
            return "Kese dolu.";
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
            var oy = OyuncuT;
            if (hedef == null || oy == null) return "—";
            var d = hedef.Value - oy.position;
            float aci = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
            if (aci < 0f) aci += 360f;
            string[] ad = { "kuzey", "kuzeydoğu", "doğu", "güneydoğu",
                            "güney", "güneybatı", "batı", "kuzeybatı" };
            return ad[Mathf.RoundToInt(aci / 45f) % 8];
        }

        [Tooltip("Ana hikâye zinciri — boşsa sahnede aranır.")]
        public Player.Perde2Dilimi perde;

        /// <summary>Ana zincirin şu anki hedefi — dünyada bir yer.</summary>
        private Vector3? PerdeHedefi()
        {
            if (perde == null) return null;
            return perde.Simdiki switch
            {
                Player.Perde2Dilimi.Asama.Talim => perde.okmeydani,
                Player.Perde2Dilimi.Asama.Kule => perde.kule,
                Player.Perde2Dilimi.Asama.Ucus => perde.dogancilar,
                Player.Perde2Dilimi.Asama.Inis => perde.dogancilar,
                Player.Perde2Dilimi.Asama.Tepki => perde.incilikosk,
                _ => null,
            };
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
            // ANA ZINCIR — HER ZAMAN EKRANDA.
            //
            // Bir bildirim uc saniye durur ve gecer; oyunun kendi
            // hikayesi ekranda kalmali. `talimHedefi = 3` ve
            // `talimMesafesi = 60 m` esikleri vardi ve ikisi de
            // oyuncuya hic gorunmuyordu: kimse "3 suzulusten 1'i
            // tamam" demeden ucmayi ogrenemez.
            // --- KAPANIS: OYUN SESSIZCE BITMEZ ---
            //
            // `TepkiKodeksi` — kese altini, surgun, ve anlatinin tek
            // kaynaga dayandiginin itirafi — depoda **sifir okuyucusu
            // olan bir sabit**ti. Incili Kosk'e varan oyuncunun
            // ekraninda olan tek sey uc saniyelik bos bir bildirim
            // kutusuydu; padisah yok, metin yok, jenerik yok. Oyuncu
            // yanlis yere geldigini saniyordu.
            //
            // Bu, 9. turda kapatilan "ana hikaye ekranda hic yoktu"
            // bulgusunun ters ucu: hikaye artik BASLIYOR ama
            // BITMIYORDU.
            if (perde != null
                && perde.Simdiki == Player.Perde2Dilimi.Asama.Bitti
                && !_kodeksGosterildi)
            {
                float kg = 720f, ky = 420f;
                var kr = new Rect((_en - kg) * 0.5f, (_boy - ky) * 0.5f, kg, ky);
                GUI.Box(kr, "", _kutu);
                GUI.Label(new Rect(kr.x + 30, kr.y + 24, kg - 60, 30),
                          "HEZARFEN AHMED ÇELEBİ", _yazi);
                GUI.Label(new Rect(kr.x + 30, kr.y + 64, kg - 60, ky - 150),
                          Player.Perde2Dilimi.TepkiKodeksi, _yazi);
                GUI.Label(new Rect(kr.x + 30, kr.y + ky - 78, kg - 60, 24),
                          $"biten görev: {(gorev != null ? gorev.Bitirilen : 0)}"
                          + $"  ·  kese: {(gorev != null ? gorev.Kese.akce : 0)} akçe",
                          _yazi);
                if (GUI.Button(new Rect(kr.x + 30, kr.y + ky - 46, kg - 60, 32),
                               "Şehirde kalmaya devam et"))
                    _kodeksGosterildi = true;

                GUI.matrix = eskiMatris;
                return;
            }

            if (perde != null && perde.Simdiki != Player.Perde2Dilimi.Asama.Bitti)
            {
                string ek = perde.Simdiki == Player.Perde2Dilimi.Asama.Talim
                    ? $"  ({perde.TalimSayisi}/{perde.talimHedefi} · "
                      + $"{perde.TalimEsigi(perde.TalimSayisi):F0} m gerek)"
                    : "";
                var pk = PerdeHedefi();
                string yon = pk != null ? YonAdi(pk) : "";
                float pm = pk != null && OyuncuT != null
                    ? Vector2.Distance(
                        new Vector2(OyuncuT.position.x, OyuncuT.position.z),
                        new Vector2(pk.Value.x, pk.Value.z)) : -1f;

                GUI.Box(new Rect(10, ust, 330, 52), "", _kutu);
                GUI.Label(new Rect(20, ust + 5, 320, 22),
                          AsamaMetni(perde.Simdiki) + ek, _yazi);
                if (pm >= 0f)
                    GUI.Label(new Rect(20, ust + 27, 320, 22),
                              $"{yon}  ·  {pm:F0} m", _yazi);
                ust += 60f;
            }

            if (gorev != null && gorev.Simdiki == null)
            {
                // ISSIZKEN NE YAPACAGINI SOYLE.
                //
                // Is artik konusulan kisiden geliyor. Oyuncuya bunu
                // soylemeyen bir tasarim, isi kaldirmakla ayni sey:
                // ekranda hicbir sey yoksa yapilacak bir sey de yoktur.
                GUI.Box(new Rect(10, ust, 330, 30), "", _kutu);
                GUI.Label(new Rect(20, ust + 6, 320, 22),
                          $"İşsizsin — birine {Glif("E", "X")} ile iş sor",
                          _yazi);
                ust += 38f;
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

            // --- VAKIT BILDIRIMI: ekranin ust ortasi ---
            //
            // Ezani saat oyunun kimligi ve bugune kadar yalniz kosede
            // kucuk bir yaziydi. Bir oyun gunu 24 gercek dakika, yani
            // oyuncu her 24 dakikada bes vakit yasiyor ve hicbirini
            // fark etmiyordu.
            if (!Duraklatildi && vakit != null && vakit.Bildirim.Length > 0)
            {
                var eskiRenk = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, vakit.Tazelik);
                var vr = new Rect(_en * 0.5f - 130f, _boy * 0.16f, 260f, 34f);
                var eskiH = _yazi.alignment;
                var eskiP = _yazi.fontSize;
                _yazi.alignment = TextAnchor.MiddleCenter;
                _yazi.fontSize = 22;
                GUI.Label(vr, $"— {vakit.Bildirim} —", _yazi);
                _yazi.alignment = eskiH;
                _yazi.fontSize = eskiP;
                GUI.color = eskiRenk;
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
                    GUI.Label(ir, $"[{Glif("E", "X")}]  {ip}", _yazi);
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
                GUI.Label(new Rect(_en - 250, 10, 240, 20), Tuslar(), _yazi);
                // OLCEK GERI VERILIR.
                //
                // Bu erken donus `GUI.matrix`i olcekli birakiyordu ve
                // ayni karede cizen her OnGUI (Editor teshis
                // pencereleri, FlightHud) yanlis olcekte ciziyordu.
                // Bir kaynak alindiysa erken donuste de birakilir.
                GUI.matrix = eskiMatris;
                return;
            }

            float g = 320f, y = 240f;
            var r = new Rect((_en - g) * 0.5f,
                             (_boy - y) * 0.5f, g, y);
            GUI.Box(r, "DURAKLATILDI", _kutu);

            // MENU KOLLA GEZILEBILMELI.
            //
            // Bu menu `GUI.Button` ile ciziliyordu ve `GUI.Button`
            // IMLEC ISTER. `Duraklat(true)` imleci serbest birakiyor
            // ama kolla oynayan biri isletim sistemi imlecini
            // OYNATAMAZ: Steam Deck'te Start'a basan oyuncu dort
            // dugme goruyor ve hicbirine erisemiyordu — "Ana menu"
            // gamepad'le ulasilamaz, oyundan cikmanin yolu Alt+F4.
            //
            // Acilis menusu bu dersi ogrenmisti
            // (`AcilisMenusu.Sec`, EventSystem ile ilk dugmeyi secer);
            // duraklat menusu ayni projede o dersten habersizdi. 2010'dan
            // beri konsol sertifikasyon sarti.
            string[] secenekler = { "Devam et", "Kaydet", "Yükle", "Ana menü" };
            for (int i = 0; i < secenekler.Length; i++)
            {
                var dr = new Rect(r.x + 30, r.y + 50 + i * 46, g - 60, 36);

                // Secili olan gorunur olmali: kolla gezen oyuncunun
                // nerede oldugunu bilmesinin TEK yolu bu.
                var eskiRenk = GUI.color;
                if (i == _menuSecili)
                    GUI.color = new Color(1f, 0.86f, 0.45f, 1f);
                bool tiklandi = GUI.Button(dr, secenekler[i]);
                GUI.color = eskiRenk;

                if (tiklandi || (i == _menuSecili && _menuOnay))
                    MenuSec(i);
            }
            _menuOnay = false;
            GUI.matrix = eskiMatris;
        }
    }
}
