using Hezarfen.Flight;
using NUnit.Framework;
using UnityEngine;

namespace Hezarfen.Tests
{
    /// <summary>
    /// Rüzgâr alanının davranışını kilitler. Rüzgâr bu oyunda bir efekt değil ANA MEKANİK:
    /// efsanenin istediği 33:1 oranı fizik sabitleriyle değil bu alanla kapatıyoruz.
    /// </summary>
    public class WindFieldTests
    {
        private GameObject root;
        private WindField field;
        private WindTuning tuning;

        [SetUp]
        public void SetUp()
        {
            tuning = ScriptableObject.CreateInstance<WindTuning>();
            tuning.globalWind = new Vector3(9f, 0f, 0f);

            root = new GameObject("WindFieldTestRoot");
            field = root.AddComponent<WindField>();
            field.tuning = tuning;
            field.autoCollectVolumes = false;   // testler hacimleri elle ekler
            // ARAZI TERMIGI DE ARANMASIN.
            //
            // Termik oyun sahnesine baglandigi gun bu dosyadaki uc test
            // kirmizi yandi: Editor'de yuklu duran sahnedeki termik
            // bulunuyordu ve "hacmin disinda katki yok" diye sorulan
            // noktaya su cokelmesi (-0,54 m/s) geliyordu. Testin sordugu
            // sey HACIMLERIN davranisi; arazininki baska bir sorudur ve
            // baska bir yerde sorulur.
            field.autoFindThermal = false;
        }

        [TearDown]
        public void TearDown()
        {
            if (root != null) Object.DestroyImmediate(root);
            if (tuning != null) Object.DestroyImmediate(tuning);
        }

        private WindVolume AddVolume(Vector3 pos)
        {
            var go = new GameObject("Vol");
            go.transform.SetParent(root.transform);
            go.transform.position = pos;
            var v = go.AddComponent<WindVolume>();
            field.volumes.Add(v);
            return v;
        }

        [Test]
        public void EmptyField_ReturnsGlobalWind()
        {
            Assert.AreEqual(tuning.globalWind, field.Sample(Vector3.zero));
        }

        [Test]
        public void OutsideVolume_ContributesNothing()
        {
            var v = AddVolume(new Vector3(1000f, 100f, 0f));
            v.shape = WindVolume.VolumeShape.Sphere;
            v.radius = 100f;
            v.liftSpeed = 5f;

            // 500 m uzakta - hacmin cok disinda
            Assert.AreEqual(tuning.globalWind, field.Sample(new Vector3(1500f, 100f, 0f)));
        }

        [Test]
        public void VolumeCenter_GivesFullLift()
        {
            var v = AddVolume(new Vector3(1000f, 100f, 0f));
            v.shape = WindVolume.VolumeShape.Sphere;
            v.radius = 100f;
            v.liftSpeed = 5f;
            v.falloff = AnimationCurve.Linear(0f, 1f, 1f, 0f);

            Vector3 w = field.Sample(new Vector3(1000f, 100f, 0f));
            Assert.AreEqual(5f, w.y, 0.01f, "Merkezde tam kaldirma bekleniyor.");
            Assert.AreEqual(9f, w.x, 0.01f, "Global lodos korunmali.");
        }

        [Test]
        public void Falloff_WeakensTowardEdge()
        {
            var v = AddVolume(Vector3.zero);
            v.shape = WindVolume.VolumeShape.Sphere;
            v.radius = 100f;
            v.liftSpeed = 4f;
            v.falloff = AnimationCurve.Linear(0f, 1f, 1f, 0f);

            float center = field.Sample(Vector3.zero).y;
            float half = field.Sample(new Vector3(50f, 0f, 0f)).y;
            float edge = field.Sample(new Vector3(99f, 0f, 0f)).y;

            Assert.Greater(center, half);
            Assert.Greater(half, edge);
            Assert.AreEqual(0f, field.Sample(new Vector3(101f, 0f, 0f)).y, 0.01f);
        }

        [Test]
        public void Column_StaysActiveWhenCraftDescends()
        {
            // Termikler SUTUNDUR. Kure olarak modellenirse aygit biraz alcalinca
            // termikten cikar - bu hata bir kez yasandi, test tekrarini engelliyor.
            var v = AddVolume(new Vector3(0f, 250f, 0f));
            v.shape = WindVolume.VolumeShape.Column;
            v.radius = 150f;
            v.columnHeight = 500f;
            v.liftSpeed = 3f;
            v.falloff = AnimationCurve.Linear(0f, 1f, 1f, 0f);

            // Merkezden 200 m asagida ama sutunun icinde - hala kaldirmali
            Assert.Greater(field.Sample(new Vector3(0f, 50f, 0f)).y, 2.5f,
                "Sutunun icinde alcalan aygit kaldirmayi kaybetmemeli.");

            // Sutunun ustunde/altinda - kaldirma yok
            Assert.AreEqual(0f, field.Sample(new Vector3(0f, 600f, 0f)).y, 0.01f);
        }

        [Test]
        public void SinkVolume_ProducesDownwardWind()
        {
            var v = AddVolume(Vector3.zero);
            v.shape = WindVolume.VolumeShape.Sphere;
            v.radius = 100f;
            v.liftSpeed = -3f;

            Assert.Less(field.Sample(Vector3.zero).y, 0f, "Cokelme bolgesi asagi ruzgar uretmeli.");
        }

        [Test]
        public void MultipleVolumes_Accumulate()
        {
            foreach (var pos in new[] { Vector3.zero, Vector3.zero })
            {
                var v = AddVolume(pos);
                v.shape = WindVolume.VolumeShape.Sphere;
                v.radius = 100f;
                v.liftSpeed = 2f;
                v.falloff = AnimationCurve.Linear(0f, 1f, 1f, 0f);
            }

            Assert.AreEqual(4f, field.Sample(Vector3.zero).y, 0.01f,
                "Ust uste binen hacimler toplanmali.");
        }

        [Test]
        public void DisabledVolume_IsIgnored()
        {
            var v = AddVolume(Vector3.zero);
            v.shape = WindVolume.VolumeShape.Sphere;
            v.radius = 100f;
            v.liftSpeed = 5f;
            v.gameObject.SetActive(false);

            Assert.AreEqual(0f, field.Sample(Vector3.zero).y, 0.01f);
        }
    }
}
