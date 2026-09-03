using System.Linq;
using Hezarfen.Editor.Lighting;
using NUnit.Framework;

namespace Hezarfen.Tests.EditMode
{
    /// <summary>
    /// <b>Prob verisi akmalı</b> — yoksa şehrin çoğunda dolaylı ışık
    /// hiç yüklenmez.
    ///
    /// ## Ölçüm
    ///
    /// Aynı turda, aynı sabit pozla: Sûriçi gölgesi <c>(36, 15, 0)</c>,
    /// Galata gölgesi mavi/kırmızı 0,755. Sıfır mavi "karanlık" değil,
    /// <b>hiç gök ışığı yok</b> demektir; ve iki yeri ayıran tek şey
    /// yerdi.
    ///
    /// Sebep koşum günlüğünde yazılıydı: <i>"Max Memory Budget for
    /// Adaptive Probe Volumes has been reached, but there is still more
    /// data to load."</i> Fırın 98 + 98 + 197 MB veri üretiyor, bütçe
    /// 1024; hücrelerin bir kısmı yükleniyor, gerisi hiç yüklenmiyor.
    ///
    /// ## Neden test
    ///
    /// Bu kusur sessiz: fırın "başarılı" der, dosyalar diske yazılır,
    /// prob sayısı milyonlarcadır ve ekranda yalnızca <b>bazı yerler</b>
    /// karanlıktır. Bir kalite seviyesi değişimi ya da yeni bir HDRP
    /// varlığı onu geri getirir. Ölçünün tek sahibi
    /// <see cref="ProbAkisi.AkisKapaliOlanlar"/>; kurulum da aynı
    /// yerden yapılır (<see cref="ProbAkisi.Ac"/>), yani bu test bir
    /// sayıyı kopyalamıyor.
    /// </summary>
    public class ProbAkisiTests
    {
        [Test]
        public void EveryPipelineUsingProbeVolumesCanStreamThem()
        {
            var kapali = ProbAkisi.AkisKapaliOlanlar();
            Assert.IsEmpty(kapali,
                "APV kullanan ama akisi kapali HDRP varligi var: "
                + string.Join(", ", kapali.OrderBy(a => a))
                + ". Bu durumda bellek butcesi dolar ve sehrin geri "
                + "kalaninda dolayli isik HIC yuklenmez — golgede mavi "
                + "kanal sifira duser. Kur: Hezarfen > Aydinlatma > "
                + "Prob akisini ac.");
        }
    }
}
