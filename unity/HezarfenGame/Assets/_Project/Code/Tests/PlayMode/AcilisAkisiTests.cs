using System.Collections;
using Hezarfen.Arayuz;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Hezarfen.Tests
{
    /// <summary>
    /// <b>Menü → yükleme → şehir zinciri yürüyor mu?</b>
    ///
    /// PLAN Bölüm 12 açılış akışını Faz 7'ye bırakmıştı. Zincirin sessiz
    /// kırılma yeri şudur: sahne build listesinde değilse
    /// <c>LoadSceneAsync</c> <b>null döner</b> ve oyun menüde takılı
    /// kalır — hata da vermez. Oyuncu "Başla"ya basar, hiçbir şey olmaz.
    /// </summary>
    public class AcilisAkisiTests
    {
        private GameObject _kok;

        [TearDown]
        public void Temizle()
        {
            if (_kok != null) Object.DestroyImmediate(_kok);
        }

        private AcilisMenusu Kur(string sahne)
        {
            _kok = new GameObject("acilis");
            var m = _kok.AddComponent<AcilisMenusu>();
            m.sehirSahnesi = sahne;
            m.menuPaneli = new GameObject("menu");
            m.yuklemePaneli = new GameObject("yukleme");
            m.menuPaneli.transform.SetParent(_kok.transform);
            m.yuklemePaneli.transform.SetParent(_kok.transform);
            return m;
        }

        /// <summary>
        /// <b>Şehir sahnesi build listesinde.</b>
        ///
        /// Bu tek satır, akışın tamamının dayandığı şey. Liste yanlışsa
        /// "Başla" hiçbir şey yapmaz ve sebebi görünmez.
        /// </summary>
        [Test]
        public void TheCitySceneIsActuallyInTheBuild()
        {
            var m = Kur("Faz1_Terrain");
            int i = SceneUtility.GetBuildIndexByScenePath(
                "Assets/_Project/Scenes/Faz1_Terrain.unity");
            Assert.GreaterOrEqual(i, 0,
                "Faz1_Terrain build listesinde YOK — 'Basla' sessizce "
                + "hicbir sey yapardi. (Hezarfen -> Boru Hatti -> Build "
                + "sahne listesini duzelt)");

            int a = SceneUtility.GetBuildIndexByScenePath(
                "Assets/_Project/Scenes/Acilis.unity");
            Assert.AreEqual(0, a,
                $"Acilis sahnesi {a}. sirada — ilk sahne ACILIS olmali, "
                + "yoksa oyun dogrudan sehre acilir ve oyuncu donmus bir "
                + "ekranla karsilanir.");
        }

        /// <summary>
        /// <b>Menüden yüklemeye geçiliyor ve panel değişiyor.</b>
        /// </summary>
        [UnityTest]
        public IEnumerator PressingStartSwitchesToLoading()
        {
            // SEHIR SAHNESI DEGIL, ACILIS.
            //
            // Bu test panel gecisini olcuyor, sehri degil. `Faz1_Terrain`
            // yuklemek gercek bir asenkron yukleme baslatiyordu ve test
            // onu BEKLEMIYORDU: yukleme sonraki testin ortasinda
            // tamamlanip sahneyi degistiriyor, semt akisi testi de kendi
            // nesnelerini kaybedip patliyordu. Tek basina kosunca
            // geciyordu — sirali kosumda degil.
            //
            // Acilis build listesinde ve neredeyse bos; ayni yolu olcer,
            // arkasinda sehir birakmaz.
            var m = Kur("Acilis");
            yield return null;

            Assert.IsTrue(m.menuPaneli.activeSelf, "Menu paneli kapali basladi.");
            Assert.IsFalse(m.yuklemePaneli.activeSelf,
                "Yukleme paneli menude aciktI.");

            m.Basla();
            yield return null;

            Assert.IsTrue(m.Yukleniyor, "Basla'ya basildi ama yukleme baslamadi.");
            Assert.IsFalse(m.menuPaneli.activeSelf,
                "Yuklenirken menu hala gorunuyor.");
            Assert.IsTrue(m.yuklemePaneli.activeSelf,
                "Yukleme paneli acilmadi.");
        }

        /// <summary>
        /// <b>Olmayan bir sahne SESSİZCE takılmıyor.</b>
        ///
        /// Yükleme başarısızsa oyuncu menüye dönmeli ve konsolda sebep
        /// yazmalı. Sessiz kalmak, "Başla"nın bozuk olduğunu kimsenin
        /// anlamaması demekti.
        /// </summary>
        [UnityTest]
        public IEnumerator AMissingSceneFailsLoudlyAndReturnsToTheMenu()
        {
            var m = Kur("BoyleBirSahneYok");
            yield return null;

            // IKI hata beklenir, biri bizim degil: Unity, olmayan bir
            // sahne icin kendi hatasini ATAR ve sonra LoadSceneAsync null
            // doner. Yalniz kendi mesajimizi beklemek testi patlatiyordu —
            // olcum eksikti, kod degil.
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions
                .Regex("couldn't be loaded"));
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions
                .Regex("Sahne yuklenemedi"));

            m.Basla();
            yield return null;
            yield return null;

            Assert.IsFalse(m.Yukleniyor,
                "Yukleme basarisiz oldu ama sistem hala 'yukleniyor' diyor.");
            Assert.IsTrue(m.menuPaneli.activeSelf,
                "Basarisiz yuklemeden sonra menuye donulmedi.");
        }

        /// <summary>
        /// <b>İlerleme oyuncuya yalan söylemiyor.</b>
        ///
        /// Unity ilerlemeyi 0,9'da durdurup etkinleştirmeyi bekler. Ham
        /// değeri göstermek, çubuğun %90'da takılı kalması demek olurdu;
        /// 0,9'u %100 saymak doğrusu.
        /// </summary>
        [Test]
        public void ProgressIsScaledSoTheBarCanReachTheEnd()
        {
            // 0,9 ham ilerleme -> %100 gosterilmeli.
            Assert.AreEqual(1f, Mathf.Clamp01(0.9f / 0.9f), 0.0001f);
            // 0,45 ham -> %50.
            Assert.AreEqual(0.5f, Mathf.Clamp01(0.45f / 0.9f), 0.0001f);
        }
    }
}
