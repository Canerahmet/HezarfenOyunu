using System;
using System.IO;
using Hezarfen.Core;
using UnityEditor;
using UnityEngine;

namespace Hezarfen.Editor.Gis
{
    /// <summary>
    /// `tools/gis/dem_fetch.py` çıktısını gerçek ölçekli bir Unity Terrain'e çevirir
    /// (plan Görev 9, Faz 1 madde 1).
    ///
    /// Sözleşme — bu üç şey birbirine bağlıdır ve testle kilitlenmiştir:
    ///   1. **1 birim = 1 metre.** Terrain'in metre cinsinden boyutu doğrudan
    ///      meta dosyasından gelir; elle çarpan YOKTUR.
    ///   2. **Dünya orijini Galata Kulesi tabanıdır** (plan Faz 1 madde 4). Terrain,
    ///      meta'daki ofset kadar ötelenerek yerleştirilir; böylece DEM, GeoJSON ve
    ///      landmark'lar aynı çerçeveyi paylaşır.
    ///   3. **y = 0 deniz seviyesidir.** Uçuş oyununda irtifa okuması deniz
    ///      seviyesine göredir.
    ///
    /// Heightmap ham veri olarak okunur (Unity'nin "Import Raw" penceresi yerine):
    /// bayt düzeni, satır sırası ve normalizasyon burada açıkça yazılıdır. Elle
    /// import, bu üç ayarın her seferinde doğru seçilmesine güvenmek demektir.
    /// </summary>
    public static class TerrainImporter
    {
        public const string DefaultDataDir = "data/gis/istanbul";
        public const string TerrainAssetPath = "Assets/_Project/Art/Terrain/TD_Istanbul.asset";
        public const string MaterialPath = "Assets/_Project/Art/Terrain/M_Terrain_Istanbul.mat";

        // --------------------------------------------------------- meta şeması

        [Serializable]
        public class WorldOrigin
        {
            public string name;
            public double lon, lat, utm_easting, utm_northing;
        }

        [Serializable]
        public class OriginOffset
        {
            public double x, z;
        }

        [Serializable]
        public class DemMeta
        {
            public string source, source_type, licence, crs;
            public string heightmap_file, heightmap_format, generated_utc;
            public bool attribution_required;
            public WorldOrigin world_origin;
            public OriginOffset world_origin_offset_m;
            public int resolution;
            public double size_x_m, size_z_m;
            public double meters_per_sample_x, meters_per_sample_z;
            /// <summary>Terrain nesnesinin yerleştirileceği kot — deniz TABANIDIR, deniz seviyesi değil.</summary>
            public double base_elevation_m;

            /// <summary>Deniz seviyesi. Sözleşme gereği 0 (dünya y ekseni buna göre).</summary>
            public double sea_level_m;

            /// <summary>Deniz tabanının deniz seviyesi altındaki derinliği (m).</summary>
            public double seabed_depth_m;

            public double height_range_m;
            public double min_elevation_m, max_elevation_m, spike_suppression_m;
        }

        // ------------------------------------------------------------ menüler

        [MenuItem("Hezarfen/GIS/DEM'den Terrain uret")]
        public static void ImportMenu()
        {
            var data = Import(DefaultDataDir);
            if (data == null) return;

            Selection.activeObject = data;
            EditorGUIUtility.PingObject(data);
            Debug.Log($"[Hezarfen] Terrain hazir: {TerrainAssetPath} " +
                      $"({data.size.x:F0} x {data.size.z:F0} m, {data.size.y:F0} m yukseklik)");
        }

        /// <summary>Depo kökünü bulur. `data/` Assets dışındadır — kaynak veri, varlık değil.</summary>
        public static string RepositoryRoot()
        {
            // .../Hezarfen_Oyunu/unity/HezarfenGame/Assets -> .../Hezarfen_Oyunu
            var assets = new DirectoryInfo(Application.dataPath);
            return assets.Parent?.Parent?.Parent?.FullName;
        }

        public static string ResolveDataDir(string relativeDir)
        {
            string root = RepositoryRoot();
            return root == null ? null : Path.Combine(root, relativeDir.Replace('/', Path.DirectorySeparatorChar));
        }

        public static DemMeta ReadMeta(string relativeDir)
        {
            string dir = ResolveDataDir(relativeDir);
            if (dir == null) return null;

            string metaPath = Path.Combine(dir, "dem_meta.json");
            if (!File.Exists(metaPath))
            {
                Debug.LogError($"[Hezarfen] dem_meta.json yok: {metaPath}\n" +
                               "Once calistir: tools/gis/.venv/Scripts/python.exe tools/gis/dem_fetch.py");
                return null;
            }

            return JsonUtility.FromJson<DemMeta>(File.ReadAllText(metaPath));
        }

        // -------------------------------------------------------------- import

