using System.Collections;
using System.Collections.Generic;
using System.IO;
using Hezarfen.Player;
using Hezarfen.Sehir;
using Hezarfen.Streaming;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEngine;

namespace Hezarfen.Editor.Diagnostics
{
    /// <summary>
    /// <b>Oyunu dolaşır, kare yakalar, ölçer.</b>
    ///
    /// Caner (2026-08-30): *"senin oyunu oynamani ve bu problemleri tespit
    /// edip duzeltene kadar devam etmeni istiyorum."*
    ///
    /// ## Neden böyle bir araç
    ///
    /// Bu oturumun bütün pahalı hataları aynı yerden çıktı: bir şeyi
    /// <b>ölçtüm</b> ama <b>bakmadım</b>. Sayı "18.338 yapı, sıfır boşluk"
    /// diyordu, Caner ekranda havada duran Ayasofya'yı görüyordu. Menü
    /// testleri yeşildi, hiçbir düğme çalışmıyordu. Kamera "arkada" diye
    /// ölçülüyordu, model tersti.
    ///
    /// Ortak sebep: elimde <b>oyunun kendisine bakan</b> bir araç yoktu.
    /// Bu sınıf onu veriyor — oyuncuyu şehrin farklı yerlerine götürür,
    /// yürütür, koşturur, her durakta kamera karesini kaydeder ve yanına
    /// sayıları yazar. Kare bir <b>gözlem</b>, sayı bir <b>kanıt</b>;
    /// ikisi yan yana olmadan hangisinin yalan söylediği anlaşılmıyor.
    ///
    /// ## Ne ölçülür
    ///
    /// Her durakta: ayağın altındaki yüzey, arazi kotundan fark, kamera
    /// kolunun boyu, görünür NPC, görünür replik, ve kare süresi. Bunlar
    /// "oyun bozuk" cümlesini <b>hangi</b> bozukluk olduğuna çevirir.
    /// </summary>
    public static class OyunTuru
    {
        private const string Cikti = "../../renders/tur";

        /// <summary>Bir durak: nereye gidilecek, ne yapılacak.</summary>
        internal struct Durak
        {
            public string ad;
            public Vector3 nokta;      // sıfırsa oyuncunun doğduğu yer
            public float bakisYaw;
            public bool kos;           // gidip koşsun mu
            public string neden;
        }

        /// <summary>
        /// Duraklar. Konumlar bu oturumda ölçülen gerçek yerlerden:
        /// Galata (dünya orijini), Haliç'in başı (−3277, 2591), kara
        /// surları x ≈ −3400, Marmara kıyısı z ≈ −2800, Ayasofya
        /// (549, −1886), kırsal doku (−2500, 0).
        /// </summary>
        private static readonly Durak[] Duraklar =
        {
            // OYUNCUNUN ILK GORDUGU SEY KULE OLMALI.
            //
            // Durak dunya orijininde (ADR 0007: orijin Galata Kulesi'nin
            // tabani) ve yerlestirme oyuncuyu (25, 25)'e koyuyor — yani
            // kulenin dibine. Ama bakis yonu 0 (+z) idi ve tur raporu
            // ne gorundugunu yaziyordu: `kadrajda TR_Istanbul @ 28 m`.
            // Kule sahnede ve tam yerinde (0, 52.24, 0 — 46 m boyunda),
            // oyuncunun 35 m otesinde, ve kadrajda YOK.
            //
            // Bu durak "oyuncu ilk burayi gorur" diyor. Ilk goreceyi sey
            // cıplak toprak degil, tirmanacagi kule.
            //
            // Aci hesapla: oyuncu (25, 25), kule (0, 0) —
            // atan2(-25, -25) = 225 derece.
            new Durak { ad = "01_dogum", nokta = Vector3.zero,
                        bakisYaw = 225f,
                        neden = "Oyuncu ilk burayi gorur — ve kuleyi." },
            new Durak { ad = "02_dogum_kosu", nokta = Vector3.zero,
                        bakisYaw = 0f, kos = true,
                        neden = "Kosarken karakter ve kamera." },
            // GALATA DURAGI OLCUMLE TASINDI.
            //
            // Durak (120, 60) idi ve semt akisi duzeltilince ne oldugu
            // goruldu: iki semt YUKLU, ama ayak altinda `TR_Istanbul`
            // (ciplak arazi), kadrajda `gok @ 0 m`, 40 m'de sifir NPC.
            // Yani sehir gelmemis degil — durak sehrin OLMADIGI yerde.
            //
            // D_Galata'nin 10.843 yerlesim ornegi sahne dosyasindan
            // okundu: x -1811..1046, z -791..1612 ve en yogun 200 m'lik
            // hucre **x 200-400, z 0-200** (271 ornek). Durak o hucrenin
            // ortasina tasindi; bakis +z, cunku yogun hucrelerin cogu
            // orada (z 600-1200).
            //
            // Bu, ADR 0078'in referans semti — katman once burada
            // olculuyor, yani duragin sehirde durmasi sart.
            new Durak { ad = "03_galata_sokak", nokta = new Vector3(300f, 0f, 100f),
                        bakisYaw = 0f,
                        neden = "Dar sokakta kamera kolu ve kalabalik." },
            new Durak { ad = "04_surici", nokta = new Vector3(-700f, 0f, -1500f),
                        bakisYaw = 90f,
                        neden = "Surici dokusu ve NPC yogunlugu." },
            // DURAK AYASOFYA'NIN ICINDEYDI.
            //
            // Nokta (549, -1886); Ayasofya sahnede (550,66, -1888,4) ve
            // 84 x 116 x 69 m. Yani durak yapinin **2,9 m** icinde:
            // oyuncu binanin icine dogar, yerlestirme onu disari iter
            // (`arazi farki +1,4` — bir doseme uzerinde) ve kadrajda
            // 77 m otedeki bir turbe kalir. Landmark duragi
            // landmark'i gostermiyordu.
            //
            // Yeni nokta merkezden 130 m: bina 84 x 116 m ve bu uzaklik
            // onu kadraja sigdirir. On iki yon 15 derece araliklarla
            // tarandi, en yakin yapiya 12 m'den fazla olanlar arasindan
            // cevresinde en cok yapi bulunan secildi — (438, -1953),
            // en yakin yapi 18,5 m, 80 m icinde 141 yapi. Bakis
            // Ayasofya'ya: atan2(112,6, 65,4) = 60 derece.
            new Durak { ad = "05_ayasofya", nokta = new Vector3(438f, 0f, -1953f),
                        bakisYaw = 60f,
                        neden = "Landmark oturmasi ve olcek." },
            new Durak { ad = "06_kara_surlari", nokta = new Vector3(-3300f, 0f, -1200f),
                        bakisYaw = 90f,
                        neden = "Sur burclarinin oturmasi." },
            new Durak { ad = "07_kirsal", nokta = new Vector3(-2500f, 0f, -600f),
                        bakisYaw = 45f,
                        neden = "Bostan, yol ve meyvelik — bos zemin sikayeti." },
            new Durak { ad = "08_halic_basi", nokta = new Vector3(-3100f, 0f, 2500f),
                        bakisYaw = 200f,
                        neden = "Dere agzi ve su." },
            // MARMARA DURAGI: DENIZ VAR AMA KIYI YOKTU.
            //
            // Durak (-1850, -2700) idi ve rapor `kadrajda gok @ 0 m`
            // diyordu. Olculdu: o noktanin **120 m icinde hicbir sey
            // yok** — en yakin icerik 253 m'de (D_Surici_Bati), 268
            // m'de (D_Halic tekneleri). Yani gok kadraji durustu, ama
            // duragin kendi gerekcesi "kiyi, iskele ve deniz" diyor ve
            // bos bir kumsal onu gostermiyor.
            //
            // Yeni nokta, en yakin yirmi yapinin merkezinden (-1654,
            // -2501) denize dogru 35 m: sehrin kenari kadrajda kalir,
            // bakis yonu hala deniz.
            new Durak { ad = "09_marmara", nokta = new Vector3(-1678f, 0f, -2526f),
                        bakisYaw = 180f,
                        neden = "Kiyi, iskele ve deniz." },
            // USKUDAR DURAGI DA OLCUMLE TASINDI — GALATA ILE AYNI KUSUR.
            //
            // Durak (3500, 200) idi ve aci aramasi duzeltildikten sonra
            // rapor hala `kadrajda gok @ 0 m` diyordu: on uc yonun
            // hicbirinde seksen metre icinde bir sey yok. Yani oyuncu
            // Uskudar'in oldugu yerde durmuyor.
            //
            // D_Uskudar'in 10.691 yerlesim ornegi sahne dosyasindan
            // okundu; en yogun 200 m'lik hucre **x 4600-4800, z
            // 600-800** (313 ornek). Durak o hucrenin ortasina tasindi.
            // Hucre ORTASI degil, hucre icinde bir SOKAK noktasi:
            // (4700, 700) en yakin yapiya 2 m kaliyordu — oyuncu duvara
            // yapisik dogar ve yerlestirme onu suruklerdi. Hucre 10 m
            // araliklarla tarandi ve en yakin yapisi 8-14 m olan (yani
            // bir sokak genisligi kadar acik) noktalar arasindan 60 m
            // icinde en cok yapi goreni secildi: (4790, 650), en yakin
            // 8,2 m, cevrede 169 yapi.
            new Durak { ad = "10_uskudar", nokta = new Vector3(4790f, 0f, 650f),
                        bakisYaw = 270f,
                        neden = "Karsi yaka — semt akisi calisiyor mu." },
        };

