using System.Collections;
using Hezarfen.Flight;
using Hezarfen.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Hezarfen.Tests
{
    /// <summary>
    /// <b>Perde 2 dikey dilimi baştan sona oynanabiliyor mu?</b>
    ///
    /// Faz 6 kabul ölçütünün son maddesi: *"Perde 2 dikey dilimi (talim →
    /// kule → uçuş → iniş → tepki sahnesi) baştan sona oynanabilir."*
    ///
    /// "Oynanabilir" bir izlenim değil bir <b>zincir</b>: her aşamanın
    /// ölçülebilir bir bitiş koşulu var ve zincirin hiçbir yerinde
    /// kopukluk olmamalı. Kopukluk sessizdir — oyuncu bir aşamada takılır
    /// ve neden takıldığını anlamaz.
    ///
    /// İniş <b>gerçek temasla</b> ölçülüyor, bir "indi" çağrısıyla değil:
    /// sahte bir iniş, iniş mekaniğinin bozuk olduğunu göremezdi.
    /// </summary>
    public class Perde2DilimiTests
    {
        private GameObject _kok;
        private GameObject _zemin;

        /// <summary>Süzülüşün başladığı yükseklik (m) — yerde değil.</summary>
        private const float KalkisYuksekligi = 25f;

        private (Perde2Dilimi dilim, UcusDizisi dizi) Kur()
        {
            _zemin = GameObject.CreatePrimitive(PrimitiveType.Plane);
            _zemin.transform.localScale = Vector3.one * 40f;

            _kok = new GameObject("TestOyuncu");

            var cc = _kok.AddComponent<CharacterController>();
            cc.height = 1.70f; cc.radius = 0.30f;
            cc.center = new Vector3(0f, 0.85f, 0f);

            var rb = _kok.AddComponent<Rigidbody>();
            rb.mass = 78f; rb.isKinematic = true; rb.useGravity = false;

            var glide = _kok.AddComponent<GlideController>();
            glide.enabled = false;

            var dizi = _kok.AddComponent<UcusDizisi>();
            dizi.kapsul = cc; dizi.govde = rb; dizi.suzulme = glide;
            dizi.kusanmaSuresi = 0.02f;
            dizi.inisSuresi = 0.02f;
            // Varsayilan olarak hicbir temas SERT degil; cakilma testi
            // bunu kendisi geri aciyor.
            dizi.cakilmaHizi = -500f;

            var dilim = _kok.AddComponent<Perde2Dilimi>();
            dilim.dizi = dizi;
            dilim.oyuncu = _kok.transform;

            Koy(dilim.okmeydani);
            return (dilim, dizi);
        }

        /// <summary>Oyuncuyu ve zemini bir yere taşır.</summary>
        private void Koy(Vector3 nereye, float yukseklik = 0.05f)
        {
            _zemin.transform.position = nereye;
            _kok.transform.position = nereye + Vector3.up * yukseklik;
        }

        [TearDown]
        public void Temizle()
        {
            if (_kok != null) Object.DestroyImmediate(_kok);
            if (_zemin != null) Object.DestroyImmediate(_zemin);
        }

        /// <summary>Kuşan → atla → taşı → in. Gerçek temasla.</summary>
        private IEnumerator Suzul(UcusDizisi dizi, Vector3 nereden,
                                  Vector3 nereye)
        {
            // KALKIS YUKSEKLIGI.
            //
            // Ilk yazimda oyuncu zeminden 5 cm yukaridaydi ve dort test
            // birden "atlandi ama ucus baslamadi" dedi: temas isini
            // govdenin 20 cm ustunden 35 cm asagi bakiyor, yani 5 cm
            // yukarida atlayan ayni karede yere degiyor. Kuleden atlayan
            // adam yerde degildir; olcum de oyle kurulmali.
            Koy(nereden, KalkisYuksekligi);
            yield return null;

            dizi.Kusan();
            float t = 0f;
            while (dizi.Simdiki != UcusDizisi.Durum.Hazir && t < 2f)
            { t += Time.deltaTime; yield return null; }
            Assert.AreEqual(UcusDizisi.Durum.Hazir, dizi.Simdiki,
                "Kusanma bitmedi.");

            dizi.Atla();
            yield return null;
            Assert.AreEqual(UcusDizisi.Durum.Ucuyor, dizi.Simdiki,
                "Atlandi ama ucus baslamadi.");

            // Zemini hedefe tasi, oyuncuyu uzerine birak: temas gercek.
            Koy(nereye, 0.15f);
            t = 0f;
            while (dizi.Simdiki == UcusDizisi.Durum.Ucuyor && t < 3f)
            { t += Time.deltaTime; yield return null; }
            Assert.AreNotEqual(UcusDizisi.Durum.Ucuyor, dizi.Simdiki,
                "Yere degdi ama ucus bitmedi.");

            // Inis/cakilma animasyonu bitsin.
            t = 0f;
            while (dizi.Simdiki != UcusDizisi.Durum.Yerde && t < 2f)
            { t += Time.deltaTime; yield return null; }
        }

        /// <summary>
        /// <b>Zincir baştan sona yürüyor.</b>
        ///
        /// Talim Okmeydanı'nda, kalkış Galata Kulesi'nden, iniş
        /// Doğancılar'a, tepki İncili Köşk'te — dördü de katalogdan gelen
        /// belgeli yerler.
        /// </summary>
        [UnityTest]
        public IEnumerator TheWholeSliceCanBePlayedThrough()
        {
            var (d, dizi) = Kur();
            Assert.AreEqual(Perde2Dilimi.Asama.Talim, d.Simdiki);

            // --- TALIM: Okmeydani'nda uc suzulus --------------------
            //
            // MESAFE ARTIK SABIT DEGIL, ARTAN BIR MERDIVEN.
            //
            // Once ucunde de 90 m suzuluyordu cunku esik 3×60 m'ydi.
            // O esik olculdu ve duz zeminden **erisilemiyordu**:
            // kalkis 12,41 m/s, kanat 11,25:1, yani duz zeminden bir
            // suzulus ~22 m. Yani oyuncu ilk uc denemesinde de "hayir"
            // aliyor ve sebebini hic ogrenmiyordu.
            //
            // Esik artik 30/60/120 ve test onu KODDAN okuyor: sabiti
            // burada tekrar yazmak, bir sayinin iki sahibi olmasi
            // demekti ve bu depoda o kusur uc kez cikti.
            for (int i = 0; i < d.talimHedefi; i++)
            {
                float gerek = d.TalimEsigi(i) + 30f;   // esigin uzerinde
                yield return Suzul(dizi, d.okmeydani,
                                   d.okmeydani + new Vector3(gerek, 0f, 0f));
            }

            Assert.AreEqual(d.talimHedefi, d.TalimSayisi,
                $"{d.TalimSayisi} talim sayildi, {d.talimHedefi} bekleniyordu.");

            // Sayac dolar, asama BIR SONRAKI karede gecer: `TalimiIzle`
            // inisi gordugu karede sayar, gecis kontrolu ondan sonra
            // kosar. Oyunda farkedilmez, olcumde beklenmeli — bu
            // dosyanin inis dalinda ayni not zaten yazili.
            for (int k = 0; k < 3; k++) yield return null;
            Assert.AreEqual(Perde2Dilimi.Asama.Kule, d.Simdiki,
                "Talim bitti ama dilim kuleye gecmedi.");

            // --- KULE: kalkis ---------------------------------------
            //
            // SEREFE KOTUNDAN KALKILIR, KULE DIBINDEN DEGIL.
            //
            // Once 25 m'den kalkiliyordu ve gecerdi, cunku asama
            // yalniz YATAY yakinlik soruyordu. Bedeli olculdu: oyuncu
            // kule DIBINDE G ve Space'e basinca "kalkis" sayiliyor,
            // bir saniye sonra yere deginca `Ucus -> Inis` oluyor ve
            // 3.336 m'lik final iki vapur biletiyle geciliyordu.
            //
            // Sart artik irtifa; test de onu kullanir.
            Koy(d.kule, Perde2Dilimi.KalkisKotu + 5f);
            yield return null;
            dizi.Kusan();
            float t = 0f;
            while (dizi.Simdiki != UcusDizisi.Durum.Hazir && t < 2f)
            { t += Time.deltaTime; yield return null; }
            dizi.Atla();
            yield return null;
            Assert.AreEqual(Perde2Dilimi.Asama.Ucus, d.Simdiki,
                "Kuleden atlandi ama ucus asamasi baslamadi.");

            // --- UCUS: Bogaz'i gec, Dogancilar'a in -----------------
            Koy(d.dogancilar, 0.15f);
            t = 0f;
            while (dizi.Simdiki == UcusDizisi.Durum.Ucuyor && t < 3f)
            { t += Time.deltaTime; yield return null; }
            // Inis ile asamanin ilerlemesi arasinda bir kare var: dizi
            // temasi bulur, dilim onu BIR SONRAKI karede gorur. Oyunda
            // farkedilmez, olcumde beklenmeli.
            yield return null;

            Assert.Greater(d.UcusMesafesi, 3000f,
                $"Ucus {d.UcusMesafesi:F0} m olculdu; Galata Kulesi ile "
                + "Dogancilar arasi ~3358 m (RESEARCH 3).");
            Assert.IsFalse(d.Cakildi, "Yumusak inis cakilma sayildi.");
            Assert.AreEqual(Perde2Dilimi.Asama.Tepki, d.Simdiki,
                "Dogancilar'a inildi ama tepki sahnesi baslamadi.");

            // --- TEPKI: Incili Kosk ---------------------------------
            Koy(d.incilikosk);
            yield return null;
            Assert.AreEqual(Perde2Dilimi.Asama.Bitti, d.Simdiki,
                "Tepki sahnesine varildi ama dilim bitmedi.");
        }

        /// <summary>
        /// <b>Başka bir yerde yapılan süzülüş talim sayılmaz.</b>
        ///
        /// Talim yeri belgeli: Okmeydanı, II. Bayezid vakfı talim alanı,
        /// Hezarfen'in talim yaptığı yer (RESEARCH §4.6). Her yerde
        /// sayılsaydı o belge bir süs olurdu.
        /// </summary>
        [UnityTest]
        public IEnumerator PracticeOnlyCountsAtOkmeydani()
        {
            var (d, dizi) = Kur();
            yield return Suzul(dizi, d.kule,
                               d.kule + new Vector3(120f, 0f, 0f));

            Assert.AreEqual(0, d.TalimSayisi,
                "Okmeydani disinda yapilan suzulus talim sayildi.");
            Assert.AreEqual(Perde2Dilimi.Asama.Talim, d.Simdiki);
        }

        /// <summary>
        /// <b>Çok kısa bir sıçrayış talim değildir.</b>
        ///
        /// Kuşanıp bir adım atlamak talim sayılırsa aşama üç saniyede
        /// geçilir ve talim diye bir şey olmaz.
        /// </summary>
        [UnityTest]
        public IEnumerator AHopIsNotAGlide()
        {
            var (d, dizi) = Kur();
            yield return Suzul(dizi, d.okmeydani,
                               d.okmeydani + new Vector3(10f, 0f, 0f));
            Assert.AreEqual(0, d.TalimSayisi,
                "10 metrelik bir sicrayis talim sayildi.");
        }

        /// <summary>
        /// <b>Çakılmak dilimi bitirmez, kuleye döndürür.</b>
        ///
        /// Aranma sistemindeki ilkeyle aynı: *kaçış VE yakalanma
        /// sonuçları*. Başarısızlık bir duvar değil bir sonuçtur; oyuncu
        /// tekrar deneyebilmeli.
        /// </summary>
        [UnityTest]
        public IEnumerator ACrashSendsYouBackToTheTowerNotToAWall()
        {
            var (d, dizi) = Kur();

            // Esikler artik artan bir merdiven (30/60/120) ve sabit 90 m
            // ikinci basamagi geciyor, ucuncuyu gecmiyordu. Esik
            // koddan okunur; burada tekrar yazmak bir sayinin iki
            // sahibi olmasi demek.
            for (int i = 0; i < d.talimHedefi; i++)
                yield return Suzul(dizi, d.okmeydani,
                                   d.okmeydani
                                   + new Vector3(d.TalimEsigi(i) + 30f, 0f, 0f));
            for (int k = 0; k < 3; k++) yield return null;
            Assert.AreEqual(Perde2Dilimi.Asama.Kule, d.Simdiki);

            // Kalkis artik IRTIFA istiyor: serefe kotundan atlanir.
            // Bundan sonraki her temas SERT sayilsin.
            dizi.cakilmaHizi = -0.01f;
            yield return Suzul(dizi, d.kule + Vector3.up * Perde2Dilimi.KalkisKotu,
                               d.kule + new Vector3(800f, 0f, 0f));
            yield return null;

            Assert.AreEqual(Perde2Dilimi.Asama.Kule, d.Simdiki,
                "Cakildiktan sonra dilim kuleye donmedi — oyuncu takildi.");
            Assert.IsFalse(d.Cakildi, "Cakilma bayragi temizlenmedi.");
        }

        /// <summary>
        /// <b>Tepki sahnesi rivayeti belge diye sunmuyor.</b>
        ///
        /// Oyunun zirvesi yalnız Evliya'da geçen bir anlatıdır. Kodeks
        /// bunu söylemezse, üç yıl boyunca kaynak dipnotu tutmanın anlamı
        /// kalmaz — oyun tam da en çok inanılmak istediği yerde susmuş
        /// olur.
        /// </summary>
        [Test]
        public void TheCodexAdmitsTheClimaxIsContested()
        {
            string k = Perde2Dilimi.TepkiKodeksi;
            Assert.IsTrue(k.Contains("Evliya"), "Kodeks kaynagi soylemiyor.");
            foreach (string beklenen in new[]
                     { "doğrulanmaz", "1638", "55:1" })
                Assert.IsTrue(k.Contains(beklenen),
                    $"Kodeks '{beklenen}' celiskisini gizliyor.");
            Assert.IsTrue(k.Contains("sürgün"),
                "Odul var, surgun yok — zirve zaferin cezalandirilmasidir.");
        }
    }
}
