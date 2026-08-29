using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Hezarfen.Editor.Lighting
{
    /// <summary>
    /// <b>Kenar kaynamasını (titreme) azaltan pas.</b>
    ///
    /// Caner (2026-08-29): *"modellerin kenar ve koselerinde titremeler var
    /// onlari da duzetelim. isiksal mi yoksa baska bir pronblem mi var?"*
    ///
    /// İlk cevap ölçüldü ve doğruydu ama <b>eksikti</b>: kameranın
    /// <c>antialiasing</c> değeri None'dı, TAA açıldı. Caner titremenin
    /// sürdüğünü söyledi. Yani TAA gerekliydi ama yeterli değil.
    ///
    /// ## Neden TAA tek başına yetmiyor
    ///
    /// Kenar kaynamasının iki ayrı kaynağı var ve TAA yalnız birine bakar:
    ///
    /// 1. <b>Geometri kenarı</b> — çokgenin sınırı piksel ızgarasına
    ///    oturmaz. TAA bunu kareleri biriktirerek çözer.
    /// 2. <b>Özgül parlaklık takozlanması</b> (specular aliasing) — kiremit
    ///    sırtı, taş silme ve mazgal gibi <b>yüksek frekanslı normal</b>
    ///    yüzeylerde, tek bir piksele düşen normaller birbirini yer.
    ///    Kamera kıpırdadıkça o piksel parlayıp söner. TAA bunu bastırmaz,
    ///    çünkü sinyal her karede <b>gerçekten</b> değişiyor.
    ///
    /// İkincisinin çözümü malzeme tarafındadır: HDRP'nin <b>Geometric
    /// Specular AA</b>'sı, normal varyansını pürüzlülüğe katarak parlamayı
    /// yumuşatır. Bu oyunun dokusu kiremit, kesme taş ve ahşap silmeden
    /// ibaret — yani tam olarak ikinci kaynağın yaşadığı yer.
    ///
    /// ## Arazi ayrı bir kaynak
    ///
    /// <c>heightmapPixelError</c> 5'ti: arazi kafesi uzakta kabalaşır ve
    /// kamera yürürken üçgenler <b>atlar</b>. Silüet titremesi olarak
    /// görünür ve hiçbir AA onu düzeltmez, çünkü geometrinin kendisi
    /// değişiyor.
    /// </summary>
    public static class KenarKaynamasi
    {
        /// <summary>
        /// Ekran uzayı varyansı — 0 kapalı, 1 en güçlü. 0,5 dokuyu
        /// bulanıklaştırmadan parlamayı kesen aralıkta.
        /// </summary>
        public const float Varyans = 0.5f;

        /// <summary>Eşik: bu değerin altındaki varyans yok sayılır.</summary>
        public const float Esik = 0.2f;

        /// <summary>Arazi kafes hatası (piksel). 5 → 1.</summary>
        public const float AraziPikselHatasi = 1f;

        [MenuItem("Hezarfen/Aydinlatma/Kenar kaynamasini azalt")]
        public static void Uygula()
        {
            int degisen = 0, toplam = 0, atlanan = 0;

            foreach (string guid in AssetDatabase.FindAssets(
                         "t:Material", new[] { "Assets/_Project/Art/Materials" }))
            {
                string yol = AssetDatabase.GUIDToAssetPath(guid);
                var m = AssetDatabase.LoadAssetAtPath<Material>(yol);
                if (m == null) continue;
                toplam++;

                // Yalniz HDRP/Lit ailesinde bu ozellik var.
                if (!m.HasProperty("_EnableGeometricSpecularAA"))
                { atlanan++; continue; }

                bool oncekiAcik =
                    m.GetFloat("_EnableGeometricSpecularAA") > 0.5f;
                m.SetFloat("_EnableGeometricSpecularAA", 1f);
                m.SetFloat("_SpecularAAScreenSpaceVariance", Varyans);
                m.SetFloat("_SpecularAAThreshold", Esik);
                EditorUtility.SetDirty(m);
                if (!oncekiAcik) degisen++;
            }

            AssetDatabase.SaveAssets();

            // Arazi kafesi
            var sahne = EditorSceneManager.OpenScene(
                "Assets/_Project/Scenes/Faz1_Terrain.unity",
                OpenSceneMode.Single);
            string araziNot = "arazi yok";
            var arazi = Object.FindAnyObjectByType<Terrain>();
            if (arazi != null)
            {
                float eski = arazi.heightmapPixelError;
                arazi.heightmapPixelError = AraziPikselHatasi;
                EditorSceneManager.MarkSceneDirty(sahne);
                EditorSceneManager.SaveScene(sahne);
                araziNot = $"heightmapPixelError {eski:0.#} -> "
                           + $"{AraziPikselHatasi:0.#}";
            }

            Debug.Log($"[Hezarfen] Kenar kaynamasi pasi: {toplam} malzeme "
                      + $"tarandi, {degisen} tanesinde Geometric Specular AA "
                      + $"ACILDI, {atlanan} tanesinde ozellik yok. {araziNot}.");
        }

        /// <summary>
        /// Kaç malzemede açık — test okur. Sayının kendisi bir ölçümdür:
        /// "pası çalıştırdım" demek yetmez, kaçının değiştiği yazmalı.
        /// </summary>
        public static int AcikMalzemeSayisi()
        {
            int n = 0;
            foreach (string guid in AssetDatabase.FindAssets(
                         "t:Material", new[] { "Assets/_Project/Art/Materials" }))
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>(
                    AssetDatabase.GUIDToAssetPath(guid));
                if (m == null) continue;
                if (!m.HasProperty("_EnableGeometricSpecularAA")) continue;
                if (m.GetFloat("_EnableGeometricSpecularAA") > 0.5f) n++;
            }
            return n;
        }
    }
}
