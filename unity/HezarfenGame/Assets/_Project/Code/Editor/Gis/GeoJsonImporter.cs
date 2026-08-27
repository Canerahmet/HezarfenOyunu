using System;
using System.Collections.Generic;
using System.IO;
using Hezarfen.Core;
using Hezarfen.Gis;
using UnityEditor;
using UnityEngine;

namespace Hezarfen.Editor.Gis
{
    /// <summary>
    /// `coastline_build.py` çıktısını sahneye alır (plan Görev 10, Faz 1 madde 4).
    ///
    /// GeoJSON'un kendisi WGS84'tür (RFC 7946) ve `refs/maps/` altında insan/QGIS
    /// için durur. Unity o dosyayı okumaz: enlem-boylamı metreye çevirmek bir
    /// projeksiyon kütüphanesi gerektirir ve Unity'de böyle bir şey yoktur. Dönüşüm,
    /// zaten rasterio'ya sahip olan Python tarafında yapılır; Unity **yerel metre**
    /// sürümünü okur. Böylece projeksiyon mantığı tek yerde yaşar (ADR 0007).
    /// </summary>
    public static class GeoJsonImporter
    {
        public const string LocalJsonPath = "data/gis/istanbul/coastline_1632_local.json";
        public const string LandmarksJsonPath = "data/gis/istanbul/landmarks_1632_local.json";
        public const string WallsJsonPath = "data/gis/istanbul/walls_1632_local.json";
        public const string DistrictsJsonPath = "data/gis/istanbul/districts_local.json";
        public const string RootName = "GIS_1632";

        /// <summary>Sahneye alınan tüm GIS katmanları — tek kök altında toplanır.</summary>
        public static readonly string[] AllLayerPaths =
            { LocalJsonPath, LandmarksJsonPath, WallsJsonPath, DistrictsJsonPath };

        // --------------------------------------------------------- json şeması

        // Bu tipler Unity tarafindan SERILESTIRILMEZ; asagidaki el yazimi
        // ayristiricinin cikti kaplaridir. [Serializable] konsaydi Unity'nin
        // serilestirme cozumleyicisi List<List<T>>'yi hakli olarak reddederdi ve
        // okuyucuya "bu sahnede saklaniyor" izlenimi verirdi. Saklanmiyor.
        public class LocalPoint { public float x, z; }

        public class LocalFeature
        {
            public string layer, id, name, tier, action, note;
            public bool closed;
            public List<List<LocalPoint>> rings = new List<List<LocalPoint>>();
        }

        public class LocalCollection
        {
            public List<LocalFeature> features = new List<LocalFeature>();
        }

        // ------------------------------------------------------------ menü

        [MenuItem("Hezarfen/GIS/Kiyi + landmark + sur + semtleri sahneye al")]
        public static void ImportMenu()
        {
            var root = Import(AllLayerPaths);
            if (root != null) Selection.activeGameObject = root;
        }

        /// <summary>Tek dosya — geriye dönük uyumluluk ve testler için.</summary>
        public static GameObject Import(string relativePath) => Import(new[] { relativePath });

        /// <summary>
        /// Faz1 sahnesini açar, tüm GIS katmanlarını alır ve **kaydeder**.
        ///
        /// Batchmode giriş noktası — Editor'ü açmadan çalıştırmak için:
        /// <code>
        /// Unity.exe -batchmode -projectPath unity/HezarfenGame -quit \
        ///   -executeMethod Hezarfen.Editor.Gis.GeoJsonImporter.BatchImportAndSave
        /// </code>
        /// Menü komutundan farkı kaydetmesidir. Etkileşimli oturumda içe aktarım
        /// yapıp sonra script derlemesi tetiklenirse domain reload sahneyi diskten
        /// yeniden yükler ve iş kaybolur; bu yol o pencereyi tamamen kapatır.
        /// </summary>
        public static void BatchImportAndSave()
        {
            const string scenePath = "Assets/_Project/Scenes/Faz1_Terrain.unity";
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
                scenePath, UnityEditor.SceneManagement.OpenSceneMode.Single);

