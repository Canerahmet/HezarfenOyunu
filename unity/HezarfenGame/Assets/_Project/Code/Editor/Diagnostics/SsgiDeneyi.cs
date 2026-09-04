using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Hezarfen.Editor.Diagnostics
{
    /// <summary>
    /// <b>Ekran uzayı GI'yi bir DENEY olarak açar/kapatır.</b>
    ///
    /// Kapalı gölgenin neyle dolduğu ADR 0087'nin açık sorusu. Boru
    /// hattı varlığında <c>supportSSGI</c> yalnız <i>High Fidelity</i>'de
    /// 1 ve profilde <c>GlobalIllumination</c> geçersiz kılması <b>hiç
    /// yok</b> — yani seviyeyi değiştirmek yeteneği açıyor, isteği
    /// açmıyor. İlk deneyde ölçüldü: kalite High Fidelity'ye alındı ve
    /// altı karede gölge oranı gürültü kadar oynadı (0,009→0,007,
    /// 0,140→0,107, 0,259→0,282). Yani o deney SSGI'yi sınamamıştı.
    ///
    /// Bu komut isteği de açar. Bir çözüm önerisi değil, tek değişkenli
    /// bir ölçüm: sonuç okunduktan sonra <c>Kapat</c> ile geri alınır.
    /// Kalıcı olacaksa kare bütçesiyle (16,7 ms) birlikte ölçülür ve
    /// ADR'ye yazılır.
    /// </summary>
    public static class SsgiDeneyi
    {
        private const string Profil =
            "Assets/_Project/Settings/VP_Kalici_Aydinlatma.asset";

        [MenuItem("Hezarfen/Denetim/DENEY: ekran uzayi GI ac")]
        public static void Ac() { Ayarla(true); }

        [MenuItem("Hezarfen/Denetim/DENEY: ekran uzayi GI kapat")]
        public static void Kapat() { Ayarla(false); }

        private static void Ayarla(bool acik)
        {
            var vp = AssetDatabase.LoadAssetAtPath<VolumeProfile>(Profil);
            if (vp == null) { Debug.LogError($"[Hezarfen] {Profil} yok."); return; }

            if (!vp.TryGet<GlobalIllumination>(out var gi))
            {
                if (!acik) { Debug.Log("[Hezarfen] SSGI zaten yok."); return; }
                // Alt-varlik olarak eklendigini DOGRULA — bu komutun
                // ilk surumu eklediginden emin olmadan "ACIK" yazdi.
                // `VolumeProfile.Add` bileseni BELLEKTE kurar; profil
                // bir varlik oldugu icin alt-varlik olarak da
                // eklenmezse diske hic yazilmaz. Ilk yazimda bu satir
                // yoktu: komut "ACIK" dedi, tur kosuldu, hicbir sey
                // degismedi ve profilde `GlobalIllumination` kelimesi
                // bile yoktu. Yine ayni ders — yazildi, diske gecmedi.
                gi = vp.Add<GlobalIllumination>(true);
                AssetDatabase.AddObjectToAsset(gi, vp);
            }
            gi.active = acik;
            gi.enable.overrideState = true;
            gi.enable.value = acik;
            // `ambientProbeDimmer` varsayilani 0: SSGI'nin veri
            // bulamadigi yerde ortam probu tamamen kisilir. Kapali
            // golge tam da o yerdir — kisik birakmak deneyi bastan
            // kaybettirir.
            gi.ambientProbeDimmer.overrideState = true;
            gi.ambientProbeDimmer.value = 1.0f;
            gi.fullResolutionSS.overrideState = true;
            gi.fullResolutionSS.value = true;

            EditorUtility.SetDirty(gi);
            EditorUtility.SetDirty(vp);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(Profil, ImportAssetOptions.ForceUpdate);
            var _kontrol = AssetDatabase.LoadAssetAtPath<VolumeProfile>(Profil);
            bool _diskte = _kontrol != null
                           && _kontrol.TryGet<GlobalIllumination>(out var _g2)
                           && _g2.enable.value == acik;
            if (!_diskte && acik)
            {
                Debug.LogError("[Hezarfen] SSGI diske YAZILMADI — deney "
                               + "kosulmus sayilmaz.");
                return;
            }
            Debug.Log($"[Hezarfen] DENEY: ekran uzayi GI "
                      + (acik ? "ACIK" : "KAPALI")
                      + $" (enable={gi.enable.value}, "
                      + $"ortamProbu={gi.ambientProbeDimmer.value:0.00}). "
                      + "Yalniz High Fidelity boru hattinda etkisi var "
                      + "(Balanced'da supportSSGI 0).");
        }
    }
}
