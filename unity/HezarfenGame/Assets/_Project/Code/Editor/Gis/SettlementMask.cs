using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Hezarfen.Editor.Gis
{
    /// <summary>
    /// <b>Yerleşimin çiğnediği zemin</b> — arazi örtüsüne verilen maske.
    ///
    /// ## Neden ADR 0024'ün reddettiği şey DEĞİL
    ///
    /// Arazi örtüsü kurulurken "mahalle maskesi" bilerek yapılmamıştı ve
    /// gerekçesi hâlâ geçerli: <c>districts.geojson</c> kendi içinde
    /// *"bu bir OYUN bölgesidir, mahalle sınırı değildir"* yazıyor; onu
    /// yerleşim sınırı gibi kullanmak kendi uyarımızı çiğnemek olurdu.
    /// Surlar da yetmez — 1632'de suriçinin batısı bostanlıktı.
    ///
    /// Bu maske o iddiayı kurmuyor. Kaynağı bir <b>sınır</b> değil, sahneye
    /// <b>fiilen koyduğumuz yapılar</b>: her yerleştirilen evin, dükkânın,
    /// mescidin çakışma dairesi. İddia şu kadar: *"buraya yapı koyduk, o
    /// hâlde burası basılmış topraktır."* Tarih hakkında hiçbir şey
    /// söylemiyor; sahne hakkında söylediği şey ise zaten doğru.
    ///
    /// ## Çözünürlük 7,49 m — ve bu dürüstlüktür
    ///
    /// Splatmap 2048², arazi 15 337 m: bir texel <b>7,49 m</b>, yani DEM'in
    /// kendi örnek aralığı. 4,6 m'lik bir sokak bir texel'den dardır; bu
    /// maske sokağı boyayamaz, <b>mahalleyi</b> boyar. Sokağın kendi zemini
    /// zaten bir mesh (`Kaldirim`). Daha ince bir splatmap, olmayan bir
    /// bilgiyi taklit etmek olurdu (ADR 0024).
    ///
    /// ## Dosya TÜRETİLMİŞTİR
    ///
    /// `data/gis/settlement.json` mahalle kurulurken yazılır ve `data/`
    /// depoya girmez (CLAUDE.md). Yoksa maske boştur ve örtü tam olarak
    /// eskisi gibi davranır.
    /// </summary>
    public static class SettlementMask
    {
        public const string RelativePath = "data/gis/settlement.json";

        /// <summary>Daire yarıçapının üstüne eklenen tam basılmış pay (m).</summary>
        private const float FullMarginM = 6.0f;

        /// <summary>Tam basılmıştan otlağa geçiş (m). Sert kenar doğada yoktur.</summary>
        private const float FadeM = 16.0f;

        [Serializable]
        public class Disc
        {
            public float x, z, r;
        }

        [Serializable]
        public class Quarter
        {
            public string name;
            public Disc[] discs;
        }

        [Serializable]
        public class File_
        {
            public string generated_by;
            public string note;
            public Quarter[] quarters;
        }

        // ------------------------------------------------------------- yazma

        /// <summary>
        /// Bir mahallenin dairelerini dosyaya yazar — <b>o mahallenin
        /// kaydını değiştirir, ötekilere dokunmaz.</b>
        ///
        /// Üzerine yazsaydı Galata kurulunca Balat'ın zemini otlağa dönerdi;
        /// aynı cinsten bir hata (paylaşılan varlık yolu) bir tur önce
        /// kaldırımda yakalandı.
        /// </summary>
        public static void Write(string quarter, IEnumerable<(Vector2 c, float r)> discs)
        {
            string path = Path.Combine(TerrainImporter.RepositoryRoot() ?? ".",
                                       RelativePath.Replace('/', Path.DirectorySeparatorChar));
            var file = ReadFile(path) ?? new File_
            {
                generated_by = "OttomanStreetBuilder",
                note = "Yerlesimin cignedigi zemin. Sinir iddiasi DEGIL: "
                     + "sahneye fiilen konan yapilarin cakisma daireleri. "
                     + "TURETILMISTIR, depoya girmez.",
                quarters = new Quarter[0],
            };

            var list = new List<Disc>();
            foreach (var (c, r) in discs)
                list.Add(new Disc { x = c.x, z = c.y, r = r });

            var others = new List<Quarter>();
            foreach (var q in file.quarters)
                if (q.name != quarter) others.Add(q);
            others.Add(new Quarter { name = quarter, discs = list.ToArray() });
            file.quarters = others.ToArray();

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            System.IO.File.WriteAllText(path, JsonUtility.ToJson(file, true));
        }

        private static File_ ReadFile(string path)
        {
            if (!System.IO.File.Exists(path)) return null;
            try { return JsonUtility.FromJson<File_>(System.IO.File.ReadAllText(path)); }
            catch (Exception e)
            {
                Debug.LogWarning($"[Hezarfen] {RelativePath} okunamadi: {e.Message}");
                return null;
            }
        }

        // ------------------------------------------------------------- okuma

        /// <summary>
        /// Sorgulanabilir maske. Daireler bir <b>ızgaraya</b> kovalanır;
        /// yoksa 4,2 milyon texel × binlerce daire dakikalar sürerdi.
        /// </summary>
        public class Grid
        {
            private const float Cell = 32f;              // m
            private readonly Dictionary<long, List<Disc>> _cells =
                new Dictionary<long, List<Disc>>();
            private float _reach;                        // en buyuk daire + pay

            public int Count { get; private set; }
            public Rect Bounds { get; private set; }

            public static Grid Load()
            {
                var g = new Grid();
                string path = Path.Combine(TerrainImporter.RepositoryRoot() ?? ".",
                    RelativePath.Replace('/', Path.DirectorySeparatorChar));
                var file = ReadFile(path);
                if (file?.quarters == null) return g;

                float minX = float.MaxValue, minZ = float.MaxValue;
                float maxX = float.MinValue, maxZ = float.MinValue;
                foreach (var q in file.quarters)
                {
                    if (q.discs == null) continue;
                    foreach (var d in q.discs)
                    {
                        g.Add(d);
                        float reach = d.r + FullMarginM + FadeM;
                        minX = Mathf.Min(minX, d.x - reach);
                        maxX = Mathf.Max(maxX, d.x + reach);
                        minZ = Mathf.Min(minZ, d.z - reach);
                        maxZ = Mathf.Max(maxZ, d.z + reach);
                    }
                }
                g.Bounds = g.Count == 0 ? new Rect()
                    : Rect.MinMaxRect(minX, minZ, maxX, maxZ);
                return g;
            }

            private void Add(Disc d)
            {
                Count++;
                _reach = Mathf.Max(_reach, d.r + FullMarginM + FadeM);
                float reach = d.r + FullMarginM + FadeM;
                int i0 = Mathf.FloorToInt((d.x - reach) / Cell);
                int i1 = Mathf.FloorToInt((d.x + reach) / Cell);
                int j0 = Mathf.FloorToInt((d.z - reach) / Cell);
                int j1 = Mathf.FloorToInt((d.z + reach) / Cell);
                for (int i = i0; i <= i1; i++)
                    for (int j = j0; j <= j1; j++)
                    {
                        long k = Key(i, j);
                        if (!_cells.TryGetValue(k, out var list))
                            _cells[k] = list = new List<Disc>();
                        list.Add(d);
                    }
            }

            private static long Key(int i, int j) => ((long)i << 32) ^ (uint)j;

            /// <summary>Dünya noktasında basılmışlık: 1 = çiğnenmiş, 0 = otlak.</summary>
            public float At(float wx, float wz)
            {
                if (Count == 0) return 0f;
                long k = Key(Mathf.FloorToInt(wx / Cell), Mathf.FloorToInt(wz / Cell));
                if (!_cells.TryGetValue(k, out var list)) return 0f;

                float best = 0f;
                foreach (var d in list)
                {
                    float dist = Mathf.Sqrt((wx - d.x) * (wx - d.x)
                                            + (wz - d.z) * (wz - d.z));
                    float full = d.r + FullMarginM;
                    float v = dist <= full ? 1f
                        : 1f - Mathf.Clamp01((dist - full) / FadeM);
                    // Yumusak gecis: daire kenarinda birden kesilmesin.
                    v = v * v * (3f - 2f * v);
                    if (v > best) best = v;
                    if (best >= 0.999f) break;
                }
                return best;
            }
        }
    }
}
