using System.Collections;
using System.Linq;
using Hezarfen.Streaming;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Hezarfen.Tests
{
    /// <summary>
    /// Bölge yayınının **gerçekten çalıştığının** kanıtı — plan Faz 1 madde 6.
    ///
    /// EditMode testleri kararın doğru olduğunu gösterir; bu test kararın Addressables
    /// üzerinden bir sahneye dönüştüğünü gösterir. İkisi ayrı sorulardır: yayın mantığı
    /// kusursuz olup adres katalogda bulunmayabilir, ya da sahne yüklenip
    /// etkinleştirilmemiş olabilir. Yalnızca ilkini test etmek, "iskelet ayakta"
    /// demenin en kolay ve en yanıltıcı yoludur.
    /// </summary>
    public class DistrictStreamingPlayTests
    {
        private const int MaxFrames = 600;   // ~10 sn @60fps; Addressables cozumlemesi icin bol

        private GameObject _root;

        private static DistrictRegistry LoadRegistry()
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<DistrictRegistry>(
                "Assets/_Project/Data/DistrictDefs/DistrictRegistry.asset");
#else
            return null;
#endif
        }

        [TearDown]
        public void TearDown()
        {
            if (_root != null) Object.DestroyImmediate(_root);
        }

        private static bool SceneIsLoaded(string districtId)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).name == districtId &&
                    SceneManager.GetSceneAt(i).isLoaded) return true;
            return false;
        }

        [UnityTest]
        public IEnumerator Streamer_LoadsDistrictWhenViewerApproaches_AndUnloadsWhenFar()
        {
            var registry = LoadRegistry();
            if (registry == null || registry.districts.Length == 0)
                Assert.Ignore("Semtler henuz ice aktarilmadi " +
                              "(Hezarfen → GIS → Semtleri ice aktar).");

            var galata = registry.Find("D_Galata");
            Assert.IsNotNull(galata, "D_Galata kayitli degil.");

            _root = new GameObject("TEST_Streamer");
            var viewerGo = new GameObject("TEST_Viewer");
            viewerGo.transform.SetParent(_root.transform);
            // Galata'nin tam ortasi: uzaklik 0, kesin yukleme.
            viewerGo.transform.position = new Vector3(galata.center.x, 0f, galata.center.y);

            var streamer = _root.AddComponent<DistrictStreamer>();
            streamer.registry = registry;
            streamer.viewer = viewerGo.transform;
            streamer.evaluateInterval = 0f;
            streamer.verbose = false;

            // --- yukleme ---
            int frames = 0;
            while (!SceneIsLoaded("D_Galata") && frames++ < MaxFrames) yield return null;

            Assert.IsTrue(SceneIsLoaded("D_Galata"),
                $"D_Galata {MaxFrames} karede yuklenmedi. Addressables katalogunda " +
                "adres yok olabilir — Hezarfen → GIS → Semtleri ice aktar calistirildi mi?");
            Assert.Contains("D_Galata", streamer.ResidentDistricts.ToList());

            // --- bosaltma: gorusu dunyanin disina tasi ---
            viewerGo.transform.position = new Vector3(100000f, 0f, 100000f);

            frames = 0;
            while (SceneIsLoaded("D_Galata") && frames++ < MaxFrames) yield return null;

            Assert.IsFalse(SceneIsLoaded("D_Galata"),
                "Uzaklasildiginda semt bosaltilmadi — bellek surekli buyur.");
            Assert.IsFalse(streamer.ResidentDistricts.Contains("D_Galata"));
        }

        [UnityTest]
        public IEnumerator Streamer_DoesNotExceedConcurrentLoadCap()
        {
            // Uc semtin kesistigi bir noktada uc sahne birden cozulurse kare duser
            // ve "yukleme ekrani yok" vaadi bir takilma olarak geri gelir.
            var registry = LoadRegistry();
            if (registry == null || registry.districts.Length == 0)
                Assert.Ignore("Semtler henuz ice aktarilmadi.");

            _root = new GameObject("TEST_Streamer");
            var viewerGo = new GameObject("TEST_Viewer");
            viewerGo.transform.SetParent(_root.transform);
            viewerGo.transform.position = Vector3.zero;   // Galata Kulesi = dunya orijini

            var streamer = _root.AddComponent<DistrictStreamer>();
            streamer.registry = registry;
            streamer.viewer = viewerGo.transform;
            streamer.evaluateInterval = 0f;
            streamer.verbose = false;

            // ÖLÇÜLEN: uçuştaki YÜKLEME sayısı.
            //
            // "Yerleşik ama sahnesi henüz isLoaded değil" saymak cazip ama YANLIŞTIR:
            // `activateOnLoad: false` yüzünden çözülmüş-ama-etkinleşmemiş bir sahne de
            // o sayıya girer, ve tavan 1 olsa bile ölçüm 2 gösterir. Bu testin ilk
            // hâli tam olarak böyle düştü — kod değil, ölçü yanlıştı.
            int maxInFlight = 0;
            int maxResidentPending = 0;
            for (int f = 0; f < 240; f++)
            {
                if (streamer.LoadsInFlight > maxInFlight) maxInFlight = streamer.LoadsInFlight;
                int pending = streamer.ResidentDistricts.Count(id => !SceneIsLoaded(id));
                if (pending > maxResidentPending) maxResidentPending = pending;
                yield return null;
            }

            Assert.LessOrEqual(maxInFlight, DistrictStreamingPlan.MaxConcurrentLoads,
                "Ayni anda tavandan fazla YUKLEME ucusta — kare dususu riski.");

            // Etkinleşme bekleyenle birlikte de sınırsız büyümemeli: tavan + etkinleşen.
            Assert.LessOrEqual(maxResidentPending, DistrictStreamingPlan.MaxConcurrentLoads + 1,
                "Yukleme + etkinlesme kuyrugu birikiyor — sirali yukleme calismiyor.");

            Assert.Greater(streamer.ResidentDistricts.Count(), 0,
                "Dunya orijininde (Galata Kulesi) hicbir semt yuklenmedi.");
        }
    }
}
