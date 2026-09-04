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

        /// <summary>
        /// <b>Fırında en az bir ışık olsun.</b>
        ///
        /// ## Ölçüm
        ///
        /// Problar L0 = <b>tam sıfır</b> pişti ve sebebi üç deneyle
        /// bulundu (ADR 0087). Gök profilini Lux kipinde 20.000'e
        /// sabitlemek de, fırına gerçek bir skybox malzemesi vermek de
        /// (ortam probu 0,037 → 0,18/0,23/0,30) `CellData`yı iki desende
        /// bıraktı. Güneş <c>Mixed</c> yapılınca desen sayısı
        /// <b>12.106</b> oldu.
        ///
        /// Yani <b>fırın, içinde hiç ışık yoksa prob aydınlatması
        /// üretmiyor.</b>
        ///
        /// ## Neden test
        ///
        /// Bu ayar bir alan: birinin güneşi <c>Realtime Only</c>'ye
        /// çevirmesi yetiyor ve fırın yine "başarılı" diyor, yalnızca
        /// şehir kararıyor. İki saatlik bir pişirmenin sessizce boşa
        /// gitmesi bu turda dört kez oldu.
        ///
        /// Ölçü sahne dosyasından okunur: yönlü, şiddeti sıfırdan büyük
        /// ve <c>m_Lightmapping</c> değeri <b>4 (Realtime)</b>
        /// olmayan en az bir ışık.
        /// </summary>
        [Test]
        public void AtLeastOneSunReachesTheBake()
        {
            const string yol = "Assets/_Project/Scenes/Faz1_Terrain.unity";
            Assert.IsTrue(System.IO.File.Exists(yol), $"{yol} yok.");

            var satir = System.IO.File.ReadAllLines(yol);
            int yonlu = 0, firinda = 0;
            for (int i = 0; i < satir.Length; i++)
            {
                if (satir[i] != "Light:") continue;
                int tur = -1, lm = -1;
                float siddet = 0f;
                for (int j = i; j < System.Math.Min(i + 60, satir.Length); j++)
                {
                    if (satir[j].StartsWith("  m_Type: "))
                        int.TryParse(satir[j].Substring(10).Trim(), out tur);
                    else if (satir[j].StartsWith("  m_Intensity: "))
                        float.TryParse(satir[j].Substring(15).Trim(),
                            System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture,
                            out siddet);
                    else if (satir[j].StartsWith("  m_Lightmapping: "))
                        int.TryParse(satir[j].Substring(18).Trim(), out lm);
                }
                if (tur != 1 || siddet <= 0f) continue;
                yonlu++;
                if (lm != 4) firinda++;   // 4 = Realtime Only
            }

            Assert.Greater(yonlu, 0,
                "Taban sahnede siddeti sifirdan buyuk yonlu isik yok — "
                + "olcu bos donuyor.");
            Assert.Greater(firinda, 0,
                "Butun yonlu isiklar Realtime Only. Firin ISIKSIZ kosar "
                + "ve problar L0 = sifir piser: sehirde dolayli isik "
                + "olmaz, ama pisirme yine 'basarili' der. Gunes Mixed "
                + "olmali (karma kip IndirectOnly, yani golgeler yine "
                + "saati izler). Gerekce: ADR 0087.");
        }

        /// <summary>
        /// <b>Fırın ayarları: CPU ve yalnız dolaylı.</b>
        ///
        /// İki alan, ikisi de bu turda ölçümle kazanıldı ve ikisi de tek
        /// tıkla geri alınabilir.
        ///
        /// <b>Arka uç CPU olmalı.</b> GPU fırını iki kez süreç hiçbir şey
        /// yazmadan yok olarak bitti ve sebebi kayıtta tek satırdı:
        /// <i>"Transformed OOTS snapshot into LightBaker scene input …
        /// Size: 7251,37 MB"</i> — bu makinenin kartı <b>8 GB</b>.
        /// Sistem belleği 32 GB.
        ///
        /// <b>Karma kip `IndirectOnly` olmalı.</b> Güneş artık fırına
        /// giriyor (yoksa problar L0 = sıfır pişiyor, ADR 0087) ve
        /// hangi yarısının pişeceğini bu alan söylüyor. `Shadowmask`
        /// seçilirse <b>gölgeler pişirildiği saate çakılır</b> ve
        /// <c>ZamanSistemi</c> güneşi döndürdükçe gölgeler yerinde
        /// kalır — günün her saati yanlış. `IndirectOnly` yalnız
        /// sıçramayı dondurur.
        /// </summary>
        [Test]
        public void TheBakeStaysOnTheCpuAndBakesOnlyTheIndirectTerm()
        {
            const string yol =
                "Assets/_Project/Settings/LS_Hezarfen.lighting";
            Assert.IsTrue(System.IO.File.Exists(yol), $"{yol} yok.");

            int karma = -1, arkaUc = -1;
            float albedo = -1f;
            foreach (string satir in System.IO.File.ReadLines(yol))
            {
                if (satir.StartsWith("  m_MixedBakeMode: "))
                    int.TryParse(satir.Substring(19).Trim(), out karma);
                else if (satir.StartsWith("  m_BakeBackend: "))
                    int.TryParse(satir.Substring(17).Trim(), out arkaUc);
                else if (satir.StartsWith("  m_AlbedoBoost: "))
                    float.TryParse(satir.Substring(17).Trim(),
                        System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out albedo);
            }

            Assert.AreEqual(1, arkaUc,
                "Fırın arka ucu Progressive CPU olmali (m_BakeBackend 1). "
                + "GPU firini bu makinede iki kez sessizce oldu: sahne "
                + "girdisi 7,25 GB, kart 8 GB.");
            // DENEY DEGERI VARLIKTA KALMASIN.
            //
            // `-hezarfenAlbedo 8` bir kez kullanildi ve deger bu
            // varlikta KALDI; sonraki pisirme farkinda olmadan sekiz
            // kat albedo ile kostu — hem de baska bir degiskeni sinayan
            // pisirme. Bir deney anahtari, kapatildiginda dunyayi eski
            // haline birakmiyorsa deney degil sessiz bir ayar
            // degisikligidir.
            Assert.AreEqual(1f, albedo, 0.001f,
                "albedoBoost 1 olmali. Baska bir deger, kapatilmayi "
                + "unutulmus bir deney anahtarindan kalmis demektir ve "
                + "sonraki her pisirmeyi sessizce kirletir.");
            Assert.AreEqual(0, karma,
                "Karma kip IndirectOnly olmali (m_MixedBakeMode 0). "
                + "Shadowmask secilirse golgeler pisirildigi saate "
                + "cakilir ve ZamanSistemi gunesi dondurdukce yanlis "
                + "yerde kalirlar. Gerekce: ADR 0087.");
        }
    }
}