using System.Collections.Generic;
using Hezarfen.Editor.Diagnostics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Hezarfen.Editor.Gis
{
    /// <summary>
    /// Okmeydanı inceleme paketi — kareler + <b>sayılar</b>.
    ///
    /// Bu dosya bir düzeltmedir. ADR 0027 turunda kareler tek seferlik
    /// komutlarla alınmıştı; yani "sadece sohbette var olan varlık" idiler
    /// (CLAUDE.md yasaklar). Kadraj bir daha üretilemiyordu, dolayısıyla
    /// "önce/sonra" kıyaslaması da yapılamıyordu. Artık kadrajlar burada
    /// yazılı: aynı komut aynı kareyi verir.
    ///
    /// Kadrajlar rastgele değil; her biri bir <b>iddiayı</b> gösterir:
    /// tekkenin minaresizliği, taşın kitabesinin ayak taşına dönmesi, ve
    /// koridorun uzunluğu — bir ok atımının yerdeki karşılığı.
    /// </summary>
    public static class OkmeydaniReview
    {
        private const string OutDir = "Captures";

        [MenuItem("Hezarfen/GIS/Okmeydani inceleme paketi")]
        public static void Capture()
        {
            EditorSceneManager.OpenScene(OkmeydaniBuilder.ScenePath,
                                         OpenSceneMode.Single);

            var stones = new Dictionary<string, Transform>();
            foreach (var t in Object.FindObjectsByType<Transform>())
                if (t.name.StartsWith("AyakTasi_") || t.name.StartsWith("BasTasi_")
                    || t.name.StartsWith("PF_Tekke") || t.name.StartsWith("PF_Namazgah"))
                    stones[t.name] = t;

            var lines = new List<string>();

            // 1) Tekke ve namazgah — meydanin cepesinden, minaresizlik gorunur.
            var tekke = stones.TryGetValue("PF_Tekke_Okcular", out var tk) ? tk : null;
            if (tekke != null)
            {
                Vector3 c = tekke.position;
                Shot("okmeydani_tekke", c + new Vector3(30f, 16f, -34f), c, 42f, lines);
            }

            // 2) Ayak tasi, goz hizasindan: okcunun durdugu yer. Bakis KORIDOR
            //    boyunca — tas bir yon gosterir, bir sus degil.
            if (stones.TryGetValue("AyakTasi_Havandelen", out var foot))
            {
                Vector3 fwd = Quaternion.Euler(0f, OkmeydaniBuilder.ShotAzimuth("yildiz"),
                                               0f) * Vector3.forward;
                Shot("okmeydani_ayaktasi", foot.position - fwd * 7f + Vector3.up * 1.7f,
                     foot.position + Vector3.up * 1.2f, 50f, lines);

                // 3) Menzilin kendisi: ayak tasindan bas tasina bakis. 845 m
                //    ilerideki tas gorunmez — gorunen sey MESAFEDIR.
                Shot("okmeydani_menzil", foot.position - fwd * 12f + Vector3.up * 2.4f,
                     foot.position + fwd * 600f + Vector3.up * 2f, 55f, lines);
            }

            // 4) Rekor tasi yakindan: kitabe ayak tasina donuk olmali.
            if (stones.TryGetValue("BasTasi_Arkuri_1282gez", out var rec))
            {
                // Iki kare, cunku tasin iki yuzu AYNI SEY DEGIL: yazi yalniz
                // ayak tasina bakan yuzdedir. Arka kare o iddianin kaniti —
                // ve bir kez kitabenin hic gorunmedigini de bu ikili gosterdi.
                Vector3 f = rec.forward;   // kitabe bu yuzde
                Shot("okmeydani_rekortasi", rec.position + f * 4.5f + Vector3.up * 1.6f,
                     rec.position + Vector3.up * 1.9f, 40f, lines);
                Shot("okmeydani_tas_arka", rec.position - f * 4.5f + Vector3.up * 1.6f,
                     rec.position + Vector3.up * 1.9f, 40f, lines);
            }

            // 5) Meydan, havadan: bos alan + cepere dizilmis yapilar.
            var areas = GreeneryBuilder.ReadAreas();
            if (areas != null)
                foreach (var a in areas)
                    if (a.id == "G_Okmeydani_Yasak")
                    {
                        var c = FrameMetric.OnGround(new Vector3(a.center_x, 0f,
                                                                 a.center_z));
                        Shot("okmeydani_meydan", c + new Vector3(0f, 520f, -900f), c,
                             55f, lines);
                    }

            Debug.Log("[Hezarfen] Okmeydani inceleme paketi:\n"
                      + string.Join("\n", lines));
        }

        private static void Shot(string name, Vector3 eye, Vector3 look, float fov,
                                 List<string> lines)
        {
            var s = FrameMetric.Capture(eye, look, fov, $"{OutDir}/{name}.png",
                                        960, 540);
            lines.Add($"  {name,-24} {s}");
        }
    }
}
