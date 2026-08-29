using System.Collections.Generic;
using System.IO;
using Hezarfen.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Hezarfen.Editor.Gis
{
    /// <summary>
    /// Galata surlarının **perde duvarını** GIS hattı boyunca üretir, burçları
    /// ve kapıları yerleştirir (Faz 3, S-kademe).
    ///
    /// ## Neden duvar prefab DEĞİL, mesh
    ///
    /// Hat **2,5 km**. 8 m'lik bir prefabla döşemek 300+ örnek eder ve her biri
    /// bir çizim çağrısıdır; hiçbir şey kazandırmaz, çünkü duvar hareket etmez.
    /// Kaldırım ve taş kaidelerde verilen kararın aynısı. Burç ve kapı ise
    /// **sayılı yapılardır** — onlar prefab.
    ///
    /// ## Ölçüler RÖLÖVEDEN
    ///
    /// İlk yazımda yükseklik taslaktı ve Caner'e soruldu (Karar 15); Caner
    /// *"tezden bulmaya çalışalım"* dedi ve tez bulundu: **Erdoğan (2013),
    /// İTÜ, dan. Ahunbay** — ayakta kalan sur, burç ve kapıların 2010 arazi
    /// ölçümleri. Duvar kalınlığı, çevre (2 800 m), alan (37 ha), hendek
    /// (15 m), burç boyutları ve kapı açıklığı artık **ölçülüdür**.
    ///
    /// ## Kapılar hattın ÜSTÜNDE DEĞİL — ve bu ölçülüp yazılır
    ///
    /// Kapı noktaları (`GT_Azapkapi`, `GT_KuleKapisi`) ile sur halkası ayrı
    /// taslak kaynaklardan geliyor ve çakışmıyorlar. Kapı hatta en yakın
    /// noktaya **taşınır**, ve taşıma mesafesi loglanır: bir kapının surdan
    /// kaç metre uzakta çizildiği, düzeltilmesi gereken bir sayıdır — sessizce
    /// yutulacak bir ayrıntı değil.
    /// </summary>
    public static class WallBuilder
    {
        public const string LocalJsonPath = "data/gis/istanbul/walls_1632_local.json";
        public const string WorldScene = "Assets/_Project/Scenes/Faz1_Terrain.unity";
        public const string RootName = "SUR_Galata";
        public const string PrefabDir = "Assets/_Project/Art/Prefabs";
        public const string MeshDir = "Assets/_Project/Art/Models/Generated";

        /// <summary>Perde duvar kalınlığı (m) — BELGELİ (~2 m).</summary>
        public const float WallThickness = 2.0f;

        /// <summary>
        /// Perde duvarının <b>yerel zeminden</b> yüksekliği (m) — <b>ÖLÇÜLÜ</b>.
        ///
        /// İlk yazımda 9,0 m taslaktı ve Caner'e soruldu (Karar 15). Caner
        /// *"tezden bulmaya çalışalım"* dedi; tez bulundu (Erdoğan 2013, İTÜ)
        /// ve ölçüleri verdi. Ayakta kalan parçalarda yükseklik
        /// <b>6,50 – 17 m</b> arasında değişiyor; aralık çelişki değil
        /// <b>eğim</b>: yüksek sayılar yamaç aşağı bakan dış yüzde, düşük
        /// sayılar sokak kotunda ölçülmüş. Buradaki 7,0 m <b>iç/yüksek
        /// kottan</b> olan yüksekliktir.
        /// </summary>
        public const float WallHeight = 7.0f;

        /// <summary>Geriye dönük ad.</summary>
        public const float WallHeightDraft = WallHeight;

        /// <summary>Mazgal dişi + boşluğu (m). Diş 1,4, boşluk 1,0.</summary>
        private const float MerlonPitch = 2.4f;
        private const float MerlonW = 1.4f;
        private const float MerlonH = 1.4f;

        /// <summary>Burçlar arası hedef mesafe (m) — TASLAK, kaynak vermiyor.</summary>
        public const float TowerSpacingDraft = 60f;

        [MenuItem("Hezarfen/GIS/Galata surlarini kur")]
        public static void BuildMenu()
        {
            var scene = EditorSceneManager.OpenScene(WorldScene, OpenSceneMode.Single);
            int n = Build(out string report);
            if (n < 0) { Debug.LogError("[Hezarfen] " + report); return; }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[Hezarfen] Galata surlari kuruldu.\n{report}");
        }

        public static int Build(out string report)
        {
            report = "";
            var terrainGo = GameObject.Find("TR_Istanbul");
            var terrain = terrainGo != null ? terrainGo.GetComponent<Terrain>() : null;
            if (terrain == null) { report = "TR_Istanbul yok."; return -1; }

            string repo = TerrainImporter.RepositoryRoot();
            string path = repo == null ? null : Path.Combine(repo,
                LocalJsonPath.Replace('/', Path.DirectorySeparatorChar));
            if (path == null || !File.Exists(path))
            { report = $"{LocalJsonPath} yok — once tools/gis/walls_build.py"; return -1; }

            var doc = GeoJsonImporter.ParseLocal(File.ReadAllText(path));
            List<Vector2> ring = null;
            var gates = new List<(string id, Vector2 p)>();
            foreach (var f in doc.features)
            {
                if (f.id == "wall_galata" && f.rings.Count > 0)
                {
                    ring = new List<Vector2>();
                    foreach (var q in f.rings[0]) ring.Add(new Vector2(q.x, q.z));
                }
                else if (f.id != null && f.id.StartsWith("GT_")
                         && f.rings.Count > 0 && f.rings[0].Count > 0
                         && (f.id.Contains("Azapkapi") || f.id.Contains("KuleKapisi")))
                    gates.Add((f.id, new Vector2(f.rings[0][0].x, f.rings[0][0].z)));
            }
            if (ring == null || ring.Count < 3)
            { report = "wall_galata halkasi yok/eksik."; return -1; }

            var old = GameObject.Find(RootName);
            if (old != null) Object.DestroyImmediate(old);
            var host = new GameObject(RootName);

            float perim = 0f;
            for (int i = 0; i < ring.Count; i++)
                perim += Vector2.Distance(ring[i], ring[(i + 1) % ring.Count]);

            // --- kapilar: hatta TASINIR, tasima mesafesi OLCULUR -------------
            var gateAt = new List<float>();          // halka boyunca s (m)
            var lines = new List<string>();
            foreach (var (id, p) in gates)
            {
                float s = NearestS(ring, p, out float dist, out Vector2 snapped);
                gateAt.Add(s);
                lines.Add($"  {id}: surdan {dist:F0} m uzakta cizilmis, hatta "
                          + $"tasindi (halka boyunca {s:F0} m).");
                PlaceOn(terrain, host.transform, "PF_SurKapisi", ring, s, lines, id);
            }

            // --- burclar: esit araliklarla, kapilarin uzerine gelmeden -------
            int towers = 0;
            int want = Mathf.Max(4, Mathf.RoundToInt(perim / TowerSpacingDraft));
            for (int i = 0; i < want; i++)
            {
                float s = perim * i / want;
                bool clash = false;
                foreach (float g in gateAt)
                    if (Mathf.Abs(s - g) < 14f || Mathf.Abs(s - g) > perim - 14f)
                        clash = true;
                if (clash) continue;
                // UC TIP DONUSUMLU. Tez hem iki BOY hem iki PLAN belgeliyor:
                // "belirli araliklarla insa edilmis DORTGEN VE U PLANLI
                // burclar". Ayakta kalan iki ornek (9,80x7,70/16,16 m ve
                // 7,02x5,84/~10 m) U planli; dortgen olanin olcusu yok ama
                // VARLIGI belgeli. Hepsini tek tip yapmak, kaynagin
                // soyledigi cesitliligi silerdi.
                string which = (i % 3 == 0) ? "PF_SurBurcu"
                             : (i % 3 == 1) ? "PF_SurBurcu_Kucuk"
                                            : "PF_SurBurcu_Dortgen";
                if (PlaceOn(terrain, host.transform, which, ring, s, null, null))
                    towers++;
            }

            int merlons = BuildCurtain(terrain, host.transform, ring, gateAt);

            // Biriken kaideler tek mesh olarak kurulur.
            OttomanStreetBuilder.KaideleriKur(host.transform,
                                              "Kaideler", "Galata");

            var tag = host.AddComponent<HistoricalTag>();
            tag.tier = HistoricalTier.Reconstruction;      // T2: hat kaba taslak
            tag.sourceNote =
                "Galata surlari (1335-1349 Ceneviz; 1864'e kadar EKSIKSIZ ayakta, "
                + "yani 1632'de tam). Duvar kalinligi 2 m, cevre 2800 m, alan "
                + "~37 ha, hendek 15 m. Perde duvar yuksekligi " + WallHeight
                + " m — OLCULU (Erdogan 2013, ITU rolovesi: ayakta kalan "
                + "parcalarda 6,50-17 m, fark egimden). Hat "
                + "walls_1632.geojson'dan ve kendi kaba taslagimiz; ADR "
                + "0029'da 37 ha'lik capaya olceklendi. RESEARCH.md 5.2";

            report = $"halka {ring.Count} nokta, cevre {perim:F0} m; "
                   + $"{towers} burc, {gates.Count} kapi, {merlons} mazgal. "
                   + $"Duvar {WallHeight} m (olculu, Erdogan 2013).\n"
                   + string.Join("\n", lines);
            return towers + gates.Count;
        }

        // ------------------------------------------------------------ yardım

        /// <summary>Halka boyunca s (m) → dünya noktası + teğet.</summary>
        private static void SampleRing(List<Vector2> ring, float s,
                                       out Vector2 p, out Vector2 tan)
        {
            int n = ring.Count;
            float acc = 0f;
            for (int i = 0; i < n; i++)
            {
                Vector2 a = ring[i], b = ring[(i + 1) % n];
                float len = Vector2.Distance(a, b);
                if (s <= acc + len || i == n - 1)
                {
                    float t = len < 1e-4f ? 0f : Mathf.Clamp01((s - acc) / len);
                    p = Vector2.Lerp(a, b, t);
                    tan = (b - a).normalized;
                    return;
                }
                acc += len;
            }
            p = ring[0]; tan = Vector2.right;
        }

        private static float NearestS(List<Vector2> ring, Vector2 q,
                                      out float dist, out Vector2 snapped)
        {
            int n = ring.Count;
            float acc = 0f, bestS = 0f;
            dist = float.MaxValue; snapped = ring[0];
            for (int i = 0; i < n; i++)
            {
                Vector2 a = ring[i], b = ring[(i + 1) % n];
                Vector2 ab = b - a;
                float len = ab.magnitude;
                float t = len < 1e-4f ? 0f : Mathf.Clamp01(Vector2.Dot(q - a, ab) / (len * len));
                Vector2 c = a + ab * t;
                float d = Vector2.Distance(q, c);
                if (d < dist) { dist = d; snapped = c; bestS = acc + t * len; }
                acc += len;
            }
            return bestS;
        }

        internal static float Ground(Terrain t, Vector2 p) =>
            t.SampleHeight(new Vector3(p.x, 0f, p.y)) + t.transform.position.y;

        /// <summary>Prefabın ayak izi yarıçapı (m).</summary>
        internal static float AyakIziYaricapi(GameObject prefab)
        {
            var rs = prefab.GetComponentsInChildren<Renderer>(true);
            if (rs.Length == 0) return 4f;
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return Mathf.Max(b.extents.x, b.extents.z);
        }

        /// <summary>
        /// Burcu/kapıyı <b>ayak izinin en yüksek köşesine</b> oturtur ve
        /// altında kalan boşluğu bildirir.
        ///
        /// Önceden yalnız MERKEZ kotu alınıyordu. Yamaçta bu, kulenin bir
        /// yanını havada bırakır: ölçüldü, sur burçlarının %18-50'si
        /// (PF_SurBurcu %50, PF_SurBurcu_Dortgen %41,7) 0,5-1,0 m boşlukla
        /// duruyordu. Sur bu şehrin en uzun yapısı; boşluk 5,5 km boyunca
        /// tekrar ediyordu.
        /// </summary>
        internal static float Oturt(Terrain t, Vector2 c, float yaricap,
                                    out float dip)
        {
            float hi = float.MinValue; dip = float.MaxValue;
            for (int i = -1; i <= 1; i++)
                for (int j = -1; j <= 1; j++)
                {
                    if (i == 0 && j == 0) continue;
                    float h = Ground(t, c + new Vector2(i * yaricap, j * yaricap));
                    hi = Mathf.Max(hi, h); dip = Mathf.Min(dip, h);
                }
            return hi;
        }

        /// <summary>Yapıyı halkanın üstüne, dışa bakacak şekilde koyar.</summary>
        private static bool PlaceOn(Terrain terrain, Transform host, string prefabName,
                                    List<Vector2> ring, float s,
                                    List<string> lines, string id)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"{PrefabDir}/{prefabName}.prefab");
            if (prefab == null)
            {
                lines?.Add($"  {prefabName} YOK — atlandi.");
                return false;
            }
            SampleRing(ring, s, out Vector2 p, out Vector2 tan);
            float yaricap = AyakIziYaricapi(prefab);
            float y = Oturt(terrain, p, yaricap, out float dip);
            if (y < 1f) return false;                       // suya kurulmaz

            // DISARI: halkanin disi. Halka saat yonunun tersineyse normal
            // isaret degistirir; merkezden uzaklasan yon secilir.
            Vector2 nrm = new Vector2(-tan.y, tan.x);
            Vector2 c = Centroid(ring);
            if (Vector2.Dot(nrm, p - c) < 0f) nrm = -nrm;

            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, host);
            inst.transform.position = new Vector3(p.x, y, p.y);
            // Prefabin onu +Z (CLAUDE.md); burc ve kapi DISARI bakar.
            inst.transform.rotation = Quaternion.LookRotation(
                new Vector3(nrm.x, 0f, nrm.y), Vector3.up);
            if (id != null)
                lines?.Add($"    -> {prefabName} @ ({p.x:F0}, {y:F0}, {p.y:F0})");
            
            if (y - dip > 0.05f)
                OttomanStreetBuilder.KaideEkle(
                    p, y, dip - 0.5f, yaricap * 2f + 0.4f,
                    yaricap * 2f + 0.4f, inst.transform.eulerAngles.y);

            return true;
        }

        private static Vector2 Centroid(List<Vector2> ring)
        {
            Vector2 s = Vector2.zero;
            foreach (var p in ring) s += p;
            return s / ring.Count;
        }

        /// <summary>
        /// Perde duvar mesh'i — gövde + mazgal dişleri, tek mesh.
        ///
        /// Duvar araziyi izler: taban her örnekte arazi kotundan, tepe
        /// <b>taban + yükseklik</b>. Sabit bir tepe kotu, yamaçta duvarı
        /// yere gömer ya da havada bırakırdı.
        /// </summary>
        private static int BuildCurtain(Terrain terrain, Transform host,
                                        List<Vector2> ring, List<float> gateAt)
        {
            const float Step = MerlonPitch;
            const float TexM = 2.0f;                 // old_stone_wall gercek olcusu
            float perim = 0f;
            for (int i = 0; i < ring.Count; i++)
                perim += Vector2.Distance(ring[i], ring[(i + 1) % ring.Count]);

            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            int merlons = 0;
            float ht = WallThickness * 0.5f;
            float bodyH = WallHeightDraft - MerlonH;

            int n = Mathf.Max(8, Mathf.RoundToInt(perim / Step));
            for (int i = 0; i < n; i++)
            {
                float s0 = perim * i / n, s1 = perim * (i + 1) / n;
                // Kapinin oldugu yerde duvar KESILIR — kapi orada duruyor.
                bool skip = false;
                foreach (float g in gateAt)
                {
                    float d = Mathf.Abs(s0 - g);
                    if (d > perim * 0.5f) d = perim - d;
                    if (d < 6.0f) skip = true;
                }
                if (skip) continue;

                SampleRing(ring, s0, out Vector2 p0, out Vector2 t0);
                SampleRing(ring, s1, out Vector2 p1, out Vector2 t1);
                Vector2 n0 = new Vector2(-t0.y, t0.x) * ht;
                Vector2 n1 = new Vector2(-t1.y, t1.x) * ht;

                float g0 = Ground(terrain, p0), g1 = Ground(terrain, p1);
                if (g0 < 1f || g1 < 1f) continue;        // denize girmez

                // Govde: dis yuz, ic yuz, ust.
                Quad(verts, uvs, tris,
                     W(p0 - n0, g0), W(p1 - n1, g1),
                     W(p1 - n1, g1 + bodyH), W(p0 - n0, g0 + bodyH), TexM);
                Quad(verts, uvs, tris,
                     W(p1 + n1, g1), W(p0 + n0, g0),
                     W(p0 + n0, g0 + bodyH), W(p1 + n1, g1 + bodyH), TexM);
                Quad(verts, uvs, tris,
                     W(p0 - n0, g0 + bodyH), W(p1 - n1, g1 + bodyH),
                     W(p1 + n1, g1 + bodyH), W(p0 + n0, g0 + bodyH), TexM);

                // Mazgal disi: adimin ortasinda, MerlonW genisliginde.
                float f = (Step - MerlonW) * 0.5f / Step;
                SampleRing(ring, Mathf.Lerp(s0, s1, f), out Vector2 m0, out Vector2 mt0);
                SampleRing(ring, Mathf.Lerp(s0, s1, 1f - f), out Vector2 m1, out Vector2 mt1);
                Vector2 k0 = new Vector2(-mt0.y, mt0.x) * ht;
                Vector2 k1 = new Vector2(-mt1.y, mt1.x) * ht;
                float mg = Mathf.Lerp(g0, g1, 0.5f) + bodyH;
                Box(verts, uvs, tris, m0 - k0, m0 + k0, m1 - k1, m1 + k1,
                    mg, MerlonH, TexM);
                merlons++;
            }

            if (verts.Count == 0) return 0;
            var mesh = new Mesh { name = "SM_SurPerde_Galata" };
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
            AssetDatabase.CreateAsset(mesh, $"{MeshDir}/{mesh.name}.asset");

            var go = new GameObject("Perde");
            go.transform.SetParent(host, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial =
                AssetDatabase.LoadAssetAtPath<Material>(
                    "Assets/_Project/Art/Materials/Ottoman/M_Stone_Rubble.mat");
            go.AddComponent<MeshCollider>().sharedMesh = mesh;
            return merlons;
        }

        internal static Vector3 W(Vector2 p, float y) => new Vector3(p.x, y, p.y);

        /// <summary>
        /// Dörtgen. Sarım <b>(0,2,1)+(0,3,2)</b>: a→b→c→d sırası yüze
        /// <b>önden bakıldığında SAAT YÖNÜNDE</b> olduğunda yüz doğru tarafa
        /// bakar.
        ///
        /// ## Bu satır bir ölçümün sonucudur
        ///
        /// İlk yazımda sarım (0,1,2)+(0,2,3) idi ve yorumda "böylece yüz
        /// dışarı bakar" yazıyordu. Ölçüm onu çürüttü: perde mesh'inin
        /// 4 199 yatay üçgeninden **4 198'i aşağı** bakıyordu — yani duvarın
        /// üstü ve mazgal tepeleri ters, tıpkı kaldırımda üç tur görünmeyen
        /// kusur gibi (ADR 0031 §2).
        ///
        /// Ve bu, o kusuru <b>bilerek</b> yazdığım yorumun altında oldu.
        /// Ders: sarım yorumla değil, <b>üçgen normali sayılarak</b>
        /// doğrulanır. `WallTests` şimdi onu sayıyor.
        /// </summary>
        internal static void Quad(List<Vector3> v, List<Vector2> uv, List<int> tri,
                                 Vector3 a, Vector3 b, Vector3 c, Vector3 d,
                                 float tex)
        {
            int i0 = v.Count;
            v.Add(a); v.Add(b); v.Add(c); v.Add(d);
            float w = Vector3.Distance(a, b), h = Vector3.Distance(b, c);
            uv.Add(new Vector2(0f, 0f));
            uv.Add(new Vector2(w / tex, 0f));
            uv.Add(new Vector2(w / tex, h / tex));
            uv.Add(new Vector2(0f, h / tex));
            tri.Add(i0); tri.Add(i0 + 2); tri.Add(i0 + 1);
            tri.Add(i0); tri.Add(i0 + 3); tri.Add(i0 + 2);
        }

        /// <summary>Mazgal dişi: dört yan + üst (alt görünmez, üretilmez).</summary>
        internal static void Box(List<Vector3> v, List<Vector2> uv, List<int> tri,
                                Vector2 a0, Vector2 b0, Vector2 a1, Vector2 b1,
                                float z, float h, float tex)
        {
            Vector3 A0 = W(a0, z), B0 = W(b0, z), A1 = W(a1, z), B1 = W(b1, z);
            Vector3 u = Vector3.up * h;
            Quad(v, uv, tri, A0, A1, A1 + u, A0 + u, tex);          // dis
            Quad(v, uv, tri, B1, B0, B0 + u, B1 + u, tex);          // ic
            Quad(v, uv, tri, B0, A0, A0 + u, B0 + u, tex);          // yan
            Quad(v, uv, tri, A1, B1, B1 + u, A1 + u, tex);          // yan
            Quad(v, uv, tri, A0 + u, A1 + u, B1 + u, B0 + u, tex);  // ust
        }
    }
}
