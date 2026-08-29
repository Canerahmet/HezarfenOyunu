using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Hezarfen.Editor.Gis
{
    /// <summary>
    /// <b>Dere yataklarını araziye oyar</b> (ADR 0074, seçenek C2).
    ///
    /// ## Neden oymak zorunda kaldık
    ///
    /// ADR 0074 önce şunu önermişti: *"yatak elle çizilmez, DEM'in en alçak
    /// çizgisinden türetilir."* O cümle <see cref="DereAgi"/> ile sınandı ve
    /// <b>yanlış çıktı</b>: denize ulaşan hatların oyuk derinliği ortalama
    /// <b>0,4 m</b>. Bu DEM'de oyulmuş vadi yok — yükseklik verisi o ölçekte
    /// düzleşmiş. "En alçak çizgiyi izle" derken varsaydığım şey (derenin
    /// araziye iz bırakmış olması) bu veride doğru değil.
    ///
    /// Caner ölçümü gördükten sonra C2'yi seçti: yatak oyulur, semtler
    /// yeniden üretilir.
    ///
    /// ## İddianın sınırı — burada AÇIKÇA yazıyor
    ///
    /// İki parça var ve ikisinin kanıt değeri farklı:
    ///
    /// - <b>Ağızlar</b> coğrafyadan: Kağıthane ve Alibey dereleri Haliç'in
    ///   başına dökülür, Lykos suriçini geçip Marmara'ya iner. Bunlar
    ///   bilinen akarsulardır.
    /// - <b>Aradaki güzergâh</b> bu araçtan: iki uç arasındaki <b>en az
    ///   yükseltiye tırmanan</b> yol (Dijkstra). Yani rota elle çizilmiş bir
    ///   eğri değil, arazinin izin verdiği en ucuz iniş.
    ///
    /// Hiçbiri T1 değil. Hepsi <c>HistoricalTier.Reconstruction</c> (T2) ve
    /// <c>sourceNote</c> alanı bunu satır satır yazıyor. RESEARCH.md yalnız
    /// Kağıthane'yi (mesire olarak) anıyor; Alibey ve Lykos için bu depoda
    /// kaynak satırı <b>yok</b> ve dahil etme kararı ADR 0074'tür.
    /// </summary>
    public static class DereKur
    {
        private const string TerrainScene =
            "Assets/_Project/Scenes/Faz1_Terrain.unity";

        /// <summary>Bir derenin tanımı — uçları coğrafyadan, arası araziden.</summary>
        public sealed class Tanim
        {
            public string ad;
            /// <summary>Denize döküldüğü nokta (dünya x,z).</summary>
            public Vector2 agiz;
            /// <summary>Haritaya girdiği kenar noktası (dünya x,z).</summary>
            public Vector2 giris;
            /// <summary>Su yüzeyi genişliği (m).</summary>
            public float genislik;
            /// <summary>Yatağın çevresinden ne kadar aşağıda olacağı (m).</summary>
            public float derinlik;
            /// <summary>Yatağın iki yanındaki eğimli kıyı bandı (m).</summary>
            public float kiyi;
            public string kaynakNotu;
        }

        /// <summary>
        /// Üç dere. Ağız ve giriş noktaları <b>haritanın kendisinden</b>
        /// okundu: Haliç'in en batı deniz noktası (−3277, 2591), kara
        /// surları x ≈ −3400 hattında z −3988…+1490 arası, Marmara kıyısı
        /// z ≈ −2800.
        /// </summary>
        public static Tanim[] Dereler() => new[]
        {
            new Tanim
            {
                ad = "Kagithane",
                agiz = new Vector2(-3150f, 2750f),
                giris = new Vector2(-2400f, 6900f),
                genislik = 26f, derinlik = 7f, kiyi = 55f,
                kaynakNotu =
                    "Kagithane deresi (T2). RESEARCH.md Eyup bolumunde "
                    + "'Kagithane mesiresi' olarak ANILIR; guzergah icin "
                    + "kaynak satiri YOK. Agiz Halic'in basindan, ara "
                    + "guzergah en-az-yukselti yolundan (Dijkstra). "
                    + "Yatak oyuldu — ADR 0074 secenek C2.",
            },
            new Tanim
            {
                ad = "Alibey",
                agiz = new Vector2(-3380f, 2500f),
                giris = new Vector2(-6250f, 3900f),
                genislik = 22f, derinlik = 6f, kiyi = 48f,
                kaynakNotu =
                    "Alibey deresi (T2). RESEARCH.md'de KAYNAK SATIRI YOK; "
                    + "dahil etme karari ADR 0074 (Caner, 2026-08-29). "
                    + "Agiz Halic'in basindan, guzergah araziden.",
            },
            new Tanim
            {
                ad = "Lykos",
                agiz = new Vector2(-1850f, -2780f),
                giris = new Vector2(-6250f, -900f),
                genislik = 16f, derinlik = 5f, kiyi = 40f,
                kaynakNotu =
                    "Lykos / Bayrampasa deresi (T2). RESEARCH.md'de KAYNAK "
                    + "SATIRI YOK; dahil etme karari ADR 0074. Suricini "
                    + "gecip Marmara'ya iner; guzergah araziden.",
            },
        };

        /// <summary>
        /// <b>Yalnız su yüzeyini yeniler — yatağı OYMAZ.</b>
        ///
        /// Ayrı bir komut olmasının sebebi bir tuzak: oyma işlemi hedefini
        /// <b>o anki</b> araziden hesaplar, yani "Dere yataklarini oy"u
        /// ikinci kez çalıştırmak yatağı bir kat daha derinleştirir. Suyu
        /// düzeltmek için o komutu tekrar koşmak, sessizce 14 m derin bir
        /// kanyon açmak olurdu.
        ///
        /// Yatak zaten oyulduğu için güzergâh şimdi kanalın tabanıdır:
        /// yol yeniden bulunur, su o tabanın biraz üstüne oturur.
        /// </summary>
        [MenuItem("Hezarfen/GIS/Dere sularini yenile")]
        public static void SuyuYenile()
        {
            var sahne = EditorSceneManager.OpenScene(
                TerrainScene, OpenSceneMode.Single);
            var arazi = Object.FindAnyObjectByType<Terrain>();
            if (arazi == null) { Debug.LogError("[Hezarfen] Arazi yok."); return; }

            var yollar = new Dictionary<string, List<Vector3>>();
            foreach (var d in Dereler())
            {
                var yol = Yumusat(EnUcuzYol(arazi, d.giris, d.agiz), 4);
                if (yol.Count < 4) continue;
                var su = new List<Vector3>(yol.Count);
                foreach (var p in yol)
                {
                    // Yol araziden geliyor ve arazi ARTIK oyulmus: p.y
                    // yatagin tabanidir. Su onun biraz ustunde durur.
                    float taban = arazi.SampleHeight(p) + arazi.transform.position.y;
                    su.Add(new Vector3(p.x, taban, p.z));
                }
                yollar[d.ad] = su;
            }

            SuyuKur(yollar, tabanHazir: true);
            EditorSceneManager.MarkSceneDirty(sahne);
            EditorSceneManager.SaveScene(sahne, TerrainScene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Hezarfen] Dere sulari yenilendi.");
        }

        [MenuItem("Hezarfen/GIS/Dere yataklarini oy")]
        public static void Oy()
        {
            var sahne = EditorSceneManager.OpenScene(
                TerrainScene, OpenSceneMode.Single);
            var arazi = Object.FindAnyObjectByType<Terrain>();
            if (arazi == null)
            {
                Debug.LogError("[Hezarfen] TR_Istanbul yok.");
                return;
            }

            var data = arazi.terrainData;
            int hm = data.heightmapResolution;
            var yuk = data.GetHeights(0, 0, hm, hm);
            var kok = arazi.transform.position;
            float boy = data.size.x / (hm - 1);
            float dikey = data.size.y;

            var rapor = new System.Text.StringBuilder();
            rapor.AppendLine("# Dere yatakları — oyuldu");
            rapor.AppendLine();
            rapor.AppendLine("ADR 0074 seçenek C2. Ağızlar coğrafyadan,");
            rapor.AppendLine("aradaki güzergâh **en az yükseltiye tırmanan**");
            rapor.AppendLine("yoldan (Dijkstra). Hepsi T2.");
            rapor.AppendLine();
            rapor.AppendLine("| dere | uzunluk (m) | ağız kotu | giriş kotu | "
                             + "genişlik | derinlik |");
            rapor.AppendLine("|---|---:|---:|---:|---:|---:|");

            var yollar = new Dictionary<string, List<Vector3>>();

            foreach (var d in Dereler())
            {
                var yol = EnUcuzYol(arazi, d.giris, d.agiz);
                if (yol.Count < 4)
                {
                    Debug.LogWarning($"[Hezarfen] {d.ad}: yol bulunamadi.");
                    continue;
                }

                yol = Yumusat(yol, 4);
                float uz = 0f;
                for (int i = 1; i < yol.Count; i++)
                    uz += Vector2.Distance(
                        new Vector2(yol[i - 1].x, yol[i - 1].z),
                        new Vector2(yol[i].x, yol[i].z));

                var yatak = YatakKotu(yol, d);
                OyYatagi(yuk, hm, kok, boy, dikey, yol, yatak, d);

                yollar[d.ad] = new List<Vector3>();
                for (int i = 0; i < yol.Count; i++)
                    yollar[d.ad].Add(new Vector3(yol[i].x, yatak[i], yol[i].z));

                rapor.AppendLine($"| {d.ad} | {uz:0} | {yatak[yatak.Length - 1]:0.0} | "
                                 + $"{yatak[0]:0.0} | {d.genislik:0} | "
                                 + $"{d.derinlik:0} |");
            }

            data.SetHeights(0, 0, yuk);

            // SU YUZEYI — yoksa oyulan sey dere degil HENDEKtir.
            SuyuKur(yollar, tabanHazir: false);
            EditorSceneManager.MarkSceneDirty(sahne);
            EditorSceneManager.SaveScene(sahne, TerrainScene);
            AssetDatabase.SaveAssets();

            rapor.AppendLine();
            rapor.AppendLine("> **Arazi değişti.** Semtler bu yükseklik");
            rapor.AppendLine("> verisine göre oturuyordu; hepsi yeniden");
            rapor.AppendLine("> üretilmeli, sonra zemin denetimi tekrar");
            rapor.AppendLine("> koşulmalı. Yoksa yatağın yakınındaki evler");
            rapor.AppendLine("> havada kalır.");

            Directory.CreateDirectory("../../renders/denetim");
            File.WriteAllText("../../renders/denetim/dere_yataklari.md",
                              rapor.ToString());
            Debug.Log("[Hezarfen] Dere yataklari oyuldu. SONRAKI ADIM: "
                      + "Hezarfen -> GIS -> Butun semtleri doldur");
        }

        /// <summary>
        /// Yatakların üstüne su şeridi kurar.
        ///
        /// Yatağı oymak tek başına bir <b>hendek</b> üretir. Su yüzeyi
        /// yatağın biraz üstünde, kıyıya doğru genişleyen düz bir şerittir;
        /// akış yönünde UV ilerler ki malzeme akıntıyı gösterebilsin.
        ///
        /// Deniz yüzeyi (<c>WATER_Bogaz_Halic</c>) y=0'dadır ve dereler
        /// ondan yukarı çıkar; ikisi ağızda üst üste biner, bu doğrudur —
        /// derenin ağzı gerçekten denizdir.
        /// </summary>
        private static void SuyuKur(Dictionary<string, List<Vector3>> yollar,
                                    bool tabanHazir)
        {
            var kok = GameObject.Find("DERELER_1632");
            if (kok != null) Object.DestroyImmediate(kok);
            kok = new GameObject("DERELER_1632");

            var malzeme = SuMalzemesi();

            foreach (var d in Dereler())
            {
                if (!yollar.TryGetValue(d.ad, out var yol)) continue;
                if (yol.Count < 3) continue;

                var verts = new List<Vector3>();
                var uvs = new List<Vector2>();
                var tris = new List<int>();
                float mesafe = 0f;

                for (int i = 0; i < yol.Count; i++)
                {
                    Vector3 ileri = i == 0 ? yol[1] - yol[0]
                                  : yol[i] - yol[i - 1];
                    ileri.y = 0f;
                    if (ileri.sqrMagnitude < 1e-4f) ileri = Vector3.forward;
                    ileri.Normalize();
                    var yan = new Vector3(-ileri.z, 0f, ileri.x);

                    // Su, yatagin icinde durur: kiyi payi kalsin.
                    // `tabanHazir` ise yol zaten oyulmus kanalin tabani;
                    // degilse yatak kotu hesaplanmis haliyle geliyor.
                    float y = yol[i].y + d.derinlik * (tabanHazir ? 0.35f : 0.6f);
                    float yariGen = d.genislik * 0.5f;

                    verts.Add(yol[i] - yan * yariGen + Vector3.up * (y - yol[i].y));
                    verts.Add(yol[i] + yan * yariGen + Vector3.up * (y - yol[i].y));
                    uvs.Add(new Vector2(0f, mesafe / 12f));
                    uvs.Add(new Vector2(1f, mesafe / 12f));

                    if (i > 0)
                    {
                        int b = (i - 1) * 2;
                        tris.Add(b); tris.Add(b + 2); tris.Add(b + 1);
                        tris.Add(b + 1); tris.Add(b + 2); tris.Add(b + 3);
                        mesafe += Vector3.Distance(yol[i - 1], yol[i]);
                    }
                }

                var mesh = new Mesh { name = $"SM_Dere_{d.ad}" };
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                mesh.SetVertices(verts);
                mesh.SetUVs(0, uvs);
                mesh.SetTriangles(tris, 0);
                mesh.RecalculateNormals();
                mesh.RecalculateTangents();
                mesh.RecalculateBounds();

                EnsureFolder("Assets/_Project/Art/Models/Generated");
                AssetDatabase.CreateAsset(
                    mesh,
                    $"Assets/_Project/Art/Models/Generated/SM_Dere_{d.ad}.asset");

                var go = new GameObject($"Dere_{d.ad}");
                go.transform.SetParent(kok.transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = go.AddComponent<MeshRenderer>();
                if (malzeme != null) mr.sharedMaterial = malzeme;

                var tag = go.AddComponent<Hezarfen.Core.HistoricalTag>();
                tag.tier = Hezarfen.Core.HistoricalTier.Reconstruction;
                tag.sourceNote = d.kaynakNotu;
            }
        }

        /// <summary>
        /// Dere suyunun malzemesi — <b>yoksa üretilir</b>.
        ///
        /// Deniz HDRP'nin <c>WaterSurface</c> bileşenini kullanıyor ama o
        /// bileşen <b>düz bir yüzey</b>dir: kıvrılan bir dereyi izlemek için
        /// yol boyunca onlarca örnek gerekirdi ve her biri ayrı bir su
        /// simülasyonu demek. Dere için şerit mesh + saydam malzeme yeterli.
        ///
        /// İlk yazımda burada var olmayan iki malzeme yolu deneniyordu ve
        /// ikisi de bulunamayınca şerit <b>malzemesiz</b> kalıyordu — yani
        /// oyunda macenta bir kurdele. Depoda su malzemesi olmadığı için
        /// artık kod onu kendisi kuruyor; sohbette kalan varlık yok
        /// (CLAUDE.md), dosyaya yazılıyor.
        /// </summary>
        private static Material SuMalzemesi()
        {
            const string yol = "Assets/_Project/Art/Materials/M_Su_Dere.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(yol);
            if (m != null) return m;

            var shader = Shader.Find("HDRP/Lit");
            if (shader == null)
            {
                Debug.LogError("[Hezarfen] HDRP/Lit bulunamadi.");
                return null;
            }

            m = new Material(shader) { name = "M_Su_Dere" };
            // Saydam, koyu yesil-mavi, cok parlak: durgun dere suyu.
            m.SetFloat("_SurfaceType", 1f);              // Transparent
            m.SetFloat("_BlendMode", 0f);                // Alpha
            m.SetFloat("_Smoothness", 0.94f);
            m.SetFloat("_Metallic", 0f);
            m.SetColor("_BaseColor", new Color(0.10f, 0.22f, 0.21f, 0.78f));
            m.SetFloat("_AlphaCutoffEnable", 0f);
            m.renderQueue = 3000;

            EnsureFolder("Assets/_Project/Art/Materials");
            AssetDatabase.CreateAsset(m, yol);
            Debug.Log("[Hezarfen] M_Su_Dere.mat uretildi.");
            return m;
        }

        private static void EnsureFolder(string yol)
        {
            if (AssetDatabase.IsValidFolder(yol)) return;
            var parca = yol.Split('/');
            string b = parca[0];
            for (int i = 1; i < parca.Length; i++)
            {
                string alt = b + "/" + parca[i];
                if (!AssetDatabase.IsValidFolder(alt))
                    AssetDatabase.CreateFolder(b, parca[i]);
                b = alt;
            }
        }

        /// <summary>
        /// İki nokta arasında <b>en az yükseltiye tırmanan</b> yol.
        ///
        /// Maliyet, hücrenin kotu artı bir adım bedelidir; yani yol düzlüğü
        /// ve alçağı sever, sırta tırmanmaz. Su fiziği bu değildir (su
        /// yalnız aşağı akar), ama burada aranan şey suyun izlediği yol
        /// değil, <b>yatağın açılacağı en makul hat</b> — arazi düzleşmiş
        /// olduğu için gerçek yatak zaten okunamıyor.
        /// </summary>
        public static List<Vector3> EnUcuzYol(Terrain arazi, Vector2 bas,
                                              Vector2 son)
        {
            int n = DereAgi.Izgara;
            var kok = arazi.transform.position;
            float boy = arazi.terrainData.size.x / n;

            var kot = new float[n, n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                    kot[x, y] = arazi.SampleHeight(new Vector3(
                        kok.x + (x + 0.5f) * boy, 0f,
                        kok.z + (y + 0.5f) * boy)) + kok.y;

            (int x, int y) Hucre(Vector2 d) => (
                Mathf.Clamp(Mathf.FloorToInt((d.x - kok.x) / boy), 0, n - 1),
                Mathf.Clamp(Mathf.FloorToInt((d.y - kok.z) / boy), 0, n - 1));

            var b = Hucre(bas);
            var s = Hucre(son);

            var maliyet = new float[n, n];
            var geldi = new (int x, int y)[n, n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                { maliyet[x, y] = float.MaxValue; geldi[x, y] = (-1, -1); }

            // Basit ikili yigin yerine sirali kume: 512² = 262k hucre,
            // bir kereye mahsus bir Editor islemi icin yeterli.
            var acik = new SortedSet<(float m, int x, int y)>();
            maliyet[b.x, b.y] = 0f;
            acik.Add((0f, b.x, b.y));

            while (acik.Count > 0)
            {
                var su = acik.Min;
                acik.Remove(su);
                if (su.x == s.x && su.y == s.y) break;
                if (su.m > maliyet[su.x, su.y]) continue;

                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        int ax = su.x + dx, ay = su.y + dy;
                        if (ax < 0 || ay < 0 || ax >= n || ay >= n) continue;

                        float adim = (dx != 0 && dy != 0) ? 1.4142f : 1f;
                        // Kot ne kadar yuksekse o kadar pahali; ayrica
                        // TIRMANIS ceza alir ki yol sirti asmasin.
                        float tirmanis = Mathf.Max(
                            0f, kot[ax, ay] - kot[su.x, su.y]);
                        float bedel = adim * (1f + Mathf.Max(0f, kot[ax, ay]) * 0.05f)
                                      + tirmanis * 4f;
                        float yeni = maliyet[su.x, su.y] + bedel;
                        if (yeni >= maliyet[ax, ay]) continue;
                        maliyet[ax, ay] = yeni;
                        geldi[ax, ay] = (su.x, su.y);
                        acik.Add((yeni, ax, ay));
                    }
            }

            var yol = new List<Vector3>();
            var p = s;
            var korkuluk = new HashSet<(int, int)>();
            while (p.x >= 0 && korkuluk.Add(p))
            {
                yol.Add(new Vector3(kok.x + (p.x + 0.5f) * boy,
                                    kot[p.x, p.y],
                                    kok.z + (p.y + 0.5f) * boy));
                if (p.x == b.x && p.y == b.y) break;
                p = geldi[p.x, p.y];
            }
            yol.Reverse();          // giristen agiza
            return yol;
        }

        /// <summary>Yolu hareketli ortalamayla yumuşatır.</summary>
        private static List<Vector3> Yumusat(List<Vector3> yol, int yaricap)
        {
            var c = new List<Vector3>(yol.Count);
            for (int i = 0; i < yol.Count; i++)
            {
                Vector3 t = Vector3.zero; int k = 0;
                for (int j = -yaricap; j <= yaricap; j++)
                {
                    int a = i + j;
                    if (a < 0 || a >= yol.Count) continue;
                    t += yol[a]; k++;
                }
                c.Add(t / k);
            }
            return c;
        }

        /// <summary>
        /// Yatak kotu — <b>ağza doğru kesintisiz iner</b>.
        ///
        /// Arazinin kendi kotunu izlemek yatağı yokuş yukarı akıtabilirdi;
        /// su bunu yapmaz. Kot bu yüzden ağızdan geriye doğru yürünerek
        /// monoton hale getirilir.
        /// </summary>
        private static float[] YatakKotu(List<Vector3> yol, Tanim d)
        {
            int n = yol.Count;
            var y = new float[n];
            for (int i = 0; i < n; i++) y[i] = yol[i].y - d.derinlik;

            // Agiz deniz seviyesine oturur.
            y[n - 1] = Mathf.Min(y[n - 1], -0.6f);
            for (int i = n - 2; i >= 0; i--)
                y[i] = Mathf.Max(y[i], y[i + 1] + 0.05f);
            return y;
        }

        /// <summary>
        /// Yatağı ve kıyı bandını yükseklik verisine işler.
        ///
        /// Kesit: ortada düz yatak, iki yanda <c>kiyi</c> boyunca araziye
        /// karışan eğim. Sert kenar bırakmak, dereyi bir hendek gibi
        /// gösterirdi.
        /// </summary>
        private static void OyYatagi(float[,] yuk, int hm, Vector3 kok,
                                     float boy, float dikey,
                                     List<Vector3> yol, float[] yatak, Tanim d)
        {
            float disYaricap = d.genislik * 0.5f + d.kiyi;

            for (int i = 0; i < yol.Count; i++)
            {
                var c = new Vector2(yol[i].x, yol[i].z);
                float hedef = yatak[i];

                int x0 = Mathf.Max(0, Mathf.FloorToInt((c.x - disYaricap - kok.x) / boy));
                int x1 = Mathf.Min(hm - 1, Mathf.CeilToInt((c.x + disYaricap - kok.x) / boy));
                int z0 = Mathf.Max(0, Mathf.FloorToInt((c.y - disYaricap - kok.z) / boy));
                int z1 = Mathf.Min(hm - 1, Mathf.CeilToInt((c.y + disYaricap - kok.z) / boy));

                for (int zi = z0; zi <= z1; zi++)
                    for (int xi = x0; xi <= x1; xi++)
                    {
                        float wx = kok.x + xi * boy, wz = kok.z + zi * boy;
                        float uzak = Vector2.Distance(new Vector2(wx, wz), c);
                        if (uzak > disYaricap) continue;

                        float ic = d.genislik * 0.5f;
                        float k = uzak <= ic ? 1f
                                : 1f - Mathf.SmoothStep(0f, 1f,
                                        (uzak - ic) / d.kiyi);

                        float suAnki = kok.y + yuk[zi, xi] * dikey;
                        float yeni = Mathf.Lerp(suAnki, hedef, k);
                        // Yalniz ASAGI oyar; dere tepe yapmaz.
                        if (yeni >= suAnki) continue;
                        yuk[zi, xi] = Mathf.Clamp01((yeni - kok.y) / dikey);
                    }
            }
        }
    }
}
