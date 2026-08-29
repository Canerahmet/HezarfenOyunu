using Hezarfen.Arayuz;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Hezarfen.Tests
{
    /// <summary>
    /// <b>Menü düğmeleri gerçekten bağlı mı — SAHNEDE.</b>
    ///
    /// Bu dosya bir hatanın üstüne yazıldı. Açılış sahnesi koddan
    /// kuruluyordu ve düğmeler <c>onClick.AddListener(...)</c> ile
    /// bağlanmıştı. O çağrı bir <b>çalışma zamanı</b> dinleyicisi ekler:
    /// kurulum anında her şey doğru görünür, sahne kaydedilirken dinleyici
    /// <b>serilestirilmez</b>, ve sahne bir daha açıldığında dört düğme de
    /// boşa basar. Build'de menü kusursuz açıldı, hiçbir düğme çalışmadı.
    ///
    /// O sırada 382 test yeşildi. Hiçbiri düğmeye basmıyordu — panelleri
    /// koddan çağırıp "menü çalışıyor" demiştim. <b>Ölçülmeyen yol,
    /// olmayan yoldur.</b>
    ///
    /// Testler sahneyi <b>diskten</b> açar: bellekteki kurulum değil,
    /// oyuncunun eline geçen dosya ölçülür.
    /// </summary>
    public class AcilisMenusuTests
    {
        private static readonly string[] MenuDugmeleri =
            { "BaslaDugme", "AyarDugme", "KrediDugme", "CikDugme" };

        private const string Yol =
            "Assets/_Project/Scenes/Acilis.unity";

        private UnityEngine.SceneManagement.Scene _sahne;

        [SetUp]
        public void Ac()
        {
            Assert.IsTrue(System.IO.File.Exists(Yol),
                "Acilis sahnesi yok — Hezarfen -> Boru Hatti -> "
                + "Acilis sahnesini kur.");
            _sahne = EditorSceneManager.OpenScene(
                Yol, OpenSceneMode.Single);
        }

        private static Button Bul(string ad)
        {
            foreach (var b in Object.FindObjectsByType<Button>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
                if (b.name == ad) return b;
            return null;
        }

        /// <summary>
        /// <b>Her düğmenin kalıcı dinleyicisi var.</b> Kalıcı olmayan
        /// dinleyici sahneye yazılmaz; sayı sıfırsa düğme build'de ölüdür.
        /// </summary>
        [Test]
        public void EveryMenuButtonHasAPersistentListener()
        {
            foreach (string ad in MenuDugmeleri)
            {
                var d = Bul(ad);
                Assert.IsNotNull(d, $"'{ad}' sahnede yok.");
                Assert.Greater(d.onClick.GetPersistentEventCount(), 0,
                    $"'{ad}' dugmesinin KALICI dinleyicisi yok. "
                    + "AddListener ile baglanmis olabilir — o cagri "
                    + "sahneye YAZILMAZ ve dugme build'de bosa basar.");
            }
        }

        /// <summary>
        /// <b>Dinleyicinin hedefi ve metodu çözülüyor.</b> Sayının sıfırdan
        /// büyük olması yetmez: hedef silinmişse ya da metot yeniden
        /// adlandırılmışsa dinleyici <b>sessizce</b> hiçbir şey yapmaz.
        /// </summary>
        [Test]
        public void EveryListenerStillPointsAtAMethodThatExists()
        {
            foreach (string ad in MenuDugmeleri)
            {
                var d = Bul(ad);
                for (int i = 0; i < d.onClick.GetPersistentEventCount(); i++)
                {
                    var hedef = d.onClick.GetPersistentTarget(i);
                    string metot = d.onClick.GetPersistentMethodName(i);
                    Assert.IsNotNull(hedef,
                        $"'{ad}' dinleyicisinin hedefi YOK.");
                    Assert.IsNotEmpty(metot,
                        $"'{ad}' dinleyicisinin metot adi bos.");
                    Assert.IsNotNull(
                        hedef.GetType().GetMethod(metot),
                        $"'{ad}' -> {hedef.GetType().Name}.{metot}() "
                        + "diye bir metot YOK.");
                }
            }
        }

        /// <summary>
        /// <b>Başla düğmesi gerçekten Başla'yı çağırıyor.</b> Dört düğmenin
        /// dördü de bağlı olup yanlış metoda gitseydi yukarıdaki testler
        /// yeşil kalırdı.
        /// </summary>
        [Test]
        public void TheStartButtonCallsStart()
        {
            var d = Bul("BaslaDugme");
            Assert.AreEqual("Basla", d.onClick.GetPersistentMethodName(0));
            Assert.IsInstanceOf<AcilisMenusu>(
                d.onClick.GetPersistentTarget(0));
        }

        /// <summary>
        /// <b>Klavye için seçili bir nesne var.</b>
        ///
        /// Submit eylemi EventSystem'in seçili nesnesine gider. Seçili
        /// nesne yoksa Enter'a basmak hiçbir yere basmaz — bu, kullanıcının
        /// "tuşlara bastım, giremedim" cümlesinin ikinci yarısıydı.
        /// </summary>
        [Test]
        public void TheKeyboardHasSomethingSelectedToPress()
        {
            var es = Object.FindAnyObjectByType<EventSystem>();
            Assert.IsNotNull(es, "Sahnede EventSystem yok.");
            Assert.IsNotNull(es.firstSelectedGameObject,
                "firstSelectedGameObject atanmamis — klavye ile menude "
                + "hicbir sey secili olmaz ve Enter bosa gider.");
            Assert.AreEqual("BaslaDugme",
                es.firstSelectedGameObject.name);
        }

        /// <summary>
        /// <b>Girdi modülü Input System'in modülü.</b> UGUI'nin varsayılan
        /// <c>StandaloneInputModule</c>'ü eski <c>Input</c> sınıfını okur ve
        /// bu projede çalışma zamanında istisna atar.
        /// </summary>
        [Test]
        public void TheInputModuleIsTheOneThisProjectCanUse()
        {
            var es = Object.FindAnyObjectByType<EventSystem>();
            Assert.IsNotNull(
                es.GetComponent<
                    UnityEngine.InputSystem.UI.InputSystemUIInputModule>(),
                "InputSystemUIInputModule yok.");
            Assert.IsNull(es.GetComponent<StandaloneInputModule>(),
                "StandaloneInputModule sahnede — eski Input sinifini okur "
                + "ve bu projede istisna atar.");
        }

        /// <summary>Canvas tıklamayı alabiliyor mu.</summary>
        [Test]
        public void TheCanvasCanReceiveClicksAtAll()
        {
            var c = Object.FindAnyObjectByType<Canvas>();
            Assert.IsNotNull(c, "Canvas yok.");
            Assert.IsNotNull(c.GetComponent<GraphicRaycaster>(),
                "GraphicRaycaster yok — hicbir tiklama UI'ya ulasmaz.");
            foreach (string ad in MenuDugmeleri)
            {
                var d = Bul(ad);
                Assert.IsTrue(d.interactable, $"'{ad}' interactable degil.");
                Assert.IsNotNull(d.targetGraphic,
                    $"'{ad}' hedef grafigi yok — isaretlenemez.");
                Assert.IsTrue(d.targetGraphic.raycastTarget,
                    $"'{ad}' raycastTarget kapali — tiklama gecmez.");
            }
        }
    }
}