            var root = Import(AllLayerPaths);
            if (root == null)
            {
                Debug.LogError("[Hezarfen] GIS ice aktarilamadi — sahne KAYDEDILMEDI.");
                FailExit(1);
                return;
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            bool saved = UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, scenePath);
            AssetDatabase.SaveAssets();

            int groups = root.transform.childCount;
            Debug.Log($"[Hezarfen] BatchImportAndSave: kaydedildi={saved}, katman grubu={groups}.");
            if (!saved || groups < 10)
            {
                Debug.LogError($"[Hezarfen] Beklenen 10 katman grubu, bulunan {groups}; " +
                               $"kaydedildi={saved}.");
                FailExit(2);
            }
        }

        /// <summary>
        /// Batchmode'da çıkış kodu verir; **etkileşimli Editor'de yalnızca hata yazar**.
        ///
        /// `EditorApplication.Exit` açık bir Editor'de çağrılırsa Unity'yi kaydetmeden
        /// kapatır. Bu metodun batchmode dışında da (elle, testten, yanlışlıkla)
        /// çağrılabilmesi, tek bir yazım hatasını oturum kaybına çevirirdi.
        /// </summary>
        private static void FailExit(int code)
        {
            if (Application.isBatchMode) EditorApplication.Exit(code);
            else Debug.LogError($"[Hezarfen] (batchmode olsaydi cikis kodu {code} olurdu)");
        }

        public static GameObject Import(string[] relativePaths)
        {
            string repo = TerrainImporter.RepositoryRoot();
            if (repo == null) { Debug.LogError("[Hezarfen] Depo koku bulunamadi."); return null; }

            // JsonUtility ic ice generic listeleri (List<List<T>>) cozemez; kucuk bir
            // el yazimi ayristirici, ek bagimliliktan daha ucuz ve daha ongorulebilir.
            var collection = new LocalCollection();
            foreach (string relativePath in relativePaths)
            {
                string path = Path.Combine(repo, relativePath.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(path))
                {
                    Debug.LogWarning($"[Hezarfen] Atlandi (yok): {relativePath}");
                    continue;
                }
                var part = ParseLocal(File.ReadAllText(path));
                if (part != null) collection.features.AddRange(part.features);
            }

            if (collection.features.Count == 0)
            {
                Debug.LogError("[Hezarfen] Hicbir ozellik okunamadi. " +
                               "Once: tools/gis/coastline_build.py ve landmarks_build.py");
                return null;
            }

            var existing = GameObject.Find(RootName);
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing);

            var root = new GameObject(RootName);
            var groups = new Dictionary<string, Transform>();

            Func<string, Transform> group = layer =>
            {
                Transform t;
                if (!groups.TryGetValue(layer, out t))
                {
                    var g = new GameObject(layer);
                    g.transform.SetParent(root.transform, false);
                    t = g.transform;
                    groups[layer] = t;
                }
                return t;
            };

            int shorelines = 0, zones = 0, landmarks = 0, walls = 0, gates = 0, districts = 0;
            float shorelineLength = 0f, wallLength = 0f;

