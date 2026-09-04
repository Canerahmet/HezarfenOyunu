using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Hezarfen.Editor.Diagnostics
{
    /// <summary>
    /// <b>Güneşin iki şiddetini yan yana yazar.</b>
    ///
    /// Pişmiş APV verisi karede ölçülemeyecek kadar sönük (ADR 0087) ve
    /// akla gelen sebeplerden biri bir <b>birim uyuşmazlığı</b>ydı:
    /// HDRP'nin fiziksel şiddeti (lux) <c>HDAdditionalLightData</c>
    /// içinde yaşar, ışık haritalayıcı ise dahili <c>Light.intensity</c>
    /// alanını okur. İkisi aynı sayı olmak zorunda değil.
    ///
    /// Sahne dosyasından okumak yetmiyor: HDRP değerleri <c>OnEnable</c>
    /// sırasında yeniden hesaplıyor, yani YAML'daki sayı ile çalışma
    /// anındaki sayı ayrışabilir. Bu yüzden ölçüm sahneyi açıp okuyor.
    ///
    /// Fırın gerektirmez; bir dakikalık bir okuma.
    /// </summary>
    public static class GunesDenetimi
    {
        private const string Sahne = "Assets/_Project/Scenes/Faz1_Terrain.unity";

        [MenuItem("Hezarfen/Denetim/Gunes siddetini oku")]
        public static void Oku()
        {
            EditorSceneManager.OpenScene(Sahne, OpenSceneMode.Single);
            int n = 0;
            foreach (var l in Object.FindObjectsByType<Light>(
                         FindObjectsSortMode.None))
            {
                if (l.type != LightType.Directional) continue;
                n++;
                var hd = l.GetComponent<HDAdditionalLightData>();
                Debug.Log($"[Hezarfen] Gunes '{l.name}': "
                          + $"Light.intensity={l.intensity:0.####}, "
                          + $"HDRP.intensity="
                          + (hd != null ? $"{hd.intensity:0.##}" : "yok")
                          + $", birim="
                          + (hd != null ? hd.lightUnit.ToString() : "-")
                          + $", bakeType={l.lightmapBakeType}"
                          + $", renk={l.color}, aktif={l.enabled}"
                          + $", oran="
                          + (hd != null && l.intensity > 1e-6f
                             ? $"{hd.intensity / l.intensity:0.####}" : "-"));
            }
            Debug.Log($"[Hezarfen] {n} yonlu isik okundu.");
        }
    }
}
