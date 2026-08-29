using System.Collections.Generic;
using UnityEngine;

namespace Hezarfen.Diagnostics
{
    /// <summary>
    /// Kare süresi ölçer — HDRP/URP karar kapısının (plan Faz 1 kabul) kanıt üreticisi.
    ///
    /// Neden ortalama FPS değil: ortalama, takılmaları gizler. Oyuncu ortalamayı değil
    /// **en kötü kareleri** hisseder. Bu yüzden medyan ve %95'lik dilim ayrı raporlanır;
    /// hedefi belirleyen p95'tir.
    ///
    /// Çözünürlük kameranın hedef dokusuyla SABİTLENİR. Editor'ün Game view boyutuna
    /// güvenmek, ölçümü pencere boyutuna bağlı ve tekrarlanamaz yapardı.
    ///
    /// Ölçüm Editor Play modunda yapılırsa Editor ek yükü işin içindedir; bu ölçümü
    /// **kötümser** yapar — yani "geçti" diyorsa gerçek build de geçer.
    /// </summary>
    [AddComponentMenu("Hezarfen/Frame Time Probe")]
    public class FrameTimeProbe : MonoBehaviour
    {
        [System.Serializable]
        public class Step
        {
            public string label = "";
            public Vector2Int resolution = new Vector2Int(1920, 1080);

            [Tooltip("Etkinleştirilecek yapı grubu sayısı (yerleşim yükü).")]
            public int activeGroups;

            [Tooltip("Kamerayı bu adım için taşı. Kapalıysa sahnedeki poz korunur.")]
            public bool overrideCamera;
            public Vector3 cameraPosition;
            public Vector3 cameraEuler;

            /// <summary>
            /// Açıksa kamera, <b>örneklenen pencere boyunca tam 360°</b>
            /// döner.
            ///
            /// <b>Neden gerekli:</b> plan Faz 4'ün ölçütü *"kule tepesinden
            /// 360° bakışta FPS hedefi tutuyor"* diyor — 360°, tek kadraj
            /// değil. Sabit bakış şehrin en ucuz yönünü seçmiş olabilir;
            /// dönen kamera <b>en pahalı</b> yönü de ölçüme sokar.
            ///
            /// <b>Neden saniyede derece DEĞİL:</b> ilk yazımda öyleydi ve
            /// ölçüm tekrarlanamaz çıktı — iki koşumda aynı adımın çizim
            /// çağrısı 28 652 ve 13 858 oldu. Sebep: dönüş saate bağlıydı,
            /// örnek sayısı kareye. Kare hızı değişince taranan yay da
            /// değişiyordu (360° değil ~216°) ve başlangıç açısı ısınmanın
            /// ne kadar sürdüğüne göre kayıyordu. Yani iki koşum şehrin iki
            /// farklı dilimini ölçüp aynı sayıymış gibi karşılaştırılıyordu.
            ///
            /// Şimdi açı <b>örnek ilerlemesinden</b> türüyor: kaçıncı
            /// örnekteysek yay o kadar dönmüş oluyor. Kare hızı ne olursa
            /// olsun pencere tam bir tur, her koşumda aynı tur.
            /// </summary>
            public bool yawSweep360;
        }

        [Tooltip("Ölçülecek yapılandırmalar, sırayla.")]
        public List<Step> steps = new List<Step>();

        [Tooltip("Yük değiştikten sonra atlanacak kare (ısınma, shader derleme, LOD oturma).")]
        public int warmupFrames = 150;

        [Tooltip("Her adımda toplanacak örnek sayısı.")]
        public int sampleFrames = 300;

        [Tooltip("Yapı grupları — probe bunları kademeli açar.")]
        public List<GameObject> loadGroups = new List<GameObject>();

        [Tooltip("Ölçüm kamerası. Boşsa Camera.main.")]
        public Camera targetCamera;

        // --- sonuclar ---
        public bool Finished { get; private set; }
        public List<string> Report { get; } = new List<string>();

        private int stepIndex = -1;
        private int frameInStep;
        private readonly List<float> samples = new List<float>();

        /// <summary>
        /// Örnekler <b>zaman sırasında</b> — sürüklenme için.
        /// <see cref="samples"/> yüzdelik hesabı için sıralanıyor ve sıra
        /// bozuluyor; "zamanla kötüleşti mi" sorusu ise sırayı gerektirir.
        /// </summary>
        private readonly List<float> zamanSirali = new List<float>();
        private RenderTexture rt;
        private Vector3 sweepBaseEuler;

