using System.Text;
using Hezarfen.Gis;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Hezarfen.Editor.Diagnostics
{
    /// <summary>
    /// <b>Ağaçlar zeminin ne olduğunu biliyor mu.</b>
    ///
    /// <see cref="Hezarfen.Gis.AraziOznitelik"/> kurulduktan sonra bu
    /// soru artık sorulabilir hâle geldi. Öncesinde sorulamıyordu ve
    /// sorulamadığı için cevabı da yoktu: 40.765 ağacın binaların
    /// içinden bittiği ancak <b>oyunda bir kubbeye saplanmış çınar
    /// görülünce</b> anlaşıldı.
    ///
    /// Ölçülen üç şey:
    ///
    /// | ölçü | kapı | neden |
    /// |---|---|---|
    /// | binanın içindeki ağaç | <b>0</b> | duvarın içinden ağaç bitmez |
    /// | sokağın üstündeki ağaç | <b>0</b> | sokak geçilebilir kalmalı |
    /// | su kenarı yoğunluğu | iç bölgeden **fazla** | söğüt ve sazlık suyun yanındadır |
    ///
    /// Üçüncüsü bir eşik değil bir <b>yön</b>: kaç kat olacağı iklime
    /// göre değişir, ama kıyının iç bölgeden seyrek olması yanlıştır.
    /// </summary>
    public static class AgacOznitelikDenetimi
    {
        private const string TerrainScene =
            "Assets/_Project/Scenes/Faz1_Terrain.unity";

        /// <summary>Yola bu kadar yakın ağaç sokağı kapatır (m).</summary>
        public const float YolPayi = 4f;

        /// <summary>"Su kenarı" sayılan kuşak (m).</summary>
        public const float KiyiKusagi = 24f;

        [MenuItem("Hezarfen/Olcum/Agaclari oznitelik katmanina gore denetle")]
        public static void Denetle() => Denetle("D_Galata");

        public static void Denetle(string semt)
        {
            var ao = AssetDatabase.LoadAssetAtPath<AraziOznitelik>(
                $"Assets/_Project/Data/AO_{semt}.asset");
            if (ao == null)
            {
                Debug.LogError($"[Hezarfen] AO_{semt}.asset yok — once "
                    + "Hezarfen/GIS/Arazi ozniteliklerini hesapla.");
                return;
            }

            EditorSceneManager.OpenScene(TerrainScene, OpenSceneMode.Single);
            var arazi = Object.FindAnyObjectByType<Terrain>();
            if (arazi == null) { Debug.LogError("[Hezarfen] Arazi yok."); return; }

            var data = arazi.terrainData;
            var pos = arazi.transform.position;
            var agaclar = data.treeInstances;

            int kapsam = 0, binada = 0, yolda = 0;
            int kiyi = 0, ic = 0;
            long kiyiHucre = 0, icHucre = 0;

            foreach (var t in agaclar)
            {
                float x = pos.x + t.position.x * data.size.x;
                float z = pos.z + t.position.z * data.size.z;
                if (!ao.Icinde(x, z)) continue;
                kapsam++;

                if (ao.BinaUzakligi(x, z) <= 0.01f) binada++;
                if (ao.YolUzakligi(x, z) < YolPayi) yolda++;

                float su = ao.SuUzakligi(x, z);
                if (su > 0.01f && su <= KiyiKusagi) kiyi++;
                else if (su > KiyiKusagi) ic++;
            }

            // Yogunluk karsilastirmasi icin ALAN da gerekir: kiyi kusagi
            // ic bolgeden cok daha kucuk. Agac sayisini alanla bolmeden
            // "kiyida az agac var" demek, kiyinin kucuk oldugunu
            // kesfetmekten baska bir sey degildir.
            for (int j = 0; j < ao.boy; j++)
                for (int i = 0; i < ao.en; i++)
                {
                    float su = ao.suUzakligi[j * ao.en + i] * AraziOznitelik.Adim;
                    if (su > 0.01f && su <= KiyiKusagi) kiyiHucre++;
                    else if (su > KiyiKusagi) icHucre++;
                }

            float hucreAlan = AraziOznitelik.Hucre * AraziOznitelik.Hucre;
            float kiyiYogun = kiyiHucre == 0 ? 0f
                : kiyi / (kiyiHucre * hucreAlan) * 10000f;   // agac/hektar
            float icYogun = icHucre == 0 ? 0f
                : ic / (icHucre * hucreAlan) * 10000f;

            var sb = new StringBuilder($"AGAC DENETIMI {semt}\n");
            sb.AppendLine($"  katman icinde {kapsam} agac "
                          + $"({data.treeInstanceCount} toplam)");
            sb.AppendLine($"  BINANIN ICINDE: {binada} "
                          + $"(%{Yuzde(binada, kapsam)})");
            sb.AppendLine($"  YOLUN {YolPayi} m'sinde: {yolda} "
                          + $"(%{Yuzde(yolda, kapsam)})");
            sb.AppendLine($"  kiyi kusagi (<={KiyiKusagi} m): {kiyi} agac, "
                          + $"{kiyiYogun:0.0} agac/ha");
            sb.AppendLine($"  ic bolge: {ic} agac, {icYogun:0.0} agac/ha");
            sb.AppendLine($"  kiyi/ic yogunluk orani: "
                          + $"{(icYogun < 0.01f ? 0f : kiyiYogun / icYogun):0.00}"
                          + (kiyiYogun >= icYogun ? "  (kiyi daha yogun ✓)"
                                                  : "  (KIYI DAHA SEYREK ✗)"));
            Debug.Log("[Hezarfen] " + sb);
        }

        private static string Yuzde(int a, int b)
            => b == 0 ? "0,0" : (100f * a / b).ToString("0.0");
    }
}
