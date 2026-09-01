using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Hezarfen.Flight;
using Hezarfen.Player;
using UnityEditor;
using UnityEngine;

namespace Hezarfen.Editor.Diagnostics
{
    /// <summary>
    /// <b>Oyunun finali gerçekten uçuluyor mu.</b>
    ///
    /// ## Neden bu ölçüm var
    ///
    /// Uçuş bu oyunun kalbi ve bugüne kadar hiç <b>oyun sahnesinde</b>
    /// ölçülmedi. Ölçülmeyince de şu görülmedi: kule (52 m) ile
    /// Doğancılar (46,6 m) arası 3.336 m yatay ve 5,4 m kot farkı var,
    /// yani gereken süzülme oranı <b>618:1</b>. Kanadın verdiği
    /// 11,56:1. Sabit 9 m/s kuyruk rüzgârıyla gidilen mesafe 1.037 m.
    /// Oyunun finali <b>aritmetik olarak</b> bitirilemezdi ve hiçbir
    /// test bunu sormuyordu — çünkü bütün testler kanadın <b>fiziğini</b>
    /// soruyordu, kanadın <b>gittiği yeri</b> değil.
    ///
    /// Açığı kapatacak şey termik: uçuş önce Galata yamacında yükselir,
    /// sonra Boğaz'ı geçer. Gereken net kazanç 283 m. Bu sınıf o
    /// kazancın gerçekten toplanıp toplanmadığını sayar.
    ///
    /// ## Neden otomatik pilot, neden elle uçmak değil
    ///
    /// Elle uçulan tek bir uçuş, bir kere yapılan bir gözlemdir; ertesi
    /// gün tekrarlanamaz ve iyileşip iyileşmediği bilinemez. Burada
    /// aynı 20 uçuş her turda aynı tohumla tekrarlanır. Pilot basittir
    /// ve bilerek öyle: <b>en iyi süzülme hızını tut, tırmanış
    /// bulduğunda dön</b>. Bir insanın yapacağından kötü uçar, yani
    /// ölçüm iyimser değil kötümserdir — ve bir kapı için doğru olan
    /// yön budur.
    /// </summary>
    public static class UcusDenemesi
    {
        private const string Cikti = "../../renders/denetim";

        /// <summary>
        /// Kule <b>tabanı</b> — dünya orijini (ADR 0007).
        ///
        /// Kalkış noktası bu DEĞİL: ilk koşumda oyuncu buraya konuldu
        /// ve yirmi uçuşun yirmisi de sıfır saniye sürdü, çünkü burası
        /// yer seviyesi. Atlayan anında zemine değip iniyordu ve tablo
        /// "0/20 vardı" diyordu — doğru bir sayı, yanlış bir soru.
        ///
        /// Tepe <see cref="KuleTepesi"/> ile <b>ölçülür</b>, yazılmaz:
        /// kule modeli değişirse sayı da değişsin.
        /// </summary>
        public static readonly Vector3 KuleTabani = new Vector3(0f, 52f, 0f);

        /// <summary>Doğancılar — iniş hedefi.</summary>
        public static readonly Vector3 Dogancilar =
            new Vector3(3267.6f, 46.6f, -672.9f);

        /// <summary>Perde2Dilimi'nin iniş yarıçapı.</summary>
        public const float InisYaricapi = 220f;

        /// <summary>Kaç uçuş denenir.</summary>
        public const int Deneme = 20;

        /// <summary>
        /// Kulenin şerefesi — atlayış buradan yapılır.
        ///
        /// Yukarıdan aşağı ışın atılır; ilk çarptığı yer kulenin
        /// tepesidir. Bir sayı yazmak yerine ölçmek, kule modeli
        /// değiştiğinde bu ölçümün sessizce yanlışa düşmesini önler —
        /// bu oturumda tam olarak öyle bir sessiz yanlış yaşandı.
        ///
        /// RESEARCH.md 168: <i>"Galata Kulesi ile Doğancılar arası kot
        /// farkı ~62 m, yatay mesafe ~3358 m"</i>. Ölçülen tepe bu
        /// sayıyla tutmalı; tutmuyorsa ya kule ya hedef yanlış yerde.
        /// </summary>
        public static Vector3 KuleTepesi()
        {
            var tepeden = new Vector3(KuleTabani.x, KuleTabani.y + 300f,
                                      KuleTabani.z);
            if (Physics.Raycast(tepeden, Vector3.down, out var v, 320f, ~0,
                                QueryTriggerInteraction.Ignore))
                return v.point;
            return KuleTabani;
        }

