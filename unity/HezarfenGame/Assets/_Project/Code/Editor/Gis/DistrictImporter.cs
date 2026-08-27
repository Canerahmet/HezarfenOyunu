using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hezarfen.Core;
using Hezarfen.Streaming;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Hezarfen.Editor.Gis
{
    /// <summary>
    /// `districts_build.py` çıktısını Unity varlıklarına çevirir — plan Faz 1 madde 6.
    ///
    /// Üretilenler:
    ///   * `Data/DistrictDefs/DD_&lt;id&gt;.asset` — bölge tanımı
    ///   * `Data/DistrictDefs/DistrictRegistry.asset` — tek liste
    ///   * `Scenes/Districts/&lt;id&gt;.unity` — YOKSA boş sahne (varsa DOKUNULMAZ)
    ///   * her sahne için Addressables girdisi, adres `Districts/&lt;id&gt;`
    ///
    /// **Var olan sahnenin üzerine asla yazılmaz.** Bu içe aktarıcı sınır poligonu
    /// değiştiğinde tekrar tekrar koşacak; sahneyi yeniden oluştursaydı Faz 4'te o
    /// semte konulmuş bütün içerik sessizce silinirdi. Sahne bir kez iskele olarak
    /// doğar, sonrası içeriğindir.
    /// </summary>
    public static class DistrictImporter
    {
        public const string LocalJsonPath = "data/gis/istanbul/districts_local.json";
        public const string DefFolder = "Assets/_Project/Data/DistrictDefs";
        public const string SceneFolder = "Assets/_Project/Scenes/Districts";
        public const string RegistryPath = DefFolder + "/DistrictRegistry.asset";
        public const string AddressableGroupName = "Districts";

        [MenuItem("Hezarfen/GIS/Semtleri ice aktar")]
        public static void ImportMenu()
        {
            var registry = Import();
            if (registry != null)
            {
                Selection.activeObject = registry;
                EditorGUIUtility.PingObject(registry);
            }
        }

        public static DistrictRegistry Import()
        {
            string repo = TerrainImporter.RepositoryRoot();
            if (repo == null) { Debug.LogError("[Hezarfen] Depo koku bulunamadi."); return null; }

            string path = Path.Combine(repo, LocalJsonPath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path))
            {
                Debug.LogError($"[Hezarfen] {LocalJsonPath} yok. Once: " +
                               "tools/gis/districts_build.py");
                return null;
            }

            var parsed = GeoJsonImporter.ParseLocal(File.ReadAllText(path));
            var raw = File.ReadAllText(path);
            var districts = parsed.features.Where(f => f.layer == "district").ToList();
            if (districts.Count == 0)
            {
                Debug.LogError("[Hezarfen] districts_local.json icinde 'district' katmani yok.");
                return null;
            }

            EnsureFolder(DefFolder);
            EnsureFolder(SceneFolder);

            var defs = new List<DistrictDef>();
            int newScenes = 0;

            foreach (var feat in districts)
            {
                // Sayisal alanlar ozellik gövdesinden okunur; GeoJsonImporter yalnizca
                // ortak alanlari cozer, bunlari tanimaz (bilerek — o ayristirici dar tutuldu).
                string body = ExtractFeatureBody(raw, feat.id);

                string assetPath = $"{DefFolder}/DD_{feat.id}.asset";
                var def = AssetDatabase.LoadAssetAtPath<DistrictDef>(assetPath);
                bool created = def == null;
                if (created) def = ScriptableObject.CreateInstance<DistrictDef>();

                def.districtId = feat.id;
                def.displayName = feat.name;
                def.summary = feat.note;
                def.priority = Mathf.Clamp(ReadInt(body, "priority", 2), 1, 3);
                def.kind = ReadString(body, "kind") == "water"
                    ? DistrictKind.Water : DistrictKind.Land;
                def.sceneAddress = ReadString(body, "scene_address");
                if (string.IsNullOrEmpty(def.sceneAddress))
                    def.sceneAddress = $"Districts/{feat.id}";

                def.loadDistanceMeters = ReadFloat(body, "load_distance_m", 700f);
                def.unloadDistanceMeters = ReadFloat(body, "unload_distance_m", 910f);
                def.center = new Vector2(ReadFloat(body, "center_x", 0f),
                                         ReadFloat(body, "center_z", 0f));
                def.radiusMeters = ReadFloat(body, "radius_m", 0f);
                def.landAreaHectares = ReadFloat(body, "land_ha", 0f);
                def.exclusiveLandAreaHectares = ReadFloat(body, "exclusive_land_ha", 0f);

                var ring = feat.rings.Count > 0 ? feat.rings[0] : null;
                def.boundary = ring == null
                    ? new Vector2[0]
                    : ring.Select(p => new Vector2(p.x, p.z)).ToArray();

                if (created) AssetDatabase.CreateAsset(def, assetPath);
                else EditorUtility.SetDirty(def);
                defs.Add(def);

                if (EnsureDistrictScene(def)) newScenes++;
            }

            // --- kayit defteri ---
            var registry = AssetDatabase.LoadAssetAtPath<DistrictRegistry>(RegistryPath);
            if (registry == null)
            {
                registry = ScriptableObject.CreateInstance<DistrictRegistry>();
                AssetDatabase.CreateAsset(registry, RegistryPath);
            }
            registry.districts = defs.OrderBy(d => d.priority)
                                     .ThenBy(d => d.districtId).ToArray();
            EditorUtility.SetDirty(registry);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            int addressed = RegisterAddressables(defs);

            float p1 = defs.Where(d => d.priority == 1).Sum(d => d.exclusiveLandAreaHectares);
            Debug.Log(
                $"[Hezarfen] {defs.Count} semt ice aktarildi ({newScenes} yeni sahne, " +
                $"{addressed} Addressables girdisi).\n" +
                $"Cakismasiz kara: oncelik-1 {p1:F0} ha " +
                $"(toplam {defs.Sum(d => d.exclusiveLandAreaHectares):F0} ha).\n" +
                "Bu poligonlar OYUN bolgeleridir — tarihsel mahalle siniri DEGILDIR (ADR 0011).");
            return registry;
        }

        // ------------------------------------------------------------- sahne

        /// <summary>Sahne yoksa iskele olarak üretir. Varsa DOKUNMAZ. Yeni ürettiyse true.</summary>
        private static bool EnsureDistrictScene(DistrictDef def)
        {
            string scenePath = $"{SceneFolder}/{def.districtId}.unity";
            if (File.Exists(scenePath)) return false;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
                                                    NewSceneMode.Additive);
            var root = new GameObject(def.districtId);
            SceneManager.MoveGameObjectToScene(root, scene);

            var anchor = root.AddComponent<DistrictAnchor>();
            anchor.districtId = def.districtId;

            var tag = root.AddComponent<HistoricalTag>();
            tag.tier = HistoricalTier.Graybox;
            tag.sourceNote =
                $"{def.displayName} — bolge yayini iskeleti (plan Faz 1 madde 6). " +
                "Bu sahne HENUZ BOSTUR; icerigi Faz 2-4'te gelir. Bolge siniri bir " +
                "OYNANIS karari olup tarihsel mahalle siniri degildir (ADR 0011).";

            EditorSceneManager.SaveScene(scene, scenePath);
            EditorSceneManager.CloseScene(scene, true);
            return true;
        }

        // ------------------------------------------------------ Addressables

        /// <summary>
        /// Addressables yapılandırmasını gerekiyorsa kurar ve semt sahnelerini adresler.
        ///
        /// **`Assets/_Project` dışına dosya koyma kuralının bilinçli istisnası.**
        /// Addressables yapılandırması `Assets/AddressableAssetsData` altına iner.
        /// Ayarlar varlığının klasörü parametreyle değiştirilebilir ama paket
        /// kaynağında `DefaultObject.asset` yolu SABİT KODLUDUR
        /// (`AddressableAssetSettingsDefaultObject.kDefaultConfigFolder`), yani
        /// yapılandırmanın bir parçası her hâlükârda dışarıda kalır. Yarısı bir
        /// yerde yarısı başka yerde duran bir kurulum, tek yerde duran ve
        /// çerçevenin beklediği yerde olandan daha kafa karıştırıcıdır — bu yüzden
        /// varsayılan kullanılıyor. `Assets/Settings/` (HDRP) zaten aynı türde bir
        /// çerçeve klasörüdür; emsal var. Gerekçe: ADR 0011.
        /// </summary>
        private static int RegisterAddressables(List<DistrictDef> defs)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                settings = AddressableAssetSettings.Create(
                    AddressableAssetSettingsDefaultObject.kDefaultConfigFolder,
                    AddressableAssetSettingsDefaultObject.kDefaultConfigAssetName,
                    createDefaultGroups: true,
                    isPersisted: true);
                if (settings == null)
                {
                    Debug.LogError("[Hezarfen] Addressables ayarlari olusturulamadi — " +
                                   "semt sahneleri adreslenmedi.");
                    return 0;
                }
                AddressableAssetSettingsDefaultObject.Settings = settings;
                Debug.Log("[Hezarfen] Addressables yapilandirmasi olusturuldu: " +
                          AddressableAssetSettingsDefaultObject.kDefaultConfigFolder +
                          " (_Project disinda — bilincli istisna, ADR 0011).");
            }

            var group = settings.FindGroup(AddressableGroupName)
                        ?? settings.CreateGroup(AddressableGroupName, false, false, false, null,
                                                settings.DefaultGroup.Schemas
                                                        .Select(s => s.GetType()).ToArray());

            int count = 0;
            foreach (var def in defs)
            {
                string scenePath = $"{SceneFolder}/{def.districtId}.unity";
                string guid = AssetDatabase.AssetPathToGUID(scenePath);
                if (string.IsNullOrEmpty(guid)) continue;

                var entry = settings.CreateOrMoveEntry(guid, group, false, false);
                if (entry == null) continue;
                entry.address = def.sceneAddress;
                count++;
            }

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            return count;
        }

        // ------------------------------------------------------- json yardımcı

        /// <summary>Verilen id'ye ait özellik gövdesini ham metinden keser.</summary>
        public static string ExtractFeatureBody(string json, string id)
        {
            int at = json.IndexOf($"\"id\": \"{id}\"", System.StringComparison.Ordinal);
            if (at < 0) at = json.IndexOf($"\"id\":\"{id}\"", System.StringComparison.Ordinal);
            if (at < 0) return "";

            int start = json.LastIndexOf('{', at);
            if (start < 0) return "";

            int depth = 0;
            bool inString = false;
            for (int i = start; i < json.Length; i++)
            {
                char c = json[i];
                if (inString) { if (c == '\\') i++; else if (c == '"') inString = false; continue; }
                if (c == '"') inString = true;
                else if (c == '{') depth++;
                else if (c == '}' && --depth == 0) return json.Substring(start, i - start + 1);
            }
            return "";
        }

        public static string ReadString(string body, string key)
        {
            int at = IndexOfValue(body, key);
            if (at < 0) return "";
            if (body[at] != '"') return "";
            var sb = new System.Text.StringBuilder();
            for (int i = at + 1; i < body.Length; i++)
            {
                if (body[i] == '\\') { i++; sb.Append(body[i]); continue; }
                if (body[i] == '"') break;
                sb.Append(body[i]);
            }
            return sb.ToString();
        }

        public static float ReadFloat(string body, string key, float fallback)
        {
            int at = IndexOfValue(body, key);
            if (at < 0) return fallback;
            int end = at;
            while (end < body.Length && (char.IsDigit(body[end]) || body[end] == '-' ||
                                         body[end] == '+' || body[end] == '.' ||
                                         body[end] == 'e' || body[end] == 'E')) end++;
            float v;
            // InvariantCulture SART: bu makinenin yerel ayari ondalik ayirici olarak
            // VIRGUL kullaniyor; varsayilan ayristirma "700.0"i 7000 yapardi.
            return float.TryParse(body.Substring(at, end - at),
                                  System.Globalization.NumberStyles.Float,
                                  System.Globalization.CultureInfo.InvariantCulture, out v)
                ? v : fallback;
        }

        public static int ReadInt(string body, string key, int fallback)
        {
            float v = ReadFloat(body, key, float.NaN);
            return float.IsNaN(v) ? fallback : Mathf.RoundToInt(v);
        }

        /// <summary>Anahtarın değerinin başladığı indeks (iki nokta ve boşluk atlanmış).</summary>
        private static int IndexOfValue(string body, string key)
        {
            string needle = $"\"{key}\":";
            int at = body.IndexOf(needle, System.StringComparison.Ordinal);
            if (at < 0) return -1;
            int i = at + needle.Length;
            // Bosluk atlanmazsa sayi taramasi hemen durur ve deger SESSIZCE 0 olur.
            // Kiyi ice aktariminda tam olarak bu yasandi (ADR 0008).
            while (i < body.Length && char.IsWhiteSpace(body[i])) i++;
            return i;
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
