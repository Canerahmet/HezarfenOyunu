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
        private RenderTexture rt;

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

            if (rt != null) { targetCamera.targetTexture = null; rt.Release(); Destroy(rt); }
            rt = new RenderTexture(step.resolution.x, step.resolution.y, 24, RenderTextureFormat.ARGB32);
            rt.Create();
            targetCamera.targetTexture = rt;
        }

        private void Update()
        {
            if (Finished || stepIndex >= steps.Count) return;

            frameInStep++;
            if (frameInStep <= warmupFrames) return;

            samples.Add(Time.unscaledDeltaTime * 1000f);
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
                $"encotu {worst,6:F2} ms";
#if UNITY_EDITOR
            line += $"  | cizim {Median(drawSamples),5}  SRPB {Median(srpSamples),5}" +
                    $"  setPass {Median(setPassSamples),4}  ucgen {MedianF(triSamples),6:F2} M";
#endif
            Report.Add(line);

            AdvanceStep();
        }

        private void Cleanup()
        {
            if (targetCamera != null) targetCamera.targetTexture = null;
            if (rt != null) { rt.Release(); Destroy(rt); rt = null; }
        }

        private void OnDisable() => Cleanup();
    }
}
