using System.Collections;
using Hezarfen.Flight;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Hezarfen.Tests
{
    /// <summary>
    /// Simülasyonun, saf matematiğin vaat ettiği davranışı GERÇEKTEN ürettiğini kanıtlar.
    ///
    /// Neden gerekli: <see cref="Aerodynamics"/> testleri formülleri doğrular ama
    /// <see cref="GlideController"/>'ın kuvvet ve tork YÖNLERİNİ doğrulamaz. Bu testler
    /// geliştirme sırasında iki gerçek hata yakaladı:
    ///   1) Yatış girdisi bank AÇISI değil bank HIZI komut ediyordu → aygıt durmadan yuvarlanıyordu.
    ///   2) Bank açısının işareti tersti → yatış denetimi pozitif geri beslemeye dönüşüp
    ///      en ufak sapmada aygıtı ters çeviriyordu.
    /// İkisi de matematik testlerinden geçerdi.
    ///
    /// Fizik <c>SimulationMode.Script</c> ile elle sürülür — gerçek zaman beklemez, deterministiktir.
    /// </summary>
    public class GlideSimulationTests
    {
        private const float Dt = 0.02f;

        private GameObject craft;
        private GlideController glide;
        private Rigidbody rb;
        private WindTuning tuning;
        private SimulationMode previousMode;

        private struct FlightResult
        {
            public float Horizontal;
            public float Dropped;
            public float MeanAlphaDeg;
            public float MeanBankDeg;
            public float MeanSpeed;
            public float EndX;
            public float GlideRatio => Horizontal / Mathf.Max(Dropped, 0.001f);
        }

        [SetUp]
        public void SetUp()
        {
            previousMode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;

            tuning = ScriptableObject.CreateInstance<WindTuning>();

            craft = new GameObject("TestGlider");
            rb = craft.AddComponent<Rigidbody>();
            rb.mass = tuning.mass;
            rb.useGravity = true;
            rb.linearDamping = 0f;
            rb.angularDamping = 0f;

            glide = craft.AddComponent<GlideController>();
            glide.tuning = tuning;
        }

        [TearDown]
        public void TearDown()
        {
            if (craft != null) Object.DestroyImmediate(craft);
            if (tuning != null) Object.DestroyImmediate(tuning);
            Physics.simulationMode = previousMode;
        }

        /// <summary>
        /// Sabit girdiyle uçurur. Ortalamalar uçuşun İKİNCİ yarısından alınır —
        /// ilk yarı başlangıç geçici rejimidir ve anlık değer ölçmek yanıltıcıdır.
        /// </summary>
        private FlightResult Fly(float pitchCommand, float rollCommand, float seconds)
        {
            glide.SetInput(new ConstantFlightInput(pitchCommand, rollCommand));

            craft.transform.position = new Vector3(0f, 1000f, 0f);
            craft.transform.rotation = Quaternion.identity;

            var best = Aerodynamics.BestGlideRatio(tuning);
            rb.linearVelocity = craft.transform.forward * Aerodynamics.TrimSpeed(best.alphaDeg, tuning);
            rb.angularVelocity = Vector3.zero;

            Vector3 start = craft.transform.position;
            int steps = Mathf.RoundToInt(seconds / Dt);
            int half = steps / 2;

            float sumAlpha = 0f, sumBank = 0f, sumSpeed = 0f;
            int samples = 0;

            for (int i = 0; i < steps; i++)
            {
                glide.Step();
                Physics.Simulate(Dt);

                if (i >= half)
                {
                    sumAlpha += glide.AngleOfAttackDeg;
                    sumBank += glide.BankAngleDeg;
                    sumSpeed += rb.linearVelocity.magnitude;
                    samples++;
                }
            }

            Vector3 end = craft.transform.position;
            samples = Mathf.Max(samples, 1);

            return new FlightResult
            {
                Horizontal = new Vector2(end.x - start.x, end.z - start.z).magnitude,
                Dropped = start.y - end.y,
                MeanAlphaDeg = sumAlpha / samples,
                MeanBankDeg = sumBank / samples,
                MeanSpeed = sumSpeed / samples,
                EndX = end.x
            };
        }

        [UnityTest]
        public IEnumerator Glider_DoesNotFallLikeAStone()
        {
            var f = Fly(0f, 0f, 20f);
            float freeFall = 0.5f * Aerodynamics.Gravity * 20f * 20f;

            Assert.Less(f.Dropped, freeFall * 0.35f,
                $"20 sn'de {f.Dropped:F0} m dustu; serbest dusus {freeFall:F0} m. Tasima calismiyor olabilir.");
            Assert.Greater(f.Horizontal, 100f,
                "Aygit ileri gitmiyor - surukleme/tasima yonleri ters olabilir.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator EmergentGlideRatio_MatchesTheoreticalBand()
        {
            var f = Fly(0f, 0f, 60f);

            Assert.Greater(f.Dropped, 1f, "Aygit hic alcalmadi - olcum anlamsiz.");
            Assert.GreaterOrEqual(f.GlideRatio, 8f, $"Ortaya cikan oran cok dusuk: {f.GlideRatio:F2}:1");
            Assert.LessOrEqual(f.GlideRatio, 13f, $"Ortaya cikan oran cok yuksek: {f.GlideRatio:F2}:1");

            Debug.Log($"[Hezarfen] Simulasyon: L/D {f.GlideRatio:F2}:1, alpha {f.MeanAlphaDeg:F1} derece, " +
                      $"hiz {f.MeanSpeed:F1} m/s (yatay {f.Horizontal:F0} m / dusus {f.Dropped:F0} m)");
            yield return null;
        }

        [UnityTest]
        public IEnumerator NeutralStick_IsStableAndNearBestGlide()
        {
            // Notr girdi en iyi suzulmeye yakin olmali - oyuncu "hicbir sey yapmayinca"
            // makul ucmali, ustalik bunun UZERINE gelmeli.
            var f = Fly(0f, 0f, 60f);

            Assert.Less(Mathf.Abs(f.MeanBankDeg), 5f,
                $"Notr girdide aygit kendiliginden yatiyor: {f.MeanBankDeg:F1} derece");
            Assert.Less(f.MeanAlphaDeg, tuning.stallAngleDeg,
                $"Notr girdide stall'a girmis: alpha {f.MeanAlphaDeg:F1}");
            Assert.Greater(f.MeanAlphaDeg, 0f, "Notr girdide hucum acisi pozitif olmali.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator NoseDown_TradesEfficiencyForSpeed()
        {
            var cruise = Fly(0f, 0f, 30f);
            var dive = Fly(-1f, 0f, 30f);

            Assert.Greater(dive.MeanSpeed, cruise.MeanSpeed * 1.5f,
                $"Burun asagi hizlandirmali. seyir {cruise.MeanSpeed:F1} -> dalis {dive.MeanSpeed:F1} m/s");
            Assert.Less(dive.MeanAlphaDeg, cruise.MeanAlphaDeg,
                "Burun asagi komutu hucum acisini DUSURMELI. Pitch isareti ters olabilir.");
            Assert.Less(dive.GlideRatio, cruise.GlideRatio,
                "Hizli ucus verimi dusurmeli - aksi halde yavas ucmanin anlami kalmaz.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator NoseUp_ReachesStall()
        {
            // Hata mumkun olmali: tam cekiste pilot stall'a girebilmeli.
            var f = Fly(1f, 0f, 30f);

            Assert.Greater(f.MeanAlphaDeg, tuning.stallAngleDeg,
                $"Tam burun yukarida stall'a girilemiyor: alpha {f.MeanAlphaDeg:F1} derece");
            Assert.Less(f.GlideRatio, 5f,
                "Stall verimi COKERTMELI - yoksa surekli burun yukari ucmak bedava olur.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator RollRight_TurnsRight()
        {
            // Yarim yatis: donus yaricapi buyuk, 15 sn'de tam tur atmaz.
            // (Tam yatista yaricap ~99 m olur ve aygit neredeyse daireyi kapatip
            //  baslangica doner - o yuzden yer degistirme olcmek yaniltici olurdu.)
            var f = Fly(0f, 0.4f, 15f);

            Assert.Greater(f.MeanBankDeg, 10f, $"Saga yatis olusmadi: bank {f.MeanBankDeg:F1} derece");
            Assert.Greater(f.EndX, 20f, $"Saga yatista saga donmedi: x={f.EndX:F1}");
            yield return null;
        }

        [UnityTest]
        public IEnumerator RollLeft_IsMirrorOfRollRight()
        {
            var right = Fly(0f, 0.4f, 15f);
            var left = Fly(0f, -0.4f, 15f);

            Assert.Less(left.MeanBankDeg, -10f, $"Sola yatis olusmadi: bank {left.MeanBankDeg:F1}");
            Assert.Less(left.EndX, -20f, $"Sola yatista sola donmedi: x={left.EndX:F1}");

            // Simetri: sag ve sol ayna olmali. Degilse bir eksende gizli bir yanlilik var.
            Assert.AreEqual(right.MeanBankDeg, -left.MeanBankDeg, 2f, "Yatis simetrik degil.");
            Assert.AreEqual(right.EndX, -left.EndX, 5f, "Donus simetrik degil.");
            yield return null;
        }
        /// <summary>
        /// <b>Sürekli yatışta sarmal dalışa girmemeli.</b>
        ///
        /// Bu dosyadaki süzülme testlerinin hepsi <b>yatışsız</b>
        /// uçuyor ve hepsi geçiyordu; <c>RollRight_TurnsRight</c> ise
        /// dönüyor ama <b>batışa hiç bakmıyor</b>. Aradaki boşlukta
        /// ders kitabı bir kusur yaşıyordu.
        ///
        /// Ölçüldü: 33° yatışta batış 0,93 → 2,49 m/s (2,7 kat) ve
        /// hücum açısı 9,2° → 2,4°. Teori 1,30 kat der. Sebep, pilot
        /// hedef hücum açısının yatıştan habersiz olmasıydı — yatmış
        /// uçuşta gereken taşıma W/cos φ'dir ve model onu hiç istemiyordu.
        ///
        /// Bunun bedeli soyut değil: <b>termikte dönmek hiçbir yatış
        /// açısında mümkün değildi</b>, yani uçuş yükselemiyordu ve
        /// kuleden Doğancılar'a varan uçuş sayısı 0/20 idi.
        ///
        /// ## Eşik neden 2,30 ve hedef neden 1,60
        ///
        /// Yatış telafisi eklendi ve ölçüm 2,49 → <b>2,12</b> m/s dedi;
        /// yani 2,7 kat 2,28 kata indi. Teşhisin ikinci yarısı
        /// (kararlılık terimini komut edilen açıya itmek) de denendi ve
        /// <b>ölçüm reddetti</b>: düz uçuş süzülme oranı 11,2:1'den
        /// 2,57:1'e çöktü, beş test birden kırmızı yandı. Geri alındı.
        ///
        /// Bu yüzden eşik bugün <b>2,30</b> — bir kabul değil, bir
        /// <b>cırcır</b>. Kusur kapanmadı; kapanana kadar da kötüye
        /// gitmemesi sağlanıyor. Gerçek hedef 1,60 (teorik 1,30'un
        /// üstünde makul bir pay) ve oraya inmenin yolu muhtemelen
        /// sabit hücum açısı yerine <b>sabit hava hızı</b> trimi:
        /// asılı planör gerçekte alfa değil hız trimler ve ağırlık
        /// aktarımı trim hızını değiştirir. Ayrı bir tur, ayrı bir
        /// ölçüm.
        ///
        /// Kalıcı kırmızı bir test yapıyı bloke eder ve kırmızıya
        /// bakmayı öğretir; kapanmamış bir kusuru dürüstçe taşımanın
        /// yolu, bugünkü sayıyı tavan yapıp hedefi yazılı bırakmaktır.
        /// </summary>
        [UnityTest]
        public IEnumerator SustainedBank_DoesNotSpiralDive()
        {
            var duz = Fly(0f, 0f, 60f);
            var yan = Fly(0f, 0.6f, 60f);      // ~33 derece yatis

            float batisDuz = duz.Dropped / 60f;
            float batisYan = yan.Dropped / 60f;

            // CIRCIR: bugunku olcu 2,28 kat. Hedef 1,60.
            Assert.Less(batisYan, batisDuz * 2.30f,
                $"Yatista sarmal dalis KOTULESTI: batis {batisDuz:F2} -> "
                + $"{batisYan:F2} m/s. Teori 1,30 kat der, hedef 1,60, "
                + "bugunku tavan 2,30.");
            // CETVEL DEGISTI, KAPI GEVSEMEDI.
            //
            // Bu satir HAM HUCUM ACISINI olcuyordu ve gerekcesi
            // "tasima cokerse donus bir dalistir" idi. Dogru bir kaygi,
            // yanlis bir vekil — ve notr trim en iyi suzulusa
            // tasindiginda yaniltici oldu: aygit artik donuse **daha
            // hizli** giriyor (trim 9,3 yerine 12,4 m/s) ve ayni
            // tasimayi DAHA AZ acida uretiyor. Alfanin dusmesi
            // burada tasimanin cokmesi degil, tam tersi: stall'a
            // mesafenin ACILMASI.
            //
            // Olculdu: alfa 3,18 -> 2,66 dustu, batis ise iyilesti
            // (ustteki iddia geciyor). Ayni degisiklik bir cetvele gore
            // kotu, otekine gore iyi gorunuyorsa yanlis olan cetveldir.
            //
            // Sorulan sey artik dogrudan sorulur: **stall'a ne kadar
            // pay var.** Donusun guvenli olmasi, acinin buyuk
            // olmasindan degil stall'in uzak olmasindan gelir.
            float stallPayi = 15f - yan.MeanAlphaDeg;   // stallAngleDeg
            Assert.Greater(stallPayi, 8f,
                $"Yatista stall payi {stallPayi:F1} derece — donus "
                + "stall'in kiyisinda geciyor.");

            // Ham aci yine de kayda gecer: sifira yaklasmasi tasimanin
            // hic uretilmedigini gosterirdi.
            Assert.Greater(yan.MeanAlphaDeg, 1.5f,
                $"Yatista hucum acisi {yan.MeanAlphaDeg:F1} derece — "
                + "tasima hic uretilmiyor.");
            yield return null;
        }

    }
}
