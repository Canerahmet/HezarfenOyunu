using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Hezarfen.Sehir;
using Hezarfen.Zaman;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Hezarfen.Tests
{
    /// <summary>
    /// <b>Şehir oyuncu bakmıyorken de yaşıyor mu?</b>
    ///
    /// Bu, kalabalık sistemlerinin asıl sorusu. Kolay yol, sakinleri
    /// oyuncunun etrafında doğurup uzaklaşınca yok etmektir; ucuzdur ve
    /// <b>dünyayı bir sahneye çevirir</b> — mahalleden çıkıp döndüğünde
    /// herkes başka bir hayat yaşıyordur, çünkü aslında hayat yoktur.
    ///
    /// Buradaki ayrım <b>yaşamak</b> ile <b>görünmek</b> arasında:
    /// bütün sakinler her zaman ilerler, yalnız yakındakiler çizilir.
    /// Testler ikisini ayrı ayrı ölçüyor.
    ///
    /// Faz 6'nın kabul ölçütü *"Galata'da 30 dk kesintisiz dolaşım"*;
    /// gövde bütçesi ve havuz o ölçütün korunduğu yer.
    /// </summary>
    public class NPCYoneticiTests
    {
        private GameObject _kok;
        private SokakGrafi _graf;
        private List<NPCMeslek> _meslekler;

        /// <summary>
        /// Küçük ama gerçek bir graf: iki ev, bir mescit, bir dükkân,
        /// hepsi birbirine bağlı.
        ///
        /// Sahnedeki 1535 düğümlük şehri yüklemiyoruz — test sahneye
        /// bağlı olursa sahne değişince kırılır ve kırıldığında NPC'yle
        /// ilgisi olmayan bir şey söyler.
        /// </summary>
        private SokakGrafi TestGrafi()
        {
            var g = ScriptableObject.CreateInstance<SokakGrafi>();
            void D(Vector3 p, SokakGrafi.Tur t) => g.dugumler.Add(
                new SokakGrafi.Dugum { konum = p, tur = t, semt = "TEST" });

            D(new Vector3(0, 0, 0), SokakGrafi.Tur.Ev);        // 0
            D(new Vector3(30, 0, 0), SokakGrafi.Tur.Ev);       // 1
            D(new Vector3(60, 0, 0), SokakGrafi.Tur.Mescit);   // 2
            D(new Vector3(90, 0, 0), SokakGrafi.Tur.Dukkan);   // 3
            D(new Vector3(120, 0, 0), SokakGrafi.Tur.Cesme);   // 4

            void K(int a, int b) => g.kenarlar.Add(new SokakGrafi.Kenar
            { a = a, b = b, uzunluk = Vector3.Distance(
                g.dugumler[a].konum, g.dugumler[b].konum) });
            K(0, 1); K(1, 2); K(2, 3); K(3, 4);
            return g;
        }

        private NPCMeslek TestMeslek()
        {
            var m = ScriptableObject.CreateInstance<NPCMeslek>();
            m.tip = NPCMeslek.Tip.Esnaf;
            m.pay = 1f;
            m.cizelge = new List<NPCMeslek.Adim>();
            foreach (VakitHesabi.Vakit v in
                     System.Enum.GetValues(typeof(VakitHesabi.Vakit)))
            {
                // Gunduz dukkana, gece eve — olcum bunu gormeli.
                var hedef = v == VakitHesabi.Vakit.Yatsi
                    ? SokakGrafi.Tur.Ev : SokakGrafi.Tur.Dukkan;
                m.cizelge.Add(new NPCMeslek.Adim
                { vakit = v, hedef = hedef, olasilik = 1f, disarida = true });
            }
            return m;
        }

        private NPCYonetici Kur(int sakin = 40, int butce = 8)
        {
            _graf = TestGrafi();
            _meslekler = new List<NPCMeslek> { TestMeslek() };

            _kok = new GameObject("yonetici");
            var y = _kok.AddComponent<NPCYonetici>();
            y.graf = _graf;
            y.meslekler = _meslekler;
            y.sakinSayisi = sakin;
            y.govdeButcesi = butce;
            y.gorunurMesafe = 40f;
            y.dilim = 4;
            y.govdePrefab = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            y.govdePrefab.SetActive(false);

            var oyuncuGo = new GameObject("oyuncu");
            oyuncuGo.transform.position = new Vector3(0, 0, 0);
            y.oyuncu = oyuncuGo.transform;

            var zGo = new GameObject("zaman");
            var z = zGo.AddComponent<ZamanSistemi>();
            z.gunDakika = 0f;
            z.gunesiSur = false;
            z.yilinGunu = 121;
            z.saat = 12f;
            z.Yenile();
            y.zaman = z;
            return y;
        }

        [TearDown]
        public void Temizle()
        {
            foreach (var go in Object.FindObjectsByType<GameObject>(
                         FindObjectsInactive.Include))
                if (go != null && (go.name.Contains("yonetici")
                    || go.name.Contains("oyuncu") || go.name.Contains("zaman")
                    || go.name.Contains("gosterici")
                    || go.name.Contains("Capsule")))
                    Object.DestroyImmediate(go);
            _kok = null;
        }

        /// <summary>
        /// <b>Şehir gerçekten konuşuyor.</b>
        ///
        /// Beş bin replik üretmek şehri konuşturmaz; korpus bir dosyada
        /// dururken oyuncu için hiç yoktur. Ölçü basit: yakındaki
        /// sakinlerin başının üstünde <b>okunacak bir şey var mı</b>.
        /// </summary>
        [UnityTest]
        public IEnumerator TheCityActuallySpeaks()
        {
            var y = Kur();
            var gGo = new GameObject("gosterici");
            var g = gGo.AddComponent<BarkGosterici>();
            g.yonetici = y;
            g.oyuncu = y.oyuncu;
            g.duyulmaMesafesi = 40f;
            g.ayniAndaEnCok = 4;

            yield return null;
            yield return null;

            int repligiOlan = y.Sakinler.Count(a => a.replik != null);
            Assert.AreEqual(y.Sakinler.Count, repligiOlan,
                $"{y.Sakinler.Count} sakinin {repligiOlan} tanesinin "
                + "repligi var — kalani sessiz.");

            foreach (var a in y.Sakinler)
                Assert.IsFalse(string.IsNullOrWhiteSpace(a.replik.metin),
                    "Bos replik atandi.");

            Assert.Greater(g.GorunurReplik, 0,
                "Oyuncunun yaninda kimse konusmuyor.");
            Assert.LessOrEqual(g.GorunurReplik, g.ayniAndaEnCok,
                $"{g.GorunurReplik} replik ayni anda gorunuyor — kalabalikta "
                + "okunacak bir sey kalmaz.");
        }

        /// <summary>
        /// <b>Uzaktaki kimse duyulmuyor.</b>
        ///
        /// Duyma mesafesi gerçek bir insan sesinin mesafesi kadar olmazsa
        /// bütün şehir aynı anda kulağına konuşur.
        /// </summary>
        [UnityTest]
        public IEnumerator NobodyIsHeardFromAcrossTheCity()
        {
            var y = Kur();
            var gGo = new GameObject("gosterici");
            var g = gGo.AddComponent<BarkGosterici>();
            g.yonetici = y;
            g.oyuncu = y.oyuncu;
            g.duyulmaMesafesi = 8f;

            // OYUNCUYU SEHIRDEN UZAKLASTIR.
            //
            // Ilk yazimda oyuncu (0,0,0)'daydi ve test patladi: orasi
            // 0 numarali EV dugumu, yani sakinlerin bir kismi tam
            // oyuncunun ustunde duruyordu. Uc kisinin duyulmasi dogruydu;
            // yanlis olan olcunun kurulusuydu.
            y.oyuncu.position = new Vector3(1000f, 0f, 1000f);
            yield return null;
            yield return null;

            Assert.AreEqual(0, g.GorunurReplik,
                $"Sehirden 1 km uzakta {g.GorunurReplik} kisi duyuluyor.");
        }

        /// <summary>Sakinler dağıtıldı ve hepsinin bir mesleği var.</summary>
        [UnityTest]
        public IEnumerator TheCityIsPopulated()
        {
            var y = Kur();
            yield return null;
            Assert.AreEqual(40, y.Sakinler.Count);
            foreach (var a in y.Sakinler)
                Assert.IsNotNull(a.meslek, "Meslekssiz sakin var.");
        }

        /// <summary>
        /// <b>Gövde bütçesi aşılmıyor.</b>
        ///
        /// Bütçe, otuz dakikalık kesintisiz dolaşımın korunduğu yer.
        /// Aşılırsa kabul ölçütü sessizce kaybedilir: oyun çalışır ama
        /// kalabalık bir mahallede kare süresi yükselir.
        /// </summary>
        [UnityTest]
        public IEnumerator TheBodyBudgetIsNeverExceeded()
        {
            var y = Kur(sakin: 60, butce: 8);
            yield return new WaitForSeconds(0.4f);
            Assert.LessOrEqual(y.GorunurSayisi, 8,
                $"{y.GorunurSayisi} govde cizildi, butce 8.");
        }

        /// <summary>
        /// <b>Uzaktakiler de yürüyor.</b>
        ///
        /// Bu testin varlık sebebi şu: bir kalabalık sistemi, sakinleri
        /// yalnız görünürken ilerleterek de "çalışıyor" görünür. Fark
        /// ancak oyuncu uzaklaşıp geri döndüğünde ortaya çıkar — ve o
        /// zaman herkes bıraktığın yerde durur.
        /// </summary>
        [UnityTest]
        public IEnumerator ResidentsWalkEvenWhereNobodyLooks()
        {
            var y = Kur(sakin: 30, butce: 4);
            y.oyuncu.position = new Vector3(1000f, 0f, 0f);  // herkes uzakta
            yield return null;

            var basla = y.Sakinler.Select(a => a.konum).ToList();
            // KARE degil SURE bekle. Yurume metre/saniyedir ve bir test
            // kosumunda kareler cok kisa surebilir: otuz kare, gercek
            // zamanda yarim saniye bile olmayabilir ve kimse gorulur
            // kadar yol almaz. Ilk yazimda kare saymistim ve olcum
            // "8/30 yurudu" dedi — sakinler yuruyordu, test bakmiyordu.
            yield return new WaitForSeconds(1.5f);

            int oynayan = 0;
            for (int i = 0; i < y.Sakinler.Count; i++)
                if (Vector3.Distance(basla[i], y.Sakinler[i].konum) > 0.05f)
                    oynayan++;

            Assert.AreEqual(0, y.GorunurSayisi,
                "Kimse yakinda degilken govde cizilmis.");
            Assert.Greater(oynayan, y.Sakinler.Count / 2,
                $"{oynayan}/{y.Sakinler.Count} sakin ilerledi. Uzaktakiler "
                + "durursa sehir oyuncunun etrafinda donen bir sahne olur.");
        }

        /// <summary>
        /// <b>Gövdeler havuzdan gelir, yeniden yaratılmaz.</b>
        ///
        /// Oyuncu yürüdükçe sakinler sürekli menzile girip çıkar. Her
        /// girişte Instantiate, her çıkışta Destroy demek, otuz dakikada
        /// binlerce tahsis ve düzenli çöp toplama duraksaması demekti.
        /// </summary>
        [UnityTest]
        public IEnumerator BodiesArePooledNotRecreated()
        {
            var y = Kur(sakin: 30, butce: 6);
            y.oyuncu.position = Vector3.zero;
            yield return new WaitForSeconds(0.3f);
            int ilk = y.transform.childCount;
            Assert.Greater(ilk, 0, "Hic govde uretilmedi.");

            // Uzaklas, geri gel — bunu birkac kez.
            for (int tur = 0; tur < 3; tur++)
            {
                y.oyuncu.position = new Vector3(1000f, 0f, 0f);
                yield return new WaitForSeconds(0.2f);
                y.oyuncu.position = Vector3.zero;
                yield return new WaitForSeconds(0.2f);
            }

            Assert.LessOrEqual(y.transform.childCount, ilk + 2,
                $"Govde sayisi {ilk} -> {y.transform.childCount}: her "
                + "menzile giriste yeni govde yaratiliyor, havuz "
                + "kullanilmiyor.");
        }

        /// <summary>
        /// <b>Vakit değişince herkes yeni hedefe döner.</b>
        ///
        /// Gündüz dükkâna, yatsıda eve. Bu olmazsa vakitlerin oyunda bir
        /// karşılığı yok demektir.
        /// </summary>
        [UnityTest]
        public IEnumerator ThePrayerChangesWhereEveryoneIsHeaded()
        {
            var y = Kur(sakin: 20, butce: 4);
            y.oyuncu.position = new Vector3(1000f, 0f, 0f);
            y.zaman.saat = 12f; y.zaman.Yenile();
            yield return null;
            yield return null;

            int dukkana = y.Sakinler.Count(
                a => a.hedefDugum >= 0
                     && _graf.dugumler[a.hedefDugum].tur == SokakGrafi.Tur.Dukkan);
            Assert.Greater(dukkana, 0, "Gunduz kimse dukkana gitmiyor.");

            // Yatsiya atla.
            y.zaman.VakteAtla(VakitHesabi.Vakit.Yatsi);
            yield return null;
            yield return null;

            int eve = y.Sakinler.Count(
                a => a.hedefDugum >= 0
                     && _graf.dugumler[a.hedefDugum].tur == SokakGrafi.Tur.Ev);
            Assert.Greater(eve, y.Sakinler.Count / 2,
                $"Yatsida yalnizca {eve}/{y.Sakinler.Count} sakin eve "
                + "yoneldi — vakit degisimi rutini surmuyor.");
        }
    }
}