            foreach (var feat in collection.features)
            {
                bool isZone = feat.layer == "correction_zone";
                for (int r = 0; r < feat.rings.Count; r++)
                {
                    var ring = feat.rings[r];
                    // Tek nokta GECERLIDIR: landmark'lar boyle gelir. Eskiden burada
                    // "< 2" filtresi vardi ve butun landmark katalogunu sessizce yutuyordu.
                    if (ring.Count < 1) continue;

                    // Kimligi olan her sey kendi adiyla anilir; yalnizca kiyi
                    // halkalarinin kimligi yoktur (marching squares'ten cikarlar).
                    string label;
                    if (isZone) label = $"CZ_{feat.name}";
                    else if (!string.IsNullOrEmpty(feat.id)) label = feat.id;
                    else label = $"{feat.layer}_{shorelines:00}";
                    if (feat.rings.Count > 1) label += $"_{r}";

                    var go = new GameObject(label);
                    go.transform.SetParent(group(feat.layer), false);

                    var gf = go.AddComponent<GisFeature>();
                    gf.layer = feat.layer;
                    gf.featureId = feat.id;
                    gf.featureName = feat.name;
                    gf.action = feat.action;
                    gf.note = feat.note;
                    gf.closed = feat.closed;

                    var pts = new Vector3[ring.Count];
                    for (int i = 0; i < ring.Count; i++)
                        pts[i] = new Vector3(ring[i].x, 0f, ring[i].z);
                    gf.points = pts;

                    var tag = go.AddComponent<HistoricalTag>();
                    tag.tier = ParseTier(feat.tier);
                    tag.sourceNote = isZone
                        ? $"{feat.note} [TASLAK — metrik ofset kaynakli degil; Caner onayi bekliyor]"
                        : feat.note;

                    if (isZone) zones++;
                    else if (feat.layer == "landmark") landmarks++;
                    else if (feat.layer == "wall_gate") gates++;
                    else if (feat.layer == "district") districts++;
                    else if (feat.layer.StartsWith("wall_")) { walls++; wallLength += gf.LengthMeters; }
                    else { shorelines++; shorelineLength += gf.LengthMeters; }
                }
            }

