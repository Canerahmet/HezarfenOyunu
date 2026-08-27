using UnityEngine;

namespace Hezarfen.Streaming
{
    public enum DistrictKind { Land = 0, Water = 1 }

    /// <summary>
    /// Tek bir bölge yayını (streaming) hücresi — plan Faz 1 madde 6.
    ///
    /// **Bu bir tarihsel mahalle sınırı DEĞİLDİR.** 1632 mahalleleri kadastral
    /// değildi; 1546/1600 Vakıf Tahrir Defterleri mahalle adlarını verir, sınır
    /// çizgisi vermez. Buradaki poligonlar oynanış kararıdır ve hepsi
    /// <see cref="HistoricalTier.Graybox"/> taşır (ayrıntı: ADR 0011).
    ///
    /// Varlıklar elle DEĞİL, `tools/gis/districts_build.py` çıktısından
    /// **Hezarfen → GIS → Semtleri ice aktar** ile üretilir. Elle düzenlenirse
    /// bir sonraki içe aktarımda üzerine yazılır.
    /// </summary>
    [CreateAssetMenu(menuName = "Hezarfen/District Def", fileName = "DD_District")]
    public class DistrictDef : ScriptableObject
    {
        [Header("Kimlik")]
        public string districtId = "";
        public string displayName = "";

        [Tooltip("1 = uçuş ekseni (dikey dilim için zorunlu), 2 = suriçi geri kalanı, 3 = sonra")]
        [Range(1, 3)] public int priority = 2;

        public DistrictKind kind = DistrictKind.Land;

        [TextArea(2, 5)] public string summary = "";

        [Header("Addressables")]
        [Tooltip("Addressables anahtarı — ör. Districts/D_Galata")]
        public string sceneAddress = "";

        [Header("Sınır (yerel metre; orijin Galata Kulesi tabanı)")]
        [Tooltip("Kapalı poligon. Son nokta ilkine BAĞLANMAZ; kapanış örtüktür.")]
        public Vector2[] boundary = new Vector2[0];

        public Vector2 center;
        public float radiusMeters;

        [Header("Yayın eşikleri (poligon KENARINA uzaklık)")]
        [Tooltip("Bu uzaklığın altına inince sahne yüklenir. İçerideyken uzaklık 0'dır.")]
        public float loadDistanceMeters = 700f;

        [Tooltip("Bu uzaklığın üstüne çıkınca boşaltılır. loadDistance'tan BÜYÜK olmalı — " +
                 "eşit olursa sınırda gidip gelen oyuncu sahneyi sürekli yükleyip boşaltır.")]
        public float unloadDistanceMeters = 910f;

        [Header("Ölçülen (districts_build.py)")]
        [Tooltip("Bölgenin içindeki tüm kara (ha). Bölgeler çakıştığı için TOPLANAMAZ.")]
        public float landAreaHectares;

        [Tooltip("Yalnızca bu bölgeye sayılan kara (ha). Faz 4 yerleştirme bütçesi budur.")]
        public float exclusiveLandAreaHectares;

        /// <summary>Nokta poligonun içinde mi? (y yok sayılır — bölgeler düşeyde sonsuzdur.)</summary>
        public bool Contains(Vector3 worldPosition)
        {
            return ContainsXZ(boundary, worldPosition.x, worldPosition.z);
        }

        /// <summary>
        /// Poligonun KENARINA uzaklık; içerideyse 0.
        ///
        /// Merkez uzaklığı kullanılmaz. Haliç ince ve bükük bir şerittir: merkezine
        /// göre yarıçapı ~2,9 km çıkar, yani bir ucundayken öbür ucu da yüklü tutardı.
        /// Kenar uzaklığı bölgenin biçiminden bağımsızdır.
        /// </summary>
        public float DistanceMeters(Vector3 worldPosition)
        {
            return DistanceXZ(boundary, worldPosition.x, worldPosition.z);
        }

        // ------------------------------------------------- saf geometri (test edilebilir)

        public static bool ContainsXZ(Vector2[] ring, float x, float z)
        {
            if (ring == null || ring.Length < 3) return false;
            bool inside = false;
            for (int i = 0, j = ring.Length - 1; i < ring.Length; j = i++)
            {
                float zi = ring[i].y, zj = ring[j].y;
                if ((zi > z) == (zj > z)) continue;
                float xc = ring[i].x + (z - zi) * (ring[j].x - ring[i].x) / (zj - zi);
                if (x < xc) inside = !inside;
            }
            return inside;
        }

        public static float DistanceXZ(Vector2[] ring, float x, float z)
        {
            if (ring == null || ring.Length == 0) return float.PositiveInfinity;
            if (ContainsXZ(ring, x, z)) return 0f;

            var p = new Vector2(x, z);
            float best = float.PositiveInfinity;
            for (int i = 0, j = ring.Length - 1; i < ring.Length; j = i++)
            {
                Vector2 a = ring[j], b = ring[i];
                Vector2 ab = b - a;
                float len2 = ab.sqrMagnitude;
                float t = len2 > 0f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / len2) : 0f;
                float d = Vector2.Distance(p, a + ab * t);
                if (d < best) best = d;
            }
            return best;
        }
    }
}