        /// <summary>
        /// Toplu kipten koşulabilen giriş — <b>ADR 0084'ün kapısı bu</b>.
        ///
        /// <c>KareBolusumu</c> bu dersi öğrendi ve yazdı: <i>elle
        /// yapılan adım, yapılmayan adımdır.</i> Bedeli ölçüldü —
        /// <c>ucus_denemesi.md</c>'nin son yazılma zamanı, uçuş
        /// modelini değiştiren commit'ten <b>üç commit eskiydi</b>.
        /// Yani kapıyı tutan sayı, kapıyı değiştiren işi hiç görmemişti.
        /// </summary>
        public static void TopluKos()
        {
            UnityEditor.SceneManagement.EditorSceneManager
                .OpenScene("Assets/_Project/Scenes/Faz1_Terrain.unity");
            EditorApplication.playModeStateChanged += DurumDegisti;
            EditorApplication.EnterPlaymode();
        }

        private static void DurumDegisti(PlayModeStateChange d)
        {
            if (d != PlayModeStateChange.EnteredPlayMode) return;
            EditorApplication.playModeStateChanged -= DurumDegisti;
            Baslat();
        }

        private static bool Toplu =>
            System.Environment.CommandLine.Contains("TopluKos");

        [MenuItem("Hezarfen/Olcum/Ucus denemesi (20 ucus)")]
        public static void Baslat()
        {
            if (!Application.isPlaying)
            {
                Debug.LogError("[Hezarfen] Once OYNAT.");
                return;
            }
            var k = Object.FindAnyObjectByType<Kosucu>()
                    ?? new GameObject("UCUS_DENEMESI").AddComponent<Kosucu>();
            k.StartCoroutine(k.Kos());
        }

        public class Kosucu : MonoBehaviour
        {
            private sealed class Sonuc
            {
                public int no;
                public float yatay;        // katedilen yatay mesafe (m)
                public float sure;         // uçuş süresi (s)
                public float enYuksek;     // en yüksek kot (m)
                public float kazanc;       // kalkış kotuna göre net kazanç
                public float hedefeUzak;   // iniş noktası → Doğancılar
                public bool vardi;
                public string hata;
                public Vector3 bas;
            }

            internal IEnumerator Kos()
            {
                var oyuncu = GameObject.Find("OYUNCU");
                if (oyuncu == null)
                {
                    Debug.LogError("[Hezarfen] OYUNCU yok.");
                    yield break;
                }
                var suzulme = oyuncu.GetComponent<GlideController>();
                var dizi = oyuncu.GetComponent<UcusDizisi>();
                var govde = oyuncu.GetComponent<Rigidbody>();
                if (suzulme == null || dizi == null || govde == null)
                {
                    Debug.LogError("[Hezarfen] Ucus bilesenleri eksik.");
                    yield break;
                }

                var alan = Object.FindAnyObjectByType<WindField>();
                var sonuclar = new List<Sonuc>();

                // ZAMAN SABITLENIR.
                //
                // Kare suresi degiskense her ucus baska bir fizik adimi
                // gorur ve yirmi ucusun dagilimi ruzgari degil bilgisayari
                // olcer. 60 Hz sabit.
                float eskiYakala = Time.captureDeltaTime;
                Time.captureDeltaTime = 1f / 60f;

                // KALABALIK VE AGACLAR KAPATILIR — OLCUM ZAMANI ICIN.
                //
                // `captureDeltaTime` ile benzetim, cizilen her karede
                // 1/60 s ilerler; yani olcumun GERCEK suresi kare
                // hizina baglidir. Kalabalik 1,2 ms, agac cizici 0,3 ms
                // ve sarmal dalis duzeltilince ucuslar uzadigi icin
                // deneme saatlere ciktı.
                //
                // Ikisi de ucusun fizigine dokunmuyor: NPC yerde
                // yuruyor, agac cizici yalniz ciziyor. Kapatmak
                // olculen seyi degistirmez, olcumun BEDELINI degistirir.
                //
                // Saatler suren bir olcum bir daha kosulmaz; kosulmayan
                // olcum de kapi tutmaz.
                var kalabalik = Object.FindAnyObjectByType<Sehir.NPCYonetici>();
                var agac = Object.FindAnyObjectByType<Tani.AgacCizici>();
                bool kalabalikAcikti = kalabalik != null && kalabalik.enabled;
                bool agacAcikti = agac != null && agac.enabled;
                if (kalabalik != null) kalabalik.enabled = false;
                if (agac != null) agac.enabled = false;

                // KONTROL DENEMESI — ONCE BU.
                //
                // Ilk alti kosumda "ucus calismiyor" denip pilota,
                // termige ve durum makinesine bakildi. Tek bir eller
                // serbest ucus bu turu ilk dakikada bitirirdi: kanat
                // teorik oranini veriyorsa supheyi dogrudan pilota
                // goturur. Bir olcum, olctugu seyi AKLAYABILMELIDIR.
                var kontrol = new Sonuc { no = 0 };
                yield return Uc(oyuncu, dizi, govde, 0f, kontrol,
                                ellerSerbest: true);
                sonuclar.Add(kontrol);

                for (int i = 0; i < Deneme; i++)
                {
                    // Baslangic yonu: kuleden yelpaze halinde bes yon,
                    // her yonde dort tekrar (turbulans ve faz farki).
                    // BES DEGIL YIRMI AYRI UCUS.
                    //
                    // Once `yaw = -40 + (i % 5) * 20` idi: bes yon,
                    // dort kez tekrar. Ama sahnede turbulans yok ve
                    // `captureDeltaTime` sabit — tekrarlari
                    // farklilastiracak HICBIR kaynak yoktu. Tablodaki
                    // 15 satir bayt bayt kopyaydi ve arac bes orneklik
                    // bir istatistigi yirmi diye rapor ediyordu.
                    float yaw = -40f + i * (80f / (Deneme - 1));
                    var s = new Sonuc { no = i + 1 };
                    yield return Uc(oyuncu, dizi, govde, yaw, s);
                    sonuclar.Add(s);
                }

                Time.captureDeltaTime = eskiYakala;
                if (kalabalik != null) kalabalik.enabled = kalabalikAcikti;
                if (agac != null) agac.enabled = agacAcikti;
                Yaz(sonuclar, alan);
            }