        /// <summary>
        /// Tam turda kare süresini <b>yöne göre</b> ayıran kovalar (12 × 30°).
        ///
        /// <b>Neden gerekli:</b> tur adımının tek bir p95'i, örnekleme
        /// penceresi değişince 15,6 ms'den 17,9 ms'ye kayıyordu — aynı
        /// sahne, aynı tur. Tek sayı "nerede pahalı" sorusuna cevap
        /// vermiyor ve bu yüzden geçti/kaldı demeye de yetmiyor.
        ///
        /// Yöne göre ayırmak soruyu ölçülebilir yapar: şehre bakınca mı
        /// pahalı, denize bakınca mı ucuz. Bir turda p95'i tek başına
        /// belirleyen sektör varsa, iş orada.
        /// </summary>
        private const int YonKova = 12;
        private readonly List<float>[] yonOrnekleri = new List<float>[YonKova];

#if UNITY_EDITOR
        // Cizim cagrisi sayimi YALNIZCA Editor'de okunabilir (UnityStats).
        //
        // Neden gerekli: kare suresi tek basina "atlas gerekli mi" sorusunu
        // cevaplamaz. Ev basina 6 malzeme, ev basina 6 alt-mesh demek; bunun
        // gercekte kac SetPass'e dondugunu SRP Batcher belirler ve bu tahmin
        // edilemez, olculur. Atlas karari bu sayilara dayanacak (ADR 0014 §8).
        // `srpBatcherDrawCalls` kritik olan: SRP Batcher'in KAPATTIGI cizim
        // cagrilari. Toplam cizim cagrisi yuksek olsa bile buyuk kismi SRP
        // Batcher'a dusuyorsa CPU maliyeti sanildigi kadar buyumez — atlas
        // kararini ayiran sey tam olarak bu oran.
        private readonly List<int> srpSamples = new List<int>();
        private readonly List<int> setPassSamples = new List<int>();
        private readonly List<int> drawSamples = new List<int>();
        private readonly List<float> triSamples = new List<float>();

        private static int Median(List<int> v)
        {
            if (v.Count == 0) return -1;
            v.Sort();
            return v[v.Count / 2];
        }

        private static float MedianF(List<float> v)
        {
            if (v.Count == 0) return -1f;
            v.Sort();
            return v[v.Count / 2];
        }
#endif

        private void Start()
        {
            if (targetCamera == null) targetCamera = Camera.main;
            Application.targetFrameRate = -1;
            QualitySettings.vSyncCount = 0;      // vsync olcumu 60'a kilitlerdi
            AdvanceStep();
        }

        private void AdvanceStep()
        {
            stepIndex++;
            frameInStep = 0;
            samples.Clear();
            zamanSirali.Clear();
            for (int i = 0; i < YonKova; i++)
            {
                if (yonOrnekleri[i] == null) yonOrnekleri[i] = new List<float>();
                else yonOrnekleri[i].Clear();
            }
#if UNITY_EDITOR
            srpSamples.Clear();
            setPassSamples.Clear();
            drawSamples.Clear();
            triSamples.Clear();
#endif

            if (stepIndex >= steps.Count)
            {
                Cleanup();
                Finished = true;
                Debug.Log("[Hezarfen] FrameTimeProbe bitti:\n" + string.Join("\n", Report));
                return;
            }

            var step = steps[stepIndex];

            for (int i = 0; i < loadGroups.Count; i++)
                if (loadGroups[i] != null) loadGroups[i].SetActive(i < step.activeGroups);

            // Bakis noktasi olcumun PARCASIDIR. Kule tepesinden bakisla sokak
            // arasindaki fark, LOD dagilimini ve dolayisiyla malzeme basina
            // cizim cagrisini tamamen degistirir: uzaktan her ev LOD2 (2
            // alt-mesh), sokakta yakin evler LOD0 (6 alt-mesh). Tek kadrajla
            // olcup "atlas gerekmiyor" demek, sorunun yarisini olcmek olurdu.
            if (step.overrideCamera && targetCamera != null)
            {
                targetCamera.transform.position = step.cameraPosition;
                targetCamera.transform.rotation = Quaternion.Euler(step.cameraEuler);
            }

            // Taramanin TABAN acisi: adim kamerayi tasidiysa yeni poz,
            // tasimadiysa sahnedeki mevcut poz. Bir onceki adimin
            // birakttigi acidan devam etmek, adimlari karsilastirilamaz
            // yapardi.
            if (targetCamera != null)
                sweepBaseEuler = targetCamera.transform.rotation.eulerAngles;

            if (rt != null) { targetCamera.targetTexture = null; rt.Release(); Destroy(rt); }
            rt = new RenderTexture(step.resolution.x, step.resolution.y, 24, RenderTextureFormat.ARGB32);
            rt.Create();
            targetCamera.targetTexture = rt;
        }

