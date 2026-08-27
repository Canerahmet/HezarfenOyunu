using System.Collections.Generic;
using System.IO;
using Hezarfen.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Hezarfen.Editor.Gis
{
    /// <summary>
    /// **Theodosius kara surlarını** GIS hattı boyunca kurar (Faz 3, A-kademe).
    ///
    /// ## Galata'dan farkı: bu bir duvar değil, bir KESİT
    ///
    /// <see cref="WallBuilder"/> tek bir perde duvar döşer. Kara surları
    /// **üç katmanlı** bir savunma sistemidir ve kaynağın verdiği asıl sayı
    /// katmanların tek tek ölçüleri değil, **toplam derinlik: 70 m**.
    ///
    /// Kesit, hattın merkezinden dışa doğru:
    ///
    /// <code>
    ///   iç sur          5,0 m   (yükseklik 12 m, 96 burç × 25 m)
    ///   peribolos      20,0 m
    ///   dış sur         2,0 m   (yükseklik 8,75 m)
    ///   parateikhion   17,0 m
    ///   hendek         20,0 m   (derinlik 10 m)
    ///   glasi           6,0 m
    ///   ------------------------
    ///                  70,0 m   = belgeli toplam
    /// </code>
    ///
    /// Ara ölçüler **D3**'tür; toplamları belgeli sayıya oturmak zorundadır
    /// ve hem Blender kiti hem bu sınıf onu denetler. Uydurulan sayı yok,
    /// **paylaşılan bir toplam** var.
    ///
    /// ## Burç aralığı elle GİRİLMEZ
    ///
    /// Galata'da "burçlar arası hedef mesafe 60 m" diye bir **taslak** sayı
    /// yazmıştım ve kaynak vermiyordu. Burada gerek yok: kaynak burç
    /// **sayısını** veriyor (**96**) ve hattın uzunluğu **ölçülü**
    /// (5 824 m). Aralık ikisinin bölümüdür: **60,7 m**.
    ///
    /// Ve bu üçlü kendi kendini denetliyor: kaynak aralığı bağımsız olarak
    /// *"21-77 m, çoğu 40-60"* diye veriyor. 60,7 o bandın üst ucunda —
    /// yani sayılan 96, ölçülen hat ve belgeli aralık **birbirini tutuyor**.
    ///
    /// ## "Dışarısı" ölçülür, yazılmaz
    ///
    /// Hangi tarafın şehir olduğu elle yazılmadı: **deniz surlarının**
    /// (Marmara + Haliç) noktalarının ağırlık merkezi alınıyor ve normal
    /// ondan **uzaklaşan** yöne çevriliyor. Şehir kendi sınırlarından
    /// türüyor.
    /// </summary>
    public static class LandWallBuilder
    {
        public const string LocalJsonPath = "data/gis/istanbul/walls_1632_local.json";
        public const string WorldScene = "Assets/_Project/Scenes/Faz1_Terrain.unity";
        public const string RootName = "SUR_Kara";
        public const string PrefabDir = "Assets/_Project/Art/Prefabs";
        public const string MeshDir = "Assets/_Project/Art/Models/Generated";

        // --- Kesit (m). Toplami TotalDepth'e ESIT olmali. ---
        public const float InnerT = 5.0f;
        public const float InnerH = 12.0f;
        public const float Peribolos = 20.0f;
        public const float OuterT = 2.0f;
        public const float OuterH = 8.75f;
        public const float Parateikhion = 17.0f;
        public const float MoatW = 20.0f;
        public const float MoatD = 10.0f;
        public const float Glacis = 6.0f;

        /// <summary>Belgeli toplam savunma derinliği (m).</summary>
        public const float TotalDepth = 70.0f;

        /// <summary>İç sur burcu sayısı — <b>sayılan</b>.</summary>
        public const int InnerTowers = 96;

        /// <summary>Her kaçıncı burç sekizgen (kaynak "bazıları sekizgen" der).</summary>
        public const int OctagonEvery = 5;

        private const float Step = 6.0f;
        private const float MerlonW = 1.6f;
        private const float MerlonH = 1.5f;
        private const float TexM = 4.0f;

        /// <summary>Kesitin toplamı — belgeli 70 m'ye eşit olmalı.</summary>
        public static float SectionTotal() =>
            InnerT + Peribolos + OuterT + Parateikhion + MoatW + Glacis;

        /// <summary>Dış surun merkez ekseni, iç sur ekseninden (m).</summary>
        public static float OuterOffset => InnerT * 0.5f + Peribolos + OuterT * 0.5f;

        /// <summary>Hendeğin iç kenarı, iç sur ekseninden (m).</summary>
        public static float MoatOffset =>
            InnerT * 0.5f + Peribolos + OuterT + Parateikhion;

        [MenuItem("Hezarfen/GIS/Kara surlarini kur")]
        public static void BuildMenu()
        {
            var scene = EditorSceneManager.OpenScene(WorldScene, OpenSceneMode.Single);
            int n = Build(out string report);
            if (n < 0) { Debug.LogError("[Hezarfen] " + report); return; }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[Hezarfen] Kara surlari kuruldu.\n{report}");
        }

        public static int Build(out string report)
        {
            report = "";
            if (Mathf.Abs(SectionTotal() - TotalDepth) > 0.01f)
            {
                report = $"kesit toplami {SectionTotal():F1} m — belgeli deger "
                       + $"{TotalDepth:F1} m. Ara olculer degistiyse toplam da "
                       + "duzeltilmeli; bu sayi UYDURULMAZ.";
                return -1;
            }

            var terrainGo = GameObject.Find("TR_Istanbul");
            var terrain = terrainGo != null ? terrainGo.GetComponent<Terrain>() : null;
            if (terrain == null) { report = "TR_Istanbul yok."; return -1; }

            string repo = TerrainImporter.RepositoryRoot();
            string path = repo == null ? null : Path.Combine(repo,
                LocalJsonPath.Replace('/', Path.DirectorySeparatorChar));
            if (path == null || !File.Exists(path))
            { report = $"{LocalJsonPath} yok — once tools/gis/walls_build.py"; return -1; }

            var doc = GeoJsonImporter.ParseLocal(File.ReadAllText(path));
            List<Vector2> line = null;
            var inside = new List<Vector2>();
            var gates = new List<(string id, Vector2 p)>();
            foreach (var f in doc.features)
            {
                if (f.rings == null || f.rings.Count == 0) continue;
                var pts = new List<Vector2>();
                foreach (var q in f.rings[0]) pts.Add(new Vector2(q.x, q.z));
                if (f.id == "wall_land") line = pts;
                // SEHRIN ICI deniz surlarindan turer, elle yazilmaz.
                else if (f.id == "wall_sea_marmara" || f.id == "wall_sea_halic")
                    inside.AddRange(pts);
                else if (LandGates.Contains(f.id) && pts.Count > 0)
                    gates.Add((f.id, pts[0]));
            }
            if (line == null || line.Count < 2)
            { report = "wall_land hatti yok/eksik."; return -1; }
            if (inside.Count == 0)
            { report = "Deniz surlari yok — 'disarisi' olculemez."; return -1; }

            Vector2 city = Vector2.zero;
            foreach (var p in inside) city += p;
            city /= inside.Count;

            float length = 0f;
            for (int i = 0; i + 1 < line.Count; i++)
                length += Vector2.Distance(line[i], line[i + 1]);

            var old = GameObject.Find(RootName);
            if (old != null) Object.DestroyImmediate(old);
            var hostGo = new GameObject(RootName);
            var host = hostGo.transform;

            int innerMerlons = Curtain(terrain, host, line, city, 0f,
                                       InnerT, InnerH, "SM_KaraSurPerde_Ic");
            int outerMerlons = Curtain(terrain, host, line, city, OuterOffset,
                                       OuterT, OuterH, "SM_KaraSurPerde_Dis");
            int moatQuads = Moat(terrain, host, line, city);

            // BURC ARALIGI: sayilan 96'dan ve olculen hattan turer.
            float spacing = length / InnerTowers;
            int placedInner = 0, placedOuter = 0, oct = 0;
            for (int i = 0; i < InnerTowers; i++)
            {
                float s = (i + 0.5f) * spacing;
                bool sekizgen = (i % OctagonEvery) == (OctagonEvery - 1);
                string pf = sekizgen ? "PF_KaraSurBurcu_Sekizgen" : "PF_KaraSurBurcu";
                if (Place(terrain, host, pf, line, city, s, 0f)) placedInner++;
                if (sekizgen) oct++;
                // DIS SUR burclari IC olanlarin ARASINDA durur: kaynak
                // ikisinin sasirtmali dizildigini soyler ve savunma mantigi
                // da odur — dis burc ic burcun onunu kapatmaz.
                if (Place(terrain, host, "PF_KaraSurBurcu_Dis", line, city,
                          s + spacing * 0.5f, OuterOffset)) placedOuter++;
            }

            // --- YEDI KARA KAPISI --------------------------------------
            //
            // Kapi noktalari ile sur hatti AYRI taslaklardan gelir ve
            // cakismazlar. Kapi hatta en yakin noktaya TASINIR ve tasima
            // mesafesi RAPORLANIR — Galata'da alinan karar (ADR 0034): bir
            // kapinin surdan kac metre uzakta cizildigi, duzeltilmesi
            // gereken bir sayidir, sessizce yutulacak bir ayrinti degil.
            var gateLines = new List<string>();
            int placedGates = 0;
            foreach (var g in gates)
            {
                float gs = NearestS(line, g.p, out float shift);
                if (Place(terrain, hostGo.transform, "PF_KaraSurKapisi",
                          line, city, gs, 0f))
                {
                    placedGates++;
                    gateLines.Add($"    {g.id}: hatta {shift:F0} m tasindi");
                }
                else gateLines.Add($"    {g.id}: yerlestirilemedi");
            }

            var tag = hostGo.AddComponent<HistoricalTag>();
            tag.tier = HistoricalTier.Documented;
            tag.sourceNote =
                "Theodosius kara surlari, 5. yy; 1632'de ayakta (yikimlar "
                + "19.-20. yy). Ic sur 12 m / 96 burc x 25 m, dis sur 8,75 m, "
                + "hendek 20x10 m; TOPLAM SAVUNMA DERINLIGI 70 m (belgeli). "
                + "Ara olculer D3, toplamlari belgeli sayiya oturur. "
                + "Burc araligi elle girilmedi: 96 (sayilan) / hat uzunlugu "
                + "(olculen). ADR 0049.";

            report =
                $"hat {length:F0} m (belgeli 7,5 km Blachernae uzantisini da "
                + $"sayar; Theodosius kesimi ~5,7 km)\n"
                + $"kesit {SectionTotal():F1} m = belgeli {TotalDepth:F1} m\n"
                + $"burc araligi {spacing:F1} m — kaynak bagimsiz olarak "
                + "'21-77 m, cogu 40-60' der\n"
                + $"ic burc {placedInner}/{InnerTowers} ({oct} sekizgen), "
                + $"dis burc {placedOuter}\n"
                + $"mazgal: ic {innerMerlons}, dis {outerMerlons}; "
                + $"hendek {moatQuads} dortgen\n"
                + $"kapi {placedGates}/{gates.Count}\n"
                + string.Join("\n", gateLines);
            return placedInner;
        }

        /// <summary>Perde duvar: hattın <paramref name="offset"/> kadar dışına.</summary>
        private static int Curtain(Terrain terrain, Transform host,
                                   List<Vector2> line, Vector2 city,
                                   float offset, float thickness, float height,
                                   string meshName)
        {
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            float ht = thickness * 0.5f;
            int merlons = 0;

            float total = 0f;
            for (int i = 0; i + 1 < line.Count; i++)
                total += Vector2.Distance(line[i], line[i + 1]);
            int steps = Mathf.Max(1, Mathf.RoundToInt(total / Step));

            for (int k = 0; k < steps; k++)
            {
                float s0 = total * k / steps, s1 = total * (k + 1) / steps;
                Sample(line, s0, out Vector2 p0, out Vector2 t0);
                Sample(line, s1, out Vector2 p1, out Vector2 t1);
                Vector2 o0 = Outward(t0, p0, city), o1 = Outward(t1, p1, city);
                p0 += o0 * offset; p1 += o1 * offset;
                Vector2 n0 = o0 * ht, n1 = o1 * ht;

                float g0 = WallBuilder.Ground(terrain, p0);
                float g1 = WallBuilder.Ground(terrain, p1);
                if (g0 < 1f || g1 < 1f) continue;

                WallBuilder.Quad(verts, uvs, tris,
                    WallBuilder.W(p0 + n0, g0), WallBuilder.W(p1 + n1, g1),
                    WallBuilder.W(p1 + n1, g1 + height),
                    WallBuilder.W(p0 + n0, g0 + height), TexM);
                WallBuilder.Quad(verts, uvs, tris,
                    WallBuilder.W(p1 - n1, g1), WallBuilder.W(p0 - n0, g0),
                    WallBuilder.W(p0 - n0, g0 + height),
                    WallBuilder.W(p1 - n1, g1 + height), TexM);
                WallBuilder.Quad(verts, uvs, tris,
                    WallBuilder.W(p0 + n0, g0 + height),
                    WallBuilder.W(p1 + n1, g1 + height),
                    WallBuilder.W(p1 - n1, g1 + height),
                    WallBuilder.W(p0 - n0, g0 + height), TexM);

                float f = (Step - MerlonW) * 0.5f / Step;
                Sample(line, Mathf.Lerp(s0, s1, f), out Vector2 m0, out Vector2 mt0);
                Sample(line, Mathf.Lerp(s0, s1, 1f - f), out Vector2 m1, out Vector2 mt1);
                Vector2 q0 = Outward(mt0, m0, city), q1 = Outward(mt1, m1, city);
                m0 += q0 * offset; m1 += q1 * offset;
                WallBuilder.Box(verts, uvs, tris, m0 - q0 * ht, m0 + q0 * ht,
                                m1 - q1 * ht, m1 + q1 * ht,
                                Mathf.Lerp(g0, g1, 0.5f) + height, MerlonH, TexM);
                merlons++;
            }
            if (verts.Count == 0) return 0;
            Commit(host, verts, uvs, tris, meshName, "M_Stone_Rubble");
            return merlons;
        }

        /// <summary>
        /// Hendek — arazi <b>oyulmaz</b>, çukur bir kabuk konur.
        ///
        /// Araziyi oymak DEM'i kalıcı olarak değiştirir ve geri alınamaz;
        /// hendek 20 m genişliğinde ve arazinin çözünürlüğü 30 m — yani DEM
        /// onu <b>zaten taşıyamaz</b>. Kabuk, hendeği görünür kılar ve
        /// araziyi bozmaz.
        /// </summary>
        private static int Moat(Terrain terrain, Transform host,
                                List<Vector2> line, Vector2 city)
        {
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            float total = 0f;
            for (int i = 0; i + 1 < line.Count; i++)
                total += Vector2.Distance(line[i], line[i + 1]);
            int steps = Mathf.Max(1, Mathf.RoundToInt(total / Step));
            int quads = 0;

            for (int k = 0; k < steps; k++)
            {
                Sample(line, total * k / steps, out Vector2 p0, out Vector2 t0);
                Sample(line, total * (k + 1) / steps, out Vector2 p1, out Vector2 t1);
                Vector2 o0 = Outward(t0, p0, city), o1 = Outward(t1, p1, city);
                Vector2 a0 = p0 + o0 * MoatOffset, a1 = p1 + o1 * MoatOffset;
                Vector2 b0 = a0 + o0 * MoatW, b1 = a1 + o1 * MoatW;

                float g0 = WallBuilder.Ground(terrain, a0);
                float g1 = WallBuilder.Ground(terrain, a1);
                if (g0 < 1f || g1 < 1f) continue;
                float f0 = g0 - MoatD, f1 = g1 - MoatD;

                // Ic sev, taban, dis sev.
                WallBuilder.Quad(verts, uvs, tris,
                    WallBuilder.W(a0, g0), WallBuilder.W(a1, g1),
                    WallBuilder.W(a1, f1), WallBuilder.W(a0, f0), TexM);
                WallBuilder.Quad(verts, uvs, tris,
                    WallBuilder.W(a0, f0), WallBuilder.W(a1, f1),
                    WallBuilder.W(b1, f1), WallBuilder.W(b0, f0), TexM);
                WallBuilder.Quad(verts, uvs, tris,
                    WallBuilder.W(b0, f0), WallBuilder.W(b1, f1),
                    WallBuilder.W(b1, g1), WallBuilder.W(b0, g0), TexM);
                quads += 3;
            }
            if (verts.Count == 0) return 0;
            Commit(host, verts, uvs, tris, "SM_KaraSurHendek", "M_Stone_Rubble");
            return quads;
        }

        private static void Commit(Transform host, List<Vector3> verts,
                                   List<Vector2> uvs, List<int> tris,
                                   string name, string mat)
        {
            var mesh = new Mesh { name = name };
            mesh.indexFormat = verts.Count > 65000
                ? UnityEngine.Rendering.IndexFormat.UInt32
                : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();

            if (!AssetDatabase.IsValidFolder(MeshDir))
                AssetDatabase.CreateFolder("Assets/_Project/Art/Models", "Generated");
            AssetDatabase.CreateAsset(mesh, $"{MeshDir}/{name}.asset");

            var go = new GameObject(name.Replace("SM_KaraSur", ""));
            go.transform.SetParent(host, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial =
                AssetDatabase.LoadAssetAtPath<Material>(
                    $"Assets/_Project/Art/Materials/Ottoman/{mat}.mat");
            go.AddComponent<MeshCollider>().sharedMesh = mesh;
        }

        private static bool Place(Terrain terrain, Transform host, string prefabName,
                                  List<Vector2> line, Vector2 city, float s,
                                  float offset)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"{PrefabDir}/{prefabName}.prefab");
            if (prefab == null) return false;
            Sample(line, s, out Vector2 p, out Vector2 tan);
            Vector2 o = Outward(tan, p, city);
            p += o * offset;
            float y = WallBuilder.Ground(terrain, p);
            if (y < 1f) return false;

            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, host);
            inst.transform.position = new Vector3(p.x, y, p.y);
            inst.transform.rotation = Quaternion.LookRotation(
                new Vector3(o.x, 0f, o.y), Vector3.up);
            return true;
        }

        /// <summary>Hattın <paramref name="s"/> metresindeki nokta ve teğet.</summary>
        private static void Sample(List<Vector2> line, float s,
                                   out Vector2 p, out Vector2 tan)
        {
            float acc = 0f;
            for (int i = 0; i + 1 < line.Count; i++)
            {
                Vector2 a = line[i], b = line[i + 1];
                float len = Vector2.Distance(a, b);
                if (s <= acc + len || i == line.Count - 2)
                {
                    float t = len < 1e-4f ? 0f : Mathf.Clamp01((s - acc) / len);
                    p = Vector2.Lerp(a, b, t);
                    tan = (b - a).normalized;
                    return;
                }
                acc += len;
            }
            p = line[0]; tan = Vector2.right;
        }

        /// <summary>Yedi kara kapısı — sur hattı üzerindedirler.</summary>
        private static readonly HashSet<string> LandGates =
            new HashSet<string>
            {
                "GT_Yedikule", "GT_Belgradkapi", "GT_Silivrikapi",
                "GT_Mevlanakapi", "GT_Topkapi", "GT_Edirnekapi",
                "GT_Egrikapi",
            };

        /// <summary>Hattın <c>q</c>'ya en yakın noktasının s'i.</summary>
        private static float NearestS(List<Vector2> line, Vector2 q,
                                      out float dist)
        {
            float acc = 0f, bestS = 0f;
            dist = float.MaxValue;
            for (int i = 0; i + 1 < line.Count; i++)
            {
                Vector2 a = line[i], b = line[i + 1];
                Vector2 ab = b - a;
                float len = ab.magnitude;
                float t = len < 1e-4f ? 0f
                        : Mathf.Clamp01(Vector2.Dot(q - a, ab) / (len * len));
                Vector2 c = a + ab * t;
                float d = Vector2.Distance(q, c);
                if (d < dist) { dist = d; bestS = acc + t * len; }
                acc += len;
            }
            return bestS;
        }

        /// <summary>Şehirden UZAKLAŞAN birim normal.</summary>
        private static Vector2 Outward(Vector2 tan, Vector2 p, Vector2 city)
        {
            var n = new Vector2(-tan.y, tan.x);
            return Vector2.Dot(n, p - city) < 0f ? -n : n;
        }
    }
}
