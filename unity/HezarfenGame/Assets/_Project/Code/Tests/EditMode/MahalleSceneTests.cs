using System.Collections.Generic;
using Hezarfen.Editor.Gis;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Hezarfen.Tests
{
    /// <summary>
    /// <b>Faz 2b'nin kabul ölçütünü</b> kilitler (PLAN.md §7.1):
    /// *"Mescidi merkez alan bir mahalle: mescit + şadırvan + çeşme + birkaç
    /// dükkân + mezarlık; sokak yerleştiricisi mescidi çekirdek olarak
    /// kullanır ve doku ondan dallanır."*
    ///
    /// ## Neden liste değil, İLİŞKİ ölçülüyor
    ///
    /// Beş yapının sahnede bulunduğunu saymak kolaydır ve az şey söyler:
    /// beşi de mahallenin dört bir yanına dağılmış olabilir. Ölçütün asıl
    /// cümlesi *"doku ondan dallanır"*dır — yani mesafeler. Aşağıdaki
    /// eşikler ölçülerek seçildi (sahnenin gerçek değerleri parantezde) ve
    /// gevşek tutuldu: sınanan şey bir konum değil, bir <b>düzen</b>.
    ///
    /// Bu test bir gerileme için var: yerleştiricide bir sıra değişir,
    /// çeşme başka bir yere düşer, hazire elenir — ve mahalle sessizce
    /// "birkaç bina"ya döner. Bir kez tam bu oldu: dükkân sırası dört slot
    /// deniyordu, ikisi eleniyordu ve sahnede <b>iki</b> dükkân kalıyordu.
    /// </summary>
    public class MahalleSceneTests
    {
        private static Scene Open() =>
            EditorSceneManager.OpenScene(OttomanStreetBuilder.ScenePath,
                                         OpenSceneMode.Additive);

        private static GameObject Root(Scene scene)
        {
            foreach (var go in scene.GetRootGameObjects())
                if (go.name == OttomanStreetBuilder.RootName) return go;
            Assert.Fail($"{OttomanStreetBuilder.RootName} yok. "
                        + "Hezarfen -> GIS -> Galata sokagi sahnesi kur");
            return null;
        }

        private static List<Transform> AllOf(GameObject root, string prefix)
        {
            var list = new List<Transform>();
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name.StartsWith(prefix)) list.Add(t);
            return list;
        }

        private static Transform One(GameObject root, string prefix)
        {
            var l = AllOf(root, prefix);
            return l.Count > 0 ? l[0] : null;
        }

        [Test]
        public void QuarterCarriesEverythingTheAcceptanceAsksFor()
        {
            var scene = Open();
            try
            {
                var root = Root(scene);

                var mescit = One(root, "PF_Mescit_A");
                Assert.IsNotNull(mescit, "Mahallenin CEKIRDEGI yok: mescit.");

                Assert.IsNotNull(One(root, "PF_Sadirvan"),
                    "Sadirvan yok — cami avlusunun abdest yeri.");
                Assert.IsNotNull(One(root, "PF_Cesme"),
                    "Mahalle CESMESIZ — toplanma sebebi sudur.");

                var dukkan = AllOf(root, "PF_Dukkan");
                Assert.GreaterOrEqual(dukkan.Count, 3,
                    $"Yalniz {dukkan.Count} dukkan var; olcut 'birkac dukkan'. "
                    + "Sira slot degil HEDEF saymali (bkz. PlaceCore).");

                var mezar = AllOf(root, "PF_Mezar");
                Assert.GreaterOrEqual(mezar.Count, 8,
                    $"Hazire {mezar.Count} tasla mezarlik olarak okunmaz.");
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }

        [Test]
        public void FabricBranchesFromTheCore()
        {
            var scene = Open();
            try
            {
                var root = Root(scene);
                var mescit = One(root, "PF_Mescit_A");
                Assert.IsNotNull(mescit, "Mescit yok.");
                Vector3 c = mescit.position;

                // Sadirvan AVLUNUN ICINDE: mescitten bir avlu boyu kadar.
                float d = Vector3.Distance(One(root, "PF_Sadirvan").position, c);
                Assert.Less(d, 14f,
                    $"Sadirvan mescitten {d:F1} m — avlunun icinde degil.");

                // Cesme cekirdegin gecis noktasinda.
                d = Vector3.Distance(One(root, "PF_Cesme").position, c);
                Assert.Less(d, 40f, $"Cesme cekirdekten {d:F1} m uzakta.");

                // Dukkan sirasi mescidin KARSISINDA, yani yakininda.
                foreach (var t in AllOf(root, "PF_Dukkan"))
                {
                    float dd = Vector3.Distance(t.position, c);
                    Assert.Less(dd, 60f,
                        $"{t.name} cekirdekten {dd:F1} m — carsi cekirdegin yaninda.");
                }

                // Hazire caminin yaninda (arkasinda ya da yanida).
                float best = float.MaxValue;
                foreach (var t in AllOf(root, "PF_Mezar"))
                    best = Mathf.Min(best, Vector3.Distance(t.position, c));
                Assert.Less(best, 30f,
                    $"En yakin mezar tasi {best:F1} m — hazire cami avlusuna aittir.");
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }

        /// <summary>
        /// Kahvehane ve bozahane — oyunun <b>iki zaman işareti</b> de sahnede.
        ///
        /// İkisi de 1632'de açıktır ve IV. Murad döneminde kapanır (kahvehane
        /// 2 Eylül 1633 fermanı, ADR 0021 §5; bozahane ADR 0030 §2). Bir 1633
        /// sahnesi kurulursa <b>ikisi birlikte</b> kaldırılır. Test onların
        /// varlığını kilitliyor ki o gün geldiğinde nereye bakılacağı belli
        /// olsun.
        /// </summary>
        [Test]
        public void BothTimeMarkersStandInThe1632Quarter()
        {
            var scene = Open();
            try
            {
                var root = Root(scene);
                Assert.IsNotNull(One(root, "PF_Kahvehane"),
                    "Kahvehane yok — 1632'nin birinci zaman isareti.");
                Assert.IsNotNull(One(root, "PF_Bozahane"),
                    "Bozahane yok — 1632'nin ikinci zaman isareti "
                    + "(1638 sayiminda sehirde 300 tane).");
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }
    }
}