        public static TerrainData Import(string relativeDir)
        {
            var meta = ReadMeta(relativeDir);
            if (meta == null) return null;

            string dir = ResolveDataDir(relativeDir);
            string rawPath = Path.Combine(dir, meta.heightmap_file);
            if (!File.Exists(rawPath))
            {
                Debug.LogError($"[Hezarfen] Heightmap yok: {rawPath}");
                return null;
            }

            int n = meta.resolution;
            byte[] bytes = File.ReadAllBytes(rawPath);
            if (bytes.Length != n * n * 2)
            {
                Debug.LogError($"[Hezarfen] Heightmap boyutu tutmuyor: {bytes.Length} bayt, " +
                               $"{n * n * 2} bekleniyordu ({n}x{n} uint16).");
                return null;
            }

            // Dosya duzeni: uint16 little-endian, satir 0 = GUNEY, sutun 0 = BATI.
            // TerrainData.SetHeights(x, y, heights[y, x]) cagrisinda y ekseni +Z
            // (kuzey) yonundedir — yani dosya duzeni birebir eslesir, ters cevirme
            // gerekmez. Bu esleme bilincli secilmisti (bkz. dem_fetch.py).
            var heights = new float[n, n];
            for (int y = 0; y < n; y++)
            {
                int rowStart = y * n * 2;
                for (int x = 0; x < n; x++)
                {
                    int i = rowStart + x * 2;
                    ushort v = (ushort)(bytes[i] | (bytes[i + 1] << 8));
                    heights[y, x] = v / 65535f;
                }
            }

            EnsureFolder("Assets/_Project/Art/Terrain");

            var data = AssetDatabase.LoadAssetAtPath<TerrainData>(TerrainAssetPath);
            bool isNew = data == null;
            if (isNew) data = new TerrainData();

            data.name = "TD_Istanbul";

            // SIRA ONEMLI: heightmapResolution atamasi size'i sifirlar.
            data.heightmapResolution = n;
            data.size = new Vector3((float)meta.size_x_m,
                                    (float)meta.height_range_m,
                                    (float)meta.size_z_m);
            data.SetHeights(0, 0, heights);

            if (isNew) AssetDatabase.CreateAsset(data, TerrainAssetPath);
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();

            Debug.Log($"[Hezarfen] DEM: {meta.source}\n" +
                      $"  {n}x{n} @ {meta.meters_per_sample_x:F2} m/ornek\n" +
                      $"  dunya {meta.size_x_m:F0} x {meta.size_z_m:F0} m, " +
                      $"irtifa {meta.min_elevation_m:F0}..{meta.max_elevation_m:F0} m\n" +
                      $"  ATIF ZORUNLU: {meta.licence}");

            return data;
        }

        // ------------------------------------------------------------- sahneye

        [MenuItem("Hezarfen/GIS/Terrain'i sahneye yerlestir")]
        public static void PlaceInSceneMenu()
        {
            var go = PlaceInScene(DefaultDataDir);
            if (go != null) Selection.activeGameObject = go;
        }

        /// <summary>
        /// Terrain'i sahneye, dünya orijinine (Galata Kulesi) göre doğru ofsette kurar.
        /// </summary>
        public static GameObject PlaceInScene(string relativeDir)
        {
            var meta = ReadMeta(relativeDir);
            var data = AssetDatabase.LoadAssetAtPath<TerrainData>(TerrainAssetPath);
            if (meta == null || data == null)
            {
                Debug.LogError("[Hezarfen] Once 'DEM'den Terrain uret' calistir.");
                return null;
            }

            var existing = GameObject.Find("TR_Istanbul");
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing);

            var go = Terrain.CreateTerrainGameObject(data);
            go.name = "TR_Istanbul";

            // Terrain'in guneybati kosesi, Galata Kulesi orijinine gore bu ofsettedir.
            // y = base_elevation (deniz seviyesi = 0).
            go.transform.position = new Vector3((float)meta.world_origin_offset_m.x,
                                                (float)meta.base_elevation_m,
                                                (float)meta.world_origin_offset_m.z);

            var mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (mat == null)
            {
                // HDRP'de arazi TerrainLit ister; Lit atanirsa arazi macenta gelir.
                var shader = Shader.Find("HDRP/TerrainLit");
                if (shader != null)
                {
                    mat = new Material(shader) { name = "M_Terrain_Istanbul" };
                    AssetDatabase.CreateAsset(mat, MaterialPath);
                    AssetDatabase.SaveAssets();
                }
                else
                {
                    Debug.LogWarning("[Hezarfen] HDRP/TerrainLit bulunamadi; " +
                                     "varsayilan arazi malzemesi kullanilacak.");
                }
            }
            if (mat != null) go.GetComponent<Terrain>().materialTemplate = mat;

            var tag = go.GetComponent<HistoricalTag>();
            if (tag == null) tag = go.AddComponent<HistoricalTag>();

            // T2: veri belgelidir ama MODERN topografyadir. 1632'nin kiyi cizgisi
            // ve dolgu oncesi hali Gorev 10'da duzeltilecek; simdiki hali bir
            // rekonstruksiyon dayanagidir, belge degil.
            tag.tier = HistoricalTier.Reconstruction;
            tag.sourceNote =
                $"{meta.source} ({meta.source_type}). {meta.crs}. " +
                $"Orijin: {meta.world_origin.name} ({meta.world_origin.lon:F6}, {meta.world_origin.lat:F6}). " +
                "Modern topografya; 1632 kiyi cizgisi duzeltmesi Gorev 10'da (plan Faz 1 madde 2). " +
                // Topografya belgeli (T2) ama YUZEY ORTUSU degil: dagilim
                // kurali arazinin kendi verisinden (kot + egim) cikiyor,
                // dokularin rengi ise sanatsal yorumdur. Iki farkli guven
                // kademesi tek bir etikette yasiyor; bunu yazmazsak kot
                // verisinin guvenilirligi sessizce ortuye de bulasir.
                "Yuzey ortusu (toprak/ot/kaya/kiyi) BELGELI DEGIL - T3 sanatsal " +
                "yorum; dagilimi kot ve egimden turetildi (ADR 0024). " +
                $"Atif zorunlu: {meta.licence}";

            Debug.Log($"[Hezarfen] TR_Istanbul yerlestirildi @ {go.transform.position} " +
                      $"(Galata Kulesi = dunya orijini)");
            return go;
        }

        private static void EnsureFolder(string assetFolder)
        {
            if (AssetDatabase.IsValidFolder(assetFolder)) return;
            string[] parts = assetFolder.Split('/');
            string acc = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{acc}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(acc, parts[i]);
                acc = next;
            }
        }
    }
}
