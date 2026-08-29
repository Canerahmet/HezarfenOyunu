using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Hezarfen.Tani;

namespace Hezarfen.Editor.Gis
{
    /// <summary>
    /// Sahneye <see cref="AgacCizici"/> kurar — ağaçları örneklemeyle
    /// çizen bileşen (ADR 0073).
    ///
    /// Kurulum tek satırlık çünkü <b>üretilecek bir şey yok</b>. Önceki
    /// iki deneme geometri üretiyordu ve ikisi de aynı duvara çarptı:
    /// birleştirilmiş mesh'ler ya sahneye gömülüyordu (23,7 MB → 805 MB)
    /// ya da varlık klasörüne (~900 MB). İkisi de arazi verisinde zaten
    /// duran bir bilgiyi ikinci kez saklamaktı.
    /// </summary>
    public static class AgacCiziciKur
    {
        public const string KokAd = "AGAC_CIZICI";

        [MenuItem("Hezarfen/Olcum/Agac cizicisini kur")]
        public static void KurMenu()
        {
            var eski = GameObject.Find(KokAd);
            if (eski != null) Object.DestroyImmediate(eski);

            var arazi = Object.FindAnyObjectByType<Terrain>();
            if (arazi == null)
            {
                Debug.LogError("[Hezarfen] Arazi yok.");
                return;
            }

            var go = new GameObject(KokAd);
            var c = go.AddComponent<AgacCizici>();
            c.arazi = arazi;
            c.Kur();

            // Arazinin kendi agac cizimi kapanir; cizici devralir.
            arazi.treeDistance = 0f;
            EditorUtility.SetDirty(arazi);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            Debug.Log($"[Hezarfen] Agac cizicisi kuruldu: "
                      + $"{arazi.terrainData.treeInstanceCount} agac, "
                      + "diskte SIFIR bayt uretildi.");
        }

        [MenuItem("Hezarfen/Olcum/Agac cizicisini kaldir")]
        public static void KaldirMenu()
        {
            var eski = GameObject.Find(KokAd);
            if (eski != null) Object.DestroyImmediate(eski);
            var arazi = Object.FindAnyObjectByType<Terrain>();
            if (arazi != null) { arazi.treeDistance = 3000f; EditorUtility.SetDirty(arazi); }
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[Hezarfen] Agac cizicisi kaldirildi, arazi agaclari geri acildi.");
        }
    }
}
