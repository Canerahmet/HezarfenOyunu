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

        /// <summary>
        /// <b>Gece gündüzden koyu mu.</b>
        ///
        /// Bir tur boyunca ay eklendi, fener yazıldı, gece karesi
        /// ölçüldü — ve aynı profildeki otomatik pozlama hepsini geri
        /// aldı: <c>limitMin</c> −1,0'dayken kelepçe ısırmıyor,
        /// histogram geceyi de orta griye çekiyordu. Ay ışığında bir
        /// sıva duvarın pozu EV ≈ −1,25; sınır onun <b>üstünde</b>
        /// olmazsa gece diye bir şey yok.
        ///
        /// Bu test o sayıyı diskten okur, çünkü kaynaktan okusaydı
        /// yalnız benim niyetimi ölçerdi.
        /// </summary>
        [Test]
        public void NightIsActuallyDarkerThanDay()
        {
            var p = Profil();
            Assert.IsTrue(p.TryGet<UnityEngine.Rendering.HighDefinition.Exposure>(
                              out var poz), "Profilde poz bileseni yok.");
            Assert.GreaterOrEqual(poz.limitMin.value, 1.5f,
                $"Poz alt siniri {poz.limitMin.value:F2} EV. Gecenin "
                + "kendi pozu ~ -1,25 EV; sinir onun altinda kalirsa "
                + "kelepce hic isirmaz ve gece gunduzle ayni "
                + "parlaklikta cizilir.");
            Assert.Greater(poz.limitMax.value, poz.limitMin.value + 8f,
                "Gunduz ile gece arasinda sekiz duraktan az fark var.");
        }

        /// <summary>
        /// <b>Uzaktaki şehir gölge düşürüyor mu.</b>
        ///
        /// Profilde <c>HDShadowSettings</c> hiç yoktu ve HDRP
        /// varsayılanı 150 m: kule şerefesinden bakan oyuncu için
        /// Süleymaniye, Ayasofya ve bütün Sûriçi gölgesizdi.
        /// </summary>
        [Test]
        public void TheCityCastsShadowsBeyondTheNeighbourhood()
        {
            var p = Profil();
            Assert.IsTrue(p.TryGet<UnityEngine.Rendering.HighDefinition.HDShadowSettings>(
                              out var g), "Profilde golge ayari yok.");
            Assert.GreaterOrEqual(g.maxShadowDistance.value, 300f,
                $"Golge mesafesi {g.maxShadowDistance.value:F0} m. "
                + "Bir acik dunyada bu, sehri kesilmis karton yapar.");
        }

        /// <summary>
        /// <b>Hiçbir evde sert LOD sıçraması kalmadı mı.</b>
        ///
        /// Geçiş yumuşatma komutu yazıldı, koştu ve <b>ev ailesi 26'dan
        /// 201'e büyümeden önce</b> koştu. Ölçüldüğünde 213 prefab hâlâ
        /// <c>FadeMode: None</c> taşıyordu ve 201'i evdi — yani şehrin
        /// 10.868 evinin tamamı tek karede mesh değiştiriyordu.
        ///
        /// Tek atımlık bir düzeltme, varlık üretiminin arkasında kalır.
        /// Kapıya bağlanan bir sayı kalmaz.
        /// </summary>
        [Test]
        public void NoPrefabStillSnapsBetweenLods()
        {
            int n = Hezarfen.Editor.Lighting.LodGecisi.SertSicramaSayisi();
            Assert.AreEqual(0, n,
                $"{n} prefabta hala sert LOD sicramasi var. Kur: "
                + "Hezarfen -> Aydinlatma -> LOD gecislerini yumusat.");
        }
    }
}
