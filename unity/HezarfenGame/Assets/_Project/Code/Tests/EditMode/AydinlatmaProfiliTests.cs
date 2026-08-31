using NUnit.Framework;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using VolumeProfile = UnityEngine.Rendering.VolumeProfile;

namespace Hezarfen.Tests
{
    /// <summary>
    /// <b>Işık profili DİSKTE ne taşıyor.</b>
    ///
    /// Bir aydınlatma geçişi çalıştı, konsola altı satır "eklendi"
    /// yazdı ve diskteki profil hiç değişmedi. Sebep:
    /// <c>VolumeProfile.Add&lt;T&gt;()</c> bileşeni yalnız bellekte
    /// kurar; <c>AddObjectToAsset</c> çağrılmazsa varlığın parçası
    /// olmaz ve <c>SaveAssets</c> onu yazmaz.
    ///
    /// Bu tuzak depoda ikinci kez kuruldu — <c>KaliciAydinlatma</c>
    /// aynı hatayı yaşayıp gerekçesini yazmıştı ve yeni dosya o deyimi
    /// kullanmadı. Yani ders <b>yazılıydı</b> ama tutulmuyordu; bir
    /// dersi tutan şey yorum değil testtir.
    ///
    /// Test menü logunu değil <b>varlık dosyasını</b> okur. Aradaki
    /// fark tam olarak bu kusurun yaşadığı yerdi.
    /// </summary>
    public class AydinlatmaProfiliTests
    {
        private const string Yol =
            "Assets/_Project/Settings/VP_Kalici_Aydinlatma.asset";

        private static VolumeProfile Profil()
        {
            var p = AssetDatabase.LoadAssetAtPath<VolumeProfile>(Yol);
            Assert.IsNotNull(p, $"{Yol} yok.");
            return p;
        }

        /// <summary>
        /// Sahnenin maket gibi durmasını engelleyen katmanlar.
        ///
        /// Liste keyfi değil: her biri karede <b>görülebilir</b> bir
        /// eksikliğe karşılık geliyor. Ortam örtme ve temas gölgesi
        /// olmadan hiçbir nesne zemine değmez; renk derecelendirme
        /// olmadan palet nötr kalır; sis olmadan derinlik okunmaz.
        /// </summary>
        [Test]
        public void TheLightingPassActuallyReachedTheDisk()
        {
            var p = Profil();

            Assert.IsTrue(p.TryGet<ScreenSpaceAmbientOcclusion>(out var ao)
                          && ao != null,
                "Ortam ortme profilde YOK — her nesne bir parmak havada "
                + "durur ve sahne maket okunur.");
            Assert.IsTrue(p.TryGet<ContactShadows>(out var temas)
                          && temas != null,
                "Temas golgesi profilde YOK.");
            Assert.IsTrue(p.TryGet<Bloom>(out var bloom) && bloom != null,
                "Bloom profilde YOK.");
            Assert.IsTrue(p.TryGet<ColorAdjustments>(out var renk)
                          && renk != null,
                "Renk duzenlemesi profilde YOK — palet notr kalir.");
            Assert.IsTrue(p.TryGet<LiftGammaGain>(out var lgg) && lgg != null,
                "Lift/Gamma/Gain profilde YOK.");
            Assert.IsTrue(p.TryGet<Vignette>(out var vig) && vig != null,
                "Vinyet profilde YOK.");
        }

        /// <summary>
        /// Temel katmanlar — bunlar zaten vardı ve gerilememeli.
        /// </summary>
        [Test]
        public void TheFoundationLayersAreStillThere()
        {
            var p = Profil();
            Assert.IsTrue(p.TryGet<Exposure>(out _), "Poz yok.");
            Assert.IsTrue(p.TryGet<Tonemapping>(out _), "Tonemap yok.");
            Assert.IsTrue(p.TryGet<Fog>(out _), "Sis yok.");
            Assert.IsTrue(p.TryGet<GlobalIllumination>(out _), "SSGI yok.");
        }

        /// <summary>
        /// Sayı da sorulur, çünkü <c>TryGet</c> tek tek sorar ve bir
        /// gün biri profili elle boşaltırsa bunu tek satırda görmek
        /// isteriz.
        /// </summary>
        [Test]
        public void TheProfileCarriesAtLeastElevenOverrides()
        {
            var p = Profil();
            Assert.GreaterOrEqual(p.components.Count, 11,
                $"Profilde {p.components.Count} bilesen var. Bes bilesenle "
                + "bir tur gecti ve 'on bir katman' diye rapor edildi; "
                + "sayiyi diskten okumak o yanilgiyi imkansiz kilar. "
                + "Kur: Hezarfen -> Aydinlatma -> ... , sonra bu testi kos.");
        }

        /// <summary>
        /// Bileşenler gerçekten <b>varlığın içinde</b> mi.
        ///
        /// En sinsi hâl bu: profil bellekte on bir bileşen sayar ama
        /// hiçbiri alt-varlık değildir, dosya beşte kalır ve oyun
        /// yüklenince beşini görür. Sayının kendisi yalan
        /// söyleyebilir; dosyanın içindekiler söyleyemez.
        /// </summary>
        [Test]
        public void EveryOverrideIsASubAssetNotJustAMemoryObject()
        {
            var p = Profil();
            var altlar = AssetDatabase.LoadAllAssetsAtPath(Yol);
            int sayilan = 0;
            foreach (var a in altlar)
                if (a is VolumeComponent) sayilan++;

            Assert.AreEqual(p.components.Count, sayilan,
                $"Profil {p.components.Count} bilesen sayiyor ama diskte "
                + $"{sayilan} alt-varlik var. Aradaki fark, "
                + "AddObjectToAsset cagrilmadan eklenmis bilesenlerdir "
                + "ve oyun onlari HIC gormez.");
        }
    }
}
