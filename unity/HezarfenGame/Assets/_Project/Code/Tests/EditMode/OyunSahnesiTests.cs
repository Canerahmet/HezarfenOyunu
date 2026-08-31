using Hezarfen.Player;
using Hezarfen.Sehir;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Hezarfen.Tests
{
    /// <summary>
    /// <b>Oyun sahnesi oyuncunun eline geçtiği hâliyle ölçülür.</b>
    ///
    /// Sahne diskten açılır, bellekte kurulmuş bir kopya değil. Bu ayrım bu
    /// projede pahalıya mal oldu: açılış menüsü kurulum anında çalışıyordu
    /// ve <b>kaydedilmiş</b> sahnede hiçbir düğmesi bağlı değildi.
    /// </summary>
    public class OyunSahnesiTests
    {
        private const string Yol =
            "Assets/_Project/Scenes/Faz1_Terrain.unity";

        [SetUp]
        public void Ac() => EditorSceneManager.OpenScene(
            Yol, OpenSceneMode.Single);

        private static Camera Kamera()
        {
            foreach (var c in Object.FindObjectsByType<Camera>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
                if (c.CompareTag("MainCamera")) return c;
            return null;
        }

        /// <summary>
        /// <b>Kenar yumuşatma AÇIK.</b>
        ///
        /// Caner (2026-08-29): *"modellerin kenar ve koselerinde titremeler
        /// var... isiksal mi yoksa baska bir problem mi var?"*
        ///
        /// Işıksal değildi: sahnedeki <c>antialiasing</c> <b>None</b>'dı ve
        /// kod tabanında AA'ya dokunan tek satır yoktu. Kiremit sırtı, sur
        /// mazgalı ve minare kenarı gibi ince yüksek kontrastlı kenarlar her
        /// karede başka pikseli örttüğü için kaynıyordu.
        ///
        /// Test SMAA'yı da kabul etmez: SMAA tek karelik bir kenar
        /// filtresidir ve <b>hareket eden</b> ince geometride kaynamayı
        /// durdurmaz — şikâyet tam olarak harekette.
        /// </summary>
        [Test]
        public void TheCameraHasTemporalAntialiasing()
        {
            var kam = Kamera();
            Assert.IsNotNull(kam, "Sahnede MainCamera yok.");
            var veri = kam.GetComponent<HDAdditionalCameraData>();
            Assert.IsNotNull(veri, "HDAdditionalCameraData yok.");
            Assert.AreEqual(
                HDAdditionalCameraData.AntialiasingMode.TemporalAntialiasing,
                veri.antialiasing,
                "Kenar yumusatma TAA degil — kenarlar harekette kaynar.");
        }

        /// <summary>
        /// <b>Kamera kipi bileşeni sahnede</b> ve görünür gövdeye bağlı.
        /// Gövdesiz bir üçüncü şahıs kamerası boşluğun arkasından bakar.
        /// </summary>
        [Test]
        public void ThePlayerCanChangeTheCameraAngle()
        {
            var kip = Object.FindAnyObjectByType<KameraKipi>();
            Assert.IsNotNull(kip,
                "KameraKipi sahnede yok — oyuncu kamerayi degistiremez.");
            Assert.IsNotNull(kip.govde,
                "Gorunur govde bagli degil — ucuncu sahiste karakter "
                + "gorunmez.");
        }

        /// <summary>
        /// <b>Görünür gövdenin çarpıştırıcısı YOK.</b> Gövde görseldir;
        /// kendi kapsülüyle kavga ederse karakteri havaya fırlatır.
        /// </summary>
        [Test]
        public void TheVisibleBodyDoesNotFightTheCharacterController()
        {
            var kip = Object.FindAnyObjectByType<KameraKipi>();
            if (kip == null || kip.govde == null) Assert.Ignore("govde yok");
            Assert.AreEqual(0,
                kip.govde.GetComponentsInChildren<Collider>(true).Length,
                "Gorsel govdede carpistirici var — CharacterController ile "
                + "kavga eder.");
        }

        /// <summary>Oyuncu ve saat sahnede — oyun oynanabilir olmalı.</summary>
        [Test]
        public void TheSceneIsActuallyPlayable()
        {
            Assert.IsNotNull(Object.FindAnyObjectByType<WalkController>(),
                "Oyuncu yok.");
            Assert.IsNotNull(Kamera(), "Kamera yok.");
        }
        /// <summary>
        /// <b>Oyuncu bir seye uzanabiliyor mu.</b>
        ///
        /// Etkilesim iki parcadan olusuyor ve ikisi de sahnede kurulur:
        /// kese govdede, nisan GOZDE. Sahne kurulumunda birinin
        /// unutulmasi sessiz bir kayiptir — sehirde 15.815 dokunulabilir
        /// esya durur, oyuncu hicbirine dokunamaz ve hicbir hata mesaji
        /// cikmaz. Testin sordugu soru tam bu: eksiklik gurultu
        /// cikarmiyorsa test cikarmali.
        /// </summary>
        [Test]
        public void ThePlayerCanCarryThingsAndReachForThem()
        {
            var oyuncu = GameObject.Find("OYUNCU");
            Assert.IsNotNull(oyuncu, "Sahnede OYUNCU yok.");

            Assert.IsNotNull(oyuncu.GetComponent<Envanter>(),
                "Oyuncunun kesesi yok — aldigi sey hicbir yere gitmez.");

            var uzan = oyuncu.GetComponent<EtkilesimAlgila>();
            Assert.IsNotNull(uzan,
                "Etkilesim algilayici yok — sehirdeki hicbir esya "
                + "cevap vermez.");
            Assert.IsNotNull(uzan.bakis,
                "Nisan yonu bos — govdenin ileri yonune duser ve omuz "
                + "ustu kamerada bakilan seyle uzanilan sey ayrilir.");
            Assert.AreEqual("Main Camera", uzan.bakis.name,
                "Nisan GOZDE olmali.");
        }

    }
}