        /// <summary>Ölçümün yapılacağı sahne.</summary>
        private const string OyunSahnesi =
            "Assets/_Project/Scenes/Faz1_Terrain.unity";

        /// <summary>Toplu koşumdan gelindiyse tur bitince çıkılır.</summary>
        private static bool Toplu =>
            System.Environment.CommandLine.Contains("OyunTuru.TopluKos");

        /// <summary>
        /// Toplu kipten koşulabilen giriş — <b>bakmak da bir adımdır</b>.
        ///
        /// Tur yalnız Editor penceresinden başlatılabiliyordu ve bu, "her
        /// turda oyun karesi yakala ve BAK" adımını elle yapılan bir işe
        /// bağlıyordu. Bu oturumda üç kusur yalnız bakınca göründü
        /// (balon kollar, tepeye tünemiş sarık, abajur yaşmak) — yani
        /// bakmak bir süs değil ölçümün kendisi. <c>KareBolusumu</c>
        /// aynı sebeple toplu girişini kazanmıştı.
        ///
        /// <c>-nographics</c> <b>verilmemeli</b>: grafik aygıtı olmadan
        /// yakalanan kare boş çıkar.
        /// </summary>
        public static void TopluKos()
        {
            UnityEditor.SceneManagement.EditorSceneManager
                .OpenScene(OyunSahnesi);
            EditorApplication.playModeStateChanged += DurumDegisti;
            EditorApplication.EnterPlaymode();
        }

        private static void DurumDegisti(PlayModeStateChange d)
        {
            if (d != PlayModeStateChange.EnteredPlayMode) return;
            EditorApplication.playModeStateChanged -= DurumDegisti;
            Baslat();
        }

        [MenuItem("Hezarfen/Denetim/Oyun turu (kare + olcum)")]
        public static void Baslat()
        {
            if (!Application.isPlaying)
            {
                Debug.LogError("[Hezarfen] Once OYNAT: tur oyun calisirken "
                               + "kosar. Play'e bas ve komutu tekrarla.");
                return;
            }
            var kosucu = Object.FindAnyObjectByType<TurKosucu>();
            if (kosucu == null)
            {
                var go = new GameObject("TUR_KOSUCU");
                kosucu = go.AddComponent<TurKosucu>();
            }
            kosucu.StartCoroutine(kosucu.Kos(Duraklar));
        }

        /// <summary>
        /// Turu <b>oyun içinde</b> yürüten davranış.
        ///
        /// Editor kodu tek karede iş yapar; tur ise fizik istiyor: ışınlama
        /// sonrası zeminin oturması, koşarken animasyonun karışması, semt
        /// akışının yüklenmesi hep <b>kare geçmesini</b> bekler. Bu yüzden
        /// coroutine.
        /// </summary>
        public class TurKosucu : MonoBehaviour
        {
            /// <summary>
            /// Oyuncunun 40 m çevresindeki <b>sakin</b> sayısı.
            ///
            /// Önce `NPCYonetici.GorunurSayisi` yazılıyordu ve tablo on
            /// durağın sekizinde <b>60</b> diyordu — çünkü 60,
            /// `govdeButcesi`nin kendisi. Yani ölçüm kalabalığı değil
            /// <b>bütçeyi</b> ölçüyor, her yerde doyuyor ve hiçbir şey
            /// söylemiyordu.
            ///
            /// Doygun bir ölçü ölçü değildir. Bu sayı bütçeden
            /// bağımsız: kalabalık gerçekten seyreldiğinde düşer —
            /// gece, kırsal, ya da rutin sakinleri içeri aldığında.
            /// </summary>
            private static int YakindakiNpc(Sehir.NPCYonetici npc, Vector3 p)
            {
                if (npc == null || npc.Sakinler == null) return 0;
                const float R2 = 40f * 40f;
                int n = 0;
                foreach (var a in npc.Sakinler)
                    if ((a.konum - p).sqrMagnitude <= R2) n++;
                return n;
            }

