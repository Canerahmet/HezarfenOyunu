using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Hezarfen.Editor.Gis
{
    /// <summary>
    /// Faz 1 gökyüzü/sis/pozlama profilini üretir (ADR 0009).
    ///
    /// **Neden ayrı bir script:** `VolumeProfile.Add&lt;T&gt;()` bileşeni bellekte kurar
    /// ama asset'e **alt-nesne olarak eklemez**. `AssetDatabase.AddObjectToAsset`
    /// çağrılmazsa bileşenler diske yazılmaz ve ilk domain reload'da sessizce kaybolur:
    /// profil "var" görünür, içi boştur, sahne varsayılan ayarlara düşer.
    ///
    /// Bu tam olarak yaşandı — performans ölçümü, sis kapalı olduğu hâlde sis açık
    /// sanılarak koşturuldu. Hata hiçbir uyarı üretmedi; yalnızca çalışma zamanında
    /// profilin bileşen sayısı okunduğu için yakalandı.
    ///
    /// Paylaşılan `Assets/Settings/SkyandFogSettingsProfile.asset` bilerek
    /// DEĞİŞTİRİLMEZ: `FlightSlice` sahnesinde Caner'in uçuş kapısı hâlâ açık.
    /// </summary>
    public static class SkyProfileBuilder
    {
        public const string ProfilePath = "Assets/_Project/Art/Settings/VP_Faz1_Sky.asset";

        [MenuItem("Hezarfen/GIS/Faz1 gokyuzu profilini uret")]
        public static void BuildMenu()
        {
            var vp = Build();
            Selection.activeObject = vp;
            Debug.Log($"[Hezarfen] {ProfilePath} — {vp.components.Count} bilesen (diske yazildi).");
        }

        public static VolumeProfile Build()
        {
            EnsureFolder("Assets/_Project/Art/Settings");

            // Temiz kur: eski alt-nesneler kalirsa profil sisirilir.
            AssetDatabase.DeleteAsset(ProfilePath);
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, ProfilePath);

            var ve = AddPersisted<VisualEnvironment>(profile);
            ve.skyType.overrideState = true;
            ve.skyType.value = (int)SkyType.PhysicallyBased;
            ve.skyAmbientMode.overrideState = true;
            // Static ambient FIRINLANMIS isik ister; firinlanmamis sahnede ortam
            // siyah kalir ve volumetrik sis her seyi yutar (ADR 0009).
            ve.skyAmbientMode.value = SkyAmbientMode.Dynamic;

            AddPersisted<PhysicallyBasedSky>(profile);

            var fog = AddPersisted<Fog>(profile);
            fog.enabled.overrideState = true; fog.enabled.value = true;
            fog.meanFreePath.overrideState = true; fog.meanFreePath.value = 8000f;
            fog.baseHeight.overrideState = true; fog.baseHeight.value = 0f;
            fog.maximumHeight.overrideState = true; fog.maximumHeight.value = 400f;

            var ex = AddPersisted<Exposure>(profile);
            ex.mode.overrideState = true;
            // Sabit pozlama: inceleme kareleri edit-mode'da tek kare render edilir ve
            // otomatik pozlamanin histogrami orada yakinsamaz (ADR 0009).
            ex.mode.value = ExposureMode.Fixed;
            ex.fixedExposure.overrideState = true; ex.fixedExposure.value = 14.5f;

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(ProfilePath, ImportAssetOptions.ForceUpdate);
            return AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
        }

        /// <summary>Bileşeni profile ekler VE asset'e alt-nesne olarak yazar.</summary>
        private static T AddPersisted<T>(VolumeProfile profile) where T : VolumeComponent
        {
            var comp = profile.Add<T>(true);
            comp.hideFlags = HideFlags.HideInHierarchy;   // Unity'nin kendi kuralı
            AssetDatabase.AddObjectToAsset(comp, profile);
            return comp;
        }

        private static void EnsureFolder(string assetFolder)
        {
            if (AssetDatabase.IsValidFolder(assetFolder)) return;
            string[] parts = assetFolder.Split('/');
            string acc = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{acc}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(acc, parts[i]);
                acc = next;
            }
        }
    }
}
