using System.Collections;
using Hezarfen.Flight;
using Hezarfen.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Hezarfen.Tests
{
    /// <summary>
    /// <b>Zincir gerçekten kesintisiz mi?</b>
    ///
    /// Faz 5'in kabul ölçütü *"kesintisiz animasyonlarla oynanabiliyor"*
    /// diyor. Kurulumun doğru olduğunu EditMode testleri söylüyor — kemik
    /// var, klip bağlı, eşikler tutuyor. Ama "kurulum doğru" ile "çalışıyor"
    /// aynı şey değil ve aradaki farkı ancak <b>çalıştırmak</b> gösterir.
    ///
    /// Burada asıl sınanan şey animasyon değil, iki fizik dünyası
    /// arasındaki dikiş: yerde <see cref="CharacterController"/>, havada
    /// <see cref="Rigidbody"/>, ve ikisinin aynı anda açık olamaması. Bu
    /// sessiz bir hata sınıfıdır — açık kalan bir kapsül Rigidbody'nin
    /// uyguladığı her kuvveti yutar ve oyun "uçuyorum ama düşmüyorum"
    /// diye görünür.
    ///
    /// Sahne yüklemiyoruz: dizinin kendisi sahneden bağımsızdır ve
    /// bileşenler burada elle kuruluyor. Sahneye bağlı bir test, sahne
    /// değişince kırılır ve kırıldığında diziyle ilgisi olmayan bir şey
    /// söyler.
    /// </summary>
    public class UcusDizisiTests
    {
        private GameObject _kok;

        private UcusDizisi Kur()
        {
            _kok = new GameObject("TestOyuncu");
            _kok.transform.position = new Vector3(0f, 50f, 0f);

            var cc = _kok.AddComponent<CharacterController>();
            cc.height = 1.70f;
            cc.radius = 0.30f;
            cc.center = new Vector3(0f, 0.85f, 0f);

            var rb = _kok.AddComponent<Rigidbody>();
            rb.mass = 78f;
            rb.isKinematic = true;
            rb.useGravity = false;

            var glide = _kok.AddComponent<GlideController>();
            glide.enabled = false;

            var dizi = _kok.AddComponent<UcusDizisi>();
            dizi.kapsul = cc;
            dizi.govde = rb;
            dizi.suzulme = glide;
            dizi.kusanmaSuresi = 0.20f;   // testte kısa
            dizi.inisSuresi = 0.20f;
            return dizi;
        }

        [TearDown]
        public void Temizle()
        {
            if (_kok != null) Object.DestroyImmediate(_kok);
        }

        /// <summary>Başlangıçta yerde ve yürüme fiziği açık.</summary>
        [UnityTest]
        public IEnumerator ItStartsOnTheGroundWithWalkingPhysics()
        {
            var d = Kur();
            yield return null;

            Assert.AreEqual(UcusDizisi.Durum.Yerde, d.Simdiki);
            Assert.IsTrue(d.kapsul.enabled, "Yerde kapsul acik olmali.");
            Assert.IsTrue(d.govde.isKinematic,
                "Yerde Rigidbody kinematik olmali — degilse karakter "
                + "kendi agirligiyla zemine gomulur.");
            Assert.IsFalse(d.suzulme.enabled, "Yerde suzulme kapali olmali.");
        }

        /// <summary>
        /// Kuşanma bir SÜREDİR: bittiği ana kadar atlanamaz.
        ///
        /// Süreyi yok sayan bir tasarım oyuncuyu kanat sırtındayken
        /// uçururdu — ve bu, animasyonun bittiğini kimsenin
        /// beklemediği yerde ortaya çıkar.
        /// </summary>
        [UnityTest]
        public IEnumerator DonningTakesTimeAndBlocksTheJump()
        {
            var d = Kur();
            yield return null;

            d.Kusan();
            Assert.AreEqual(UcusDizisi.Durum.Kusaniyor, d.Simdiki);

            // Kusanma bitmeden atlamak ISLEMEZ.
            d.Atla();
            Assert.AreEqual(UcusDizisi.Durum.Kusaniyor, d.Simdiki,
                "Kusanma bitmeden atlanabiliyor — kanat sirttayken ucar.");
            Assert.IsTrue(d.kapsul.enabled,
                "Kusanma sirasinda hala yerde olmali.");

            yield return new WaitForSeconds(0.35f);
            Assert.AreEqual(UcusDizisi.Durum.Hazir, d.Simdiki,
                "Kusanma suresi doldu ama durum Hazir'a gecmedi.");
        }

        /// <summary>
        /// Atlayınca fizik el değiştirir — <b>ikisi birden açık kalmaz</b>.
        ///
        /// Bu testin varlık sebebi tam olarak bu: `CharacterController`
        /// her karede konumu kendisi yazar ve acik kalirsa Rigidbody'nin
        /// kuvvetlerini sessizce yutar.
        /// </summary>
        [UnityTest]
        public IEnumerator JumpingHandsPhysicsOverExactlyOnce()
        {
            var d = Kur();
            yield return null;

            d.Kusan();
            yield return new WaitForSeconds(0.35f);
            d.Atla();
            yield return null;

            Assert.AreEqual(UcusDizisi.Durum.Ucuyor, d.Simdiki);
            Assert.IsFalse(d.kapsul.enabled,
                "Havada kapsul ACIK kalmis — Rigidbody'nin uyguladigi her "
                + "kuvveti yutar ve karakter ucar gibi gorunup dusmez.");
            Assert.IsFalse(d.govde.isKinematic,
                "Havada Rigidbody kinematik kalmis — kuvvet almaz.");
            Assert.IsTrue(d.govde.useGravity, "Havada yercekimi kapali.");
            Assert.IsTrue(d.suzulme.enabled, "Havada suzulme kapali.");
        }

        /// <summary>
        /// Yere değince fizik geri döner ve zincir başa gelir.
        ///
        /// "Kesintisiz" olmanın ölçüsü bu: dizi bir tur atıp aynı yere
        /// dönebiliyorsa oyuncu ikinci kez uçabilir. Tek yönlü bir zincir
        /// ilk inişte oyunu bitirirdi.
        /// </summary>
        [UnityTest]
        public IEnumerator TheChainReturnsSoYouCanFlyAgain()
        {
            // Zemin: temas isini bir seye carpmalı.
            var zemin = GameObject.CreatePrimitive(PrimitiveType.Plane);
            zemin.transform.position = new Vector3(0f, 49.5f, 0f);
            zemin.transform.localScale = Vector3.one * 5f;

            var d = Kur();
            yield return null;

            d.Kusan();
            yield return new WaitForSeconds(0.35f);
            d.Atla();
            yield return null;
            Assert.AreEqual(UcusDizisi.Durum.Ucuyor, d.Simdiki);

            // Dusup zemine degsin.
            float t = 0f;
            while (d.Simdiki == UcusDizisi.Durum.Ucuyor && t < 4f)
            {
                t += Time.deltaTime;
                yield return null;
            }
            Assert.AreNotEqual(UcusDizisi.Durum.Ucuyor, d.Simdiki,
                "Dort saniyede yere degmedi — temas isini calismiyor.");

            yield return new WaitForSeconds(0.35f);
            Assert.AreEqual(UcusDizisi.Durum.Yerde, d.Simdiki,
                "Inis bitti ama zincir basa donmedi; ikinci ucus yapilamaz.");
            Assert.IsTrue(d.kapsul.enabled, "Inince kapsul geri acilmali.");
            Assert.IsTrue(d.govde.isKinematic,
                "Inince Rigidbody kinematige donmeli.");

            Object.DestroyImmediate(zemin);
        }

        /// <summary>
        /// Sert çarpma çakılmadır, iniş değil.
        ///
        /// Ayrımı yapan şey dikey hızdır ve eşik `cakilmaHizi` alanında
        /// yazılıdır. Ayrım olmasaydı oyuncu kuleden düşüp ayağa kalkardı
        /// ve uçuşun tek gerçek riski ortadan kalkardı.
        /// </summary>
        [UnityTest]
        public IEnumerator AHardImpactIsACrashNotALanding()
        {
            var zemin = GameObject.CreatePrimitive(PrimitiveType.Plane);
            zemin.transform.position = new Vector3(0f, 49.5f, 0f);
            zemin.transform.localScale = Vector3.one * 5f;

            var d = Kur();
            d.cakilmaHizi = -0.5f;        // her temas sert sayilsin
            yield return null;

            d.Kusan();
            yield return new WaitForSeconds(0.35f);
            d.Atla();
            yield return null;

            float t = 0f;
            while (d.Simdiki == UcusDizisi.Durum.Ucuyor && t < 4f)
            {
                t += Time.deltaTime;
                yield return null;
            }
            Assert.AreEqual(UcusDizisi.Durum.Cakildi, d.Simdiki,
                "Sert carpma inis sayildi — ucusun tek riski yok olur.");

            Object.DestroyImmediate(zemin);
        }
    }
}