            /// <summary>
            /// <b>Duyma menzilindeki sakin sayısı</b> — "replik 0"ın ne
            /// demek olduğunu söyleyen sayı.
            ///
            /// `replik` sütunu on durağın <b>onunda da</b> 0 yazıyor ve
            /// bu tek başına iki ayrı şey demek olabilir: bark sistemi
            /// çalışmıyor, ya da o mesafede zaten kimse yok. Replik
            /// yalnızca <see cref="BarkGosterici.duyulmaMesafesi"/>
            /// içinde görünür (12 m); tur ise 40 m'deki kalabalığı
            /// sayıyordu.
            ///
            /// Sıfırın yanına bu sayı yazılınca ayrım kendiliğinden
            /// çıkıyor: "0 replik / 0 sakin" beklenen, "0 replik /
            /// 6 sakin" kusur. Menzil <b>bark'ın kendi alanından</b>
            /// okunuyor — bir sayının iki sahibi olmasın.
            /// </summary>
            private static int DuymaMenzilinde(Sehir.NPCYonetici npc,
                                               Sehir.BarkGosterici bark,
                                               Vector3 p)
            {
                if (npc == null || npc.Sakinler == null) return 0;
                float r = bark != null ? bark.duyulmaMesafesi : 12f;
                float r2 = r * r;
                int n = 0;
                foreach (var a in npc.Sakinler)
                    if ((a.konum - p).sqrMagnitude <= r2) n++;
                return n;
            }

            /// <summary>
            /// O anda ekrana <b>çizilen</b> sakin gövdesi sayısı.
            ///
            /// Turda "40 m'de 93 NPC" yazıyordu ve karede sokak
            /// bomboştu. İki sayı iki ayrı şeyi ölçüyor: biri kaç
            /// AJANIN yakında olduğunu, öteki kaç GÖVDENİN gerçekten
            /// çizildiğini. Aradaki fark, ajanın var olup gövdesinin
            /// olmadığı yerdir — ve o fark ölçülmedikçe kalabalık
            /// "sayıya göre kalabalık, ekrana göre boş" kalır.
            /// </summary>
            private static int CizilenGovde(Sehir.NPCYonetici npc)
            {
                if (npc == null) return 0;
                int n = 0;
                foreach (Transform t in npc.transform)
                    if (t.gameObject.activeInHierarchy) n++;
                return n;
            }

            /// <summary>
            /// Durak noktasindan en cok bu kadar uzaga kayilabilir (m).
            ///
            /// Onceki halde sinir YOKTU: yerlestirici butun sehri
            /// tariyor ve yakindaki her aday elenirse duragi
            /// kilometrelerce oteye tasiyordu. 05_ayasofya ve
            /// 09_marmara kareleri bombos araziyi gosteriyordu ve
            /// sayilar "acik dugum: E, kayma 0,0, tepe acik: E" diye
            /// KUSURSUZ okunuyordu — cunku olculmeyen sey buydu.
            /// </summary>
            private const float EnCokDurakSapmasi = 150f;

