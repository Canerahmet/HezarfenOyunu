using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Hezarfen.Editor.Lighting
{
    /// <summary>
    /// <b>APV kareye ulaşıyor mu</b> — pişmiş verinin ekrana çıktığı
    /// yolu tek tek sorar.
    ///
    /// ## Neden gerekti
    ///
    /// Fırın bitti, problar <b>ışık taşıyor</b> (D_Galata: 62 hücre,
    /// 155 MB, <c>CellData</c> 38.238 farklı desen — ışıksız pişirmede
    /// 2 idi), çalışma zamanı <c>kurulu/kume var</c> diyor, ve sokağın
    /// gölgesi hâlâ mavi/kırmızı <b>0,000</b>.
    ///
    /// Yani veri var, yol açık görünüyor, sonuç değişmiyor. Geriye
    /// aradaki anahtarlar kalıyor ve hepsi tek tek okunabilir:
    ///
    /// * boru hattı varlığında <c>lightProbeSystem</c> APV mi,
    /// * kamera <b>kare ayarlarında</b> (Frame Settings) APV açık mı,
    /// * sahnedeki kameralar kendi kare ayarlarını ezmiş mi.
    ///
    /// Bu deponun dersi burada da geçerli: "çalışıyor olmalı" bir ölçü
    /// değildir.
    /// </summary>
    public static class ApvDenetimi
    {
        [MenuItem("Hezarfen/Aydinlatma/APV yolunu denetle")]
        public static void DenetleMenu() => Debug.Log(Denetle());

        /// <summary>Toplu kip girişi — raporu yazar ve çıkar.</summary>
        public static void DenetleToplu()
        {
            Debug.Log(Denetle());
            EditorApplication.Exit(0);
        }

        public static string Denetle()
        {
            var sb = new StringBuilder("[Hezarfen] APV YOL DENETIMI\n");

            foreach (string guid in AssetDatabase.FindAssets(
                         "t:HDRenderPipelineAsset"))
            {
                string yol = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<
                    HDRenderPipelineAsset>(yol);
                if (asset == null) continue;

                var so = new SerializedObject(asset);
                var sistem = so.FindProperty(
                    "m_RenderPipelineSettings.lightProbeSystem");
                sb.Append($"  {System.IO.Path.GetFileName(yol)}: ");
                sb.Append(sistem != null
                    ? $"lightProbeSystem={sistem.intValue}"
                    : "lightProbeSystem BULUNAMADI");

                // KARE AYARLARI: APV biti burada acik olmazsa prob verisi
                // hic okunmaz — diskte ne oldugu fark etmez.
                var fs = asset.currentPlatformRenderPipelineSettings;
                sb.Append($", probeVolumeMemoryBudget="
                          + $"{fs.probeVolumeMemoryBudget}");
                sb.Append(", kareAyari=");
                sb.Append(KareAyari(asset));
                sb.Append('\n');
            }

            // SAHNEDEKI KAMERALAR KENDI AYARINI EZMIS OLABILIR.
            int ezen = 0, kamera = 0;
            foreach (var cam in Object.FindObjectsByType<Camera>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var hd = cam.GetComponent<HDAdditionalCameraData>();
                if (hd == null) continue;
                kamera++;
                if (!hd.customRenderingSettings) continue;
                ezen++;
                bool acik = hd.renderingPathCustomFrameSettings.IsEnabled(
                    FrameSettingsField.AdaptiveProbeVolume);
                sb.Append($"  KAMERA {cam.name}: kendi kare ayari, "
                          + $"APV={(acik ? "ACIK" : "KAPALI")}\n");
            }
            sb.Append($"  {kamera} HDRP kamerasi, {ezen} tanesi kendi "
                      + "kare ayarini kullaniyor.\n");
            if (kamera == 0)
                sb.Append("  (Sahne acik degil — kamera sayimi bos. "
                          + "Toplu kipte once bir sahne acilmali.)\n");
            return sb.ToString();
        }

        /// <summary>
        /// <b>Kameraya APV'yi açıkça yazar</b> — varsayılan kare
        /// ayarları okunamadığı için tek elimizde kalan kaldıraç.
        ///
        /// ## Neden
        ///
        /// A/B ölçüldü: boru hattı varlıklarının hepsinde
        /// <c>lightProbeSystem</c> <b>kapatılıp</b> tur yeniden koşuldu
        /// ve aynı kare, aynı gölge çıktı —
        /// <c>0,0217/0,0075/0,0001</c> (APV kapalı) ile
        /// <c>0,0202/0,0061/0,0001</c> (APV açık). Yani pişmiş veri
        /// kareye <b>hiç ulaşmıyor</b>: açıp kapatmak hiçbir şeyi
        /// değiştirmiyor.
        ///
        /// Zincirdeki okunabilen her halka sağlam: veri diskte ve
        /// ışıklı (38.238 desen), çalışma zamanı `kurulu/kume var`,
        /// çiziciler `m_LightProbeUsage: 1`, boru hattı
        /// `lightProbeSystem: 1`. Okunamayan tek halka kameranın
        /// <b>kare ayarı</b> — Unity 6'da varsayılanı tutan tür
        /// internal.
        ///
        /// Bu yüzden kameraya kendi kare ayarı verilir ve APV biti
        /// açıkça yazılır. Hipotez doğruysa gölge maviye döner;
        /// yanlışsa ölçü yine değişmez ve halka elenmiş olur.
        /// </summary>
        [MenuItem("Hezarfen/Aydinlatma/Kameraya APV'yi ac")]
        public static void KameraApvAc()
        {
            int n = 0;
            foreach (var cam in Object.FindObjectsByType<Camera>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var hd = cam.GetComponent<HDAdditionalCameraData>();
                if (hd == null) continue;
                hd.customRenderingSettings = true;
                hd.renderingPathCustomFrameSettingsOverrideMask
                    .mask[(uint)FrameSettingsField.AdaptiveProbeVolume] = true;
                hd.renderingPathCustomFrameSettings.SetEnabled(
                    FrameSettingsField.AdaptiveProbeVolume, true);
                EditorUtility.SetDirty(hd);
                n++;
            }
            if (n > 0)
                UnityEditor.SceneManagement.EditorSceneManager
                    .MarkAllScenesDirty();
            Debug.Log($"[Hezarfen] {n} kameraya APV kare ayari yazildi.");
        }

        /// <summary>
        /// <c>ProbeReferenceVolume</c>'un okunabilir üyelerini yazar.
        ///
        /// Zincirin okunabilen her halkası "açık" diyor ve kare
        /// değişmiyor. Sıradaki soru — <b>hücreler gerçekten belleğe
        /// yükleniyor mu</b> — bir sayı ister, ve o sayının adı sürümden
        /// sürüme değişiyor. Tahmin etmek yerine sınıfın kendisine
        /// soruluyor.
        /// </summary>
        [MenuItem("Hezarfen/Aydinlatma/APV uyelerini listele")]
        public static void UyeleriListele()
        {
            var t = typeof(ProbeReferenceVolume);
            var sb = new StringBuilder("[Hezarfen] ProbeReferenceVolume uyeleri\n");
            var inst = ProbeReferenceVolume.instance;
            foreach (var pr in t.GetProperties(
                         System.Reflection.BindingFlags.Public
                         | System.Reflection.BindingFlags.Instance
                         | System.Reflection.BindingFlags.Static))
            {
                object v = "?";
                try { v = pr.GetValue(pr.GetGetMethod().IsStatic ? null : inst); }
                catch (System.Exception e) { v = "hata: " + e.GetType().Name; }
                sb.Append($"  {pr.PropertyType.Name} {pr.Name} = {v}\n");
            }
            Debug.Log(sb.ToString());
        }

        private static string KareAyari(HDRenderPipelineAsset asset)
        {
            var so = new SerializedObject(asset);
            var kok = so.FindProperty(
                "m_RenderingPathDefaultCameraFrameSettings");
            if (kok == null) return "BULUNAMADI";
            // `bitDatas` HDRP'nin kendi bit alani; adi surumden surume
            // degisebilir, o yuzden once dogrudan API denenir.
            // VARSAYILAN KARE AYARLARI OKUNAMIYOR — VE BU KAYDA GECIYOR.
            //
            // Unity 6'da varsayilan kare ayarlari boru hatti varligindan
            // `GraphicsSettings`e tasindi ve tasiyici tur
            // (`RenderingPathFrameSettings`) **internal**: bu derlemeden
            // gorulemiyor. Varliktaki eski alanlar (`bitDatas`) duruyor
            // ama hepsi 0/0 — yani kullanilmiyor; kullanilsalardi golge
            // ve sis de kapali olurdu, oysa karelerde ikisi de var.
            //
            // Yani "kamera kare ayarinda APV acik mi" sorusu buradan
            // cevaplanamiyor. Kamera BAZINDA ezme varsa asagida
            // goruluyor; varsayilan icin Editor penceresi gerekiyor
            // (Project Settings > Graphics > HDRP > Frame Settings).
            var eski = kok.FindPropertyRelative("bitDatas.data1");
            var eski2 = kok.FindPropertyRelative("bitDatas.data2");
            string ham = (eski != null && eski2 != null)
                ? $"eski bitDatas {eski.longValue}/{eski2.longValue} "
                  + "(kullanilmiyor)"
                : "eski bitDatas yok";
            return "varsayilan OKUNAMADI — " + ham;
        }
    }
}
