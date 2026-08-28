using Hezarfen.Editor.Gis;
using Hezarfen.Editor.Lighting;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Hezarfen.Tests
{
    /// <summary>
    /// Sokağın yaya seviyesinde <b>okunabilir</b> olduğunu kilitler.
    ///
    /// ## Neden bu test bir uygulamayı değil bir GEREKLİLİĞİ ölçüyor
    ///
    /// Şu an okunabilirliği sağlayan şey geçici bir aydınlatma takımıdır
    /// (<see cref="KaliciAydinlatma"/>) ve geçici takım **silindi**. Test onun
    /// varlığını sınasaydı, kalıcı ışık pası geldiğinde yanlış yere düşerdi.
    /// Sınanan şey şu: kim sağlarsa sağlasın, göz hizasından bakıldığında
    /// kare okunabilir olmalı.
    ///
    /// ## Ölçü parlaklık DEĞİL, ayrıntı
    ///
    /// İlk yazımda ölçü "30/255'in altındaki piksel oranı"ydı ve yanlış aletti:
    /// Balat'ın paleti <b>bilerek</b> koyudur (zimmî renk kısıtı) ve gayet
    /// okunabilir bir Balat cephesi %56 "okunmaz" çıkıyordu. Karanlık ışıkla
    /// karanlık malzeme aynı sayıyı veriyordu. Sorulacak soru şu: <b>doku
    /// deseni görünüyor mu.</b> Ölçü artık ayrıntı enerjisi (her pikselin 3×3
    /// komşu ortalamasından sapması) — palete kör, ışığa duyarlı.
    ///
    /// Ölçülen (gölgedeki ev cephesi, 8 m, göz hizası):
    /// <code>
    ///            takimsiz   takimla
    ///   Galata     0,53      2,28
    ///   Balat      0,46      2,22
    /// </code>
    /// İki semtin parlaklığı iki kat farklı ama ayrıntı enerjisi aynı — alet
    /// doğru şeyi ölçüyor. Eşik <b>1,2</b>: ezilmiş hâlin iki katı üstünde,
    /// bugünkünün yarısı altında.
    /// </summary>
    public class LightingTests
    {
        [Test]
        public void StreetIsReadableAtEyeLevel()
        {
            // Sahne TEK olarak açılır, kitteki öteki testlerin yaptığı gibi
            // EK olarak değil.
            //
            // Sebep ölçüldü: ek açılışta önceki sahnenin küresel Volume'u ve
            // güneşi de yüklü kalıyor, iki gökyüzü aynı öncelikte yarışıyor ve
            // aynı sokak %12 yerine **%52** okunmaz çıkıyor. Geometri ölçen
            // testler bundan etkilenmez; RENDER ölçen bir test sahne
            // yalıtımı olmadan yalan söyler.
            EditorSceneManager.OpenScene(OttomanStreetBuilder.ScenePath,
                                         OpenSceneMode.Single);
            string report;
            float detail;
            try
            {
                report = SokakOkunabilirligi.Measure(out detail);
            }
            finally
            {
                // Sahneyi ARDIMIZDA BIRAKMA. Tek olarak açtığımız için sahne
                // yüklü kalıyor ve sonraki testler onu EK olarak açmaya
                // çalışınca Unity "son sahneyi boşaltamazsın" diye uyarıyor.
                // Boş bir sahneye dönmek, sonraki testlerin varsaydığı hâli
                // geri verir.
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
                                            NewSceneMode.Single);
            }
            Assert.Greater(detail, 1.2f,
                $"Golgedeki cephe OKUNMUYOR (doku deseni ayirt edilemiyor).\n{report}\n"
                + "Aydinlatma: Hezarfen -> Aydinlatma -> Kalici isik pasini kur, sonra Problari pisir.");
        }

        /// <summary>
        /// Güneş, 41° kuzeyde <b>mümkün</b> bir yerde mi.
        ///
        /// Bir kez değildi: ışık 205°'ye doğru yol alıyordu, yani güneş 25°
        /// azimutta — kuzeykuzeydoğuda. Bu enlemde güneş oraya hiçbir gün,
        /// hiçbir saat gelmez. Yükseklik makuldü, gölgeler bir yöne düşüyordu
        /// ve kare makul görünüyordu; kusuru gözle yakalamak mümkün değildi.
        ///
        /// Test bir AÇIYI değil bir GEREKLİLİĞİ kilitliyor: mevsim ya da saat
        /// değişebilir, güneşin gökyüzünde bulunabileceği yer değişmez.
        /// </summary>
        [Test]
        public void SunIsAstronomicallyPossibleForIstanbul()
        {
            EditorSceneManager.OpenScene(OttomanStreetBuilder.ScenePath,
                                         OpenSceneMode.Single);
            try
            {
                var sun = SunPlacement.Find();
                Assert.IsNotNull(sun, "Sahnede yonlu gunes yok.");

                double azi = SunPlacement.AzimuthOf(sun);
                float alt = sun.transform.eulerAngles.x;
                if (alt > 180f) alt -= 360f;

                Assert.Greater(alt, 3f, $"Gunes ufkun {alt:F1} derece altinda/dibinde.");
                Assert.Less(alt, 90f, "Yukseklik 90 dereceyi asamaz.");

                // Gunesin en KUZEY azimutu gun dogumunda, en buyuk deklinasyonda
                // olur. Bunun kuzeyinde kalan bir azimut imkansizdir.
                double lim = SunPlacement.NorthernmostAzimuth(SunPlacement.LatitudeDeg);
                bool northOfLimit = azi < lim || azi > 360.0 - lim;
                Assert.IsFalse(northOfLimit,
                    $"Gunes azimutu {azi:F1} derece — {SunPlacement.LatitudeDeg:F1} K'de "
                    + $"gunes {lim:F1}..{360 - lim:F1} disina CIKAMAZ. "
                    + "Menu: Hezarfen -> Aydinlatma -> Gunesi tarihten yerlestir");

                // Ayrica: o azimutta gunes gercekten bu yukseklige cikabilir mi.
                // Yaz gundonumu ogleni bu enlemin tavani.
                double maxAlt = 90.0 - SunPlacement.LatitudeDeg + 23.45;
                Assert.Less(alt, maxAlt + 1.0,
                    $"Yukseklik {alt:F1} derece — bu enlemde tavan {maxAlt:F1} derece.");
            }
            finally
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
                                            NewSceneMode.Single);
            }
        }
    }
}
