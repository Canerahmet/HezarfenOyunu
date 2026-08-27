using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace Hezarfen.Streaming
{
    /// <summary>
    /// Bölge yayını (streaming) — plan Faz 1 madde 6:
    /// *"semtler arası geçişte yükleme ekranı YOK."*
    ///
    /// Karar mantığı burada DEĞİL, <see cref="DistrictStreamingPlan"/>de yaşar
    /// (orada test edilebilir). Bu bileşen yalnızca kararı Addressables çağrılarına
    /// çevirir ve tutamaçları (handle) sahiplenir.
    ///
    /// **Yükleme ekransız geçişin şartı** yüklemenin kareyi bloke etmemesidir.
    /// İki önlem var: (1) Addressables zaten eşzamansızdır, (2) aynı anda en fazla
    /// <see cref="DistrictStreamingPlan.MaxConcurrentLoads"/> yükleme uçuşta tutulur —
    /// üç semt sınırının kesiştiği yerde üç sahne birden çözülürse kare düşer.
    ///
    /// `activateOnLoad: false` bilinçlidir: sahne arka planda çözülür, etkinleştirme
    /// ayrı bir kareye bırakılır. Yoksa yükleme biter bitmez tüm nesnelerin Awake'i
    /// tek karede koşar ve tam da gizlemeye çalıştığımız takılma orada oluşur.
    /// </summary>
    [AddComponentMenu("Hezarfen/District Streamer")]
    public class DistrictStreamer : MonoBehaviour
    {
        [Tooltip("Yayının merkezi — oyuncu ya da kamera. Boşsa MainCamera denenir.")]
        public Transform viewer;

        public DistrictRegistry registry;

        [Tooltip("Değerlendirme sıklığı (sn). Her kare değerlendirmek gereksiz: " +
                 "eşikler yüzlerce metre, oyuncu bir karede o kadar yol almaz.")]
        public float evaluateInterval = 0.25f;

        [Tooltip("Kapalıyken hiçbir sahne yüklenmez — ölçüm ve graybox sahneleri için.")]
        public bool streamingEnabled = true;

        [Tooltip("Konsola her yükleme/boşaltmayı yaz.")]
        public bool verbose = true;

        private readonly Dictionary<string, AsyncOperationHandle<SceneInstance>> _loaded =
            new Dictionary<string, AsyncOperationHandle<SceneInstance>>();
        private readonly HashSet<string> _resident = new HashSet<string>();
        private readonly HashSet<string> _inFlight = new HashSet<string>();

        // Katalogda olmayan adresi her degerlendirmede yeniden denemek, konsolu
        // saniyede dortlerce hata ile doldurur. Bir kez dener, bir kez soyleriz.
        private readonly HashSet<string> _unavailable = new HashSet<string>();

        private readonly List<DistrictDef> _toLoad = new List<DistrictDef>();
        private readonly List<DistrictDef> _toUnload = new List<DistrictDef>();
        private float _nextEvaluate;

        /// <summary>
        /// Yüklü ya da yüklenmekte olan bölge kimlikleri — tanılama ve testler için.
        /// "Yerleşik" niyeti ifade eder: bir kimlik burada görünürken sahnesi henüz
        /// çözülüyor ya da etkinleşiyor olabilir.
        /// </summary>
        public IEnumerable<string> ResidentDistricts => _resident;

        /// <summary>
        /// Şu an uçuşta olan Addressables yükleme sayısı.
        ///
        /// <see cref="ResidentDistricts"/> içinde sahnesi henüz `isLoaded` olmayan
        /// kimlikleri saymak bunun yerine geçmez: `activateOnLoad: false` yüzünden
        /// bir sahne çözülmüş ama henüz etkinleşmemiş olabilir. Tavan **eşzamanlı
        /// yükleme** tavanıdır; etkinleşme bekleyeni ona saymak yanlış ölçüdür.
        /// </summary>
        public int LoadsInFlight => _inFlight.Count;

        private void Awake()
        {
            if (viewer == null && Camera.main != null) viewer = Camera.main.transform;
        }

        private void Update()
        {
            if (!streamingEnabled || registry == null || viewer == null) return;
            if (Time.unscaledTime < _nextEvaluate) return;
            _nextEvaluate = Time.unscaledTime + evaluateInterval;

            DistrictStreamingPlan.Evaluate(registry.districts, _resident,
                                           viewer.position, _toLoad, _toUnload);

            // Bosaltma ONCE: yukleme yavas, bosaltma ucuz. Once yer acmak,
            // iki semtin sinirinda gereksiz bellek tepesi olusmasini engeller.
            for (int i = 0; i < _toUnload.Count; i++) Unload(_toUnload[i]);

            for (int i = 0; i < _toLoad.Count; i++)
            {
                if (_inFlight.Count >= DistrictStreamingPlan.MaxConcurrentLoads) break;
                Load(_toLoad[i]);
            }
        }

        private void Load(DistrictDef d)
        {
            if (string.IsNullOrEmpty(d.sceneAddress)) return;
            if (_unavailable.Contains(d.districtId)) return;
            if (_inFlight.Contains(d.districtId) || _resident.Contains(d.districtId)) return;

            _inFlight.Add(d.districtId);
            _resident.Add(d.districtId);   // niyet ANINDA kaydedilir: yoksa yukleme
                                           // surerken bir sonraki degerlendirme ayni
                                           // semti tekrar yuklemeye kalkar.

            var handle = Addressables.LoadSceneAsync(d.sceneAddress, LoadSceneMode.Additive,
                                                     activateOnLoad: false);
            handle.Completed += op =>
            {
                _inFlight.Remove(d.districtId);
                if (op.Status != AsyncOperationStatus.Succeeded)
                {
                    _resident.Remove(d.districtId);
                    _unavailable.Add(d.districtId);
                    Debug.LogWarning(
                        $"[Hezarfen] Semt yuklenemedi: {d.districtId} ({d.sceneAddress}). " +
                        "Addressables katalogunda yok olabilir — " +
                        "Hezarfen → GIS → Semtleri ice aktar calistirildi mi? " +
                        "Bu semt icin bir daha denenmeyecek.");
                    return;
                }
                _loaded[d.districtId] = op;
                op.Result.ActivateAsync();
                if (verbose) Debug.Log($"[Hezarfen] Semt yuklendi: {d.districtId}");
            };
        }

        private void Unload(DistrictDef d)
        {
            AsyncOperationHandle<SceneInstance> handle;
            if (!_loaded.TryGetValue(d.districtId, out handle))
            {
                // Ucusta olan bir yukleme: iptal edilmez, indiginde bosaltilir.
                if (!_inFlight.Contains(d.districtId)) _resident.Remove(d.districtId);
                return;
            }
            _loaded.Remove(d.districtId);
            _resident.Remove(d.districtId);
            Addressables.UnloadSceneAsync(handle);
            if (verbose) Debug.Log($"[Hezarfen] Semt bosaltildi: {d.districtId}");
        }

        private void OnDisable()
        {
            foreach (var kv in _loaded) Addressables.UnloadSceneAsync(kv.Value);
            _loaded.Clear();
            _resident.Clear();
            _inFlight.Clear();
        }

        private void OnDrawGizmosSelected()
        {
            if (registry == null || registry.districts == null) return;
            Vector3 eye = viewer != null ? viewer.position : transform.position;

            foreach (var d in registry.districts)
            {
                if (d == null || d.boundary == null || d.boundary.Length < 2) continue;
                float dist = d.DistanceMeters(eye);
                bool resident = _resident.Contains(d.districtId);

                Gizmos.color = resident ? new Color(0.35f, 1f, 0.45f)
                             : dist <= d.loadDistanceMeters ? new Color(1f, 0.85f, 0.3f)
                             : new Color(0.45f, 0.5f, 0.6f);

                for (int i = 0; i < d.boundary.Length; i++)
                {
                    Vector2 a = d.boundary[i];
                    Vector2 b = d.boundary[(i + 1) % d.boundary.Length];
                    Gizmos.DrawLine(new Vector3(a.x, 0f, a.y), new Vector3(b.x, 0f, b.y));
                }
            }
        }
    }
}
