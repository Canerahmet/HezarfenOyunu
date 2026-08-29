using System.Collections;
using Hezarfen.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Hezarfen.Tests
{
    /// <summary>
    /// <b>Kamera kipi gerçekten yer değiştiriyor mu.</b>
    ///
    /// Caner (2026-08-29, oynarken): *"oyunun kamera acisini degistirmeye
    /// izin versin. karakterin gozlerinden veya gta rdr ac gibi karakterin
    /// ustunden bir kamera olsun."*
    ///
    /// Bu dosya bir dersin üstüne yazıldı: menü düğmeleri de "çalışıyor"
    /// görünüyordu ve build'de hiçbiri çalışmıyordu, çünkü doğrulama
    /// oyuncunun geçtiği yoldan geçmiyordu. Burada ölçülen şey alanların
    /// değeri değil, <b>kameranın nereye gittiği</b>.
    /// </summary>
    public class KameraKipiTests
    {
        private GameObject _kok;
        private KameraKipi _kip;
        private Camera _kam;

        [SetUp]
        public void Kur()
        {
            _kok = new GameObject("oyuncu");
            _kok.AddComponent<CharacterController>();
            _kok.AddComponent<WalkController>();
            _kip = _kok.AddComponent<KameraKipi>();

            var kamGo = new GameObject("kam");
            kamGo.transform.SetParent(_kok.transform, false);
            _kam = kamGo.AddComponent<Camera>();
        }

        [TearDown]
        public void Temizle()
        {
            if (_kok != null) Object.DestroyImmediate(_kok);
        }

        /// <summary>
        /// <b>Birinci şahısta kamera GÖZDEDİR.</b> Göz yüksekliği
        /// karakter modelinden türetilmiş bir sayı (1,59 m) ve kameranın
        /// oraya gitmesi bütün inceleme paketlerinin kadrajıyla aynı
        /// yükseklikten bakmak demek.
        /// </summary>
        [UnityTest]
        public IEnumerator FirstPersonPutsTheCameraAtTheEye()
        {
            _kip.Kip(Bakis.BirinciSahis);
            yield return null;

            var yurume = _kok.GetComponent<WalkController>();
            Assert.AreEqual(yurume.eyeHeight,
                _kam.transform.localPosition.y, 0.01f,
                "Birinci sahista kamera goz yuksekliginde degil.");
            Assert.AreEqual(0f, _kam.transform.localPosition.z, 0.01f,
                "Birinci sahista kamera geride duruyor.");
        }

        /// <summary>
        /// <b>Üçüncü şahısta kamera GERİDEDİR.</b> Kipi değiştirmenin
        /// hiçbir şeyi değiştirmemesi de olabilirdi — alanı yazıp
        /// kameranın yerinde kalması tam olarak menü düğmelerinin
        /// başına gelen şeydi.
        /// </summary>
        [UnityTest]
        public IEnumerator ThirdPersonPullsTheCameraBehindTheCharacter()
        {
            _kip.Kip(Bakis.UcuncuSahis);
            yield return null;

            var geri = _kok.transform.position - _kam.transform.position;
            float arka = Vector3.Dot(geri, _kok.transform.forward);
            Assert.Greater(arka, 1.0f,
                "Ucuncu sahista kamera karakterin arkasinda degil.");
            Assert.Greater(_kam.transform.position.y,
                           _kok.transform.position.y + 0.5f,
                "Ucuncu sahista kamera omuz hizasinin altinda.");
        }

        /// <summary>
        /// <b>İki kip birbirinden farklı yer veriyor.</b> Tek başına
        /// "arkada" ve "gözde" testleri, iki kipin aynı noktaya
        /// bakması hâlinde bile geçebilirdi.
        /// </summary>
        [UnityTest]
        public IEnumerator TheTwoModesActuallyDiffer()
        {
            _kip.Kip(Bakis.BirinciSahis);
            yield return null;
            var goz = _kam.transform.position;

            _kip.Kip(Bakis.UcuncuSahis);
            yield return null;
            var omuz = _kam.transform.position;

            Assert.Greater(Vector3.Distance(goz, omuz), 1.0f,
                "Kip degisti, kamera ayni yerde kaldi.");
        }

        /// <summary>
        /// <b>Değiştir() gidiş-dönüş yapıyor.</b> Oyuncu V'ye iki kez
        /// basınca başladığı yere dönmeli.
        /// </summary>
        [UnityTest]
        public IEnumerator TogglingTwiceReturnsToTheStartingMode()
        {
            var bas = _kip.kip;
            _kip.Degistir();
            Assert.AreNotEqual(bas, _kip.kip, "Degistir() kipi degistirmedi.");
            _kip.Degistir();
            Assert.AreEqual(bas, _kip.kip, "Iki kez degistirince donmedi.");
            yield return null;
        }

        /// <summary>
        /// <b>Boom engele girmiyor.</b> Kameranın arkasına bir duvar
        /// konur; kol kısalmalı. Bu olmazsa dar sokakta oyuncu evin
        /// arkasını görür.
        /// </summary>
        [UnityTest]
        public IEnumerator TheBoomShortensWhenAWallIsBehind()
        {
            _kip.Kip(Bakis.UcuncuSahis);
            _kip.yumusatma = 0f;          // olcum aninda, gecikmesiz
            yield return null;
            float serbest = _kip.SonMesafe;

            var duvar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            duvar.transform.localScale = new Vector3(10f, 10f, 0.5f);
            duvar.transform.position = _kok.transform.position
                                       + Vector3.up * 1.45f
                                       - _kok.transform.forward * 1.6f;
            Physics.SyncTransforms();
            yield return null;
            yield return null;

            Assert.Less(_kip.SonMesafe, serbest - 0.3f,
                $"Arkada duvar var ama kol kisalmadi ({_kip.SonMesafe:0.00} m).");

            Object.DestroyImmediate(duvar);
        }

        /// <summary>
        /// <b>Yürüme hızı Caner'in istediği gibi arttı</b> ve hâlâ bir
        /// insan hızı. Sınırsız bırakmak şehri küçültürdü.
        /// </summary>
        [Test]
        public void TheWalkSpeedIsFasterButStillHuman()
        {
            var w = _kok.GetComponent<WalkController>();
            Assert.Greater(w.walkSpeed, 1.4f,
                "Hiz artmamis — Caner 'biraz yavas' demisti.");
            Assert.LessOrEqual(w.walkSpeed, 3.0f,
                "Yuruyus insan hizini asti; sehir kuculur.");
            Assert.Greater(w.runSpeed, w.walkSpeed,
                "Kosu yuruyusten yavas.");
        }
    }
}
