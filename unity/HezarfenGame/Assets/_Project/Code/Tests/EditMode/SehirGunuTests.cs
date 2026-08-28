using System.Collections.Generic;
using System.Linq;
using Hezarfen.Sehir;
using Hezarfen.Zaman;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Hezarfen.Tests
{
    /// <summary>
    /// <b>Şehir gerçekten yaşıyor mu — sayıyla.</b>
    ///
    /// Plan Bölüm 11.3: *"açık dünya hissinin büyük kısmı rutin ve
    /// tepkilerden gelir, diyalogdan değil"*. Rutin görünmüyorsa yoktur,
    /// ve görünüp görünmediğini ancak ölçüm söyler.
    ///
    /// Rutin saf bir işlev olduğu için (vakit + tohum → hedef) bir günü
    /// oynatmak için ne model ne kare gerekiyor: bin sakin, altı vakit,
    /// bir test.
    /// </summary>
    public class SehirGunuTests
    {
        private const int Sakin = 1200;
        private const int BirMayis1632Gun = 121;

        private static SokakGrafi Graf()
        {
            var g = AssetDatabase.LoadAssetAtPath<SokakGrafi>(
                "Assets/_Project/Data/SG_Sehir.asset");
            Assert.IsNotNull(g, "Sokak grafi yok — once grafi kur.");
            return g;
        }

        private static List<NPCMeslek> Meslekler()
        {
            var liste = AssetDatabase.FindAssets("t:NPCMeslek")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<NPCMeslek>)
                .Where(m => m != null).ToList();
            Assert.IsNotEmpty(liste, "Meslek cizelgesi yok — once uret.");
            return liste;
        }

        private static List<SehirGunu.Olcum> Gun(int yil = 1632,
                                                 int gun = BirMayis1632Gun)
        {
            var g = Graf();
            var s = SehirGunu.Sakinler(g, Meslekler(), Sakin);
            Assert.AreEqual(Sakin, s.Count, "Sakin dagitimi eksik.");
            return SehirGunu.Gun(g, s, yil, gun);
        }

        /// <summary>Her mesleğin her vakitte bir işi var.</summary>
        [Test]
        public void NoProfessionStandsIdleAtAnyPrayer()
        {
            foreach (var m in Meslekler())
                foreach (VakitHesabi.Vakit v in
                         System.Enum.GetValues(typeof(VakitHesabi.Vakit)))
                {
                    bool var_ = m.cizelge.Any(a => a.vakit == v);
                    Assert.IsTrue(var_,
                        $"{m.tip}: '{v}' vaktinde cizelgede adim yok — o "
                        + "vakitte NPC yerinde doner.");
                }
        }

        /// <summary>
        /// Çizelgedeki her hedef türü şehirde <b>var</b>.
        ///
        /// Olmayan bir hedefe gönderilen NPC yerinde döner ve oyuncu
        /// "yapay zekâ bozuk" der. Oysa bozuk olan çizelgedir.
        /// </summary>
        [Test]
        public void EveryScheduledTargetExistsInTheCity()
        {
            var g = Graf();
            foreach (var m in Meslekler())
                foreach (var a in m.cizelge)
                    Assert.Greater(g.Say(a.hedef), 0,
                        $"{m.tip} '{a.vakit}' vaktinde '{a.hedef}' hedefliyor "
                        + "ama sehirde o turden hic dugum yok.");
        }

        /// <summary>
        /// Neredeyse herkes hedefine <b>yürüyerek</b> gidebiliyor.
        ///
        /// Ulaşılamayan hedef, evi bir kara parçasında olup hedefi
        /// başkasında olan sakin demek. Sıfır beklenmiyor — Kasımpaşa
        /// gibi coğrafyanın ayırdığı cepler var — ama %5'i geçerse
        /// şehrin dokusu değil <b>haritası</b> bozuktur.
        /// </summary>
        [Test]
        public void AlmostEveryoneCanWalkToTheirDestination()
        {
            foreach (var o in Gun())
            {
                float oran = o.toplam > 0 ? o.ulasilamaz / (float)o.toplam : 0f;
                Assert.Less(oran, 0.05f,
                    $"{o.vakit}: sakinlerin %{oran * 100:0.0}'i hedefine "
                    + $"yuruyerek gidemiyor ({o.ulasilamaz}/{o.toplam}).");
            }
        }

        /// <summary>
        /// <b>Öğle ezanında mescide akış var.</b>
        ///
        /// Bu, günün en görünür ritmi ve şehrin canlı hissedilmesinin
        /// büyük kısmı. Öğlede mescide gidenlerin oranı, ikindi ile
        /// güneş arasındaki "iş vakti" ortalamasından belirgin yüksek
        /// olmalı — yoksa vakitlerin oyunda hiçbir karşılığı yoktur.
        /// </summary>
        [Test]
        public void TheNoonCallDrawsACrowdToTheMosques()
        {
            var gun = Gun().ToDictionary(o => o.vakit);
            float ogle = gun[VakitHesabi.Vakit.Ogle].MescitOrani;
            float is_ = gun[VakitHesabi.Vakit.Gunes].MescitOrani;

            Assert.Greater(ogle, is_ * 1.5f,
                $"Ogle vaktinde mescide giden %{ogle * 100:0.0}, is "
                + $"vaktinde %{is_ * 100:0.0} — ezanin sehirde bir "
                + "karsiligi yok.");
            Assert.Greater(ogle, 0.15f,
                $"Ogle vaktinde sakinlerin yalnizca %{ogle * 100:0.0}'i "
                + "mescitte; mahalle mescidi 130 tane ve bos duruyor.");
        }

        /// <summary>
        /// <b>Yatsıdan sonra sokaklar boşalıyor.</b>
        ///
        /// 1633 sonrası gece fenersiz dolaşmak yasak ve ases devriyesi
        /// var (RESEARCH §6). Ama yasaktan önce de gece sokakta kimse
        /// olmaz: aydınlatma yok, kolluk var, iş yok.
        ///
        /// Dışarıda kalan tek grup <b>ases</b> olmalı — bekçinin işi
        /// zaten budur.
        /// </summary>
        [Test]
        public void TheStreetsEmptyAfterTheNightPrayer()
        {
            var gun = Gun().ToDictionary(o => o.vakit);
            float gunduz = gun[VakitHesabi.Vakit.Ikindi].DisariOrani;
            float gece = gun[VakitHesabi.Vakit.Yatsi].DisariOrani;

            Assert.Less(gece, gunduz * 0.35f,
                $"Yatsida disarida %{gece * 100:0.0}, ikindide "
                + $"%{gunduz * 100:0.0} — gece sokaklar bosalmiyor.");
            Assert.Greater(gece, 0.001f,
                "Gece sokakta HIC kimse yok — ases devriyesi de yok "
                + "demektir, oysa gece kollugu 1632'de vardir.");
        }

        /// <summary>
        /// <b>Aynı çizelge, 1634'te başka bir şehir.</b>
        ///
        /// 2 Eylül 1633 fermanıyla kahvehaneler kapandı (RESEARCH §6,
        /// TDV "Kahve"; BA, A.DVN, nr. 25/47). Oyun o eşiği geçtiğinde
        /// akşam rutini değişmeli — kimse yıkılmış bir binaya gitmemeli.
        ///
        /// Bu test kronolojinin oyunda bir <b>karşılığı</b> olduğunu
        /// söylüyor: tarih bir metin değil, davranış.
        /// </summary>
        [Test]
        public void TheCoffeehouseBanChangesTheEvening()
        {
            var once = Gun(1632, BirMayis1632Gun)
                .First(o => o.vakit == VakitHesabi.Vakit.Aksam);
            var sonra = Gun(1634, BirMayis1632Gun)
                .First(o => o.vakit == VakitHesabi.Vakit.Aksam);

            int k1 = once.hedefler.TryGetValue(SokakGrafi.Tur.Kahvehane,
                                               out var a) ? a : 0;
            int k2 = sonra.hedefler.TryGetValue(SokakGrafi.Tur.Kahvehane,
                                                out var b) ? b : 0;

            Assert.Greater(k1, 0,
                "1632 aksaminda kahvehaneye giden yok — oysa yasak 1633'te.");
            Assert.AreEqual(0, k2,
                $"1634 aksaminda {k2} kisi kahvehaneye gidiyor; o binalar "
                + "2 Eylul 1633 fermaniyla kapatildi ve yikildi.");
        }

        /// <summary>Aynı tohum aynı günü verir — rutin düzendir, rastgelelik değil.</summary>
        [Test]
        public void TheSameSeedLivesTheSameDay()
        {
            var g = Graf();
            var m = Meslekler();
            var a = SehirGunu.Sakinler(g, m, 300, 7);
            var b = SehirGunu.Sakinler(g, m, 300, 7);
            for (int i = 0; i < a.Count; i++)
            {
                Assert.AreEqual(a[i].evDugum, b[i].evDugum);
                Assert.AreEqual(a[i].tohum, b[i].tohum);
                Assert.AreSame(a[i].meslek, b[i].meslek);
            }

            var o1 = SehirGunu.Olc(g, a, VakitHesabi.Vakit.Ogle, 1632, 121);
            var o2 = SehirGunu.Olc(g, b, VakitHesabi.Vakit.Ogle, 1632, 121);
            Assert.AreEqual(o1.mescitte, o2.mescitte,
                "Ayni tohum ayni vakitte farkli sonuc verdi — rutin "
                + "deterministik olmali, yoksa hata ayiklanamaz.");
        }
    }
}
