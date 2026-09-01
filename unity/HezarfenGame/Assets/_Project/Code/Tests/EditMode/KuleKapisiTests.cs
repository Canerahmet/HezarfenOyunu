using Hezarfen.Sehir;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Hezarfen.Tests
{
    /// <summary>
    /// <b>Kuleye çıkılabiliyor mu.</b>
    ///
    /// ## Neden bu dosya var
    ///
    /// Oyunun adı Hezarfen ve tamamı bir kuleden atlamak için. İki tur
    /// üst üste oyuncu kuleye çıkamadı.
    ///
    /// Birinci turda kapı <b>yoktu</b>: kule tek parça mühürlü bir
    /// kabuk, iç mekân yok, tırmanma yok. Kapı eklendi ve "kapatıldı"
    /// diye rapor edildi.
    ///
    /// İkinci turda kapı <b>vardı ve taşın içindeydi</b>. Kule
    /// çarpıştırıcısı 8,225 m yarıçapında dolu bir silindir; kapı
    /// tetikleyicisi eksenden 6,5 m'ye konmuştu, yani duvarın 1,7 m
    /// içine. <see cref="Hezarfen.Player.EtkilesimAlgila"/> birinci
    /// turda tam bu iş için görüş hattı ışını eklemişti ve kapıyı
    /// doğru biçimde reddediyordu. Ekranda "Kuleye çık" hiç belirmedi;
    /// bir oyuncu otuz dakika kuleyi dolandı.
    ///
    /// Ve aynı sahnede kapının <b>dört üst üste binmiş kopyası</b>
    /// vardı: üreteç her koşuşta bir yenisini ekliyor, eskisini
    /// silmiyordu.
    ///
    /// İkisi de yazıldı, diske geçti ve <b>ölçülmedi</b>. Bu dosya o
    /// boşluğu kapatıyor.
    /// </summary>
    public class KuleKapisiTests
    {
        private const string Sahne =
            "Assets/_Project/Scenes/Faz1_Terrain.unity";

        private static KuleKapisi[] Kapilar()
        {
            var s = EditorSceneManager.GetSceneByPath(Sahne);
            if (!s.isLoaded)
                s = EditorSceneManager.OpenScene(Sahne, OpenSceneMode.Additive);

            var liste = new System.Collections.Generic.List<KuleKapisi>();
            foreach (var kok in s.GetRootGameObjects())
                liste.AddRange(kok.GetComponentsInChildren<KuleKapisi>(true));
            return liste.ToArray();
        }

        [Test]
        public void TheTowerHasExactlyOneDoor()
        {
            var k = Kapilar();
            Assert.AreEqual(1, k.Length,
                $"Sahnede {k.Length} kule kapisi var. Sifir ise oyunun "
                + "doruk noktasi erisilemez; birden fazlaysa ureteç her "
                + "kosumda bir yenisini ekliyor ve eskisini silmiyor "
                + "demektir.");
        }

        /// <summary>
        /// <b>Kapı taşın dışında mı.</b>
        ///
        /// Etkileşim fizikten geçiyor ve görüş hattı istiyor. Kulenin
        /// kendi çarpıştırıcısının içinde duran bir tetikleyiciye
        /// oyuncu asla ulaşamaz — sistem çalışır, kapı görünmez.
        /// </summary>
        [Test]
        public void TheDoorIsOutsideTheStoneNotInsideIt()
        {
            var k = Kapilar();
            Assert.AreEqual(1, k.Length, "Once kapi sayisi duzelmeli.");
            var kapi = k[0];

            var kule = kapi.transform.parent;
            Assert.IsNotNull(kule, "Kapinin ebeveyni yok.");

            Collider tas = null;
            foreach (var c in kule.GetComponentsInChildren<Collider>(true))
                if (!c.isTrigger) { tas = c; break; }
            Assert.IsNotNull(tas, "Kulede kati carpistirici yok.");

            // Kapinin merkezi tasin siniri DISINDA olmali.
            var p = kapi.transform.position;
            var enYakin = tas.ClosestPoint(p);
            float d = Vector3.Distance(p, enYakin);

            Assert.Greater(d, 0.5f,
                $"Kapi tasin {(d < 0.01f ? "ICINDE" : $"{d:F2} m disinda")}. "
                + "Etkilesim gorus hatti istiyor; tasin icindeki bir "
                + "tetikleyiciye oyuncu asla ulasamaz ve 'Kuleye cik' "
                + "yazisi hic belirmez.");
        }

        /// <summary>Şerefe kotu gerçekten kalkış eşiğinin üstünde mi.</summary>
        [Test]
        public void TheGalleryIsHighEnoughToCountAsALaunch()
        {
            var k = Kapilar();
            Assert.AreEqual(1, k.Length, "Once kapi sayisi duzelmeli.");
            Assert.Greater(k[0].serefeKotu,
                Hezarfen.Player.Perde2Dilimi.KalkisKotu,
                "Serefe, perdenin kalkis esiginin altinda — kuleye "
                + "cikan oyuncu yine 'kalkis sayilmadi' gorur.");
        }
    }
}
