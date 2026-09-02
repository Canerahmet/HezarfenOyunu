using System.Collections;
using System.Collections.Generic;
using System.IO;
using Hezarfen.Player;
using Hezarfen.Sehir;
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
            new Durak { ad = "01_dogum", nokta = Vector3.zero, bakisYaw = 0f,
                        neden = "Oyuncu ilk burayi gorur." },
            new Durak { ad = "02_dogum_kosu", nokta = Vector3.zero,
                        bakisYaw = 0f, kos = true,
                        neden = "Kosarken karakter ve kamera." },
            new Durak { ad = "03_galata_sokak", nokta = new Vector3(120f, 0f, 60f),
                        bakisYaw = 200f,
                        neden = "Dar sokakta kamera kolu ve kalabalik." },
            new Durak { ad = "04_surici", nokta = new Vector3(-700f, 0f, -1500f),
                        bakisYaw = 90f,
                        neden = "Surici dokusu ve NPC yogunlugu." },
            new Durak { ad = "05_ayasofya", nokta = new Vector3(549f, 0f, -1886f),
                        bakisYaw = 270f,
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
            new Durak { ad = "09_marmara", nokta = new Vector3(-1850f, 0f, -2700f),
                        bakisYaw = 180f,
                        neden = "Kiyi, iskele ve deniz." },
            new Durak { ad = "10_uskudar", nokta = new Vector3(3500f, 0f, 200f),
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

            internal IEnumerator Kos(Durak[] duraklar)
            {
                Directory.CreateDirectory(Cikti);
                var satirlar = new List<string>();
                satirlar.Add("# Oyun turu");
                satirlar.Add("");
                satirlar.Add("Her durakta kamera karesi kaydedildi ve yanina");
                satirlar.Add("sayilar yazildi. Kare bir GOZLEM, sayi bir KANIT.");
                satirlar.Add("");
                satirlar.Add("| durak | ayak altinda | arazi farki | kamera kolu "
                             + "| 40 m'de NPC | cizilen govde | replik | kare (ms) | neden |");
                satirlar.Add("|---|---|---:|---:|---:|---:|---:|---:|---|");

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
                    Vector3? kalabalikMerkez = null;
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

                    // Semt akisi ve zemin otursun.
                    for (int i = 0; i < 90; i++) yield return null;

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
                        for (int i = 0; i < 4; i++) yield return null;

                        var rt2 = new RenderTexture(1280, 720, 24,
                                                    RenderTextureFormat.ARGB32);
                        kam.targetTexture = rt2;
                        for (int i = 0; i < 6; i++) kam.Render();
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

                    satirlar.Add($"| {d.ad} | {altinda} | "
                                 + $"{p.y - araziKot:+0.0;-0.0} | "
                                 + $"{(kip != null ? kip.SonMesafe.ToString("0.00") : "?")} | "
                                 + $"{YakindakiNpc(npc, oyuncu.transform.position)} | "
                                 + $"{CizilenGovde(npc)} | "
                                 + $"{(bark != null ? bark.GorunurReplik : 0)} | "
                                 + $"{ms:0.0} | {d.neden} |");
                    Debug.Log($"[Hezarfen] tur {d.ad}: {altinda}, "
                              + $"kol {(kip != null ? kip.SonMesafe : 0f):0.0}, "
                              + $"npc {YakindakiNpc(npc, oyuncu.transform.position)}, "
                              + $"govde {CizilenGovde(npc)}, "
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
