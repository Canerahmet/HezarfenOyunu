using System.IO;
using System.Text;
using Hezarfen.City;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Hezarfen.Editor.Gis
{
    /// <summary>
    /// Yerleşmiş her eve <see cref="EvTonu"/> takar. Gerekçe orada.
    ///
    /// Ayrı bir geçiş, çünkü semtleri yeniden kurmak gerekmiyor:
    /// bileşen davranıştır, geometri değil. Yeniden kurmak 10.900 evi
    /// yeniden dizmek ve ikili varlıkları boş yere değiştirmek olurdu —
    /// CLAUDE.md'nin "yeniden üretim gürültüsü LFS'e kalıcı yazılır"
    /// kuralı tam bunu yasaklıyor.
    /// </summary>
    public static class EvTonuKur
    {
        private const string DistrictDir =
            "Assets/_Project/Scenes/Districts";

        [MenuItem("Hezarfen/GIS/Ev tonlarini kur (D_Galata)")]
        public static void Galata() => Kur("D_Galata");

        [MenuItem("Hezarfen/GIS/Ev tonlarini kur (tum semtler)")]
        public static void Hepsi()
        {
            foreach (var y in Directory.GetFiles(DistrictDir, "D_*.unity"))
                Kur(Path.GetFileNameWithoutExtension(y));
        }

        public static void Kur(string semt)
        {
            string yol = $"{DistrictDir}/{semt}.unity";
            if (!File.Exists(yol))
            {
                Debug.LogError($"[Hezarfen] {yol} yok.");
                return;
            }

            var sahne = EditorSceneManager.OpenScene(yol, OpenSceneMode.Single);
            int ev = 0, eklenen = 0;
            foreach (var go in sahne.GetRootGameObjects())
                foreach (var t in go.GetComponentsInChildren<Transform>())
                {
                    if (!t.name.StartsWith("PF_House")) continue;
                    ev++;
                    if (t.GetComponent<EvTonu>() != null) continue;
                    t.gameObject.AddComponent<EvTonu>();
                    eklenen++;
                }

            EditorSceneManager.MarkSceneDirty(sahne);
            EditorSceneManager.SaveScene(sahne);

            var sb = new StringBuilder($"EV TONU {semt}\n");
            sb.AppendLine($"  {ev} ev, {eklenen} bilesen eklendi "
                          + $"({ev - eklenen} zaten vardi)");
            Debug.Log("[Hezarfen] " + sb);
        }
    }
}
