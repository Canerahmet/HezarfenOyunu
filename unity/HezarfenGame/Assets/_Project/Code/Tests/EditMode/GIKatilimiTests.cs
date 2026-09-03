using System.Linq;
using Hezarfen.Editor.Lighting;
using NUnit.Framework;

namespace Hezarfen.Tests.EditMode
{
    /// <summary>
    /// <b>Şehir sıçrama ışığına katılsın.</b>
    ///
    /// ## Kök sebep
    ///
    /// Üstten çekilen denetim karesinde Sûriçi sokağı simsiyahtı ve
    /// karanlık bölge üç ayrı noktada <b>aynı</b> rengi okuyordu
    /// (37,0/15,7/0,2 · 36,5/14,9/0,2 · 36,5/14,8/0,2). Gölge yüzeye
    /// göre değişir; değişmeyen renk gölge değil, hiç ışık almayan
    /// yüzeydir. Aynı yerin göz hizası karesinde gölgeli kaldırım 0,63
    /// mavi/kırmızı okuyordu — aynı yer, aynı saniye, iki kamera, iki
    /// sonuç. Farkı açı yaratıyorsa ışığın kaynağı ekran uzayıdır
    /// (SSGI); yani pişmiş dolaylı ışık <b>hiç yoktu</b>.
    ///
    /// Sebep sahne dosyalarında sayıldı: <c>D_Surici_Dogu</c> 498
    /// nesne, hepsi <c>m_StaticEditorFlags: 0</c>; <c>D_Galata</c> 401
    /// nesne, hepsi 0. Şehrin <b>105.192 çizicisinin tamamı</b> ışığa
    /// katılmıyordu. Prob fırını probu katılan geometrinin çevresine
    /// koyar; katılan hiçbir şey yoksa 2,8 milyon prob boş yamaca
    /// pişer — ve fırın "başarılı" der.
    ///
    /// ## Neden test
    ///
    /// Bu kusur üç ayrı aşamada sessiz kaldı: pişirme başarılı döndü,
    /// dosyalar diske yazıldı, prob sayısı milyonlarcaydı. Onu ancak
    /// <b>bir semtte katılan hiçbir şey yok</b> sorusu yakalar; yeni
    /// bir semt kurulduğunda da aynı soru yakalayacak.
    /// </summary>
    public class GIKatilimiTests
    {
        [Test]
        public void EverySceneHasGeometryThatContributesToBouncedLight()
        {
            var sayim = GIKatilimi.DosyaSayimi();
            Assert.IsNotEmpty(sayim, "Hic sahne bulunamadi.");

            var bos = sayim.Where(x => x.katilan == 0).ToList();
            Assert.IsEmpty(bos,
                "Isiga katilan hicbir nesnesi olmayan sahne(ler): "
                + string.Join(", ", bos.Select(
                    x => $"{x.sahne} (0/{x.toplam})"))
                + ". Prob firini bu sahnede hicbir sey bulamaz; orada "
                + "dolayli isik dogmaz ve golge, yuzeyden bagimsiz, sabit "
                + "bir renge duser. Kur: Hezarfen > Aydinlatma > Sehri "
                + "GI'ya kat, sonra Problari pisir.");
        }
    }
}
