using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Hezarfen.Editor.Gis
{
    /// <summary>
    /// <b>Binanın üstündeki ağaçları kaldırır.</b>
    ///
    /// Oyun turunda çekilen karede bir mahalle mescidinin <b>kubbesine dev
    /// bir çınar saplanmıştı</b>. Ağaçlar araziye <c>TreeInstance</c>
    /// olarak konuyor ve arazi binaları bilmiyor: bina semt sahnesinde,
    /// ağaç arazi verisinde. İki ayrı yerde duran iki gerçek, kimse
    /// karşılaştırmayınca çakışıyor.
    ///
    /// ## Neden yerleştirme anında engellenmedi
    ///
    /// <see cref="GreeneryBuilder"/> ağaçları semtlerden <b>önce</b> diker
    /// ve o sırada bina yoktur. <see cref="KirsalDoku"/> bina yakınlığına
    /// bakıyor ama yalnız kendi eklediklerine. Geriye taban yeşil dokunun
    /// binaların altında kalan kısmı kalıyor.
    ///
    /// Bu yüzden temizlik ayrı bir adım: semtler açılır, bina kutuları
    /// toplanır, o kutuların içine düşen ağaç <b>silinir</b>.
    /// </summary>
    public static class AgacTemizligi
    {
        private const string TerrainScene =
            "Assets/_Project/Scenes/Faz1_Terrain.unity";
        private const string DistrictDir =
            "Assets/_Project/Scenes/Districts";

        /// <summary>Bina kenarına bu kadar pay bırakılır (m).</summary>
        public const float Pay = 1.2f;

        [MenuItem("Hezarfen/GIS/Bina ustundeki agaclari temizle")]
        public static void Temizle()
        {
            var sahne = EditorSceneManager.OpenScene(
                TerrainScene, OpenSceneMode.Single);
            var arazi = Object.FindAnyObjectByType<Terrain>();
            if (arazi == null) { Debug.LogError("[Hezarfen] Arazi yok."); return; }

            // --- bina kutulari (XZ) ---
            var kutular = new List<Rect>();
            var acilan = new List<UnityEngine.SceneManagement.Scene>();
            foreach (string sy in Directory.GetFiles(DistrictDir, "*.unity"))
            {
                try
                {
                    acilan.Add(EditorSceneManager.OpenScene(
                        sy.Replace("\\", "/"), OpenSceneMode.Additive));
                }
                catch { }
            }
            foreach (var sc in acilan)
            {
                if (!sc.IsValid()) continue;
                foreach (var kok in sc.GetRootGameObjects())
                    foreach (var mr in kok.GetComponentsInChildren<MeshRenderer>(false))
                    {
                        var b = mr.bounds;
                        // Birlesik yuzeyler (kaldirim, kaide, duvar mesh'i)
                        // bina degil; onlarin ustunde agac olabilir.
                        if (b.size.x > 120f || b.size.z > 120f) continue;
                        if (b.size.y < 2.0f) continue;      // alcak sey bina degil
                        kutular.Add(new Rect(
                            b.min.x - Pay, b.min.z - Pay,
                            b.size.x + Pay * 2f, b.size.z + Pay * 2f));
                    }
            }
            // Ana sahnedeki landmark ve surlar da bina.
            foreach (var kok in sahne.GetRootGameObjects())
            {
                if (!kok.name.StartsWith("LANDMARK") && !kok.name.StartsWith("SUR_"))
                    continue;
                foreach (var mr in kok.GetComponentsInChildren<MeshRenderer>(false))
                {
                    var b = mr.bounds;
                    if (b.size.x > 200f || b.size.z > 200f) continue;
                    if (b.size.y < 2.0f) continue;
                    kutular.Add(new Rect(b.min.x - Pay, b.min.z - Pay,
                                         b.size.x + Pay * 2f, b.size.z + Pay * 2f));
                }
            }
            foreach (var sc in acilan)
                if (sc.IsValid()) EditorSceneManager.CloseScene(sc, true);

            // --- izgara: kutu sayisi buyuk, her agaci hepsiyle
            //     karsilastirmak milyonlarca deneme demek ---
            const float H = 64f;
            var izgara = new Dictionary<(int, int), List<Rect>>();
            foreach (var r in kutular)
            {
                int x0 = Mathf.FloorToInt(r.xMin / H), x1 = Mathf.FloorToInt(r.xMax / H);
                int z0 = Mathf.FloorToInt(r.yMin / H), z1 = Mathf.FloorToInt(r.yMax / H);
                for (int z = z0; z <= z1; z++)
                    for (int x = x0; x <= x1; x++)
                    {
                        if (!izgara.TryGetValue((x, z), out var l))
                        { l = new List<Rect>(); izgara[(x, z)] = l; }
                        l.Add(r);
                    }
            }

            // --- agaclari sure ---
            var data = arazi.terrainData;
            var kok2 = arazi.transform.position;
            var kalan = new List<TreeInstance>(data.treeInstances.Length);
            int silinen = 0;
            foreach (var ti in data.treeInstances)
            {
                float wx = kok2.x + ti.position.x * data.size.x;
                float wz = kok2.z + ti.position.z * data.size.z;
                bool binada = false;
                if (izgara.TryGetValue((Mathf.FloorToInt(wx / H),
                                        Mathf.FloorToInt(wz / H)), out var liste))
                    foreach (var r in liste)
                        if (r.Contains(new Vector2(wx, wz))) { binada = true; break; }
                if (binada) { silinen++; continue; }
                kalan.Add(ti);
            }

            data.SetTreeInstances(kalan.ToArray(), true);
            EditorSceneManager.MarkSceneDirty(sahne);
            EditorSceneManager.SaveScene(sahne, TerrainScene);
            AssetDatabase.SaveAssets();

            Debug.Log($"[Hezarfen] Bina ustu agac temizligi: {kutular.Count} "
                      + $"bina kutusu, {silinen} agac SILINDI, "
                      + $"{kalan.Count} kaldi.");
        }
    }
}
