using System.Linq;
using Hezarfen.Core;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Hezarfen.Tests
{
    /// <summary>
    /// Faz 0 graybox sahnesinin ölçülerini kilitler (docs/PLAN.md Bölüm 5, Görev 4).
    /// Bu sayılar keyfi değil: uçuş ayarının tamamı bunlara göre yapılacak. Biri sessizce
    /// değişirse süzülme oranı hesapları da sessizce yanlışlanır.
    ///
    /// Sahne ek (additive) olarak açılır ve test sonunda kapatılır — Editor'de açık olan
    /// sahneye dokunmaz.
    /// </summary>
    public class GrayboxSceneTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/FlightSlice.unity";
        private Scene scene;

        [SetUp]
        public void SetUp()
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        }

        [TearDown]
        public void TearDown()
        {
            if (scene.IsValid() && scene.isLoaded)
            {
                EditorSceneManager.CloseScene(scene, removeScene: true);
            }
        }

        private GameObject Find(string name)
        {
            var go = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .FirstOrDefault(t => t.name == name)?.gameObject;
            Assert.IsNotNull(go, $"'{name}' sahnede bulunamadi.");
            return go;
        }

        [Test]
        public void Scene_Opens()
        {
            Assert.IsTrue(scene.IsValid() && scene.isLoaded, $"{ScenePath} acilamadi.");
        }

        [Test]
        public void Tower_Is100MetersTall()
        {
            var tower = Find("GB_Tower_Galata");
            // Unity Cylinder primitifi 2 birim yuksektir; y olcegi 50 => 100 m.
            float heightMeters = GameUnits.UnitsToMeters(tower.transform.localScale.y * 2f);
            Assert.AreEqual(100f, heightMeters, 0.01f, "Kule 100 m olmali (plan Bolum 5).");

            float topMeters = GameUnits.UnitsToMeters(tower.transform.position.y + tower.transform.localScale.y);
            Assert.AreEqual(100f, topMeters, 0.01f, "Kule tepesi y=100 m'de olmali.");
        }

        [Test]
        public void Target_IsAtLegendaryGlideDistance()
        {
            var tower = Find("GB_Tower_Galata");
            var target = Find("GB_Target_Dogancilar");

            Vector3 a = tower.transform.position;
            Vector3 b = target.transform.position;
            float horizontal = GameUnits.UnitsToMeters(new Vector2(b.x - a.x, b.z - a.z).magnitude);

            Assert.AreEqual(GameUnits.LegendaryGlideDistanceMeters, horizontal, 1f,
                "Hedef, efsanevi sizulus mesafesinde olmali (3358 m).");
        }

        [Test]
        public void RequiredGlideRatio_IsFarBeyondUnpoweredGlide()
        {
            // Kabul kriteri (plan Bolum 5): ruzgarsiz oran 8-12:1.
            // Kule tepesinden hedefe duz sizulus 33:1'den fazlasini ister.
            // Yani graybox KASTEN ulasilamaz; farki ruzgar akintilari kapatacak.
            // Bu test o tasarim niyetini kayit altina alir: gun gelir de oran 12'nin
            // altina duserse, ruzgar sistemi anlamsizlasmis demektir.
            var tower = Find("GB_Tower_Galata");
            float launchHeight = GameUnits.UnitsToMeters(tower.transform.position.y + tower.transform.localScale.y);
            float required = GameUnits.LegendaryGlideDistanceMeters / launchHeight;

            Assert.Greater(required, 12f,
                "Gereken oran 12:1'in uzerinde kalmali - aksi halde ruzgar sistemi gereksizlesir.");
            Assert.AreEqual(33.58f, required, 0.1f);
        }

        [Test]
        public void EveryGrayboxElement_HasHistoricalTag()
        {
            // CLAUDE.md kurali: her sahne ogesi etiketli.
            foreach (var name in new[] { "GB_Ground", "GB_Tower_Galata", "GB_LaunchPlatform", "GB_Target_Dogancilar" })
            {
                var go = Find(name);
                var tag = go.GetComponent<HistoricalTag>();
                Assert.IsNotNull(tag, $"'{name}' HistoricalTag tasimiyor.");
                Assert.IsTrue(tag.IsValid, $"'{name}' etiketi gecersiz (T1 ise kaynak notu bos).");
            }
        }

        [Test]
        public void AllTagsInScene_AreValid()
        {
            var invalid = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<HistoricalTag>(true))
                .Where(t => !t.IsValid)
                .Select(t => t.name)
                .ToArray();

            Assert.IsEmpty(invalid,
                "Kaynaksiz T1 etiketi tasiyan ogeler: " + string.Join(", ", invalid));
        }
    }
}
