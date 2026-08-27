using System.Collections.Generic;
using UnityEngine;

namespace Hezarfen.Streaming
{
    /// <summary>
    /// Tüm bölgelerin tek listesi. `districts_build.py` çıktısından üretilir.
    /// </summary>
    [CreateAssetMenu(menuName = "Hezarfen/District Registry", fileName = "DistrictRegistry")]
    public class DistrictRegistry : ScriptableObject
    {
        public DistrictDef[] districts = new DistrictDef[0];

        public DistrictDef Find(string districtId)
        {
            if (districts == null) return null;
            for (int i = 0; i < districts.Length; i++)
                if (districts[i] != null && districts[i].districtId == districtId)
                    return districts[i];
            return null;
        }
    }

    /// <summary>
    /// Hangi bölgenin yüklenip hangisinin boşaltılacağına karar veren **saf** mantık.
    ///
    /// Addressables'tan ve MonoBehaviour'dan bilerek ayrıldı: yayın kararının doğru
    /// olduğunu kanıtlamak için bir Addressables build'i, bir oyun oturumu ya da
    /// dosya sistemi gerekmemeli. Buradaki kural EditMode testinde saniyeler içinde
    /// binlerce konum için koşturulabilir — özellikle "histerezis gerçekten
    /// titremeyi (thrash) engelliyor mu" sorusu ancak böyle yanıtlanır.
    /// </summary>
    public static class DistrictStreamingPlan
    {
        /// <summary>Aynı anda uçuşta olabilecek yükleme sayısı. Kare düşüşünü sınırlar.</summary>
        public const int MaxConcurrentLoads = 1;

        /// <summary>
        /// Verilen konum ve halihazırda yüklü küme için yapılacakları hesaplar.
        /// `toLoad` öncelik (küçük önce), sonra uzaklık sırasındadır.
        /// </summary>
        public static void Evaluate(
            IList<DistrictDef> districts,
            ICollection<string> loadedIds,
            Vector3 viewerPosition,
            List<DistrictDef> toLoad,
            List<DistrictDef> toUnload)
        {
            toLoad.Clear();
            toUnload.Clear();
            if (districts == null) return;

            for (int i = 0; i < districts.Count; i++)
            {
                var d = districts[i];
                if (d == null || string.IsNullOrEmpty(d.districtId)) continue;

                float dist = d.DistanceMeters(viewerPosition);
                bool loaded = loadedIds != null && loadedIds.Contains(d.districtId);

                if (!loaded && dist <= d.loadDistanceMeters) toLoad.Add(d);
                else if (loaded && dist > d.unloadDistanceMeters) toUnload.Add(d);
            }

            toLoad.Sort((a, b) =>
            {
                int p = a.priority.CompareTo(b.priority);
                if (p != 0) return p;
                return a.DistanceMeters(viewerPosition)
                        .CompareTo(b.DistanceMeters(viewerPosition));
            });
        }
    }
}
