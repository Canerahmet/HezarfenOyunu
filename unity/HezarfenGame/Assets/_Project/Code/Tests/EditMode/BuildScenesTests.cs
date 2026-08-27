using Hezarfen.Editor.Pipeline;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Hezarfen.Tests
{
    /// <summary>
    /// Build sahne listesinin <b>bize ait</b> olduğunu kilitler.
    ///
    /// Denetim turunda (2026-08-24) listede tek sahne vardı ve o da HDRP
    /// şablonunun boş örnek sahnesiydi. Hiçbir şey hata vermiyordu — Faz 7'de
    /// paketlenene kadar da vermezdi.
    ///
    /// Test bir listeyi değil bir <b>gerekliliği</b> sınıyor: pakete giren her
    /// sahne <c>Assets/_Project/Scenes</c> altında ve bizim olacak; yayınla
    /// gelen semt sahneleri (Addressables) ve sandbox/ölçüm sahneleri
    /// girmeyecek.
    /// </summary>
    public class BuildScenesTests
    {
        [Test]
        public void BuildListIsNotEmpty()
        {
            Assert.Greater(EditorBuildSettings.scenes.Length, 0,
                "Build sahne listesi BOS. Menu: Hezarfen -> Boru Hatti -> "
                + "Build sahne listesini duzelt");
        }

        [Test]
        public void BuildListCarriesOnlyOurScenes()
        {
            foreach (var s in EditorBuildSettings.scenes)
            {
                StringAssert.StartsWith("Assets/_Project/Scenes/", s.path,
                    $"{s.path} bizim sahnemiz degil — sablondan kalmis olabilir.");
                StringAssert.DoesNotContain("/Sandbox/", s.path,
                    $"{s.path} bir sandbox/inceleme sahnesi; pakete girmez.");
                StringAssert.DoesNotContain("Bench_", s.path,
                    $"{s.path} bir olcum sahnesi; pakete girmez.");
            }
        }

        /// <summary>
        /// Semt sahneleri Addressables ile yükleniyor (ADR 0011). Aynı sahneyi
        /// build listesine de koymak onu <b>iki kez</b> paketler: liste dolu
        /// görünür, yayın hattı bozulur.
        /// </summary>
        [Test]
        public void DistrictScenesAreStreamedNotBuilt()
        {
            foreach (var s in EditorBuildSettings.scenes)
                StringAssert.DoesNotContain("/Districts/", s.path,
                    $"{s.path} Addressables ile yayinlaniyor — build listesinde "
                    + "olmamali.");
        }

        [Test]
        public void EveryWantedSceneExists()
        {
            foreach (string p in BuildScenes.Wanted)
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Object>(p),
                    $"Build listesinde istenen sahne yok: {p}");
        }
    }
}
