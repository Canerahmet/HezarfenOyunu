using Hezarfen.Sehir;
using Hezarfen.Zaman;
using NUnit.Framework;
using UnityEngine;

namespace Hezarfen.Tests
{
    /// <summary>
    /// <b>Aranma sistemi tarihe mi bakıyor, sayaca mı?</b>
    ///
    /// Bu sistemin en kolay yanlış hâli, modern bir "wanted level"
    /// sayacıdır: ihlal işle, çubuk dolsun. Buradaki fark iki yerde:
    ///
    /// <list type="number">
    /// <item><b>Görülme.</b> İhlal tek başına aranma üretmez — kimse
    ///       görmediyse suç yoktur.</item>
    /// <item><b>Tarih.</b> Aynı davranış 1632'de serbest, 1634'te suç.
    ///       Kahve taşımak ve gece fenersiz gezmek bunun iki örneği.</item>
    /// </list>
    ///
    /// Ve bir sert kural: <b>şiddetsizlik</b>. Yakalanmanın bedeli akçe,
    /// mal ve zamandır; bu sistemde hasar diye bir kavram yoktur.
    /// </summary>
    public class AranmaTests
    {
        private GameObject _kok;

        private AranmaSistemi Kur(bool gece, int yil, int gun,
                                  float asesMesafesi = -1f,
                                  int kalabalik = 0)
        {
            _kok = new GameObject("aranma");
            var z = _kok.AddComponent<ZamanSistemi>();
            z.gunDakika = 0f;
            z.gunesiSur = false;
            z.yil = yil;
            z.yilinGunu = gun;
            z.Yenile();
            // Gece istiyorsak batistan sonraya, gunduz istiyorsak ogleye.
            z.saat = gece ? (float)z.Bugun.aksam + 1.5f : 12f;
            z.Yenile();
            Assert.AreEqual(gece, z.Gece, "Test kurulumu yanlis vakitte.");

            var sehirGo = new GameObject("sehir");
            var sehir = sehirGo.AddComponent<NPCYonetici>();
            sehir.graf = Graf();
            sehir.meslekler.Add(Meslek(NPCMeslek.Tip.Ases));
            sehir.sakinSayisi = 0;      // sakinleri elle koyacagiz
            sehir.enabled = false;      // Update calismasin

            var oyuncuGo = new GameObject("oyuncu");
            oyuncuGo.transform.position = Vector3.zero;

            var a = _kok.AddComponent<AranmaSistemi>();
            a.zaman = z;
            a.sehir = sehir;
            a.oyuncu = oyuncuGo.transform;

            // Ases ve kalabaligi elle yerlestir — algiyi olcmek icin
            // sehri kosturmaya gerek yok.
            var liste = new System.Collections.Generic.List<NPCAjan>();
            if (asesMesafesi >= 0f)
                liste.Add(new NPCAjan
                {
                    meslek = Meslek(NPCMeslek.Tip.Ases),
                    konum = new Vector3(asesMesafesi, 0f, 0f),
                });
            for (int i = 0; i < kalabalik; i++)
                liste.Add(new NPCAjan
                {
                    meslek = Meslek(NPCMeslek.Tip.Esnaf),
                    konum = new Vector3(i * 0.5f, 0f, 1f),
                });
            Sakinleri(sehir, liste);
            return a;
        }

        /// <summary>Yöneticinin sakin listesini doğrudan doldurur.</summary>
        private static void Sakinleri(NPCYonetici y,
            System.Collections.Generic.List<NPCAjan> liste)
        {
            var alan = typeof(NPCYonetici).GetField("_sakinler",
                System.Reflection.BindingFlags.NonPublic
                | System.Reflection.BindingFlags.Instance);
            var hedef = (System.Collections.Generic.List<NPCAjan>)
                alan.GetValue(y);
            hedef.Clear();
            hedef.AddRange(liste);
        }

        private static SokakGrafi Graf()
        {
            var g = ScriptableObject.CreateInstance<SokakGrafi>();
            g.dugumler.Add(new SokakGrafi.Dugum
            { konum = Vector3.zero, tur = SokakGrafi.Tur.Ev, semt = "TEST" });
            return g;
        }

        private static NPCMeslek Meslek(NPCMeslek.Tip t)
        {
            var m = ScriptableObject.CreateInstance<NPCMeslek>();
            m.tip = t;
            m.pay = 1f;
            return m;
        }

        [TearDown]
        public void Temizle()
        {
            foreach (var go in Object.FindObjectsByType<GameObject>(
                         FindObjectsInactive.Include))
                if (go != null && (go.name == "aranma" || go.name == "sehir"
                                   || go.name == "oyuncu"))
                    Object.DestroyImmediate(go);
        }