        private void Update()
        {
            if (Finished || stepIndex >= steps.Count) return;

            frameInStep++;
            var suAnki = steps[stepIndex];

            // ACIYI ORNEK ILERLEMESINDEN KUR — saatten degil.
            //
            // Isinma boyunca kamera BASLANGIC acisinda durur; boylece her
            // kosum ayni yerden baslar. Ornekleme baslayinca yay, kacinci
            // ornekte oldugumuza gore aciliyor: pencere tam 360 derece,
            // kare hizi ne olursa olsun.
            if (suAnki.yawSweep360 && targetCamera != null)
            {
                float ilerleme = frameInStep <= warmupFrames
                    ? 0f
                    : Mathf.Clamp01(samples.Count / (float)Mathf.Max(1, sampleFrames));
                targetCamera.transform.rotation =
                    Quaternion.Euler(sweepBaseEuler + new Vector3(0f, 360f * ilerleme, 0f));
            }

            if (frameInStep <= warmupFrames) return;

            float suAnkiMs = Time.unscaledDeltaTime * 1000f;
            samples.Add(suAnkiMs);
            zamanSirali.Add(suAnkiMs);
            if (suAnki.yawSweep360 && targetCamera != null)
            {
                float aci = Mathf.Repeat(
                    targetCamera.transform.rotation.eulerAngles.y
                    - sweepBaseEuler.y, 360f);
                yonOrnekleri[Mathf.Clamp((int)(aci / (360f / YonKova)),
                                         0, YonKova - 1)].Add(suAnkiMs);
            }
#if UNITY_EDITOR
            srpSamples.Add(UnityEditor.UnityStats.srpBatcherDrawCalls);
            setPassSamples.Add(UnityEditor.UnityStats.setPassCalls);
            drawSamples.Add(UnityEditor.UnityStats.drawCalls);
            triSamples.Add(UnityEditor.UnityStats.triangles / 1e6f);
#endif
            if (samples.Count < sampleFrames) return;

            samples.Sort();
            float median = samples[samples.Count / 2];
            float p95 = samples[Mathf.Min(samples.Count - 1, (int)(samples.Count * 0.95f))];
            float worst = samples[samples.Count - 1];

            var step = steps[stepIndex];
            string line =
                $"{step.label,-26} {step.resolution.x}x{step.resolution.y}  " +
                $"medyan {median,6:F2} ms ({1000f / median,6:F1} fps)  " +
                $"p95 {p95,6:F2} ms ({1000f / p95,6:F1} fps)  " +
                $"encotu {worst,6:F2} ms" +
                (step.yawSweep360 ? "  [360 tur]" : "");
#if UNITY_EDITOR
            line += $"  | cizim {Median(drawSamples),5}  SRPB {Median(srpSamples),5}" +
                    $"  setPass {Median(setPassSamples),4}  ucgen {MedianF(triSamples),6:F2} M";
#endif
            Report.Add(line);

            // SURUKLENME: ilk ucte bir ile son ucte bir.
            //
            // Uzun oturumun sorusu "hizli mi" degil, "hizli KALIYOR mu".
            // Bellek buyumesi, havuz sizintisi, isinma — hepsi zamanla
            // agirlasir ve tek bir medyanin altinda gorunmez. Ornekleri
            // ikiye bolup karsilastirmak bunu tek sayiyla soyluyor.
            if (samples.Count >= 30)
            {
                // `samples` yukarida SIRALANDI; suruklenme icin SIRA
                // gerekli, o yuzden ayri bir kopyadan olculur.
                int ucte = zamanSirali.Count / 3;
                float ilk = OrtaDeger(zamanSirali, 0, ucte);
                float son = OrtaDeger(zamanSirali, zamanSirali.Count - ucte,
                                      ucte);
                Report.Add($"        suruklenme: ilk {ilk:F2} ms -> son "
                           + $"{son:F2} ms  ({(son - ilk):+0.00;-0.00} ms)");
            }

            // YON DOKUMU: turun neresi pahali.
            if (step.yawSweep360)
            {
                var yon = new System.Text.StringBuilder("        yon (medyan ms): ");
                for (int i = 0; i < YonKova; i++)
                {
                    var v = yonOrnekleri[i];
                    if (v == null || v.Count == 0) { yon.Append("  --- "); continue; }
                    v.Sort();
                    yon.Append($"{i * (360 / YonKova),3}d:{v[v.Count / 2],5:F1} ");
                }
                Report.Add(yon.ToString());
            }

            AdvanceStep();
        }

        /// <summary>Bir dilimin ortanca değeri (kopya alır, sırayı bozmaz).</summary>
        private static float OrtaDeger(List<float> v, int bas, int adet)
        {
            if (adet <= 0) return 0f;
            var dilim = v.GetRange(bas, Mathf.Min(adet, v.Count - bas));
            dilim.Sort();
            return dilim[dilim.Count / 2];
        }

        private void Cleanup()
        {
            if (targetCamera != null) targetCamera.targetTexture = null;
            if (rt != null) { rt.Release(); Destroy(rt); rt = null; }
        }

        private void OnDisable() => Cleanup();
    }
}
