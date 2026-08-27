using System.IO;
using System.Text;
using Hezarfen.Editor.Gis;
using Hezarfen.Flight;
using UnityEditor;
using UnityEngine;

namespace Hezarfen.Editor.Flight
{
    /// <summary>
    /// Hezarfen uçuşunun <b>bütçesini</b> ölçer: gerçek arazi, gerçek kule,
    /// gerçek iniş noktası ve gerçek aerodinamik ayar.
    ///
    /// ## Neden bir araç, neden bir kere hesap değil
    ///
    /// "Bu uçuş mümkün mü" sorusu tek seferlik bir aritmetik değil: kule
    /// yüksekliği, Doğancılar'ın kotu ve kanadın ayarı ayrı ayrı değişebilir
    /// ve her biri sonucu değiştirir. Sayı elle hesaplanıp bir belgeye
    /// yazılırsa, o belge ilk değişiklikte sessizce yanlışa döner.
    ///
    /// ## Ölçülen şey
    ///
    /// Bağlayıcı kısıt <b>süzülme oranı değil, alçalma hızıdır</b>. Menzil =
    /// irtifa × süzülme oranı; arkadan rüzgâr ise menzili yalnızca uçuş
    /// süresini kısaltarak etkiler, alçalmayı değil. Bu yüzden araç iki şeyi
    /// ayrı raporlar: sakin hava menzili, ve mesafeyi kapatmak için gereken
    /// <b>ortalama yükselen hava</b>.
    ///
    /// İlk ölçüm kendi önerimi düzeltti: 3336 m'yi yalnız rüzgârla kapatmak
    /// <b>57 m/s (205 km/h)</b> arkadan rüzgâr isterdi — yani rüzgâr tek
    /// başına çözüm değil. Oysa gereken ortalama yükselen hava yalnızca
    /// <b>~0,9 m/s</b>, zayıf bir termiğin bile altında.
    /// </summary>
    public static class FlightBudget
    {
        public const string TuningPath =
            "Assets/_Project/Data/WindProfiles/WT_Faz0_Default.asset";

        /// <summary>
        /// 17. yüzyıl kanadı için savunulabilir üst sınır (süzülme oranı).
        ///
        /// Karşılaştırma: yamaç paraşütü ~10:1, modern delta kanat ~15:1,
        /// yarış planörü 50-60:1. Bu sınır bir zevk meselesi değil bir
        /// bekçi: finali "çalışır" kılmanın en kolay yolu oranı 65:1'e
        /// çekmektir ve bu, oyunun bütün iddiasını çöpe atar.
        /// </summary>
        public const float MaxDefensibleGlideRatio = 15f;

        public struct Budget
        {
            public float glideRatio, trimSpeed, sinkRate;
            public float launchZ, landingZ, distance, drop;
            public float stillAirRange, shortfall;
            public float requiredUpdraft;      // sakin havada, m/s
        }

