using Hezarfen.Editor.Lighting;
using NUnit.Framework;
using UnityEditor.SceneManagement;

namespace Hezarfen.Tests.EditMode
{
    /// <summary>
    /// <b>Prob fırınının bir gökyüzü olsun.</b>
    ///
    /// ## Ölçüm
    ///
    /// APV pişirildikten sonra turun kareleri ölçüldü. Gölgedeki sokak
    /// zemini <c>(36, 15, 0)</c> çıktı — <b>mavi kanal sıfır</b>. Açık
    /// gök altındaki gölge güneşten daha mavidir, daha az değil; mavinin
    /// hiç olmaması "burası karanlık" demek değil, <b>gök hiç katkı
    /// vermiyor</b> demektir. Aynı turda, aynı pozla çekilen Galata
    /// sokağının gölgesi 0,76 mavi/kırmızı oranı taşıyordu; yani kusur
    /// pozda ya da tonlamada değil, yerdeydi.
    ///
    /// Sebep sahne dosyasında yazılıydı: <c>StaticLightingSky</c> nesnesi
    /// vardı ve <c>m_Profile</c> alanı <c>{fileID: 0}</c>. Fırın
    /// 2.829.507 probu gökyüzüsüz pişirdi.
    ///
    /// ## Neden test
    ///
    /// Pişirme "başarılı" döner, dosyalar diske yazılır, sayı büyüktür —
    /// hiçbir uyarı çıkmaz. Bu depoda aynı sınıf kusur dördüncü kez
    /// görüldü: nesne var, iş koştu, taşıması gereken şey bağlanmamış.
    /// Onu tutan tek şey, bağın <b>kendisini</b> okuyan bir ölçüdür.
    ///
    /// Test bir sayıyı kopyalamıyor: <see cref="KaliciAydinlatma.GokBagli"/>
    /// ölçünün tek sahibi ve boru hattı da aynı yerden kuruyor
    /// (<see cref="KaliciAydinlatma.GokAyari"/>).
    /// </summary>
    public class FirinGokyuzuTests
    {
        private const string Sahne =
            "Assets/_Project/Scenes/Faz1_Terrain.unity";

        [Test]
        public void TheProbeBakeHasASkyToBakeFrom()
        {
            var s = EditorSceneManager.GetSceneByPath(Sahne);
            if (!s.isLoaded)
                EditorSceneManager.OpenScene(Sahne, OpenSceneMode.Additive);

            bool bagli = KaliciAydinlatma.GokBagli(out string neden);
            Assert.IsTrue(bagli,
                "Firinin gokyuzu bagli DEGIL: " + neden
                + " — bu durumda APV problari yalniz gunesin sicrayan "
                + "sicak payini tasir, golgede mavi kanal sifira duser. "
                + "Kur: Hezarfen > Aydinlatma > Kalici isik pasini kur, "
                + "sonra problari yeniden pisir.");
        }
    }
}
