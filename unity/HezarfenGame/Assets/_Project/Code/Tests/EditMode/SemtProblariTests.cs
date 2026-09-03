using System.Linq;
using Hezarfen.Editor.Lighting;
using NUnit.Framework;
using UnityEditor;

namespace Hezarfen.Tests.EditMode
{
    /// <summary>
    /// <b>Şehrin yaşadığı sahneler pişirme kümesinde olsun.</b>
    ///
    /// ## Ölçüm
    ///
    /// Üstten çekilen denetim karesinde Sûriçi sokağı simsiyahtı ve
    /// karanlık bölge her yerde <b>tıpatıp aynı</b> rengi okuyordu
    /// (37,1/16,0/0,2 · 36,6/15,0/0,2 · 36,4/14,7/0,2). Gerçek gölge
    /// altındaki yüzeye göre değişir; değişmeyen renk, ışık almayan
    /// yüzeydir. Aynı yerin göz hizası karesinde ise gölgeli kaldırım
    /// mavi/kırmızı 0,63 — <b>aynı yer, aynı saniye, iki kamera, iki
    /// sonuç</b>. Farkı yaratan açıydı, yani dolaylı ışığın tek kaynağı
    /// ekran uzayıydı (SSGI).
    ///
    /// Sebep: pişirme kümesi <b>tek sahne</b> içeriyordu ve o, binaların
    /// olduğu sahne değildi. Şehir sekiz semt sahnesinde (35 + 41 + 34 +
    /// 27 MB…), taban sahne 1,1 MB. 2,8 milyon prob boş bir yamacın
    /// üstüne pişmişti.
    ///
    /// ## Neden test
    ///
    /// Pişirme başarıyla biter, dosya diske yazılır, prob sayısı
    /// milyonlarcadır — ve şehirde dolaylı ışık yoktur. Yeni bir semt
    /// sahnesi eklendiğinde aynı sessizlik geri gelir. Ölçünün tek
    /// sahibi <see cref="SemtProblari.BaglanmamisSemtler"/>; kurulum da
    /// aynı yerden yapılır.
    /// </summary>
    public class SemtProblariTests
    {
        [Test]
        public void EveryDistrictSceneIsInTheProbeBakingSet()
        {
            Assert.IsNotEmpty(SemtProblari.Semtler(),
                "Hic semt sahnesi bulunamadi — yol yanlis olabilir: "
                + SemtProblari.SemtDizini);

            var eksik = SemtProblari.BaglanmamisSemtler();
            Assert.IsEmpty(eksik,
                "Pisirme kumesine bagli olmayan semt(ler): "
                + string.Join(", ", eksik.OrderBy(a => a))
                + ". Bagli olmayan semtin probu HIC pismez ve orada "
                + "dolayli isik yoktur. Kur: Hezarfen > Aydinlatma > "
                + "Semt problarini kur, sonra Problari pisir.");
        }

        /// <summary>
        /// Aralık, ölçünün sahibinden okunur — testte kopya sayı yok.
        /// </summary>
        [Test]
        public void TheProbeSpacingIsTheOneTheCityWasSizedFor()
        {
            var kume = SemtProblari.Kume();
            Assert.IsNotNull(kume, "Projede ProbeVolumeBakingSet yok.");

            var aralik = new SerializedObject(kume)
                .FindProperty("minDistanceBetweenProbes");
            Assert.IsNotNull(aralik,
                "minDistanceBetweenProbes alani bulunamadi.");
            Assert.AreEqual(SemtProblari.ProbAraligi, aralik.floatValue, 0.01f,
                "Prob araligi sehir icin secilen degerde degil. 1 m'lik "
                + "izgara kucuk bir odada anlamli; 10 km'lik sehirde "
                + "bellek butcesini doldurur ve hucrelerin bir kismi hic "
                + "yuklenmez. Kur: Hezarfen > Aydinlatma > Semt "
                + "problarini kur.");
        }
    }
}