            /// <summary>
            /// Bir uçuş — <b>durum makinesini atlayarak</b>.
            ///
            /// İlk üç koşum <see cref="UcusDizisi"/> üzerinden gitti ve
            /// üçü de başka bir yerde takıldı: kule tabanında doğuldu,
            /// kuşanırken dama düşüldü, kalkışta zemin ayağın altında
            /// bulundu. Her seferinde tabloda sıfır vardı ve her
            /// seferinde sebep <b>uçuşun kendisi değildi</b>.
            ///
            /// Sorulan soru şu: <i>kanat ve termik, 3.336 m'yi
            /// kapatıyor mu.</i> Bu sorunun kuşanma animasyonuyla,
            /// temas denetimiyle ya da giriş durumuyla ilgisi yok.
            /// Onları ölçüme karıştırmak, ölçülmek istenen şeyi üç
            /// ayrı gürültünün arkasına saklamak oldu.
            ///
            /// Burada uçuş fiziği <b>doğrudan</b> sürülüyor: gövde
            /// kinematiklikten çıkar, süzülme açılır, fırlatma hızı
            /// verilir ve zemine değene kadar entegre edilir. Durum
            /// makinesinin çalışıp çalışmadığı ayrı bir soru ve ayrı
            /// bir yerde sorulmalı.
            /// </summary>
            private IEnumerator Uc(GameObject oyuncu, UcusDizisi dizi,
                                   Rigidbody govde, float yaw, Sonuc s,
                                   bool ellerSerbest = false)
            {
                var cc = oyuncu.GetComponent<CharacterController>();
                var yurume = oyuncu.GetComponent<Hezarfen.Player.WalkController>();
                var kapsul = oyuncu.GetComponent<CapsuleCollider>();
                var suzulme = oyuncu.GetComponent<GlideController>();
                var girdi = oyuncu.GetComponent<PlayerFlightInput>();

                // --- yurume fizigini kapat, ucus fizigini ac ---
                if (dizi != null) dizi.enabled = false;
                if (yurume != null) yurume.enabled = false;
                if (cc != null) cc.enabled = false;
                if (kapsul != null) kapsul.enabled = true;
                if (girdi != null) girdi.enabled = false;

                // ISINLAMA FIZIK YOLUYLA.
                //
                // Onceki kosum `transform.SetPositionAndRotation` ile
                // kuleye koydu ve tutmadi: tabloda "en yuksek 69-71 m,
                // hedefe 3401 m" yaziyordu ve bunlar kulenin degil
                // DOGUM YERININ sayilari. Bir Rigidbody varken konumun
                // sahibi transform degil govdedir; ikisine ayri ayri
                // yazmak, bir sayinin iki sahibi olmasinin bu
                // dosyadaki hali.
                var tepe = KuleTepesi();
                var kalkis = tepe + Vector3.up * 2.0f;

                govde.isKinematic = false;
                govde.useGravity = true;
                govde.position = kalkis;
                govde.rotation = Quaternion.Euler(0f, yaw, 0f);
                oyuncu.transform.SetPositionAndRotation(
                    kalkis, Quaternion.Euler(0f, yaw, 0f));
                suzulme.enabled = true;
                var pilot = new ConstantFlightInput();
                suzulme.SetInput(pilot);

                var firlatma = oyuncu.GetComponent<FlightLaunch>();
                if (firlatma != null) firlatma.Launch();
                yield return new WaitForFixedUpdate();

                var bas = oyuncu.transform.position;
                s.bas = bas;
                float t = 0f, enYuksek = bas.y;
                var arazi = Terrain.activeTerrain;

                // 300 s TAVAN.
                //
                // 900 idi ve termik calisinca bir ucus tavana kadar
                // donebiliyordu: 21 ucus x 900 s, saatler. 300 s,
                // 12,4 m/s trim hiziyla ~3,7 km yol demek — gereken
                // 3.336 m'nin ustunde, yani basarili bir ucusu kesmez.
                while (t < 300f)
                {
                    t += Time.deltaTime;
                    var p = oyuncu.transform.position;
                    enYuksek = Mathf.Max(enYuksek, p.y);
                    if (!ellerSerbest) Pilotla(oyuncu, govde, pilot);

                    // YERE DEGDI MI: arazi kotunun 1,5 m altina inen
                    // ucus bitmistir. Carpistirici degil KOT sorulur —
                    // carpistirici sorusu, bu olcumun uc kez takildigi
                    // yer.
                    float kot = arazi != null
                        ? arazi.SampleHeight(p) + arazi.transform.position.y
                        : 0f;
                    if (p.y < Mathf.Max(kot, 0f) + 1.5f) break;
                    yield return null;
                }

                var son = oyuncu.transform.position;
                s.sure = t;
                s.enYuksek = enYuksek;
                s.kazanc = enYuksek - bas.y;
                s.yatay = Vector2.Distance(new Vector2(bas.x, bas.z),
                                           new Vector2(son.x, son.z));
                s.hedefeUzak = Vector2.Distance(
                    new Vector2(son.x, son.z),
                    new Vector2(Dogancilar.x, Dogancilar.z));
                s.vardi = s.hedefeUzak <= InisYaricapi;
                if (t >= 300f) s.hata = "sure doldu";

                // --- yurume fizigine don ---
                govde.linearVelocity = Vector3.zero;
                govde.angularVelocity = Vector3.zero;
                govde.isKinematic = true;
                govde.useGravity = false;
                suzulme.enabled = false;
                if (kapsul != null) kapsul.enabled = false;
                if (cc != null) cc.enabled = true;
                if (yurume != null) yurume.enabled = true;
                if (girdi != null) girdi.enabled = true;
                if (dizi != null) dizi.enabled = true;
                yield return null;
            }