        /// <summary>
        /// <b>Gündüz fenersiz dolaşmak suç değil.</b>
        ///
        /// Basit görünüyor ama bu kuralı kaçırmak, oyuncuyu güpegündüz
        /// suçlu yapan bir sistem üretir — ve o hata ancak oynarken
        /// fark edilir.
        /// </summary>
        [Test]
        public void CarryingNoLanternIsNoCrimeByDay()
        {
            var a = Kur(gece: false, yil: 1634, gun: 121, asesMesafesi: 3f);
            a.FenerVar = false;
            for (int i = 0; i < 60; i++) a.Adim(0.1f);
            Assert.AreEqual(0f, a.Seviye, 0.001f,
                "Gunduz fenersiz gezmek aranma uretti.");
            Assert.AreEqual(AranmaSistemi.Durum.Temiz, a.SuAn);
        }

        /// <summary>
        /// <b>1632'de gece fenersiz gezmek de suç değil; 1634'te suç.</b>
        ///
        /// Aynı oyuncu, aynı saat, aynı davranış — iki yıl arayla iki
        /// farklı sonuç. Oyunun tarihi anlatmasının en doğrudan yolu bu.
        /// </summary>
        [Test]
        public void TheSameNightWalkIsLegalIn1632AndNotIn1634()
        {
            // Olcut ANLIK SEVIYE degil CEZA.
            //
            // Once seviyeye bakiyordum ve test yanlis ani olcuyordu:
            // oyuncu zaten yakalanmis, cezasini odemis ve serbest
            // birakilmisti — seviye o yuzden sifirdi. Asil iddia zaten
            // ceza: ayni gece yuruyusu 1632'de bedava, 1634'te akceye
            // mal olur.
            var once = Kur(gece: true, yil: 1632, gun: 121, asesMesafesi: 3f);
            once.FenerVar = false;
            for (int i = 0; i < 60; i++) once.Adim(0.1f);
            int ceza1632 = once.OdenenCeza;
            var durum1632 = once.SuAn;
            Temizle();

            var sonra = Kur(gece: true, yil: 1634, gun: 121, asesMesafesi: 3f);
            sonra.FenerVar = false;
            int ceza1634 = 0;
            sonra.Yakalandi += (i, c) => ceza1634 += c;
            for (int i = 0; i < 60; i++) sonra.Adim(0.1f);

            Assert.AreEqual(0, ceza1632,
                "1632'de gece fenersiz gezmek ceza uretti — yasak 1633'te.");
            Assert.AreEqual(AranmaSistemi.Durum.Temiz, durum1632,
                "1632'de kolluk oyuncuyu fark etti; o yil suc degil.");
            Assert.Greater(ceza1634, 0,
                "1634'te gece fenersiz gezildi ama ceza kesilmedi; "
                + "fener zorunlulugu isliyor olmali.");
        }

        /// <summary>
        /// <b>Kimse görmediyse suç yok.</b>
        ///
        /// İhlal tek başına aranma üretmez. Bu hem oyun olarak doğru
        /// (saklanmanın anlamı var) hem tarih olarak: mahalleyi tutan şey
        /// kayıt ve kefalettir, her sokakta bir bekçi değil.
        /// </summary>
        [Test]
        public void ACrimeNobodySeesIsNoCrime()
        {
            var a = Kur(gece: true, yil: 1634, gun: 121, asesMesafesi: -1f);
            a.FenerVar = false;
            for (int i = 0; i < 60; i++) a.Adim(0.1f);
            Assert.AreEqual(0f, a.Seviye, 0.001f,
                "Ortalikta ases yokken aranma yukseldi.");
        }

        /// <summary>Aranma kademeli tırmanıyor: fark → uyarı → kovalamaca.</summary>
        [Test]
        public void TheResponseEscalatesInSteps()
        {
            var a = Kur(gece: true, yil: 1634, gun: 121, asesMesafesi: 2f);
            a.FenerVar = false;

            var gorulen = new System.Collections.Generic.List<
                AranmaSistemi.Durum>();
            a.DurumDegisti += d => gorulen.Add(d);
            for (int i = 0; i < 200; i++) a.Adim(0.1f);

            Assert.Contains(AranmaSistemi.Durum.FarkEdildi, gorulen);
            Assert.Contains(AranmaSistemi.Durum.Uyarildi, gorulen);
            Assert.Contains(AranmaSistemi.Durum.Kovalaniyor, gorulen);
            Assert.Contains(AranmaSistemi.Durum.Yakalandi, gorulen);

            int fark = gorulen.IndexOf(AranmaSistemi.Durum.FarkEdildi);
            int uyari = gorulen.IndexOf(AranmaSistemi.Durum.Uyarildi);
            int kov = gorulen.IndexOf(AranmaSistemi.Durum.Kovalaniyor);
            Assert.Less(fark, uyari, "Uyari, fark edilmeden once geldi.");
            Assert.Less(uyari, kov, "Kovalamaca, uyaridan once basladi.");
        }

