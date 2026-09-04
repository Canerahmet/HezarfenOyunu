using System.Linq;
using Hezarfen.Editor.Pipeline;
using NUnit.Framework;

namespace Hezarfen.Tests.EditMode
{
    /// <summary>
    /// <b>Hiçbir prefabın malzeme yuvası boş kalmasın</b> — boş yuva
    /// HDRP'de macentadır.
    ///
    /// ## Neden bu test var
    ///
    /// <c>ModelImportPolicy</c> her FBX malzemesini adına göre proje
    /// malzemesiyle eşler ve sonucu <b>içe aktarma anında</b> prefaba
    /// pişirir. Proje malzemesi o an yoksa FBX'in gömülü malzemesi
    /// kalır; malzeme sonradan üretilince model düzelir ama prefabın
    /// yuvası <b>boşa düşer</b>. İki durumda da ekranda macenta çıkar.
    ///
    /// Ölçüldüğünde 204 prefabta 346 boş yuva vardı — yani şehrin büyük
    /// bir bölümü. Ve hiçbir test kırmızı dönmüyordu: bir malzeme
    /// referansı boş olduğunda Unity ne hata verir ne uyarır, yalnızca
    /// boyar. Bu, bu depoda tekrar eden dersin bir örneği daha —
    /// <i>görülmediği yerde ölçülmesi gerekir</i>. Kusur ancak oyun turu
    /// karesine bakınca göründü: oyuncunun sırtındaki kanat, on durağın
    /// onunda da ekranın yarısını kaplayan parlak mor bir levhaydı.
    ///
    /// Onarımı <c>ModelYeniden.Yenile</c> yapar; bu test onun
    /// yapıldığını <b>doğrular</b> ve bir daha kaçmasını engeller.
    /// </summary>
    public class MalzemeBagiTests
    {
        [Test]
        public void NoPrefabHasAnEmptyMaterialSlot()
        {
            var bos = ModelYeniden.BosYuvalar();
            Assert.IsEmpty(bos,
                $"{bos.Count} bos malzeme yuvasi — HDRP bunlari MACENTA "
                + "cizer. Kur: Hezarfen > Boru Hatti > Modelleri yeniden "
                + "ice aktar. Ilk on tanesi: "
                + string.Join(", ", bos.Take(10)));
        }

        /// <summary>
        /// Bildirimde <c>doubleSided</c> yazan her malzeme gerçekten iki
        /// yüzlü olmalı.
        ///
        /// Kanat zarı tek katmanlı bir yamuktur ve <c>M_Feather</c>
        /// <c>_CullMode: 2</c> ile geliyordu. Oyun turu karesinde ölçüldü:
        /// karakter arkadan görünürken kanatlardan yalnız çıtalar
        /// çiziliyordu — "sırtında merdiven taşıyan adam". Mesh eksik
        /// değildi, alt yüzü elenmişti.
        ///
        /// Bu, aynı varlığın ikinci sessiz kusuru (ilki macenta yuvaydı) ve
        /// ikisi de yalnız KAREYE BAKINCA göründü. Test o bakışın yerine
        /// geçmiyor; bulunanı bir daha kaçırmamak için duruyor.
        /// </summary>
        [Test]
        public void EveryDoubleSidedMaterialIsActuallyDoubleSided()
        {
            var eksik = OttomanMaterialBuilder.EksikCiftTarafli();
            Assert.IsEmpty(eksik,
                $"{eksik.Count} malzeme bildiriminde iki yuzlu yaziyor ama "
                + "malzemesinde kapali — ince yuzey alttan bakilinca YOK "
                + "olur. Kur: Hezarfen > Boru Hatti > Malzemeleri uret. "
                + string.Join(", ", eksik));
        }

        [Test]
        public void NoModelStillCarriesAnEmbeddedMaterial()
        {
            // Gomulu malzeme, esmelenin HIC yapilmadigi anlamina gelir:
            // proje malzemesi yok, Unity de FBX'in kendi Standard
            // gölgelendiricili malzemesini birakmis. Olcu aletinin
            // (`SM_AxisCalibration`) renk kodlari bilerek muaf ve
            // muafiyet `ModelYeniden` icinde yazili — burada ikinci bir
            // liste tutmuyoruz.
            var gomulu = ModelYeniden.GomuluMalzemeler();
            Assert.IsEmpty(gomulu,
                $"{gomulu.Count} model ici malzeme kaldi: "
                + string.Join(", ", gomulu.OrderBy(a => a))
                + ". Once: blender tools/textures/build_unity_maps.py, "
                + "sonra Hezarfen > Boru Hatti > Osmanli malzemelerini "
                + "uret, sonra Modelleri yeniden ice aktar.");
        }
    }
}