            /// <summary>
            /// Basit pilot: hedefe yönel, tırmanış varsa dön (termikte kal),
            /// yoksa en iyi süzülme hızını tut.
            ///
            /// Kasten basit — bkz. sınıf belgesi. İnsandan kötü uçar,
            /// yani ölçüm kötümser tarafta durur.
            /// </summary>
            private static void Pilotla(GameObject oyuncu, Rigidbody govde,
                                        ConstantFlightInput pilot)
            {
                var p = oyuncu.transform.position;
                var hedefYon = new Vector3(Dogancilar.x - p.x, 0f,
                                           Dogancilar.z - p.z).normalized;

                // TIRMANIS HAVA EKSENINDE OLCULUR.
                //
                // Once `govde.linearVelocity.y > 0.15f` deniyordu ve bu
                // YER eksenli: sarmal dalistan cikarken dikey hiz
                // +7,9 m/s'ye ciktigi icin pilot ortada hic kaldirac
                // yokken "termikteyim" saniyor, 55 derece donuyor,
                // tekrar daliyordu. Ucus 8. saniyeden sonra bir
                // dal-zoom-don limit cevrimine giriyordu.
                //
                // Dogru soru: HAVA yukseliyor mu. Havanin kendi dikey
                // hizini cikarinca fugoid salinimi termik sanilmaz.
                float havaDikey = 0f;
                var alan = Object.FindAnyObjectByType<WindField>();
                if (alan != null) havaDikey = alan.Sample(p).y;
                // ...VE SONRA BU FAZLA ILERI GITTI.
                //
                // Yukaridaki duzeltme (yer ekseni yerine hava ekseni)
                // dogruydu ve amaci fugoid salinimini termik
                // sanmamakti. Ama sorulan soru yanlis oldu: **suzulen
                // bir kanat havaya gore HER ZAMAN batar** (en az batis
                // 0,94 m/s). `linearVelocity.y - havaDikey > 0,15`
                // kosulu bu yuzden neredeyse hicbir zaman dogru olmaz
                // ve pilot termigin icindeyken bile "termikte degilim"
                // der. 21 ucusun 21'inde kazanc 0 m cikmasinin sebebi
                // budur.
                //
                // Bir variometrenin sordugu sey bu degil: **hava
                // yukseliyor mu.** Pilotun karari da ona bagli.
                bool tirmaniyor = havaDikey > 0.3f;

                // Yeterince yuksekse termikte oyalanma, yola cik.
                float ground = 0f;
                var arazi = Terrain.activeTerrain;
                if (arazi != null)
                    ground = arazi.SampleHeight(p) + arazi.transform.position.y;
                float agl = p.y - ground;
                bool yeter = agl > 380f;

                // ONCE KALDIRACI ARA, SONRA HEDEFE GIT.
                //
                // Pilot ilk kareden itibaren hedefe (dogu, 101 derece)
                // yoneliyordu ve orada kaldirac YOK: yamac gunese
                // sirtini donmus ve lodosun ruzgar altinda. Olculdu —
                // en iyi kaldirac +1,87 m/s ve kuleden 160 m BATIDA.
                // Yani ucus dogru kanatla, dogru termikle, yanlis yone
                // gidiyordu: yatay mesafe 1.206 m'ye ciktiginda bile
                // kazanc 0 m kaldi.
                //
                // Pilot kaldiracin yerini BILMIYOR, ARIYOR: cevresinde
                // sekiz yone bakip havanin en cok yukseldigi yone
                // doner. Hazir cevap vermek olcumu iyimserlestirirdi;
                // aramak, gercek bir planorcunun yaptigi seydir.
                Vector3 istenen;
                if (yeter)
                {
                    istenen = hedefYon;                 // yeterince yuksek
                }
                else if (tirmaniyor)
                {
                    // Termigin icindeyiz: daire ciz, cikmayi surdur.
                    istenen = Quaternion.Euler(0f, 55f, 0f)
                              * oyuncu.transform.forward;
                }
                else
                {
                    // DONMEYE DEGIYORSA DON.
                    //
                    // Kaldiraci aramak tek basina olcumu KOTULESTIRDI:
                    // ortalama yatay 1.206 m -> 210 m, kazanc yine 0.
                    // Pilot kulenin dibinde termik kovalayip hic yol
                    // almiyordu.
                    //
                    // Sebep aritmetik: bu arazideki en iyi kaldirac
                    // +1,87 m/s, 33 derece yatista batis 2,12 m/s.
                    // Yani EN IYI termikte bile donmek net -0,25 m/s.
                    // Donus verimi kaldiracin altinda kaldigi surece
                    // termik aramak, yalnizca yol kaybetmektir.
                    //
                    // Gercek bir planorcu de boyle yapar: zayif termige
                    // durup girmez, suzulup gecer. Esik donus batisidir
                    // ve `SustainedBank_DoesNotSpiralDive` onu olcuyor
                    // — dusunce oradan gelir, buraya elle yazilmaz.
                    istenen = hedefYon;
                    var enIyi = KaldiracaDogru(p, hedefYon, out float kaldirac);
                    if (kaldirac > DonusBatisi()) istenen = enIyi;
                }

                float aci = Vector3.SignedAngle(oyuncu.transform.forward,
                                                istenen, Vector3.up);
                // YAVASLAMAK, YALNIZ DEGMEYE DEGER KALDIRACTA.
                //
                // `tirmaniyor` duzeltildikten sonra (hava yukseliyor
                // mu) bu satir en ufak yukselmede burnu kaldiriyor ve
                // aygiti yavaslatiyordu: olculdu, ortalama mesafe
                // 1.437'den 717 m'ye dustu. Zayif kaldiracta yavaslamak
                // yalnizca yol kaybettirir — gercek bir planorcu de
                // zayif termige durup girmez.
                //
                // Esik donusunkiyle ayni olmali: ikisi de ayni soruyu
                // soruyor — "bu kaldirac, onun icin odeyecegim
                // batisi karsiliyor mu".
                bool degerli = tirmaniyor && havaDikey > DonusBatisi();
                pilot.Pitch = degerli && !yeter ? 0.10f : 0f;

                // YATIS KIRPILIR — SARMAL DALIS BURADAN BASLIYORDU.
                //
                // `Clamp(aci / 45f, -1, 1)` idi. Kalkis yonleri
                // -40..+40, hedef 101 derecede: baslangic yon hatasi
                // 62-142 derece, yani roll ILK KAREDE +-1'e doyuyor ve
                // 55 derece yatis komut ediliyor.
                //
                // Olculdu: 22 derece yatista batis teorinin %14
                // ustunde (kabul edilebilir), 33 derecede %78 ustunde,
                // 55 derecede 4,2 KATI. Yani pilot her ucusu ilk
                // karede sarmal dalisa sokuyordu.
                //
                // 120'ye bolmek ve 0,4'te kirpmak ~22 dereceyle
                // sinirlar. Donus yaricapi 39 m; en iyi kaldirac
                // bandi ~160 m genisliginde, rahat sigar.
                pilot.Roll = Mathf.Clamp(aci / 120f, -0.4f, 0.4f);
            }

