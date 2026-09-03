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

        /// <summary>
        /// <b>Açık suya prob hacmi konmasın.</b>
        ///
        /// ## Ölçüm
        ///
        /// Semt sınırları iki topluluğa ayrılıyor ve arada hiçbir şey
        /// yok: gerçek mahalleler 2045-3184 m, iki tanesi 7478-7913 m.
        /// İkincilerin içine bakıldı — <c>D_Bogaz</c>'ın tek kök nesnesi
        /// <c>TEKNELER_1632</c>, yani Boğaz boyunca serpilmiş
        /// <b>tekneler</b>; <c>D_Halic</c> aynı.
        ///
        /// Prob hacmi gökyüzünün kapandığı yerler içindir ve açık suda
        /// kapanan bir şey yoktur. Bedeli ölçüldü: 15 km'ye yayılan bir
        /// kutu, dondurulmuş ızgarada <c>D_Bogaz</c>'ı yirmi dakikada
        /// yalnızca %1'e getirdi — tek semt için on bir saat.
        ///
        /// ## Neden test
        ///
        /// Kurulum bir daha koşulduğunda o hacimler sessizce geri
        /// gelebilir ve kimse fark etmez: fırın yine "başarılı" der,
        /// yalnızca bir gecede bitmez. Ölçü kümenin kendi kaydında
        /// duruyor — her sahne için sınır ve <c>hasProbeVolume</c>.
        /// </summary>
        [Test]
        public void AScatterOverOpenWaterCarriesNoProbeVolume()
        {
            var kume = SemtProblari.Kume();
            Assert.IsNotNull(kume, "Projede ProbeVolumeBakingSet yok.");

            var so = new SerializedObject(kume);
            var deger = so.FindProperty("m_SceneBakeData.m_Values");
            Assert.IsNotNull(deger,
                "m_SceneBakeData.m_Values bulunamadi — paket surumu "
                + "degismis olabilir.");

            int bakilan = 0;
            for (int i = 0; i < deger.arraySize; i++)
            {
                var e = deger.GetArrayElementAtIndex(i);
                var sinir = e.FindPropertyRelative("bounds");
                var varMi = e.FindPropertyRelative("hasProbeVolume");
                if (sinir == null || varMi == null) continue;
                var b = sinir.boundsValue;
                bakilan++;
                bool genis = b.size.x > SemtProblari.EnBuyukSemt
                             || b.size.z > SemtProblari.EnBuyukSemt;
                if (!genis) continue;
                Assert.IsFalse(varMi.boolValue,
                    $"Yayilimi {b.size.x:0}x{b.size.z:0} m olan bir sahne "
                    + "prob hacmi tasiyor. Bu bir mahalle degil, acik su "
                    + "ustunde bir serpme (tekneler); o kutu firini "
                    + "saatlerce mesgul eder ve karsiliginda hicbir yerde "
                    + "gokyuzunu kapatmaz. Kur: Hezarfen > Aydinlatma > "
                    + "Semt problarini kur.");
            }
            Assert.Greater(bakilan, 0,
                "Kumede sahne kaydi yok — olcu bos donuyor.");
        }
    }
}