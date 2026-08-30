using System.Collections.Generic;
using System.IO;
using System.Text;
using Hezarfen.Gis;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Hezarfen.Editor.Gis
{
    /// <summary>
    /// <b>Ağaçları arazi öznitelik katmanına göre eler.</b>
    ///
    /// <see cref="AgacTemizligi"/>'nin yaptığı işin aynısı değil, onun
    /// yapamadığı iş: temizlik kendi bina kutularını <b>sezgiyle</b>
    /// topluyordu (<c>size.y &lt; 2 m ise bina değildir</c>,
    /// <c>size.x &gt; 120 m ise birleşik yüzeydir</c>) ve sokakları hiç
    /// bilmiyordu. Ölçüldü: Galata'da <b>1.671 ağaç sokağın 4 m'sinde</b>
    /// duruyordu — %12,3 — ve bunu hiçbir araç sormamıştı.
    ///
    /// Artık soran bir yer var: <see cref="AraziOznitelik"/>. Burada
    /// sezgi yok, katmana bakılıyor.
    ///
    /// ## Neden hâlâ bir eleme adımı var
    ///
    /// Doğrusu ağacın oraya hiç dikilmemesidir ve
    /// <see cref="GreeneryBuilder"/> katmana bakacak. Ama o, ağaçları
    /// semtlerden <b>önce</b> diker; katman semt kurulduktan sonra
    /// hesaplanabilir. Sıra tersine çevrilene kadar eleme adımı kalır —
    /// ve o gün geldiğinde bu araç sıfır ağaç eler, yani kendini
    /// gereksiz kıldığını <b>ölçerek</b> gösterir.
    /// </summary>
    public static class AgacKatmanUygula
    {
        private const string TerrainScene =
            "Assets/_Project/Scenes/Faz1_Terrain.unity";
        private const string VeriDir = "Assets/_Project/Data";

        /// <summary>Sokak eksenine bu kadar yakın ağaç sokağı kapatır (m).</summary>
        public const float YolPayi = 4f;

        [MenuItem("Hezarfen/GIS/Agaclari oznitelik katmanina gore ele")]
        public static void Uygula()
        {
            // SAHNE ONCE ACILIR, KATMAN SONRA YUKLENIR.
            //
            // Tersi denendi ve arac sifir agac kapsadigini bildirdi,
            // oysa ayni hesap elle kosturulunca 13.545 cikiyordu.
            // `OpenScene(..., Single)` kullanilmayan varliklari bosaltir
            // ve o sirada yuklenmis bir ScriptableObject referansi
            // guvenilir degil. Sira degistirmek bedava; tanisi zor bir
            // sessiz sifiri kovalamak degil.
            EditorSceneManager.OpenScene(TerrainScene, OpenSceneMode.Single);
            var arazi = Object.FindAnyObjectByType<Terrain>();
            if (arazi == null) { Debug.LogError("[Hezarfen] Arazi yok."); return; }

            var katmanlar = new List<AraziOznitelik>();
            if (Directory.Exists(VeriDir))
                foreach (var y in Directory.GetFiles(VeriDir, "AO_*.asset"))
                {
                    string ay = y.Replace("\\", "/");
                    if (ay.EndsWith(".meta")) continue;
                    var ao = AssetDatabase.LoadAssetAtPath<AraziOznitelik>(ay);
                    if (ao != null && ao.en > 0 && ao.boy > 0) katmanlar.Add(ao);
                }
            if (katmanlar.Count == 0)
            {
                Debug.LogError("[Hezarfen] Hic gecerli AO_*.asset yok — once "
                    + "Hezarfen/GIS/Arazi ozniteliklerini hesapla.");
                return;
            }

            var data = arazi.terrainData;
            var pos = arazi.transform.position;
            var kalan = new List<TreeInstance>(data.treeInstances.Length);
            int binada = 0, yolda = 0, kapsam = 0;

            foreach (var ti in data.treeInstances)
            {
                float x = pos.x + ti.position.x * data.size.x;
                float z = pos.z + ti.position.z * data.size.z;

                AraziOznitelik ao = null;
                foreach (var k in katmanlar)
                    if (k.Icinde(x, z)) { ao = k; break; }

                // KATMANI OLMAYAN YER DOKUNULMADAN GECER.
                //
                // Referans semt kurali (CLAUDE.md): yeni katman once
                // D_Galata'da bitirilir ve ORADA olculur. Katmani
                // olmayan semtte eski davranis surer; yenisi oraya
                // kapidan gectikten sonra yayilir.
                if (ao == null) { kalan.Add(ti); continue; }

                kapsam++;
                if (ao.BinaUzakligi(x, z) <= 0.01f) { binada++; continue; }
                if (ao.YolUzakligi(x, z) < YolPayi) { yolda++; continue; }
                kalan.Add(ti);
            }

            int once = data.treeInstances.Length;
            data.SetTreeInstances(kalan.ToArray(), true);
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveOpenScenes();

            var sb = new StringBuilder("AGAC ELEME (oznitelik katmani)\n");
            foreach (var k in katmanlar)
            {
                // Sifir kapsam bir daha sessiz kalmasin: katmanin
                // gercekten nereyi kapsadigi yazilir.
                sb.AppendLine($"  katman {k.semt}: {k.en}x{k.boy} @ "
                    + $"x[{k.kok.x:0}..{k.kok.x + k.en * AraziOznitelik.Hucre:0}] "
                    + $"z[{k.kok.y:0}..{k.kok.y + k.boy * AraziOznitelik.Hucre:0}]");
            }
            sb.AppendLine($"  kapsamdaki agac: {kapsam}");
            sb.AppendLine($"  binanin icinde elenen: {binada}");
            sb.AppendLine($"  yolun {YolPayi} m'sinde elenen: {yolda}");
            sb.AppendLine($"  agac {once} -> {kalan.Count}");
            Debug.Log("[Hezarfen] " + sb);
        }
    }
}
