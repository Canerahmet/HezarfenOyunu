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

        // ------------------------------------------------ Faz 4: semt karakteri
        //
        // BU ALANLAR GIS'TEN GELMEZ ve `DistrictImporter` onlara DOKUNMAZ.
        // Importer yalnızca kendi yazdığı alanları set eder; varlığı yeniden
        // yüklediği için buradakiler yeniden içe aktarımda korunur. Yeni bir
        // alan eklerken kural şudur: **ölçülen GIS'ten gelir, seçilen buraya
        // yazılır.** Sınır poligonu ölçülür; mahallenin hangi cemaate ait
        // olduğu seçilir.

        [Header("Faz 4 — semt karakteri (elle yazılır, GIS'ten GELMEZ)")]
        [Tooltip("Ev paleti: 'default' (müslüman mahalle) ya da 'nonmuslim'.")]
        public string housePalette = "default";

        [Tooltip("Mahalle çekirdeği: 'mescit' ya da 'sinagog'. Sıbyan mektebi " +
                 "ve türbe bundan TÜREr — ikisi de müslüman vakfın parçasıdır.")]
        public string coreKind = "mescit";

        [Tooltip("İkinci cemaatin ibadet yapısı. Galata'da Latin, Balat'ta Rum.")]
        public string[] churchPrefabs = new string[0];

        [Tooltip("Han ticarî semte aittir; her mahallede bulunmaz.")]
        public bool hasHan = false;

        [Header("Faz 4 — yerleşim yoğunluğu")]
        [Tooltip("İki mahalle çekirdeği arası hedef uzaklık (m). Mahalle birkaç " +
                 "yüz metredir; küçültmek semti kalabalıklaştırır.")]
        [Range(120f, 700f)] public float quarterSpacingMeters = 320f;

        [Tooltip("Kurulacak mahalle sayısına çarpan. 0 = semt boş kalır " +
                 "(henüz sırası gelmemiş semtler için).")]
        [Range(0f, 1.5f)] public float settlementDensity = 1f;

        [Header("Faz 4 — nadir kurum bütçesi (SEMT başına, mahalle başına DEĞİL)")]
        //
        // Neden burada: tek örnek sokak sahnesinde hamam, medrese ve kilise
        // koşulsuz konuyordu — o mahalle semtin tamamını temsil ettiği için
        // doğruydu. Semt 34 mahalleye bölününce aynı kod 22 hamam, 22 medrese
        // ve 22 Latin kilisesi üretti (ölçüldü). Sayı artık semtin kendisine
        // aittir ve mahallelere deterministik olarak dağıtılır.
        //
        // BU SAYILAR TASLAKTIR (T2). Kaynaklarda 1632 Galata'sının hamam ya da
        // medrese sayısı yok; buradakiler yapı tipinin şehirdeki yaygınlığına
        // göre seçilmiş, ölçülmemiş değerlerdir. Kaynak bulunursa değişir.

        [Tooltip("Semtteki ikinci cemaat ibadet yapısı sayısı (taslak).")]
        public int churchCount = 6;

        [Tooltip("Semtteki hamam sayısı (taslak).")]
        public int hamamCount = 5;

        [Tooltip("Semtteki medrese sayısı (taslak). Vakıf yapısıdır; " +
                 "coreKind 'mescit' değilse yok sayılır.")]
        public int medreseCount = 2;

        [Tooltip("Semtteki han sayısı (taslak). hasHan kapalıysa yok sayılır.")]
        public int hanCount = 1;

        [Tooltip("Semtteki fırın sayısı (taslak). Fırın mahalle ölçeğinde " +
                 "yaygındır; hamamdan çok olur.")]
        public int firinCount = 10;

        [Tooltip("Semtteki kahvehane sayısı (taslak). 1632'de kahvehaneler " +
                 "AÇIKTIR — IV. Murad'ın yasağı 1633'tür.")]
        public int kahvehaneCount = 8;

        [Tooltip("Semtteki bozahane sayısı (taslak).")]
        public int bozahaneCount = 3;

        [Header("Faz 4 — nereye kurulmaz")]
        [Tooltip("Bu eğimin üstünde mahalle kurulmaz (derece). İstanbul'un " +
                 "yamaçlarında sokak yokuşu yanlamasına tarar; dikleştiği " +
                 "yerde yerleşim seyrelir.")]
        [Range(5f, 45f)] public float maxSlopeDegrees = 22f;

        [Tooltip("Bu kotun altına mahalle kurulmaz (m). y=0 deniz seviyesidir; " +
                 "pay kıyı şeridi ve gelgit için.")]
        public float minElevationMeters = 3f;

        [Tooltip("Bir landmark'a bu mesafeden yakın çekirdek kurulmaz (m) — " +
                 "külliyenin kendi alanı vardır.")]
        public float landmarkClearanceMeters = 70f;

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
