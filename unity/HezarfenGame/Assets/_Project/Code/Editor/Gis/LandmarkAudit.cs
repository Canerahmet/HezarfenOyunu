using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Hezarfen.Editor.Gis
{
    /// <summary>
    /// Landmark konumlarını <b>araziye karşı</b> denetler.
    ///
    /// ## Neden var
    ///
    /// Faz 3 boyunca <b>beş</b> konum hatası çıktı ve hiçbiri gözle
    /// görülmedi: Doğancılar 771 m, Okmeydanı 700 m, Üsküdar Mihrimah
    /// 164 m, İncili Köşk 156 m, Yeni Cami 148 m. Hepsi elle girilmişti,
    /// hepsi <c>approx</c> etiketliydi, ve her biri ancak <b>başka bir
    /// ölçümle çeliştiğinde</b> ortaya çıktı — biri literatürdeki mesafeyle,
    /// biri külliyenin kendi göreli konumlarıyla, biri "denize taşan yapı
    /// 125 m içeride" çelişkisiyle.
    ///
    /// Ortak nokta: <b>arazi, koordinatın doğruluğu hakkında konuşur.</b>
    /// Bir cami tepesini taçlandırıyorsa yerel zirveye yakın durmalı; bir
    /// tersane suyun kenarında olmalı; bir ok meydanı düz olmalı. Bunlar
    /// zevk değil, ölçülebilir iddialardır.
    ///
    /// Bu araç onları tek tek sorar ve şüphelileri listeler. Amaç
    /// koordinatı düzeltmek değil — <b>hangisine bakılacağını söylemek</b>.
    /// 16 <c>approx</c> koordinattan üçünü böyle ayıkladık; on üçü için
    /// kaynak aramak gerekmedi.
    /// </summary>
    public static class LandmarkAudit
    {
        /// <summary>Tepeyi taçlandırdığı bilinen yapılar (yerel zirveye yakın olmalı).</summary>
        public static readonly Dictionary<string, float> CrownsAHill =
            new Dictionary<string, float>
            {
                // deger = yerel zirveye izin verilen EN BUYUK fark (m)
                { "LM_Ayasofya", 8f },
                { "LM_Suleymaniye", 8f },
                { "LM_Sultanahmet", 8f },
                { "LM_FatihCamii", 8f },
                { "LM_Sehzade", 8f },
                { "LM_EskiSaray", 8f },
                { "LM_TopkapiSiluet", 8f },
                // Yavuz Selim BESINCI TEPEYI taclandirir; kaynak onu "Halic'e
                // hakim yuksek bir tepede" diye tarif eder. Ilk koordinat
                // zirvenin 27,7 m altindaydi ve hata boyle yakalandi.
                { "LM_YavuzSelim", 8f },
                { "LM_Yedikule", 8f },
            };

        /// <summary>Suyun kenarında olması gereken yapılar (m).</summary>
        public static readonly Dictionary<string, float> OnTheShore =
            new Dictionary<string, float>
            {
                // Tersane bir TERSANEDIR: gemi suya iner.
                { "LM_Tersane", 60f },
                { "LM_Tophane", 260f },
                { "LM_IncliKosk", 60f },
                { "LM_RustemPasa", 260f },
                { "LM_ArapCamii", 260f },
                { "LM_UskudarMihrimah", 220f },
            };

        /// <summary>Düz zemin isteyen yerler: yayılım sınırı (m / 150 m).</summary>
        public static readonly Dictionary<string, float> NeedsFlatGround =
            new Dictionary<string, float>
            {
                // Ok meydaninda 845,66 m'lik menzil rekoru atildi.
                { "LM_Okmeydani", 12f },
            };

        public struct Row
        {
            public string id;
            public float elevation, spread150, peak200, belowPeak, toWater;
            public List<string> complaints;
        }

        /// <summary>
        /// Arazi <b>varlıktan</b> okunur: sahne gerekmez.
        ///
        /// İlk yazımda denetim sahnedeki <c>TR_Istanbul</c>'u arıyordu ve
        /// testi her koşumda ATLANDI — tam olarak ADR 0041'de eleştirdiğim
        /// hatanın aynısı: atlanan bir test geçen bir test gibi görünür.
        /// </summary>
        public static System.Func<float, float, float> HeightFromAsset()
        {
            var meta = TerrainImporter.ReadMeta(TerrainImporter.DefaultDataDir);
            var data = AssetDatabase.LoadAssetAtPath<TerrainData>(
                TerrainImporter.TerrainAssetPath);
            if (meta == null || data == null) return null;

            float ox = (float)meta.world_origin_offset_m.x;
            float oz = (float)meta.world_origin_offset_m.z;
            float baseElev = (float)meta.min_elevation_m;
            var size = data.size;
            return (x, z) =>
            {
                float u = Mathf.Clamp01((x - ox) / size.x);
                float v = Mathf.Clamp01((z - oz) / size.z);
                return data.GetInterpolatedHeight(u, v) + baseElev;
            };
        }

        public static List<Row> Measure(Terrain terrain)
        {
            if (terrain == null) return Measure((System.Func<float, float, float>)null);
            return Measure((x, z) => terrain.SampleHeight(new Vector3(x, 0f, z))
                                     + terrain.transform.position.y);
        }

        public static List<Row> Measure(System.Func<float, float, float> height)
        {
            var rows = new List<Row>();
            string root = TerrainImporter.RepositoryRoot();
            string path = root == null ? null : Path.Combine(root,
                LandmarkPlacer.LocalJsonPath.Replace('/', Path.DirectorySeparatorChar));
            if (path == null || !File.Exists(path)) return rows;

            if (height == null) height = HeightFromAsset();
            if (height == null) return rows;

            var doc = GeoJsonImporter.ParseLocal(File.ReadAllText(path));
            foreach (var f in doc.features)
            {
                if (f.rings == null || f.rings.Count == 0 || f.rings[0].Count == 0)
                    continue;
                var q = f.rings[0][0];
                var c = new Vector2(q.x, q.z);

                float h = height(c.x, c.y);
                float lo = float.MaxValue, hi = float.MinValue, peak = float.MinValue;
                for (int i = -3; i <= 3; i++)
                    for (int j = -3; j <= 3; j++)
                    {
                        float v = height(c.x + i * 25f, c.y + j * 25f);
                        lo = Mathf.Min(lo, v); hi = Mathf.Max(hi, v);
                    }
                for (int i = -4; i <= 4; i++)
                    for (int j = -4; j <= 4; j++)
                        peak = Mathf.Max(peak, height(c.x + i * 50f, c.y + j * 50f));

                float sea = 9999f;
                for (int i = 0; i < 36; i++)
                {
                    float a = Mathf.PI * 2f * i / 36f;
                    var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                    for (float r = 20f; r < 1200f; r += 20f)
                        if (height(c.x + d.x * r, c.y + d.y * r) < 0f)
                        { sea = Mathf.Min(sea, r); break; }
                }

                var row = new Row
                {
                    id = f.id, elevation = h, spread150 = hi - lo,
                    peak200 = peak, belowPeak = peak - h, toWater = sea,
                    complaints = new List<string>(),
                };

                if (CrownsAHill.TryGetValue(f.id, out float maxBelow)
                    && row.belowPeak > maxBelow)
                    row.complaints.Add(
                        $"tepeyi taclandirmali ama yerel zirvenin "
                        + $"{row.belowPeak:F1} m altinda (sinir {maxBelow:F0})");

                if (OnTheShore.TryGetValue(f.id, out float maxWater)
                    && row.toWater > maxWater)
                    row.complaints.Add(
                        $"kiyida olmali ama sudan {row.toWater:F0} m uzakta "
                        + $"(sinir {maxWater:F0})");

                if (NeedsFlatGround.TryGetValue(f.id, out float maxSpread)
                    && row.spread150 > maxSpread)
                    row.complaints.Add(
                        $"duz zemin istiyor ama 150 m'de {row.spread150:F1} m "
                        + $"yayilim (sinir {maxSpread:F0})");

                rows.Add(row);
            }
            return rows;
        }

        [MenuItem("Hezarfen/GIS/Landmark konumlarini denetle")]
        public static void AuditMenu()
        {
            var go = GameObject.Find("TR_Istanbul");
            var terrain = go != null ? go.GetComponent<Terrain>() : null;
            var rows = Measure(terrain);
            if (rows.Count == 0)
            { Debug.LogError("[Hezarfen] Arazi ya da landmark verisi yok."); return; }
            var sb = new StringBuilder();
            sb.AppendLine("id                        kot  yayilim  zirveye  denize");
            int bad = 0;
            foreach (var r in rows)
            {
                sb.AppendLine($"{r.id,-24} {r.elevation,6:F1} {r.spread150,8:F1} "
                              + $"{r.belowPeak,8:F1} {(r.toWater > 900 ? 999 : r.toWater),7:F0}"
                              + (r.complaints.Count > 0 ? "   <-- " : ""));
                foreach (var c in r.complaints)
                { sb.AppendLine($"      ! {c}"); bad++; }
            }
            sb.AppendLine($"\n{rows.Count} landmark, {bad} sikayet.");
            if (bad > 0) Debug.LogWarning("[Hezarfen] KONUM DENETIMI\n" + sb);
            else Debug.Log("[Hezarfen] KONUM DENETIMI — hepsi tutarli\n" + sb);
        }
    }
}