            /// <summary>
            /// Çevredeki en iyi kaldıracın yönü.
            ///
            /// Sekiz yönde, 120 m ötede, aynı kotta havanın dikey
            /// hızına bakılır. Hiçbiri hedeften iyi değilse hedefe
            /// gidilir — kaldıraç aramak, hedefi unutmak değil.
            ///
            /// 120 m: en iyi kaldıraç kuleden 160 m ötede ve bandı
            /// ~160 m geniş; daha kısa bir kol bandın içinde kalır ve
            /// hiçbir fark göremez, daha uzunu suyun üstüne taşar.
            /// </summary>
            private static Vector3 KaldiracaDogru(Vector3 p, Vector3 hedefYon,
                                                  out float kaldirac)
            {
                kaldirac = 0f;
                var alan = Object.FindAnyObjectByType<WindField>();
                if (alan == null) return hedefYon;

                // ARAMA YARICAPI 120 m IDI VE KALDIRAC 480 m OTEDEYDI.
                //
                // Alan olculdu: en guclu dikey ruzgar **1,99 m/s** ve
                // kulenin **480 m batisinda**. Pilot 120 m'lik bir
                // cemberde ariyordu, yani var olan tek kaldiraci
                // yapisal olarak goremiyordu — 21 ucusun 21'inde
                // "kazanc 0 m" cikmasinin son sebebi bu.
                //
                // Uc yaricap: yakin (120 m) donulecek termik, orta
                // (300 m) suzulerek gidilecek, uzak (550 m) yolu
                // degistirmeye deger olan. Uzagi secmek bir maliyet —
                // 550 m gitmek 48 m irtifa yer — ama tavan 620 m ve
                // hedef 3.336 m: yukselmeden gidilemiyor.
                var yon = hedefYon;
                kaldirac = alan.Sample(p + hedefYon * 120f).y;
                // ...VE GENISLETMEK OLCUMU KOTULESTIRDI: 720 -> 662 m.
                //
                // Sebep ucuncu bir sayida ve onu ancak alan olcumu
                // gosterdi: kaldiracin en guclu noktasi **80 m
                // irtifada**, yani yamaci siyiran bir kotta. Kuleden
                // (100 m) oraya suzulmek 42 m yiyor ve varista irtifa
                // 58 m kaliyor; oradan +0,77 m/s ile 430 m'ye cikmak
                // **8 dakika** suruyor. Denemenin sure tavani 300 s.
                //
                // Yani kapiyi tutan sey artik pilot degil: bu rotanin
                // termikle gecilmesi SEKIZ DAKIKALIK bir suzulus
                // demek ve bu bir TASARIM karari (ADR 0084) — kaldiraci
                // guclendirmek, hedefi yaklastirmak, ya da uzun
                // suzulusu kabul etmek.
                //
                // Genis arama kayda geciyor ama kapali: bugunku sure
                // butcesinde yalnizca yol kaybettiriyor.
                float[] yaricaplar = { 120f };
                for (int i = 0; i < 16; i++)
                {
                    float a = i * 22.5f * Mathf.Deg2Rad;
                    var d = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
                    foreach (float r in yaricaplar)
                    {
                        float v = alan.Sample(p + d * r).y;
                        // Uzagi secmenin bedeli var: oraya suzulmek
                        // irtifa yer. Kaldiraci mesafeyle indirimle ki
                        // pilot yakindaki iyiyi uzaktaki cok iyiye
                        // tercih edebilsin.
                        float indirimli = v - r / 550f * 0.35f;
                        if (indirimli <= kaldirac) continue;
                        kaldirac = indirimli;
                        yon = d;
                    }
                }
                return yon;
            }

