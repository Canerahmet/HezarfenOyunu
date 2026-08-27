using System.Collections.Generic;
using UnityEngine;

namespace Hezarfen.Flight
{
    /// <summary>
    /// Sahnedeki rüzgârın tek kaynağı: global lodos + tüm <see cref="WindVolume"/> katkıları.
    ///
    /// Neden ayrı bir sınıf: efsanenin istediği 33:1 oranı fizik sabitleriyle kapatmıyoruz
    /// (docs/RESEARCH.md — anlatı aerodinamik olarak imkânsız). Farkı kapatan şey bu alan.
    /// Yani rüzgâr burada bir "efekt" değil, oyunun ANA MEKANİĞİ.
    /// </summary>
    [AddComponentMenu("Hezarfen/Wind Field")]
    public class WindField : MonoBehaviour
    {
        [Tooltip("Global lodos vektörü buradan okunur. Boşsa yalnızca hacimler etkir.")]
        public WindTuning tuning;

        [Tooltip("Boşsa sahne taranarak otomatik doldurulur.")]
        public List<WindVolume> volumes = new List<WindVolume>();

        [Tooltip("Açıksa Awake'te sahnedeki tüm WindVolume'lar toplanır.")]
        public bool autoCollectVolumes = true;

        [Tooltip("Araziden tureyen termik/cokelme kaynagi. Bos birakilirsa " +
                 "sahnede aranir.")]
        public TerrainThermal terrainThermal;

        bool autoFindThermal = true;

        private void Awake()
        {
            if (autoCollectVolumes) CollectVolumes();
        }

        /// <summary>Sahnedeki tüm hacimleri toplar (Editor aracı da çağırabilir).</summary>
        public void CollectVolumes()
        {
            volumes.Clear();
            volumes.AddRange(FindObjectsByType<WindVolume>(FindObjectsInactive.Include));
        }

        /// <summary>Verilen dünya noktasındaki toplam rüzgâr (m/s).</summary>
        public Vector3 Sample(Vector3 worldPos)
        {
            Vector3 wind = tuning != null ? tuning.globalWind : Vector3.zero;

            for (int i = 0; i < volumes.Count; i++)
            {
                var v = volumes[i];
                if (v == null || !v.isActiveAndEnabled) continue;
                wind += v.SampleWind(worldPos);
            }

            // ARAZIDEN TUREYEN kaldirac (ADR 0037: gercek termik simulasyonu).
            // Elle konan hacimlerin USTUNE eklenir, yerine gecmez: hacim hala
            // tasarimcinin ozel bir yere koyabilecegi arac, bu ise arazinin
            // kendi cevabi. Ikisi ayni havayi iki kez saymaz — hacimler artik
            // yalnizca istisna icin kullanilir.
            if (terrainThermal == null && autoFindThermal)
            {
                terrainThermal = FindFirstObjectByType<TerrainThermal>();
                autoFindThermal = terrainThermal != null;
            }
            if (terrainThermal != null && terrainThermal.isActiveAndEnabled)
                wind.y += terrainThermal.SampleVertical(worldPos, wind);

            return wind;
        }

        /// <summary>Yalnızca dikey bileşen — HUD'daki variometre için.</summary>
        public float SampleVerticalSpeed(Vector3 worldPos) => Sample(worldPos).y;
    }
}
