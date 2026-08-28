using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Hezarfen.Editor.Gis
{
    /// <summary>
    /// Araziye <b>ağaç</b> diker — belgeli yerlere, belgeli olmayan yerlere değil.
    ///
    /// ## Neden gerekliydi
    ///
    /// Arazi örtüsü (ADR 0024) bittiğinde manzara hâlâ çıplak görünüyordu ve
    /// ölçüm sebebin doku olmadığını gösterdi: zeminin üstünde hiçbir şey yoktu.
    /// RESEARCH.md §4 ise İstanbul'u <b>yeşil kütleleriyle</b> anlatır — servi
    /// mezarlıkları, mesireler, bostanlar. Yeşil doku süs değil, siluetin parçası.
    ///
    /// ## Ağaç NEREYE dikilir: iki kaynak, iki farklı güven
    ///
    /// <b>1. Adlı alanlar</b> (`greenery_1632.geojson`): Karacaahmet ve Eyüp
    /// mezarlıkları, Kağıthane ve Göksu mesireleri, Pera bağları. Bunların
    /// varlığı ve yeri belgelidir; <b>sınırları çizilmiştir, ölçülmemiştir</b>
    /// (Osmanlı kaynakları dönüm vermez) ve hepsi `status: draft`.
    ///
    /// <b>2. Genel yamaç</b>: kural belgeden değil <b>araziden</b> gelir —
    /// ağaç yalnız suyun durduğu <b>içbükey</b> yerde, otun baskın olduğu
    /// yamaçta. İstanbul'un yakın çevresi 1632'de orman değil makilikti;
    /// ormanlar kuzeydeydi (Belgrad ormanı). Bu yüzden genel serpme
    /// <b>seyrektir</b> ve vadi tabanlarını izler.
    ///
    /// ## Ağacın dikilmeyeceği yerler de belgelidir
    ///
    /// En keskin kural bir <b>yokluk</b>: II. Bayezid'in Okmeydanı vakfiyesi
    /// meydanda "yapı, mezar, su yolu, <b>bağ ve bahçe</b>" yapılmasını kesin
    /// olarak yasaklar. Orası bilinçle boş tutulmuş bir talim alanıdır — ve
    /// Hezarfen'in talim yaptığı yerdir. Oraya ağaç dikmek belgeye aykırı olur.
    /// Ayrıca sur içi, Galata ve Üsküdar'ın yapılı çekirdekleri.
    /// </summary>
    public static class GreeneryBuilder
    {
        public const string DataFile = "greenery_local.json";

        /// <summary>Ağaç türü → prefab yolları. Sıra prototip sırasıdır.</summary>
        private static readonly (string species, string[] prefabs)[] Species =
        {
            ("servi", new[] { "PF_Servi_A", "PF_Servi_B", "PF_Servi_C" }),
            ("cinar", new[] { "PF_Cinar_A", "PF_Cinar_B" }),
            // Bağ/bahçe: elimizde asma ya da meyve ağacı varlığı YOK. Seyrek
            // çınarla temsil ediliyor ve bu bir eksiktir, çözüm değil — ADR
            // 0026 §6'da kayıtlı.
            ("meyve", new[] { "PF_Cinar_B", "PF_Cinar_A" }),
        };

        private const string PrefabDir = "Assets/_Project/Art/Prefabs";

        // Genel yamaç serpmesi.
        private const float WildSpacingM = 34f;     // aday ızgara aralığı
        private const float WildConcaveM = -1.2f;   // bundan ÇUKUR yerler
        private const float WildMaxSlopeDeg = 26f;

        /// <summary>Alan kenarında korunun seyreldiği bant (m).</summary>
        private const float FeatherM = 80f;

        // Ortak elemeler.
        private const float MinElevationM = 2.0f;   // kıyı bandına ağaç yok
        private const float MaxSlopeDeg = 32f;

        // ------------------------------------------------------------- şema

        [Serializable]
        public class LocalPoint { public float x, z; }

        [Serializable]
        public class Area
        {
            public string id, name, kind, species, tier;
            /// <summary>
            /// Sınırın DAYANAĞI: <c>documented</c> (yayımlanmış alan ölçüsüne
            /// oturtuldu), <c>walls</c> (sınır sur çizgisinin kendisidir),
            /// <c>terrain</c> (sınırı arazi tanımlar, iddia ölçülür) ya da
            /// <c>drawn</c> (çapası yok, kaba kutu).
            ///
            /// Bu alan tur boyunca eklendi: on bir sınır da aynı şekilde
            /// çizilmişti ve tek sınandıkları yerde biri yarı yarıya küçük,
            /// bir başkası altı kat büyük çıktı. Hangi sınırın taşıyıcı
            /// olduğu artık veride yazıyor.
            /// </summary>
            public string basis;
            public float density, area_ha, land_fraction;
            public float center_x, center_z, radius_m;
            public LocalPoint[] ring;
        }

        [Serializable]
        public class AreaFile { public Area[] areas; }

        // ---------------------------------------------------------- menüler

        [MenuItem("Hezarfen/GIS/Yesil dokuyu dik")]
        public static void BuildMenu()
        {
            var data = AssetDatabase.LoadAssetAtPath<TerrainData>(
                TerrainImporter.TerrainAssetPath);
            if (data == null)
            {
                Debug.LogError("[Hezarfen] TerrainData yok.");
                return;
            }
            if (data.terrainLayers == null || data.terrainLayers.Length < 4)
            {
                Debug.LogError("[Hezarfen] Once 'Arazi ortusunu kur' — " +
                               "agac yerlestirme ORTUYE bakar (ot baskin mi).");
                return;
            }

            var areas = ReadAreas();
            if (areas == null) return;

            var meta = TerrainImporter.ReadMeta(TerrainImporter.DefaultDataDir);
            if (meta == null) return;

            var protos = BuildPrototypes(out var speciesRange);
            if (protos == null) return;
            data.treePrototypes = protos;

            string report = Plant(data, areas, (float)meta.base_elevation_m,
                                  speciesRange);
            ApplyToOpenScenes();

            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
            Debug.Log("[Hezarfen] Yesil doku dikildi.\n" + report);
        }

        /// <summary>
        /// Adlı alanların her biri için bir kare — inceleme paketi (ADR 0006).
        ///
        /// Bakış noktası ALANIN KENDİSİNDEN türetilir (merkez + yarıçap), elle
        /// yazılmış koordinattan değil: poligon düzeltilirse kamera da onunla
        /// birlikte gider. Bir kez elle yazılmış bir ölçüm noktası, sahne
        /// yeniden kurulunca sessizce başka bir yere bakıyordu (ADR 0023).
        /// </summary>
        [MenuItem("Hezarfen/GIS/Yesil doku inceleme paketi")]
        public static void ReviewPackMenu()
        {
            var areas = ReadAreas();
            if (areas == null) return;

            var lines = new List<string>();
            foreach (var a in areas)
            {
                if (a.density <= 0f) continue;      // ağaçsız alanın karesi olmaz
                Vector3 c = Diagnostics.FrameMetric.OnGround(
                    new Vector3(a.center_x, 0f, a.center_z));

                // Yükseklik yarıçapla ölçeklenir ki alan kadraja SIĞSIN.
                float d = a.radius_m * 1.35f;
                var eye = c + new Vector3(-d * 0.72f, d * 0.60f, -d * 0.72f);
                var st = Diagnostics.FrameMetric.Capture(
                    eye, c, 50f, $"Captures/yesil_{Slug(a.id)}.png", 960, 540);
                lines.Add($"  {a.name,-26} {st}");
            }
            Debug.Log("[Hezarfen] Yesil doku inceleme paketi -> Captures/yesil_*.png\n"
                      + string.Join("\n", lines));
        }

        private static string Slug(string id) =>
            id.StartsWith("G_") ? id.Substring(2).ToLowerInvariant() : id.ToLowerInvariant();

        /// <summary>
        /// Ağaçların kare süresine maliyetini ölçer — Faz 1c'nin kabul ölçütü.
        ///
        /// Ölçüm <b>yıkıcı değildir</b>: ağaçlar silinmez, yalnızca
        /// <c>Terrain.drawTreesAndFoliage</c> bayrağı kapatılır. Bir kez
        /// öncesi/sonrası ölçmek için katmanları boşaltmıştım ve ölçüm
        /// ölçtüğü şeyi bozmuştu (ADR 0024 §7); aynı hataya iki kez düşmemek
        /// için burada durum hiç değişmiyor.
        ///
        /// Editör render'ı bir yapının kare süresi değildir; ölçülen şey
        /// <b>oran</b>dır — ağaç açıkken kare kaç kat pahalı.
        /// </summary>
        [MenuItem("Hezarfen/Olcum/Agac maliyetini olc")]
        public static void MeasureCostMenu()
        {
            var terrains = UnityEngine.Object.FindObjectsByType<Terrain>();
            if (terrains.Length == 0)
            { Debug.LogError("[Hezarfen] Sahnede arazi yok."); return; }

            int treeCount = terrains[0].terrainData.treeInstanceCount;
            var views = new (string name, Vector3 eye, Vector3 look)[]
            {
                ("kusbakisi 400 m", Diagnostics.FrameMetric.OnGround(Vector3.zero)
                                    + new Vector3(-600f, 400f, -600f),
                                    Diagnostics.FrameMetric.OnGround(Vector3.zero)),
                ("mezarlik 500 m", Diagnostics.FrameMetric.OnGround(new Vector3(4247f, 0f, -1180f))
                                   + new Vector3(-700f, 420f, -700f),
                                   Diagnostics.FrameMetric.OnGround(new Vector3(4247f, 0f, -1180f))),
            };

            var lines = new List<string> { $"  {treeCount} agac" };
            foreach (var v in views)
            {
                Measure(terrains, v.eye, v.look, out float on, out float onSpread,
                        out float off, out float offSpread);
                float diff = on - off;
                float noise = onSpread + offSpread;

                // ALET KENDI BELIRSIZLIGINI SOYLER. Fark, iki tarafin
                // sacilmasinin toplamindan kucukse olculen sey agac degil
                // gurultudur — ve "0,67x" gibi bir sayi yazmak, inanmadigim
                // bir olcumu rapora sokmak olurdu.
                string verdict = Mathf.Abs(diff) > noise
                    ? $"fark {diff,+6:F1} ms ({(off > 0.01f ? on / off : 0f):F2}x)"
                    : "OLCULEMEDI — fark sacilmanin altinda";
                lines.Add($"  {v.name,-18} agacli {on,5:F1}±{onSpread:F1} ms | "
                          + $"agacsiz {off,5:F1}±{offSpread:F1} ms | {verdict}");
            }
            foreach (var t in terrains) t.drawTreesAndFoliage = true;   // durum geri
            lines.Add("  NOT: editor render'i kararli bir olcum ortami DEGIL "
                      + "(asenkron shader derlemesi, arka plan varlik isi). "
                      + "Gercek FPS yargisi bir OYUNCU YAPISI ister; batchmode "
                      + "build bu makinede bloklu (SETUP.md).");
            Debug.Log("[Hezarfen] Agac maliyeti\n" + string.Join("\n", lines));
        }

        /// <summary>
        /// Ağaçlı ve ağaçsız kare süresini <b>dönüşümlü</b> ölçer.
        ///
        /// İlk yazımda önce hepsi ağaçlı, sonra hepsi ağaçsız ölçülüyordu ve
        /// sonuç saçmaydı: ağaçlı kare ağaçsızdan <b>hızlı</b> çıktı
        /// (20,7 ms / 44,4 ms). Sebep ağaç değil <b>sıra</b>ydı — editörde
        /// arka planda dönen iş, gölge önbelleği ve ilk çağrının ısıttığı
        /// kaynaklar iki bloğu farklı koşullara sokuyor.
        ///
        /// Dönüşümlü ölçüm bunu ortadan kaldırır: aynı kamera, aynı hedef
        /// doku, sırayla açık/kapalı, ve her birinin <b>ortancası</b> alınır
        /// (ortalama tek bir takılmayla kayar).
        /// </summary>
        private static void Measure(Terrain[] terrains, Vector3 eye, Vector3 look,
                                    out float withTrees, out float withSpread,
                                    out float without, out float withoutSpread,
                                    int rounds = 16)
        {
            var go = new GameObject("__hz_olcum_kamera");
            var cam = go.AddComponent<Camera>();
            go.AddComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData>();
            go.transform.position = eye;
            go.transform.LookAt(look);
            cam.fieldOfView = 55f;
            cam.farClipPlane = 6000f;
            var rt = new RenderTexture(1920, 1080, 24);
            cam.targetTexture = rt;

            for (int i = 0; i < 12; i++) cam.Render();       // isinma

            var on = new List<float>();
            var off = new List<float>();
            var sw = new System.Diagnostics.Stopwatch();
            for (int r = 0; r < rounds; r++)
                for (int phase = 0; phase < 2; phase++)
                {
                    bool trees = phase == 0;
                    foreach (var t in terrains) t.drawTreesAndFoliage = trees;
                    cam.Render();                            // gecise ait kare atilir
                    sw.Restart();
                    cam.Render();
                    sw.Stop();
                    (trees ? on : off).Add((float)sw.Elapsed.TotalMilliseconds);
                }

            cam.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(go);
            rt.Release();
            UnityEngine.Object.DestroyImmediate(rt);

            on.Sort(); off.Sort();
            withTrees = on[on.Count / 2];
            without = off[off.Count / 2];
            // Sacilma = ceyrekler arasi acikligin yarisi. Ortalama ve standart
            // sapma tek bir takilmayla kayar; ceyrekler kaymaz.
            withSpread = (on[on.Count * 3 / 4] - on[on.Count / 4]) * 0.5f;
            withoutSpread = (off[off.Count * 3 / 4] - off[off.Count / 4]) * 0.5f;
        }

        [MenuItem("Hezarfen/GIS/Yesil dokuyu kaldir")]
        public static void ClearMenu()
        {
            var data = AssetDatabase.LoadAssetAtPath<TerrainData>(
                TerrainImporter.TerrainAssetPath);
            if (data == null) return;
            data.SetTreeInstances(new TreeInstance[0], true);
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
            Debug.Log("[Hezarfen] Agaclar kaldirildi.");
        }

        public static Area[] ReadAreas()
        {
            string dir = TerrainImporter.ResolveDataDir(TerrainImporter.DefaultDataDir);
            string p = dir == null ? null : Path.Combine(dir, DataFile);
            if (p == null || !File.Exists(p))
            {
                Debug.LogError($"[Hezarfen] {DataFile} yok.\nOnce calistir: " +
                               "tools/gis/.venv/Scripts/python.exe tools/gis/greenery_build.py");
                return null;
            }
            return JsonUtility.FromJson<AreaFile>(File.ReadAllText(p))?.areas;
        }

        // ------------------------------------------------------- prototipler

        private static TreePrototype[] BuildPrototypes(
            out Dictionary<string, Vector2Int> speciesRange)
        {
            speciesRange = new Dictionary<string, Vector2Int>();
            var list = new List<TreePrototype>();

            foreach (var (sp, names) in Species)
            {
                int start = list.Count;
                foreach (string n in names)
                {
                    var go = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/{n}.prefab");
                    if (go == null)
                    {
                        Debug.LogError($"[Hezarfen] Agac prefabi yok: {n}");
                        speciesRange = null;
                        return null;
                    }
                    list.Add(new TreePrototype { prefab = go, bendFactor = 0.4f });
                }
                // Ayni tur icin BIRDEN COK prototip: tek varyant dikilirse
                // mezarlik kopyalanmis tek bir agactan olusur ve bunu goz
                // hemen yakalar. Varyant secimi konuma gore belirlenir.
                if (!speciesRange.ContainsKey(sp))
                    speciesRange[sp] = new Vector2Int(start, list.Count);
            }
            return list.ToArray();
        }

        // ------------------------------------------------------------ dikim

        public static string Plant(TerrainData data, Area[] areas, float baseElevation,
                                   Dictionary<string, Vector2Int> speciesRange)
        {
            int hm = data.heightmapResolution;
            float[,] h = data.GetHeights(0, 0, hm, hm);
            float mps = data.size.x / (hm - 1);
            float yScale = data.size.y;

            int am = data.alphamapResolution;
            float[,,] alpha = data.GetAlphamaps(0, 0, am, am);
            const int grassLayer = 1;

            // Yasakli alanlar: yerlesim, yasak, bostan. Bostanda agac yok
            // cunku bostan sebze tarhidir.
            var blocked = new List<Area>();
            foreach (var a in areas)
                if (a.kind == "yerlesim" || a.kind == "yasak" || a.kind == "bostan")
                    blocked.Add(a);

            var trees = new List<TreeInstance>(80000);
            var lines = new List<string>();
            var rng = new System.Random(1632);

            // --- 1) ADLI ALANLAR
            int named = 0;
            foreach (var a in areas)
            {
                if (a.density <= 0f || a.ring == null || a.ring.Length < 3) continue;
                if (!speciesRange.TryGetValue(a.species, out var range))
                {
                    lines.Add($"  {a.id}: bilinmeyen tur '{a.species}' — atlandi");
                    continue;
                }

                float spacing = 1f / Mathf.Sqrt(Mathf.Max(a.density, 1e-6f));
                int before = trees.Count;
                Scatter(data, h, hm, mps, yScale, alpha, am, grassLayer, baseElevation,
                        a.ring, spacing, range, blocked, rng, trees,
                        requireGrass: a.kind == "mesire" || a.kind == "bag",
                        concaveOnly: false, maxSlope: MaxSlopeDeg,
                        feather: FeatherM);
                int n = trees.Count - before;
                named += n;
                lines.Add($"  {a.name,-30} {a.kind,-9} {a.species,-6} " +
                          $"{spacing,5:F1} m aralik -> {n,6} agac");
            }

            // --- 2) GENEL YAMAC: yalniz VADI, yalniz OT
            int wildBefore = trees.Count;
            var whole = new[]
            {
                new LocalPoint { x = -data.size.x, z = -data.size.z },
                new LocalPoint { x = data.size.x, z = -data.size.z },
                new LocalPoint { x = data.size.x, z = data.size.z },
                new LocalPoint { x = -data.size.x, z = data.size.z },
            };
            Scatter(data, h, hm, mps, yScale, alpha, am, grassLayer, baseElevation,
                    whole, WildSpacingM, speciesRange["cinar"], blocked, rng, trees,
                    requireGrass: true, concaveOnly: true, maxSlope: WildMaxSlopeDeg,
                    feather: 0f);
            int wild = trees.Count - wildBefore;

            data.SetTreeInstances(trees.ToArray(), true);

            lines.Add($"  {"genel yamac (vadi + ot)",-30} {"dogal",-9} {"cinar",-6} " +
                      $"{WildSpacingM,5:F1} m aralik -> {wild,6} agac");
            lines.Add($"  TOPLAM {trees.Count} agac ({named} adli alanda, {wild} yamacta)");
            lines.Add($"  Agacsiz alanlar: {blocked.Count} poligon " +
                      "(yerlesim + Okmeydani yasagi + bostanlar)");
            return string.Join("\n", lines);
        }

        /// <summary>
        /// Halka içine <b>sarsılmış ızgarayla</b> serper.
        ///
        /// Rastgele serpme değil: rastgelelik kendi başına kümelenir (Poisson)
        /// ve bir mezarlık yer yer bomboş, yer yer tıkış olur. Aynı ders doku
        /// tarafında da ölçülmüştü (ADR 0024 §6) — orada karo tekrarını ele
        /// veren şey buydu.
        /// </summary>
        private static void Scatter(TerrainData data, float[,] h, int hm, float mps,
                                    float yScale, float[,,] alpha, int am, int grassLayer,
                                    float baseElevation, LocalPoint[] ring, float spacing,
                                    Vector2Int protoRange, List<Area> blocked,
                                    System.Random rng, List<TreeInstance> outTrees,
                                    bool requireGrass, bool concaveOnly, float maxSlope,
                                    float feather)
        {
            float minX = float.MaxValue, maxX = float.MinValue;
            float minZ = float.MaxValue, maxZ = float.MinValue;
            foreach (var p in ring)
            {
                minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x);
                minZ = Mathf.Min(minZ, p.z); maxZ = Mathf.Max(maxZ, p.z);
            }

            // Arazinin dunya kutusu (terrain guneybati kosesi orijine goredir).
            var pos = TerrainOrigin(data);
            minX = Mathf.Max(minX, pos.x + 1f); maxX = Mathf.Min(maxX, pos.x + data.size.x - 1f);
            minZ = Mathf.Max(minZ, pos.z + 1f); maxZ = Mathf.Min(maxZ, pos.z + data.size.z - 1f);

            for (float z = minZ; z <= maxZ; z += spacing)
                for (float x = minX; x <= maxX; x += spacing)
                {
                    float jx = x + (float)(rng.NextDouble() - 0.5) * spacing * 0.9f;
                    float jz = z + (float)(rng.NextDouble() - 0.5) * spacing * 0.9f;
                    if (!PointInRing(jx, jz, ring)) continue;

                    // KENARI TÜYLENDIR. Poligon bir çizim aracıdır, doğada
                    // karşılığı yoktur: ilk üretimde Karacaahmet düzgün bir
                    // altıgen olarak okunuyordu, mezarlık gibi değil. Koru
                    // kenardan seyrelir; seyrelme de düz bir gradyan değil,
                    // gürültüyle kırılmış olmalı — yoksa bu sefer yumuşak bir
                    // altıgen görünür.
                    if (feather > 0f)
                    {
                        float edge = DistanceToRing(jx, jz, ring);
                        float t = Mathf.Clamp01(edge / feather);
                        float n = TerrainCoverBuilder.Noise01(jx / 55f, jz / 55f, 907);
                        if (n > t * 0.85f + 0.15f) continue;
                    }

                    float u = (jx - pos.x) / data.size.x;
                    float v = (jz - pos.z) / data.size.z;
                    if (u < 0f || u > 1f || v < 0f || v > 1f) continue;

                    int hx = Mathf.Clamp(Mathf.RoundToInt(u * (hm - 1)), 1, hm - 2);
                    int hy = Mathf.Clamp(Mathf.RoundToInt(v * (hm - 1)), 1, hm - 2);

                    float elev = baseElevation + h[hy, hx] * yScale;
                    if (elev < MinElevationM) continue;

                    float dx = (h[hy, hx + 1] - h[hy, hx - 1]) * yScale / (2f * mps);
                    float dz = (h[hy + 1, hx] - h[hy - 1, hx]) * yScale / (2f * mps);
                    float slope = Mathf.Atan(Mathf.Sqrt(dx * dx + dz * dz)) * Mathf.Rad2Deg;
                    if (slope > maxSlope) continue;

                    if (concaveOnly)
                    {
                        // Su nerede durur: ICBUKEY yerde. Olcut, noktanin
                        // ~30 m komsulari ortalamasindan KAC METRE alcakta
                        // oldugudur — arazi ortusundeki sirt kuralinin tersi.
                        const int K = 4;
                        int lx = Mathf.Max(hx - K, 0), rx = Mathf.Min(hx + K, hm - 1);
                        int ly = Mathf.Max(hy - K, 0), ry = Mathf.Min(hy + K, hm - 1);
                        float around = (h[hy, lx] + h[hy, rx] + h[ly, hx] + h[ry, hx]) * 0.25f;
                        if ((h[hy, hx] - around) * yScale > WildConcaveM) continue;
                    }

                    if (requireGrass)
                    {
                        int ax = Mathf.Clamp((int)(u * am), 0, am - 1);
                        int az = Mathf.Clamp((int)(v * am), 0, am - 1);
                        if (alpha[az, ax, grassLayer] < 0.55f) continue;
                    }

                    bool inBlocked = false;
                    foreach (var b in blocked)
                    {
                        // Once ucuz daire testi: 11 poligonu her aday icin
                        // kenar kenar taramak milyonlarca cagri eder.
                        float ddx = jx - b.center_x, ddz = jz - b.center_z;
                        if (ddx * ddx + ddz * ddz > b.radius_m * b.radius_m) continue;
                        if (PointInRing(jx, jz, b.ring)) { inBlocked = true; break; }
                    }
                    if (inBlocked) continue;

                    // Varyant KONUMDAN secilir, kur'adan degil: ayni sahne iki
                    // kez uretildiginde ayni agac ayni yerde olsun.
                    int span = Mathf.Max(1, protoRange.y - protoRange.x);
                    int idx = protoRange.x + (Mathf.Abs(Mathf.RoundToInt(jx * 7.3f + jz * 3.1f)) % span);

                    float s = 0.82f + (float)rng.NextDouble() * 0.42f;
                    outTrees.Add(new TreeInstance
                    {
                        position = new Vector3(u, 0f, v),   // y snapToHeightmap ile
                        prototypeIndex = idx,
                        widthScale = s,
                        heightScale = s * (0.92f + (float)rng.NextDouble() * 0.18f),
                        rotation = (float)rng.NextDouble() * Mathf.PI * 2f,
                        color = Color.white,
                        lightmapColor = Color.white,
                    });
                }
        }

        private static Vector3 TerrainOrigin(TerrainData data)
        {
            foreach (var t in UnityEngine.Object.FindObjectsByType<Terrain>())
                if (t.terrainData == data) return t.transform.position;

            var meta = TerrainImporter.ReadMeta(TerrainImporter.DefaultDataDir);
            return meta == null ? Vector3.zero
                : new Vector3((float)meta.world_origin_offset_m.x,
                              (float)meta.base_elevation_m,
                              (float)meta.world_origin_offset_m.z);
        }

        /// <summary>Noktanın halkanın EN YAKIN KENARINA uzaklığı (m).</summary>
        private static float DistanceToRing(float x, float z, LocalPoint[] ring)
        {
            float best = float.MaxValue;
            for (int i = 0; i < ring.Length; i++)
            {
                var a = ring[i];
                var b = ring[(i + 1) % ring.Length];
                float vx = b.x - a.x, vz = b.z - a.z;
                float len2 = vx * vx + vz * vz;
                float t = len2 < 1e-6f ? 0f
                    : Mathf.Clamp01(((x - a.x) * vx + (z - a.z) * vz) / len2);
                float dx = x - (a.x + t * vx), dz = z - (a.z + t * vz);
                best = Mathf.Min(best, Mathf.Sqrt(dx * dx + dz * dz));
            }
            return best;
        }

        private static bool PointInRing(float x, float z, LocalPoint[] ring)
        {
            bool inside = false;
            for (int i = 0; i < ring.Length; i++)
            {
                var a = ring[i];
                var b = ring[(i + 1) % ring.Length];
                if ((a.z > z) != (b.z > z))
                {
                    float xc = a.x + (z - a.z) * (b.x - a.x) / (b.z - a.z);
                    if (x < xc) inside = !inside;
                }
            }
            return inside;
        }

        /// <summary>Ağaç görüntüleme mesafeleri — bileşenin üstünde yaşarlar.</summary>
        public static void ApplyToOpenScenes()
        {
            foreach (var t in UnityEngine.Object.FindObjectsByType<Terrain>())
            {
                // Uçuş oyunu: ağaç kütlesi siluetin parçası, yalnız yakın süsü
                // değil.
                //
                // `treeBillboardDistance` ÖLÇÜLEREK ayarlandı. 160 m'de
                // bırakılmıştı ve 400 m yükseklikten alınan karede ağaçlar
                // **hiç görünmüyordu**: Unity billboard'ı yalnız SpeedTree ve
                // Tree Creator varlıkları için üretir; bizimkiler LOD Group'lu
                // normal prefablar, yani billboard mesafesinin ötesinde
                // billboard'a geçmiyor, tamamen **kayboluyorlar**. Mesafeyi
                // görüntüleme mesafesine eşitlemek onları LOD'lu meshler
                // olarak sonuna kadar çizdirir.
                t.treeDistance = 3000f;
                t.treeBillboardDistance = 3000f;
                t.treeCrossFadeLength = 40f;
                t.treeMaximumFullLODCount = 150;

                // GPU ORNEKLEME: gorsel bedeli YOK, o yuzden burada.
                //
                // Bir tur boyunca bu mesafeleri kisaltmayi denedim
                // (billboard 180 m) ve olcum "iyilesti" — cunku orman
                // 180 m'nin otesinde CIZILMIYORDU. Ustteki not zaten
                // soyluyordu: bizim agaclar SpeedTree degil, Unity onlar
                // icin billboard uretmez, mesafenin otesinde tamamen
                // KAYBOLURLAR. Geri alindiginda sayilar kimildamadi
                // (bos arazi 1440p 10,05 -> 10,03) — yani kazanc hic
                // orada degilmis; SSGI'yi kapatan kademe gecisindenmis.
                t.drawInstanced = true;
                EditorUtility.SetDirty(t);
            }
        }
    }
}