            // Sahneyi KIRLI isaretle.
            //
            // Script'ten `new GameObject(...)` ile nesne uretmek sahneyi otomatik
            // kirli yapmaz. Isaretlenmezse `SaveOpenScenes()` temiz bir sahne gorup
            // HICBIR SEY YAZMAZ ve basarili doner; hemen ardindan `isDirty` okumak da
            // `false` verir. Yani hata, kaydetme adiminda GORUNMEZ — dosya eski hâliyle
            // diskte kalir ve ilk domain reload'da bellekteki is de kaybolur.
            // Tam olarak bu yasandi: sur ve semt katmanlari iki kez sessizce kayboldu.
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            Debug.Log($"[Hezarfen] GIS: {shorelines} kiyi halkasi ({shorelineLength / 1000f:F1} km), " +
                      $"{zones} duzeltme alani, {landmarks} landmark, " +
                      $"{walls} sur hatti ({wallLength / 1000f:F1} km), {gates} kapi, " +
                      $"{districts} semt.\n" +
                      "Duzeltme alanlari, sur hatlari ve landmark KONUMLARI TASLAKTIR " +
                      "(ADR 0008/0011; konumlar Faz 1 madde 3'te georeferanslanacak). " +
                      "Semt siniri bir OYNANIS karadir, tarihsel mahalle siniri DEGILDIR.");
            return root;
        }

        private static HistoricalTier ParseTier(string tier)
        {
            HistoricalTier parsed;
            return Enum.TryParse(tier, out parsed) ? parsed : HistoricalTier.Reconstruction;
        }

        // ------------------------------------------------------------ ayrıştırıcı

        /// <summary>
        /// Yalnızca `coastline_build.py`nin ürettiği dar biçimi okur. Genel amaçlı bir
        /// JSON ayrıştırıcısı DEĞİLDİR ve olmamalıdır — girdi bizim ürettiğimiz,
        /// biçimi sabit bir dosyadır.
        /// </summary>
        public static LocalCollection ParseLocal(string json)
        {
            var result = new LocalCollection { features = new List<LocalFeature>() };

            int featuresAt = json.IndexOf("\"features\"", StringComparison.Ordinal);
            if (featuresAt < 0) return result;

            int i = json.IndexOf('[', featuresAt);
            if (i < 0) return result;

            while (true)
            {
                int objStart = FindFeatureStart(json, ref i);
                if (objStart < 0) break;

                int objEnd = MatchBrace(json, objStart);
                if (objEnd < 0) break;

                string body = json.Substring(objStart, objEnd - objStart + 1);
                result.features.Add(new LocalFeature
                {
                    layer = ReadString(body, "layer"),
                    id = ReadString(body, "id"),
                    name = ReadString(body, "name"),
                    tier = ReadString(body, "tier"),
                    action = ReadString(body, "action"),
                    note = ReadString(body, "note"),
                    closed = body.Contains("\"closed\":true") || body.Contains("\"closed\": true"),
                    rings = ReadRings(body),
                });

                i = objEnd + 1;
            }

            return result;
        }

        private static int FindFeatureStart(string s, ref int i)
        {
            while (i < s.Length)
            {
                char c = s[i];
                if (c == '{') return i;
                if (c == ']') return -1;
                i++;
            }
            return -1;
        }

        private static int MatchBrace(string s, int start)
        {
            int depth = 0;
            bool inString = false;
            for (int i = start; i < s.Length; i++)
            {
                char c = s[i];
                if (inString)
                {
                    if (c == '\\') i++;
                    else if (c == '"') inString = false;
                    continue;
                }
                if (c == '"') inString = true;
                else if (c == '{') depth++;
                else if (c == '}' && --depth == 0) return i;
            }
            return -1;
        }

        private static string ReadString(string body, string key)
        {
            string needle = $"\"{key}\":";
            int at = body.IndexOf(needle, StringComparison.Ordinal);
            if (at < 0) return "";
            int q = body.IndexOf('"', at + needle.Length);
            if (q < 0) return "";

            var sb = new System.Text.StringBuilder();
            for (int i = q + 1; i < body.Length; i++)
            {
                char c = body[i];
                if (c == '\\' && i + 1 < body.Length)
                {
                    char n = body[++i];
                    sb.Append(n == 'n' ? '\n' : n == 't' ? '\t' : n);
                    continue;
                }
                if (c == '"') break;
                sb.Append(c);
            }
            return sb.ToString();
        }

        private static List<List<LocalPoint>> ReadRings(string body)
        {
            var rings = new List<List<LocalPoint>>();
            int at = body.IndexOf("\"rings\":", StringComparison.Ordinal);
            if (at < 0) return rings;

            int i = body.IndexOf('[', at);
            if (i < 0) return rings;

            int depth = 0;
            List<LocalPoint> current = null;

            for (; i < body.Length; i++)
            {
                char c = body[i];
                if (c == '[')
                {
                    depth++;
                    if (depth == 2) current = new List<LocalPoint>();
                }
                else if (c == ']')
                {
                    depth--;
                    if (depth == 1 && current != null) { rings.Add(current); current = null; }
                    if (depth == 0) break;
                }
                else if (c == '{' && current != null)
                {
                    int end = MatchBrace(body, i);
                    if (end < 0) break;
                    string pt = body.Substring(i, end - i + 1);
                    current.Add(new LocalPoint
                    {
                        x = ReadFloat(pt, "x"),
                        z = ReadFloat(pt, "z"),
                    });
                    i = end;
                }
            }

            return rings;
        }

        private static float ReadFloat(string body, string key)
        {
            string needle = $"\"{key}\":";
            int at = body.IndexOf(needle, StringComparison.Ordinal);
            if (at < 0) return 0f;

            // Iki nokta ustunden sonraki BOSLUGU atla: Python'un json.dump'i
            // varsayilan olarak `"x": -6306.5` yazar (ayirici ": "). Bu atlanmazsa
            // sayi taramasi hemen durur, deger sessizce 0 olur ve butun geometri
            // orijine coker. Tam olarak bu yasandi.
            int start = at + needle.Length;
            while (start < body.Length && char.IsWhiteSpace(body[start])) start++;

            int end = start;
            while (end < body.Length && (char.IsDigit(body[end]) || body[end] == '-' ||
                                         body[end] == '+' || body[end] == '.' ||
                                         body[end] == 'e' || body[end] == 'E'))
                end++;

            float value;
            // InvariantCulture SART: bu makinenin yerel ayari ondalik ayirici olarak
            // VIRGUL kullaniyor; varsayilan ayristirma "-6306.5"i 63065 yapardi.
            return float.TryParse(body.Substring(start, end - start),
                                  System.Globalization.NumberStyles.Float,
                                  System.Globalization.CultureInfo.InvariantCulture,
                                  out value) ? value : 0f;
        }
    }
}