            /// <summary>
            /// 33° yatışta batış (m/s) — dönmeye değer kaldıracın alt sınırı.
            ///
            /// <b>Kendi yorumunu ihlal ediyordu.</b> Belge "elle
            /// yazılmıyor olması önemli: dönüş verimi iyileşince bu
            /// eşik de düşmeli, yoksa pilot iyileşmeyi kullanmaz"
            /// diyordu — ve altında <c>const 2.12f</c> yazıyordu.
            /// Bu turda dönüş batışı gerçekten düzeldi ve eşik yerinde
            /// kaldığı için pilot düzelmeyi <b>kullanamadı</b>: en iyi
            /// kaldıraç +1,87 m/s, eski eşik 2,12 — yani hiçbir termiğe
            /// hiç girmedi.
            ///
            /// Artık modelden türüyor: en az batış açısındaki batış,
            /// yatışın yük katsayısıyla ölçeklenmiş
            /// (<c>(1/cos φ)^1.5</c> — taşıma katsayısı 1/cos ile,
            /// indüklenmiş sürükleme onun karesiyle artar).
            /// </summary>
            private static float DonusBatisi()
            {
                var t = Object.FindAnyObjectByType<GlideController>()?.tuning;
                if (t == null) return 2.12f;
                float duz = Aerodynamics.MinSinkRate(t).sink;
                const float Yatis = 33f;
                float yuk = 1f / Mathf.Cos(Yatis * Mathf.Deg2Rad);
                return duz * Mathf.Pow(yuk, 1.5f);
            }