        /// <summary>
        /// <b>Yakalanmanın bedeli akçe ve mal — hasar YOK.</b>
        ///
        /// Plan Bölüm 11.1: *"Şiddetsiz tasarım: silahlı çatışma yok."*
        /// Bu test o kararı kilitliyor. Sisteme bir gün sağlık ya da
        /// vuruş eklenirse burada değil, tasarımda tartışılsın.
        /// </summary>
        [Test]
        public void BeingCaughtCostsCoinAndGoodsNeverBlood()
        {
            var a = Kur(gece: true, yil: 1634, gun: 121, asesMesafesi: 2f);
            a.FenerVar = false;
            a.YasakMalTasiyor = true;

            Ihlal yakalanan = Ihlal.Yok;
            int ceza = -1;
            int yakalanmaSayisi = 0;
            a.Yakalandi += (i, c) =>
            {
                yakalanan = i; ceza = c; yakalanmaSayisi++;
            };
            for (int i = 0; i < 300; i++) a.Adim(0.1f);

            Assert.AreNotEqual(Ihlal.Yok, yakalanan, "Hic yakalanmadi.");
            Assert.Greater(ceza, 0, "Ceza akce olarak kesilmedi.");
            Assert.Greater(a.OdenenCeza, 0);
            Assert.AreEqual(1, a.ElKonanMal,
                "Yasak mal tasiniyordu ama el konmadi.");
            Assert.IsFalse(a.YasakMalTasiyor,
                "El konan mal oyuncuda kalmis.");

            // Yakalanma sonrasi mesele KAPANIR: bir kez ceza, sonra
            // yoluna. Ceza dongusu olsaydi ayni adam saniyede bir
            // yakalanirdi ve oyuncunun cikisi olmazdi.
            Assert.AreEqual(1, a.ElKonanMal,
                "Ayni mala birden fazla kez el konuldu.");
            // CEZA BIR DONGU DEGIL.
            //
            // Muafiyet olmadan, hala fenersiz olan adam saniyede bir
            // yeniden yakalanir ve oyuncunun cikisi olmaz. Otuz saniyede
            // 25 saniyelik muafiyetle en fazla iki kez durdurulabilir.
            Assert.LessOrEqual(yakalanmaSayisi, 2,
                $"30 saniyede {yakalanmaSayisi} kez yakalandi — ceza "
                + "dongusune girildi, oyuncunun cikisi yok.");
        }

        /// <summary>
        /// <b>Kalabalığa karışmak işe yarıyor.</b>
        ///
        /// Plan Bölüm 11.1'in saydığı kaçış yollarından biri ve şehrin
        /// kendi dokusunu bir mekaniğe çeviriyor: kalabalık mahallede
        /// saklanmak kolaydır.
        /// </summary>
        [Test]
        public void SlippingIntoACrowdCoolsThingsDown()
        {
            // Seviyeyi YANSIMAYLA atamiyoruz: kovalamaca gercekten
            // yasanir, sonra ases uzaklasir. Ozel bir ayarlayiciya
            // uzanan test, olctugu seyin disina cikmis olurdu.
            float bosSokak = SonumOlc(kalabalik: 0);
            Temizle();
            float kalabalik = SonumOlc(kalabalik: 25);

            Assert.Less(kalabalik, bosSokak,
                $"Kalabalikta {kalabalik:0.000}, bos sokakta {bosSokak:0.000} "
                + "— kalabaliga karismanin bir karsiligi yok.");
        }

        /// <summary>
        /// Ases yakınken seviyeyi yükseltir, sonra ases uzaklaşır ve
        /// kalan seviyeyi döndürür. Düşük değer = daha iyi kaçış.
        /// </summary>
        private float SonumOlc(int kalabalik)
        {
            var a = Kur(gece: true, yil: 1634, gun: 121,
                        asesMesafesi: 3f, kalabalik: kalabalik);
            a.FenerVar = false;

            // Kovalamaca: seviye kovalama esigine dayansin.
            for (int i = 0; i < 40 && a.Seviye < 0.55f; i++) a.Adim(0.1f);
            Assert.Greater(a.Seviye, 0.3f, "Aranma hic yukselmedi.");

            // Ases uzaklasir — artik goren yok.
            foreach (var s2 in a.sehir.Sakinler)
                if (s2.meslek != null && s2.meslek.tip == NPCMeslek.Tip.Ases)
                    s2.konum = new Vector3(900f, 0f, 0f);

            for (int i = 0; i < 20; i++) a.Adim(0.1f);
            if (kalabalik > 0)
                Assert.Greater(a.YakindakiKalabalik, kalabalik / 2,
                    "Kalabalik sayilmadi.");
            return a.Seviye;
        }
    }
}