        public static bool TryMeasure(out Budget b, out string report)
        {
            b = default;
            var sb = new StringBuilder();

            var t = AssetDatabase.LoadAssetAtPath<WindTuning>(TuningPath);
            if (t == null) { report = TuningPath + " yok."; return false; }

            var terrainGo = GameObject.Find("TR_Istanbul");
            var terrain = terrainGo != null ? terrainGo.GetComponent<Terrain>() : null;
            if (terrain == null)
            { report = "TR_Istanbul yok — Faz1_Terrain sahnesini ac."; return false; }

            if (!TryLandmarks(out Vector2 kule, out Vector2 dog, out string err))
            { report = err; return false; }

            // FIRLATMA KOTU: kulenin ARAZI kotu degil, PREFABIN TEPESI.
            // Ikisi 46 m farkli ve o farkin tamami ucusun irtifasidir.
            float launch = LaunchHeight(kule, terrain);
            float land = terrain.SampleHeight(new Vector3(dog.x, 0f, dog.y))
                         + terrain.transform.position.y;

            var best = Aerodynamics.BestGlideRatio(t);
            float v = Aerodynamics.TrimSpeed(best.alphaDeg, t);
            float sink = v / best.ratio;

            b.glideRatio = best.ratio; b.trimSpeed = v; b.sinkRate = sink;
            b.launchZ = launch; b.landingZ = land;
            b.distance = Vector2.Distance(kule, dog);
            b.drop = launch - land;
            b.stillAirRange = b.drop * best.ratio;
            b.shortfall = b.distance - b.stillAirRange;
            float timeAloft = b.distance / v;                 // sakin hava
            b.requiredUpdraft = sink - b.drop / timeAloft;

            sb.AppendLine($"kanat: {t.mass} kg / {t.wingArea} m2, en iyi suzulme "
                          + $"{best.ratio:F2} : 1 (alfa {best.alphaDeg:F1} derece)");
            sb.AppendLine($"trim {v:F1} m/s ({v * 3.6f:F0} km/h), "
                          + $"alcalma {sink:F2} m/s");
            sb.AppendLine($"kule tepesi {launch:F1} m -> Dogancilar {land:F1} m "
                          + $"= dusus {b.drop:F1} m");
            sb.AppendLine($"mesafe {b.distance:F0} m; sakin hava menzili "
                          + $"{b.stillAirRange:F0} m; EKSIK {b.shortfall:F0} m");
            sb.AppendLine();
            sb.AppendLine("arkadan ruzgar | yer hizi | sure | GEREKEN YUKSELEN HAVA");
            foreach (float w in new[] { 0f, 3f, 6f, 9f, 12f })
            {
                float vg = v + w, tt = b.distance / vg;
                sb.AppendLine($"  {w,4:F0} m/s      | {vg,5:F1}    | {tt,4:F0} s"
                              + $" | {sink - b.drop / tt,5:F2} m/s");
            }
            sb.AppendLine();
            sb.AppendLine("Karsilastirma: zayif termik 1-2 m/s, guclu 3-5 m/s.");
            report = sb.ToString();
            return true;
        }

        /// <summary>
        /// Fırlatma kotu: kule prefabının tepesi; prefab sahnede yoksa
        /// arazi + <b>46 m</b>.
        ///
        /// Yedek sayı uydurma değil — 1632 kulesinin gövde + külah
        /// yüksekliği (ADR 0033). Bütçe, sahne açık olmasa da ölçülebilmeli.
        /// </summary>
        private static float LaunchHeight(Vector2 c, Terrain terrain)
        {
            var host = GameObject.Find(LandmarkPlacer.RootName);
            var kule = host != null ? host.transform.Find("PF_GalataKulesi") : null;
            if (kule != null)
            {
                var rs = kule.GetComponentsInChildren<Renderer>();
                if (rs.Length > 0)
                {
                    var bb = rs[0].bounds;
                    foreach (var r in rs) bb.Encapsulate(r.bounds);
                    return bb.max.y;
                }
            }
            return terrain.SampleHeight(new Vector3(c.x, 0f, c.y))
                   + terrain.transform.position.y + 46f;
        }

        public static bool TryLandmarks(out Vector2 kule, out Vector2 dog,
                                        out string err)
        {
            kule = dog = Vector2.zero; err = null;
            string root = TerrainImporter.RepositoryRoot();
            string path = root == null ? null : Path.Combine(root,
                LandmarkPlacer.LocalJsonPath.Replace('/', Path.DirectorySeparatorChar));
            if (path == null || !File.Exists(path))
            { err = LandmarkPlacer.LocalJsonPath + " yok."; return false; }

            var doc = GeoJsonImporter.ParseLocal(File.ReadAllText(path));
            bool k = false, d = false;
            foreach (var f in doc.features)
            {
                if (f.rings == null || f.rings.Count == 0 || f.rings[0].Count == 0)
                    continue;
                var q = f.rings[0][0];
                if (f.id == "LM_GalataKulesi") { kule = new Vector2(q.x, q.z); k = true; }
                if (f.id == "LM_Dogancilar") { dog = new Vector2(q.x, q.z); d = true; }
            }
            if (!k || !d) { err = "Kule veya Dogancilar katalogda yok."; return false; }
            return true;
        }

        [MenuItem("Hezarfen/Ucus/Ucus butcesini olc")]
        public static void Measure()
        {
            if (!TryMeasure(out _, out string report))
            { Debug.LogError("[Hezarfen] " + report); return; }
            Debug.Log("[Hezarfen] UCUS BUTCESI\n" + report);
        }
    }
}
