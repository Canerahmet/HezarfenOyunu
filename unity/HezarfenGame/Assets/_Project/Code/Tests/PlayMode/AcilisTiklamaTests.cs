using System.Collections;
using Hezarfen.Arayuz;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Hezarfen.Tests
{
    /// <summary>
    /// <b>Düğmeye BASILIYOR — çalışırken, gerçek sahnede.</b>
    ///
    /// Menüyü daha önce "doğrulamıştım": panelleri koddan çağırıp ekran
    /// görüntüsüne bakmıştım. Bu, tıklama yolunun hiçbir adımına
    /// dokunmuyordu — ne dinleyiciye, ne EventSystem'e, ne seçime. Sonuç:
    /// build'de menü açıldı ve hiçbir düğme çalışmadı.
    ///
    /// Burada olay <b>EventSystem üzerinden</b> gönderiliyor; yani
    /// oyuncunun bastığı yolun kendisi ölçülüyor. Yükleme başlatmayan
    /// "Krediler" düğmesi seçildi: test bir sahne yüklemesini tetiklemeden
    /// aynı zinciri kanıtlıyor.
    /// </summary>
    public class AcilisTiklamaTests
    {
        [UnitySetUp]
        public IEnumerator Ac()
        {
            SceneManager.LoadScene("Acilis");
            yield return null;
            yield return null;
        }

        private static GameObject Bul(string ad)
        {
            foreach (var b in Object.FindObjectsByType<Button>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
                if (b.name == ad) return b.gameObject;
            return null;
        }

        private static void Bas(GameObject dugme)
        {
            var veri = new BaseEventData(EventSystem.current);
            ExecuteEvents.Execute(dugme, veri, ExecuteEvents.submitHandler);
        }

        /// <summary>
        /// <b>"Krediler"e basmak kredi panelini açar.</b> Dinleyici
        /// kaydedilmemişse bu test kırmızı olur — yeşil kaldığı sürece
        /// düğmeler build'de de çalışır.
        /// </summary>
        [UnityTest]
        public IEnumerator PressingCreditsActuallyOpensCredits()
        {
            var m = Object.FindAnyObjectByType<AcilisMenusu>();
            Assert.IsNotNull(m, "Acilis sahnesinde AcilisMenusu yok.");
            Assert.AreEqual("MenuPaneli", m.AcikPanel.name,
                "Acilista menu paneli acik olmali.");

            Bas(Bul("KrediDugme"));
            yield return null;

            Assert.AreEqual("KrediPaneli", m.AcikPanel.name,
                "Krediler dugmesine basildi, panel DEGISMEDI — kalici "
                + "dinleyici yok demektir.");
        }

        /// <summary>
        /// <b>"Geri" menüye döndürür</b> — gidiş yolu kadar dönüş yolu da
        /// bağlı olmalı; oyuncuyu kredi ekranında kilitlemek de bir kusur.
        /// </summary>
        [UnityTest]
        public IEnumerator TheWayBackIsWiredToo()
        {
            var m = Object.FindAnyObjectByType<AcilisMenusu>();
            Bas(Bul("KrediDugme"));
            yield return null;
            Bas(Bul("KrediGeri"));
            yield return null;

            Assert.AreEqual("MenuPaneli", m.AcikPanel.name,
                "Geri dugmesi menuye dondurmedi.");
        }

        /// <summary>
        /// <b>Klavye boşa basmıyor.</b> Açılışta seçili bir nesne var ve
        /// panel değişince seçim yeni panele geçiyor. Seçim düşerse
        /// <c>Submit</c> hiçbir yere gitmez ve menü klavyeyle ölür.
        /// </summary>
        [UnityTest]
        public IEnumerator TheKeyboardAlwaysHasSomethingSelected()
        {
            var m = Object.FindAnyObjectByType<AcilisMenusu>();
            yield return null;

            Assert.IsNotNull(EventSystem.current.currentSelectedGameObject,
                "Acilista secili nesne YOK — Enter bosa basar.");
            Assert.AreEqual("BaslaDugme",
                EventSystem.current.currentSelectedGameObject.name);

            Bas(Bul("AyarDugme"));
            yield return null;
            var s = EventSystem.current.currentSelectedGameObject;
            Assert.IsNotNull(s, "Panel degisince secim dustu.");
            Assert.IsTrue(s.activeInHierarchy,
                "Secili nesne KAPALI bir panelde — tuslar bosa gider.");
            Assert.AreSame(m.AcikPanel, s.transform.parent.gameObject,
                "Secim acik panelde degil.");

            // Secimi elle dusur: guvenlik agi geri koymali.
            EventSystem.current.SetSelectedGameObject(null);
            yield return null;
            yield return null;
            Assert.IsNotNull(EventSystem.current.currentSelectedGameObject,
                "Secim dustu ve geri konmadi.");
        }

        /// <summary>
        /// <b>Enter tuşunun bastığı şey "Başla".</b> Oyuncunun menüde
        /// yapacağı ilk hareket budur ve hiçbir şey öğrenmeden
        /// yapabilmelidir.
        /// </summary>
        [UnityTest]
        public IEnumerator SubmitOnTheDefaultSelectionStartsTheGame()
        {
            var m = Object.FindAnyObjectByType<AcilisMenusu>();
            yield return null;

            // Hedef sahne KUCUK olani: Faz1_Terrain'i baslatmak testin
            // omrunden uzun surer ve yuklemesi SONRAKI teste tasar —
            // bir kez tam olarak bu yuzden kirilgan bir test yazmistim.
            m.sehirSahnesi = "Acilis";

            Bas(EventSystem.current.currentSelectedGameObject);

            // Basla() yuklemeyi ilk yield'den once baslatir; olcum
            // burada, hic kare gecirmeden yapiliyor.
            Assert.IsTrue(m.Yukleniyor,
                "Varsayilan secime Submit gonderildi, yukleme BASLAMADI.");
            Assert.AreEqual("YuklemePaneli", m.AcikPanel.name,
                "Yukleme paneli acilmadi.");

            // Sahne degisimi bitene kadar bekle: yarim kalmis bir yukleme
            // sonraki testin icinde tamamlanirsa oradaki hata BURADAN
            // gelir ve orada aranir.
            yield return null;
            yield return null;
            yield return null;
        }
    }
}