            internal IEnumerator Kos(Durak[] duraklar)
            {
                Directory.CreateDirectory(Cikti);
                var satirlar = new List<string>();
                satirlar.Add("# Oyun turu");
                satirlar.Add("");
                satirlar.Add("Her durakta kamera karesi kaydedildi ve yanina");
                satirlar.Add("sayilar yazildi. Kare bir GOZLEM, sayi bir KANIT.");
                satirlar.Add("");
                satirlar.Add("| durak | konum (x, z) | ayak altinda "
                             + "| arazi farki | kamera kolu "
                             + "| 40 m'de NPC | cizilen govde | replik / menzilde "
                             + "| kadrajda | semt (bekleme) | apv | acik dugum "
                             + "| durak sapmasi (m) "
                             + "| kayma (m) | tepe acik / neyin altinda "
                             + "| aci kaymasi "
                             + "| kare (ms) | neden |");
                satirlar.Add("|---|---|---|---:|---:|---:|---:|---:|---|"
                             + "---|---|:-:|---:|---:|:-:|---:|---:|---|");

                var oyuncu = Object.FindAnyObjectByType<WalkController>();
                var kip = Object.FindAnyObjectByType<KameraKipi>();
                var npc = Object.FindAnyObjectByType<NPCYonetici>();
                var bark = Object.FindAnyObjectByType<BarkGosterici>();
                var arazi = Object.FindAnyObjectByType<Terrain>();
                var graf = npc != null ? npc.graf : null;
                var kam = Camera.main;
                if (oyuncu == null || kam == null)
                {
                    Debug.LogError("[Hezarfen] Oyuncu ya da kamera yok.");
                    yield break;
                }

                // TOPLU KIPTE POZ SABITLENIR — YOKSA KARE BEYAZ CIKAR.
                //
                // Olculdu: toplu kosumda yakalanan on karenin onu da
                // tamamen beyazdi. Sebep sahne degil OLCUM ORTAMIYDI —
                // kalici profil otomatik histogram pozu kullaniyor ve o,
                // bir onceki karenin renk tamponunu okur; penceresiz
                // toplu kipte o gecmis olusmuyor ve poz en parlak ucta
                // takili kaliyor. Ayni sinif kisitin bir baskasi bu
                // depoda zaten yazili: `UnityStats` toplu kipte sifir
                // doner.
                //
                // Sabit poz VERILINCE ayni sahne dogru cikti (Surici
                // sokagi, tezgah, kaldirim). Yani kare bir gozlem olarak
                // ise yarar hale geliyor; kaydedilen sey oyunun pozu
                // degil, TURUN pozudur ve rapor bunu yazar.
                //
                // Profil DOSYASINA dokunulmuyor: daha yuksek oncelikli
                // gecici bir Volume kuruluyor ve tur bitince siliniyor.
                GameObject pozGo = null;
                if (Toplu)
                {
                    pozGo = new GameObject("TUR_POZ");
                    var v = pozGo.AddComponent<
                        UnityEngine.Rendering.Volume>();
                    v.isGlobal = true;
                    v.priority = 1000f;
                    var pr = ScriptableObject
                        .CreateInstance<UnityEngine.Rendering.VolumeProfile>();
                    var poz = pr.Add<
                        UnityEngine.Rendering.HighDefinition.Exposure>(true);
                    poz.mode.overrideState = true;
                    poz.mode.value = UnityEngine.Rendering.HighDefinition
                        .ExposureMode.Fixed;
                    poz.fixedExposure.overrideState = true;
                    // 13 EV: acik gunes altinda disarisi. Olculdu —
                    // 9 EV'de kare hala beyaz, 13'te sokak okunuyor.
                    poz.fixedExposure.value = 13f;
                    v.sharedProfile = pr;
                    satirlar.Add("");
                    satirlar.Add("> **Poz toplu kosum icin 13 EV'de "
                                 + "sabitlendi.** Otomatik histogram pozu "
                                 + "penceresiz kipte oturmuyor ve butun "
                                 + "kareler beyaz cikiyordu; bu kareler "
                                 + "oyunun pozunu degil TURUN pozunu "
                                 + "gosterir.");
                    satirlar.Add("");
                }

                var cc = oyuncu.GetComponent<CharacterController>();
                var dogum = oyuncu.transform.position;

                foreach (var d in duraklar)
                {
                    // --- ISINLA ---
                    // ISINLANAN YER ILE DURULAN YER AYNI SEY DEGIL.
                    //
                    // Uc durakta kare bir duvar, birinde de TAVAN
                    // gosterdi: 07_kirsal karesinde oyuncu bir evin
                    // icinde, ahsap tavanin altinda duruyor. Oysa
                    // yerlestirici "tepesi acik dugum" ariyor. Ikisi
                    // ayni anda dogru olamaz; hangisinin yalan
                    // soyledigini bilmek icin ucu birden yaziliyor:
                    // acik dugum bulundu mu, oyuncu konduktan sonra ne
                    // kadar KAYDI, ve son yerinde tepesi hala acik mi.
                    bool acikBulundu = false;
                    var hedef = d.nokta == Vector3.zero ? dogum : d.nokta;
                    if (d.nokta != Vector3.zero)
                    {
                        // DURAK SOKAGA OTURUR.
                        //
                        // Ilk turda duraklar elle secilmis koordinatlardi
                        // ve olculdu: en yakin sokak dugumu 211-463 m
                        // oteydi. Tur "burada kimse yok" diyordu, oysa
                        // oyuncu oralarda zaten yurumez — insanlar sokakta.
                        // Yanlis yere bakan bir olcu aleti, olmayan bir
                        // kusur bildirir; bu oturumda tam olarak bu hata
                        // bes kez tekrarlandi.
                        if (graf != null && graf.dugumler.Count > 0)
                        {
                            // BOS dugum aranir, en yakin degil.
                            //
                            // En yakini almak yetmedi: dugum bir yapinin
                            // icindeyse oyuncu orada dogar, fizik onu
                            // disari iterken zeminden gecirir ve arazinin
                            // 15 m altina duser. Turda uc durakta birden
                            // oldu.
                            var sirali = new List<Vector3>();
                            foreach (var n in graf.dugumler) sirali.Add(n.konum);
                            sirali.Sort((a, b) =>
                                (new Vector2(a.x, a.z) - new Vector2(hedef.x, hedef.z))
                                    .sqrMagnitude.CompareTo(
                                (new Vector2(b.x, b.z) - new Vector2(hedef.x, hedef.z))
                                    .sqrMagnitude));

                            // IKI GECIS: once tepesi ACIK bir dugum
                            // aranir, bulunamazsa eski davranisa dusulur
                            // (Surici gercekten kapali olabilir ve kare
                            // hic olmamasindan iyidir).
                            for (int gecis = 0; gecis < 2; gecis++)
                            {
                            bool acikAra = gecis == 0;
                            bool bulundu = false;
                            foreach (var aday in sirali)
                            {
                                // DURAK BIR YERDIR; 2 km oteden cekilen
                                // kare baska bir yerin karesidir.
                                //
                                // Liste mesafeye gore SIRALI, yani ilk
                                // asan noktada durmak yeterli. Sinir
                                // 150 m: bir mahalle capinin yarisi —
                                // durak hala "orasi" sayilir, ama
                                // sehrin obur ucuna kacamaz.
                                if ((new Vector2(aday.x, aday.z)
                                     - new Vector2(hedef.x, hedef.z))
                                    .sqrMagnitude > EnCokDurakSapmasi
                                                    * EnCokDurakSapmasi)
                                    break;

                                float ak2 = arazi != null
                                    ? arazi.SampleHeight(aday)
                                      + arazi.transform.position.y
                                    : aday.y;
                                float yz = ak2;
                                if (Physics.Raycast(
                                        new Vector3(aday.x, ak2 + 8f, aday.z),
                                        Vector3.down, out var vv, 20f, ~0,
                                        QueryTriggerInteraction.Ignore))
                                    yz = vv.point.y;
                                // ZEMIN, EVIN YA DA CESMENIN USTU DEGIL.
                                //
                                // Esik once 2 m idi ve mektebin catisini
                                // (+5,8 m) eledi — ama sadirvani elemedi:
                                // kenari araziden 1,0 m yukarida ve tur
                                // dort durakta birden oyuncuyu cesmenin
                                // USTUNE koydu. Karelerde adam suyun
                                // uzerinde duruyor.
                                //
                                // 0,35 m secildi cunku olculebilir bir
                                // seye dayaniyor: kaldirim bir rihtta
                                // 0,17 m yukselir (kaldirim_denetimi.md),
                                // iki riht 0,34. Yani kaldirim gecer,
                                // cesme kenari gecmez.
                                if (yz - ak2 > 0.35f) continue;
                                if (Physics.CheckCapsule(
                                        new Vector3(aday.x, yz + 0.45f, aday.z),
                                        new Vector3(aday.x, yz + 1.55f, aday.z),
                                        0.32f, ~0,
                                        QueryTriggerInteraction.Ignore))
                                    continue;
                                // BASIN USTU DE ACIK OLMALI.
                                //
                                // Kapsul denetimi 1,55 m'ye bakiyor ve
                                // cumbali bir sokakta o yukseklik hep
                                // bostur; oysa cikma 2,5 m'de baslar.
                                // Sonuc olculdu: on duragin dordunde kare,
                                // oyuncunun UZERINDEKI katin tabanini
                                // gosteriyordu — sehir degil, bir tavan.
                                // Bir gozlem araci, gozleyecegi seyin
                                // altinda duramaz.
                                if (acikAra && Physics.Raycast(
                                        new Vector3(aday.x, yz + 1.8f, aday.z),
                                        Vector3.up, 3.2f, ~0,
                                        QueryTriggerInteraction.Ignore))
                                    continue;
                                hedef = new Vector3(aday.x, hedef.y, aday.z);
                                bulundu = true;
                                acikBulundu = acikAra;
                                break;
                            }
                            if (bulundu) break;
                            }
                        }

                        // Yuzeyi bul: arazi kotu yeterli degil, kaldirim
                        // ve kaide arazinin USTUNDE.
                        // ZEMIN KATINA IN — CATIYA DEGIL.
                        //
                        // Yukaridan atilan isin once CATIYA carpiyor ve
                        // durak orada aciliyordu: olcumde Ayasofya'nin
                        // kubbesinde +70,1 m, Uskudar Mihrimah'ta +41,6 m
                        // cikti. Cati uzerinden alinan kare ne kalabaligi
                        // ne dokuyu gosterir.
                        //
                        // Cozum: yuzey arazi kotundan 2 m'den fazla
                        // yukaridaysa yok sayilir ve arazi kotu kullanilir.
                        float ilkKot = arazi != null
                            ? arazi.SampleHeight(hedef) + arazi.transform.position.y
                            : 0f;
                        float yuzey = ilkKot;
                        var tepe = new Vector3(hedef.x, ilkKot + 6f, hedef.z);
                        if (Physics.Raycast(tepe, Vector3.down, out var v, 12f,
                                            ~0, QueryTriggerInteraction.Ignore)
                            && v.point.y - ilkKot <= 2f)
                            yuzey = v.point.y;
                        hedef = new Vector3(hedef.x, yuzey + 0.3f, hedef.z);
                    }

                    var konulan = hedef;
                    cc.enabled = false;
                    oyuncu.transform.position = hedef;
                    oyuncu.transform.rotation = Quaternion.Euler(0f, d.bakisYaw, 0f);
                    cc.enabled = true;
                    Physics.SyncTransforms();

                    // KALABALIK VARSA KAMERA ONA DONER.
                    //
                    // Duraklarin bakis acisi elle secilmisti ve iki
                    // duragin amaci acikca "NPC yogunlugu" oldugu halde
                    // kare hep bir duvar gosteriyordu: 40 m'de 93 kisi
                    // sayiliyor, 60 govde ciziliyor ve karede kimse yok.
                    // Sayan ile bakan ayni yere bakmiyorsa gozlem
                    // sayidan kopar — bu oturumda uc kusur yalnizca
                    // bakinca goruldu.
                    //
                    // Esik 12: birkac yoldan gecen kalabalik degildir.
                    // BU BLOK DA AKIS BEKLEMESINDEN ONCE KOSUYOR.
                    //
                    // Aci aramasi bu turda akistan SONRAYA tasindi
                    // (bos bir dunyayi olcuyordu). Kalabalik merkezi
                    // hala once hesaplaniyor ve bu bilincli: `Sakinler`
                    // NPC yoneticisinin kendi havuzu, semt akisina bagli
                    // degil — turda duraklarda 57 NPC sayildi. Yine de
                    // kalabalik bulunamazsa karar aci aramasina dusuyor
                    // ve o artik yuklu dunyayi olcuyor, yani en kotu
                    // durumda geri cekilme dogru yerde.
                    Vector3? kalabalikMerkez = null;
                    bool kalabaligaDonuldu = false;
                    if (npc != null && npc.Sakinler != null)
                    {
                        var toplam = Vector3.zero;
                        int adet = 0;
                        foreach (var ajan in npc.Sakinler)
                        {
                            var fark = ajan.konum - oyuncu.transform.position;
                            fark.y = 0f;
                            if (fark.sqrMagnitude > 40f * 40f) continue;
                            toplam += ajan.konum;
                            adet++;
                        }
                        if (adet >= 12)
                        {
                            kalabalikMerkez = toplam / adet;
                        }
                        if (adet >= 12)
                        {
                            kalabaligaDonuldu = true;
                            var yon = toplam / adet - oyuncu.transform.position;
                            yon.y = 0f;
                            if (yon.sqrMagnitude > 1e-4f)
                            {
                                cc.enabled = false;
                                oyuncu.transform.rotation =
                                    Quaternion.LookRotation(yon.normalized);
                                cc.enabled = true;
                                Physics.SyncTransforms();
                            }
                        }
                    }

                    // SEMT AKISI SAYIYLA DEGIL, DURUMLA BEKLENIR.
                    //
                    // Burada 90 kare bekleniyordu ve turun kendi
                    // raporu bedelini yaziyordu: on duragin ALTISINDA
                    // ayak altinda `TR_Istanbul` (cıplak arazi), 40
                    // m'de 0 NPC, 0 cizilen govde. Kareler de oyle
                    // — `03_galata_sokak` bos bir kum duzlugu, sehir
                    // ufukta ince bir serit. Oysa durak (120, 60) ve
                    // D_Galata'nin siniri x -1944..1296, z -972..1944:
                    // durak semtin TAM ICINDE.
                    //
                    // Yani sehir yok degil, HENUZ YUKLENMEMIS. Akis
                    // Addressables ile asenkron yukluyor ve 90 kare
                    // bunun bittigini GORMUYOR — bu deponun tekrar eden
                    // dersi: bir bekleme, bekledigi seyin bittigini
                    // gormeden bitiyorsa bekleme degildir. Ayni kusur
                    // APV firininda da vardi ve orada `_pisirmeGoruldu`
                    // ile kapandi.
                    float akisBekleme;
                    int akisSemt;
                    // APV CALISIYOR MU — PISEN VERI KAREYE ULASTI MI.
                    //
                    // Bu turda fırın bitti ve diske 157 MB prob verisi
                    // yazdi (62 hucre), ama Galata sokaginin golgesi
                    // hala mavi/kirmizi 0,000. Yani soru artik "pisti
                    // mi" degil: "pisen sey KAREYE ULASIYOR mu".
                    //
                    // Bu deponun ikinci tekrar eden dersi tam olarak
                    // bu: yazildi, diske gecti, baglanmadi. Rapor
                    // artik calisma zamaninin kendi cevabini tasiyor.
                    string apv;
                    var akis = Object.FindAnyObjectByType<DistrictStreamer>();
                    // Akisin en az bir kez degerlendirmesi icin
                    // (`evaluateInterval` 0,25 sn) yarim saniye.
                    for (int i = 0; i < 40; i++) yield return null;
                    float _bekleT0 = Time.realtimeSinceStartup;
                    while (akis != null && akis.LoadsInFlight > 0
                           && Time.realtimeSinceStartup - _bekleT0 < 30f)
                        yield return null;
                    akisBekleme = Time.realtimeSinceStartup - _bekleT0;
                    akisSemt = 0;
                    if (akis != null)
                        foreach (var _ in akis.ResidentDistricts) akisSemt++;
                    // KAMERA KARE AYARI DENENDI VE OLCUM ELEDI.
                    //
                    // Kameraya `AdaptiveProbeVolume` biti acikca
                    // yazildi (`ApvDenetimi.KameraApvAc`) ve kare
                    // DEGISMEDI: golge 0,0203/0,0062/0,0001 — biti
                    // yazmadan onceki 0,0202/0,0061/0,0001 ile ayni.
                    // Halka elendi; komut menude duruyor ama turda
                    // kosmuyor, cunku her durakta sahneyi kirletiyordu
                    // ve karsiliginda hicbir sey vermiyordu.
                    var _prv = ProbeReferenceVolume.instance;
                    apv = _prv == null
                        ? "yok"
                        : $"{(_prv.isInitialized ? "kurulu" : "KURULMADI")}"
                          + $"/kume {(_prv.currentBakingSet != null ? "var" : "YOK")}";
                    // Yuklenen sahnenin ciziciler ve fizik olarak
                    // oturmasi.
                    for (int i = 0; i < 60; i++) yield return null;

                    // ACI ARAMASI **SEMT YUKLENDIKTEN SONRA**.
                    //
                    // Bu blok akis beklemesinden ONCE kosuyordu ve
                    // olculdu: sehir henuz gelmemisken her yon "80 m,
                    // hicbir sey" okuyor, ilk aday (0 derece) 12 m
                    // esigini gecmis sayiliyor ve arama daha ilk
                    // adimda bitiyor. Rapor bunu yaziyordu: dort
                    // durakta `kadrajda gok @ 0 m` — kamera hicbir seye
                    // bakmiyor.
                    //
                    // Yani aciyi secen olcu BOS BIR DUNYAYI olcuyordu.
                    // Bu turdaki ucuncu ornegi: bir olcum, olctugu sey
                    // daha ortada yokken calisiyor.
                    // DUVARA BAKAN BIR OLCU ALETI SUS URETIR.
                    //
                    // Duraklarin bakis acisi elle secilmisti ve karelere
                    // BAKINCA goruldu: 10_uskudar'da on metredeki bir tas
                    // blok karenin %60'ini kapliyor, 07_kirsal'da kare bir
                    // ahsap tavan ve bos siva duvari gosteriyor. Oysa
                    // yerlestirme dogruydu — ayni turda "acik dugum: E,
                    // kayma 0,0 m, tepe acik: E" yaziyor. Yani kusur
                    // oyuncunun DURDUGU yerde degil, BAKTIGI yondeydi.
                    //
                    // Elle secilen aci atilmiyor: durak "Ayasofya" diyorsa
                    // oraya bakmasinin bir sebebi var. Aci korunur ve
                    // yalnizca onu goren en yakin ACIK yone kaydirilir.
                    // Hicbiri acik degilse en uzagi gorene gidilir —
                    // kapali bir avluda bile en iyi kare o.
                    //
                    // Kalabaliga donulduyse dokunulmaz: o karar bundan
                    // daha iyi bir sebebe dayaniyor.
                    float aciKaymasi = 0f;
                    if (!kalabaligaDonuldu)
                    {
                        var goz = oyuncu.transform.position
                                  + Vector3.up * 1.6f;
                        // ARAMA ARTIK ERKEN KESILMIYOR.
                        //
                        // Eski dongü ilk ACIK yonde duruyordu ve acik
                        // olmak yetmiyor: hicbir seye carpmayan bir yon
                        // de 80 m okur ve ilk adimda kazanir. Raporun
                        // karsiligi `kadrajda gok @ 0 m` — kamera hicbir
                        // seye bakmiyor, yani o kare bir inceleme karesi
                        // degil.
                        //
                        // Butun adaylar taranir ve sirasiyla:
                        //   1. hem ACIK (>=12 m) hem KONULU (bir seye
                        //      carpan) yonlerden ORIJINALE EN YAKINI,
                        //   2. yoksa en cok acikligi olan.
                        // Ikincisi kirda ve denizde dogru cevap: orada
                        // gok gercekten konudur.
                        float enIyiAci = 0f, enIyiUzak = -1f;
                        float konuAci = 0f;
                        bool konuVar = false;
                        for (int adim = 0; adim <= 6; adim++)
                        {
                            for (int isaret = 1; isaret >= -1; isaret -= 2)
                            {
                                float sap = adim * 15f * isaret;
                                var yon = Quaternion.Euler(
                                    0f, d.bakisYaw + sap, 0f) * Vector3.forward;
                                bool vurdu = Physics.Raycast(
                                    goz, yon, out var carp, 80f, ~0,
                                    QueryTriggerInteraction.Ignore);
                                float uzak = vurdu ? carp.distance : 80f;
                                if (uzak > enIyiUzak)
                                { enIyiUzak = uzak; enIyiAci = sap; }
                                // 12 m: dar sokak bile bu kadar derindir
                                // (sokak eni 7,2 m, ADR 0075). Bundan
                                // yakini duvardir.
                                if (vurdu && uzak >= 12f && !konuVar)
                                { konuVar = true; konuAci = sap; }
                                if (adim == 0) break;   // 0 derecenin esi yok
                            }
                        }
                        if (konuVar) enIyiAci = konuAci;
                        aciKaymasi = enIyiAci;
                        if (Mathf.Abs(aciKaymasi) > 0.01f)
                        {
                            cc.enabled = false;
                            oyuncu.transform.rotation = Quaternion.Euler(
                                0f, d.bakisYaw + aciKaymasi, 0f);
                            cc.enabled = true;
                            Physics.SyncTransforms();
                        }
                    }


                    // --- KOS ---
                    if (d.kos)
                    {
                        float t0 = Time.time;
                        while (Time.time - t0 < 2.0f)
                        {
                            cc.Move(oyuncu.transform.forward
                                    * oyuncu.runSpeed * Time.deltaTime
                                    + Vector3.down * 4f * Time.deltaTime);
                            yield return null;
                        }
                    }

                    // --- OLC ---
                    var p = oyuncu.transform.position;
                    string altinda = "?";
                    if (Physics.Raycast(p + Vector3.up * 0.6f, Vector3.down,
                                        out var alt, 12f, ~0,
                                        QueryTriggerInteraction.Ignore))
                        altinda = alt.collider.name;
                    float araziKot = arazi != null
                        ? arazi.SampleHeight(p) + arazi.transform.position.y
                        : 0f;

                    // KAYMA: kondugu yer ile durdugu yer arasindaki
                    // yatay mesafe. Fizik oyuncuyu cakisan bir
                    // carpistiricidan disari itiyorsa yerlestiricinin
                    // secimi bir sey ifade etmez ve kusur secimde
                    // aranmaz.
                    // DURAK SAPMASI: ISTENEN YER ILE DURULAN YER.
                    //
                    // 05_ayasofya ve 09_marmara karelerine BAKINCA
                    // goruldu: oyuncu bombos, kumsal rengi bir arazide
                    // duruyor ve ufukta kucucuk bir kubbe var. Sayilar
                    // ise "acik dugum: E, kayma 0,0 m, tepe acik: E"
                    // diyordu — yani yerlestirme kendi olcusune gore
                    // KUSURSUZ calismisti.
                    //
                    // Eksik olan olcu buydu: yerlestirici en yakin ACIK
                    // sokak dugumunu ariyor ve arama butun sehri
                    // tariyor, MESAFE SINIRI YOK. Yakindaki dugumlerin
                    // hepsi elenirse durak kilometrelerce oteye kayar ve
                    // hicbir sayi bunu soylemez.
                    //
                    // Bir olcu aleti nerede durdugunu bilmiyorsa
                    // olctugu sey de belirsizdir.
                    var sapmaV = p - (d.nokta == Vector3.zero ? dogum : d.nokta);
                    sapmaV.y = 0f;
                    float durakSapmasi = sapmaV.magnitude;

                    var kaymaV = p - konulan;
                    kaymaV.y = 0f;
                    float kayma = kaymaV.magnitude;

                    // TEPE: SON yerinde gok gorunuyor mu. Yerlestirici
                    // ayni soruyu ADAY icin soruyor; burada SONUC icin
                    // soruluyor. Ikisi ayrilirsa arada gecen sey
                    // (fizik, akis, semt yuklemesi) suclu demektir.
                    // KAPALIYSA NEYIN KAPATTIGI DA YAZILIR.
                    //
                    // Sutun on durakta bir kez "H" diyor
                    // (`06_kara_surlari`) ve kareye bakilinca oyuncunun
                    // uc metre ustunde karenin tamamini kaplayan duz bir
                    // TAS KUTLE goruluyor. "Kapali" bilgisi kusuru
                    // gosteriyor ama pesine dusulecek bir ip vermiyor;
                    // carpan seyin ADI veriyor. Ayni sey `kadrajda`
                    // sutununda zaten yapiliyor — bu, onun yukari
                    // bakani.
                    bool tepeAcik = !Physics.Raycast(
                        p + Vector3.up * 1.8f, Vector3.up,
                        out var tepeVurus, 3.2f, ~0,
                        QueryTriggerInteraction.Ignore);
                    string tepeAd = tepeAcik
                        ? "-"
                        : $"{tepeVurus.collider.name} @ "
                          + $"{tepeVurus.distance:0.0} m";

                    // KARE SURESI: OTURDUKTAN SONRA, ORTANCA.
                    //
                    // Once isinlanmadan hemen sonra on karenin ORTALAMASI
                    // aliniyordu ve o sayi oturmus kareyi degil, akisin
                    // ve havuzun o andaki telasini olcuyordu: ayni yerde
                    // 14,7 ms de cikti 30,6 ms de. Ortalama, tek bir
                    // sicramayla suruklenir; ortanca surumez.
                    for (int i = 0; i < 120; i++) yield return null;   // otur
                    var ornekler = new List<float>(90);
                    for (int i = 0; i < 90; i++)
                    { yield return null; ornekler.Add(Time.unscaledDeltaTime); }
                    ornekler.Sort();
                    float ms = ornekler[ornekler.Count / 2] * 1000f;

                    // REPLIK SAYISI BURADA OKUNUR — SONRA DEGIL.
                    //
                    // Satir en sonda `bark.GorunurReplik` okuyordu ve
                    // arada KALABALIK KARESI var: o blok kamerayi 13 m
                    // yukari tasiyip dort kare bekliyor, `BarkGosterici`
                    // de her LateUpdate'te sayiyi o kameraya gore
                    // yeniden hesapliyor. Yani rapor, oyuncunun
                    // gordugu repligi degil denetim kamerasinin
                    // gordugunu yaziyordu — ve on duragin onunda da 0
                    // cikiyordu.
                    //
                    // Kusur bark sisteminde degil OLCUMUN YERINDEYDI.
                    // Bu depoda tekrar eden ders: bozuk olan cogu zaman
                    // olctugun sey degil, olcme bicimin.
                    int replikSayisi = bark != null ? bark.GorunurReplik : 0;

                    // KADRAJDA NE VAR — TURUN SORMADIGI SORU.
                    //
                    // Tablo "ayak altinda", "acik dugum", "tepe acik"
                    // yaziyor; hepsi oyuncunun DURDUGU yerle ilgili.
                    // Oysa bir tur karesi bir GORUNTUdur ve karelere
                    // bakinca kusur hep orada cikti: 07_kirsal bir
                    // tavan, 10_uskudar on metredeki bir tas blok,
                    // 05_ayasofya bombos arazi gosteriyor. Hicbiri
                    // sayida yoktu.
                    //
                    // Kameranin merkezinden bir isin: ne var, ne kadar
                    // uzakta. Bir gozlem aracinin en az soylemesi
                    // gereken sey, neye baktigidir.
                    string kadrajda = "gok";
                    float kadrajUzak = 0f;
                    if (Physics.Raycast(kam.transform.position,
                                        kam.transform.forward,
                                        out var kadrajCarp, 400f, ~0,
                                        QueryTriggerInteraction.Ignore))
                    {
                        kadrajda = kadrajCarp.collider.name;
                        kadrajUzak = kadrajCarp.distance;
                    }

                    // --- YAKALA ---
                    var rt = new RenderTexture(1280, 720, 24,
                                               RenderTextureFormat.ARGB32);
                    kam.targetTexture = rt;
                    for (int i = 0; i < 6; i++) kam.Render();
                    RenderTexture.active = rt;
                    var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
                    tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                    tex.Apply();
                    RenderTexture.active = null;
                    kam.targetTexture = null;
                    File.WriteAllBytes($"{Cikti}/{d.ad}.png",
                                       ImageConversion.EncodeToPNG(tex));
                    Object.DestroyImmediate(tex);
                    rt.Release();

                    // --- KALABALIK KARESI --------------------------------
                    //
                    // Duraklarin bakis acisi elle secilmis ve iki duragin
                    // amaci acikca "NPC yogunlugu" oldugu halde kare hep
                    // bir duvar gosteriyordu: 40 m'de 93 kisi sayiliyor,
                    // 60 govde ciziliyor, karede kimse yok. Kamerayi
                    // kalabaliga DONDURMEK yetmedi — arada duvar var.
                    //
                    // Bir gozlem araci, gozleyecegi seyi goremiyorsa
                    // olcum degil sus uretir. Bu kare kamerayi
                    // kalabaligin USTUNE cikarir: insanlar orada mi,
                    // birbirinden ayrisiyor mu, ancak boyle gorulur.
                    // Oyunun kadraji degil, bir DENETIM karesidir ve
                    // adi da bunu soyler.
                    if (kalabalikMerkez.HasValue)
                    {
                        var eskiKonum = kam.transform.position;
                        var eskiDonus = kam.transform.rotation;
                        // KAMERA KIPI OYUNCUDA, KAMERADA DEGIL.
                        //
                        // Ilk denemede `kam.GetComponent<KameraKipi>()`
                        // yaziliydi ve null donuyordu; bilesen her
                        // LateUpdate'te kameranin konumunu yeniden
                        // yaziyor, benim koydugum yer bir kare bile
                        // yasamiyordu. Sonuc: "kalabalik karesi" normal
                        // kareyle piksel piksel ayniydi — yani yeni bir
                        // olcum uretmis gibi gorunup hicbir sey
                        // olcmuyordu. Turun kendi `kip` degiskeni zaten
                        // dogru nesneyi tutuyor.
                        var kip2 = kip;
                        if (kip2 != null) kip2.enabled = false;

                        var mrk = kalabalikMerkez.Value;
                        // YUKSEKLIK OLCULDU: 7,5 m'de kamera bir catinin
                        // ARDINDA kaldi ve kare kiremit gosterdi. Sehrin
                        // catilari 6-9 m; 13 m onlarin ustune cikar ve
                        // 9 m'lik geri cekilme ~55 derecelik bir bakis
                        // acisi verir — insanlarin hem boyu hem araligi
                        // ayni karede okunur.
                        kam.transform.position = mrk
                            + new Vector3(0f, 13f, -9f);
                        kam.transform.rotation = Quaternion.LookRotation(
                            (mrk + Vector3.up * 1.0f)
                            - kam.transform.position);
                        // ZAMANSAL ETKILER YAKINSASIN — DORT KARE AZ.
                        //
                        // Denetim karesinde catilarin ve pencerelerin
                        // uzerinde yuzlerce beyaz BENEK vardi; kar gibi.
                        // Ayni yerin goz hizasi karesi (210 kare
                        // oturma) tertemiz. Yani benek oyunun degil
                        // TURUN kusuru: hacimsel bulut 0,90 zamansal
                        // birikim kullaniyor, SSGI ve hacimsel sis de
                        // kare kare temizleniyor; dort karede hicbiri
                        // oturmuyor.
                        //
                        // Ayni sinif kusur bu araçta zaten yazili
                        // (otomatik poz penceresiz kipte yakinsamiyor).
                        // Bir gozlem araci, gozledigi seyin oturmasini
                        // BEKLEMEK zorunda.
                        for (int i = 0; i < 90; i++) yield return null;

                        var rt2 = new RenderTexture(1280, 720, 24,
                                                    RenderTextureFormat.ARGB32);
                        kam.targetTexture = rt2;
                        for (int i = 0; i < 24; i++) kam.Render();
                        RenderTexture.active = rt2;
                        var tex2 = new Texture2D(1280, 720,
                                                 TextureFormat.RGB24, false);
                        tex2.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                        tex2.Apply();
                        RenderTexture.active = null;
                        kam.targetTexture = null;
                        File.WriteAllBytes($"{Cikti}/{d.ad}_kalabalik.png",
                                           ImageConversion.EncodeToPNG(tex2));
                        Object.DestroyImmediate(tex2);
                        rt2.Release();

                        kam.transform.position = eskiKonum;
                        kam.transform.rotation = eskiDonus;
                        if (kip2 != null) kip2.enabled = true;
                    }

                    // OYUNCU NEREDE — RAPORDA EN BASIT VE EN EKSIK OLCU.
                    //
                    // 01_dogum karesi ufka kadar bos toprak gosteriyor
                    // ve 03_galata_sokak da oyle; ikisi de sehirde
                    // olmali. Tabloda bunu soyleyecek tek sey yoktu:
                    // "ayak altinda TR_Istanbul" bir yer adi degil,
                    // yalnizca carpistiricinin adi. Bir tur raporu,
                    // turun NEREDE yapildigini yazmalidir.
                    satirlar.Add($"| {d.ad} | {p.x:0}, {p.z:0} | {altinda} | "
                                 + $"{p.y - araziKot:+0.0;-0.0} | "
                                 + $"{(kip != null ? kip.SonMesafe.ToString("0.00") : "?")} | "
                                 + $"{YakindakiNpc(npc, oyuncu.transform.position)} | "
                                 + $"{CizilenGovde(npc)} | "
                                 + $"{replikSayisi} / "
                                 + $"{DuymaMenzilinde(npc, bark, p)} | "
                                 + $"{kadrajda} @ {kadrajUzak:0} m | "
                                 + $"{akisSemt} ({akisBekleme:0.0} sn) | "
                                 + $"{apv} | "
                                 + $"{(acikBulundu ? "E" : "H")} | "
                                 + $"{durakSapmasi:0} | "
                                 + $"{kayma:0.0} | "
                                 + $"{(tepeAcik ? "E" : tepeAd)} | "
                                 + $"{aciKaymasi:+0;-0;0}° | "
                                 + $"{ms:0.0} | {d.neden} |");
                    Debug.Log($"[Hezarfen] tur {d.ad}: {altinda}, "
                              + $"kol {(kip != null ? kip.SonMesafe : 0f):0.0}, "
                              + $"npc {YakindakiNpc(npc, oyuncu.transform.position)}, "
                              + $"govde {CizilenGovde(npc)}, "
                              + $"semt {akisSemt} ({akisBekleme:0.0} sn), "
                              + $"{ms:0.0} ms");
                }

                File.WriteAllText($"{Cikti}/tur.md",
                                  string.Join("\n", satirlar));
                if (pozGo != null) Object.DestroyImmediate(pozGo);
                Debug.Log($"[Hezarfen] OYUN TURU BITTI -> {Cikti}/tur.md");
                if (Toplu)
                {
                    // Kare dosyalari diske yazilsin diye bir kare daha.
                    yield return null;
                    EditorApplication.Exit(0);
                }
            }
        }
    }
}