            private void Yaz(List<Sonuc> l, WindField alan)
            {
                int varan = 0;
                float toplamYatay = 0f, toplamKazanc = 0f, toplamSure = 0f;
                foreach (var s in l)
                {
                    if (s.vardi) varan++;
                    toplamYatay += s.yatay;
                    toplamKazanc += s.kazanc;
                    toplamSure += s.sure;
                }
                int n = Mathf.Max(1, l.Count);

                float gereken = Vector2.Distance(
                    new Vector2(KuleTabani.x, KuleTabani.z),
                    new Vector2(Dogancilar.x, Dogancilar.z));
                var tepe = KuleTepesi();
                float kot = tepe.y - Dogancilar.y;

                var sb = new StringBuilder();
                sb.AppendLine("# Uçuş denemesi — kuleden Doğancılar'a");
                sb.AppendLine();
                sb.AppendLine($"Gereken yatay mesafe **{gereken:F0} m**, "
                              + $"iniş yarıçapı {InisYaricapi:F0} m.");
                sb.AppendLine($"Ölçülen şerefe kotu **{tepe.y:F1} m**, hedef "
                              + $"{Dogancilar.y:F1} m, kot farkı "
                              + $"**{kot:F1} m** (RESEARCH.md 168: ~62 m).");
                sb.AppendLine($"Kanadın en iyi süzülme oranı 11,56:1, yani "
                              + $"kaldıraçsız menzil {kot * 11.56f:F0} m. "
                              + $"Gereken oran **{gereken / Mathf.Max(1f, kot):F0}:1**. "
                              + $"Açığı kapatmak için gereken net yükselme "
                              + $"**{gereken / 11.56f - kot:F0} m** — bu uçuş "
                              + "**termikle** yapılır ya da hiç yapılmaz.");
                sb.AppendLine();
                sb.AppendLine($"Rüzgâr alanı: "
                              + (alan == null ? "**YOK**"
                                 : (alan.terrainThermal != null
                                    ? "termik BAĞLI" : "termik yok")));
                sb.AppendLine();
                sb.AppendLine("| # | kalkış | yatay (m) | süre (s) "
                              + "| en yüksek (m) | kazanç (m) | hedefe (m) "
                              + "| vardı |");
                sb.AppendLine("|---:|---|---:|---:|---:|---:|---:|:--:|");
                foreach (var s in l)
                    sb.AppendLine($"| {s.no} "
                                  + $"| ({s.bas.x:F0}, {s.bas.y:F0}, {s.bas.z:F0}) "
                                  + $"| {s.yatay:F0} | {s.sure:F1} "
                                  + $"| {s.enYuksek:F0} | {s.kazanc:F0} "
                                  + $"| {s.hedefeUzak:F0} "
                                  + $"| {(s.vardi ? "✔" : s.hata ?? "—")} |");
                sb.AppendLine();
                sb.AppendLine($"**Varan: {varan}/{l.Count} "
                              + $"(%{100f * varan / n:F0})** · "
                              + $"ortalama yatay {toplamYatay / n:F0} m · "
                              + $"ortalama kazanç {toplamKazanc / n:F0} m · "
                              + $"ortalama süre {toplamSure / n:F0} s");
                sb.AppendLine();
                sb.AppendLine("Kapı: varan ≥ %70.");

                // DENEME, DAYANDIGI KALDIRACIN VAR OLUP OLMADIGINI
                // SOYLEMELI.
                //
                // Yirmi bir ucusun yirmi birinde "kazanc 0 m" cikti ve
                // ben once pilotu sucladim: tirmanis testini duzelttim,
                // donus esigini modelden turettim — ve ortalama mesafe
                // 1.437'den 720 m'ye DUSTU, kazanc yine 0. Yani pilot
                // artik kaldirac ariyor ve bulamiyor.
                //
                // Bir olcum, girdisinin var olup olmadigini
                // raporlamiyorsa, cikti hakkinda soyledigi her sey
                // eksiktir. Alan burada dogrudan orneklenir.
                if (alan != null)
                {
                    float enCok = float.MinValue, toplam = 0f;
                    int n2 = 0; Vector3 enIyiNokta = Vector3.zero;
                    for (int gx = -8; gx <= 8; gx++)
                    for (int gz = -8; gz <= 8; gz++)
                    for (int gy = 1; gy <= 4; gy++)
                    {
                        var q = new Vector3(gx * 60f, 40f + gy * 40f, gz * 60f);
                        float w = alan.Sample(q).y;
                        toplam += w; n2++;
                        if (w > enCok) { enCok = w; enIyiNokta = q; }
                    }
                    sb.AppendLine();
                    sb.AppendLine("## Kaldıraç alanı — kule çevresi 960×960 m");
                    sb.AppendLine();
                    sb.AppendLine($"En güçlü dikey rüzgâr **{enCok:F2} m/s** "
                                  + $"@ ({enIyiNokta.x:F0}, {enIyiNokta.y:F0}, "
                                  + $"{enIyiNokta.z:F0}); ortalama "
                                  + $"{toplam / Mathf.Max(1, n2):F2} m/s, "
                                  + $"{n2} örnek.");
                    sb.AppendLine();
                    sb.AppendLine($"Dönmeye değer eşik: **{DonusBatisi():F2} m/s** "
                                  + "(en az batış × yük katsayısı).");
                    // SUSLU PARANTEZ UNUTULDU VE RAPOR YALAN SOYLEDI.
                    //
                    // Ilk kosumda bu blok parantezsizdi: `if` yalnizca
                    // bos `AppendLine()`i tutuyor, uyari satiri
                    // KOSULSUZ basiliyordu. Rapor "bu arazide termikle
                    // yukselmek mumkun degil" yazdi — oysa ayni raporun
                    // iki satir ustunde kaldirac 1,99 ve esik 1,23
                    // yaziyordu, yani donmek net +0,76 m/s.
                    //
                    // Bir olcum aracinin yanlis cumle kurmasi, olcumun
                    // kendisinden daha pahalidir: dogru sayilari
                    // toplayip yanlis sonucu yazdi.
                    if (enCok < DonusBatisi())
                    {
                        sb.AppendLine();
                        sb.AppendLine("> Alanın en güçlü noktası bile dönüş "
                                      + "batışının altında: bu arazide termikle "
                                      + "yükselmek **mümkün değil**.");
                    }
                    else
                    {
                        sb.AppendLine();
                        sb.AppendLine($"> Dönmek net **+{enCok - DonusBatisi():F2} "
                                      + "m/s** kazandırıyor — yükselmek mümkün. "
                                      + "Kaldıraç hedefin TERSİ yönde (batıda), "
                                      + "yani uçuş önce geriye gitmeli.");
                    }
                }

                Directory.CreateDirectory(Cikti);
                File.WriteAllText($"{Cikti}/ucus_denemesi.md", sb.ToString());
                Debug.Log($"[Hezarfen] Ucus denemesi: {varan}/{l.Count} vardi "
                          + $"-> {Cikti}/ucus_denemesi.md");
                if (Toplu) EditorApplication.Exit(0);
            }
        }
    }
}
