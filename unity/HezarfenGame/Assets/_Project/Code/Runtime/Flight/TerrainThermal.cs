using UnityEngine;

namespace Hezarfen.Flight
{
    /// <summary>
    /// <b>Araziden türeyen termik ve çökelme</b> — ADR 0037'nin kararı.
    ///
    /// ## Neden elle yerleştirilen hacim değil
    ///
    /// <see cref="WindVolume"/> termik sütununu, yamaç kaldıracını ve çökelme
    /// bölgesini zaten üretebiliyor — ama <b>elle konarak</b>. Caner gerçek
    /// simülasyonu seçti (ADR 0037), yani kaldıraç bir tasarım nesnesi değil,
    /// <b>arazinin sonucu</b> olmalı.
    ///
    /// Ben mekanik (tasarlanmış) kaldıraç önermiştim; ölçüm gereken ortalamayı
    /// 0,88 m/s veriyordu ve bu zayıf termiğin bile altında görünüyordu. Uyarım
    /// <b>düz bir süzülüşü</b> varsayıyordu ve yanlıştı. Fizik şunu söylüyor:
    ///
    /// > <b>Termik kara üstünde doğar, su üstünde doğmaz.</b>
    ///
    /// Uçuş Boğaz'ı geçiyor. Yani süzülüş sırasında zaten kaldıraç
    /// olmayacak — doğru model <b>önce karada yüksel, sonra geçişe bağlan</b>.
    /// Bu, uçuşu şanstan beceriye çevirir.
    ///
    /// ## Model — dört terim, dördü de araziye sorulur
    ///
    /// <list type="number">
    /// <item><b>Termik.</b> Güneş yamacı ısıtır, ısınan hava yükselir.
    ///       Güçlü olduğu yer: güneşe <b>bakan</b> yamaç (güneybatı, öğleden
    ///       sonra), eğimli ve kuru zemin. Kuzeye bakan gölgeli yamaçta
    ///       yoktur.</item>
    /// <item><b>Su çökelmesi.</b> Deniz gündüz karadan serindir; üstündeki
    ///       hava alçalır. Boğaz ve Haliç bu yüzden <b>negatif</b>.</item>
    /// <item><b>Yamaç kaldıracı.</b> Rüzgâr yamaca çarpar ve yukarı saptırılır.
    ///       Termikten farkı: güneşe değil <b>rüzgâra</b> bağlıdır, ve
    ///       yamacın rüzgâra bakan yüzünde olur.</item>
    /// <item><b>Tavan.</b> Termik sonsuza yükselmez; bulut tabanında biter.
    ///       Tavansız bir model oyuncuyu stratosfere çıkarırdı.</item>
    /// </list>
    ///
    /// ## Ne uydurulmadı
    ///
    /// Sayılar 1632'nin havasından değil, <b>termik uçuşun fiziğinden</b>
    /// geliyor ve hepsi ayarlanabilir alanlardır. Kaynak İstanbul'un 1632
    /// termiklerini kaydetmiyor — kaydedemezdi. Kodeks notu: Hezarfen'in
    /// gerçekte nasıl uçtuğu bilinmiyor; oyun bunu dönemin havasıyla
    /// <b>mümkün</b> kılıyor, olduğunu iddia etmiyor.
    /// </summary>
    [RequireComponent(typeof(Terrain))]
    public class TerrainThermal : MonoBehaviour
    {
        [Header("Termik")]
        [Tooltip("Güneşe tam bakan, ideal eğimli yamacın ürettiği tırmanış (m/s).")]
        public float peakLift = 2.6f;

        [Tooltip("Termiğin en güçlü olduğu eğim (derece). Düz zemin ısınır ama " +
                 "havayı toplamaz; çok dik yamaç güneşi yalayarak alır.")]
        [Range(4f, 40f)] public float bestSlopeDeg = 17f;

        [Tooltip("Bu eğim farkının ötesinde termik sıfırlanır (derece).")]
        [Range(6f, 45f)] public float slopeFalloffDeg = 22f;

        [Header("Güneş")]
        [Tooltip("Güneşin geldiği yön (derece, 0 = kuzey). Öğleden sonra " +
                 "güneybatı: 225.")]
        [Range(0f, 360f)] public float sunAzimuthDeg = 225f;

        [Tooltip("Güneşin yüksekliği (derece). Alçak güneş yamacı ısıtmaz.")]
        [Range(0f, 90f)] public float sunElevationDeg = 46f;

        [Header("Tavan")]
        [Tooltip("Termiğin bittiği kot (m). Bulut tabanı. Tavansız bir model " +
                 "oyuncuyu stratosfere çıkarırdı.")]
        public float ceilingMeters = 620f;

        [Tooltip("Yerden bu kadar yükseğe kadar termik güçlenir (m); altında " +
                 "henüz toplanmamıştır.")]
        public float rampMeters = 60f;

        [Header("Su")]
        [Tooltip("Su üstünde alçalan hava (m/s, negatif). Deniz gündüz " +
                 "karadan serindir.")]
        public float waterSink = -0.55f;

        [Tooltip("Kıyıdan bu kadar açıkta çökelme tam değerine ulaşır (m).")]
        public float waterBlendMeters = 220f;

        [Header("Yamaç kaldıracı")]
        [Tooltip("Rüzgârın yamaca çarpmasından doğan tırmanış çarpanı. " +
                 "Rüzgâr hızıyla ölçeklenir.")]
        [Range(0f, 1f)] public float ridgeGain = 0.34f;

        [Tooltip("Yamaç kaldıracının eriştiği en yüksek kot, yamacın " +
                 "tepesinden itibaren (m).")]
        public float ridgeReachMeters = 110f;

        Terrain _terrain;
        Terrain T => _terrain != null ? _terrain : (_terrain = GetComponent<Terrain>());

        /// <summary>
        /// Bu noktadaki <b>dikey</b> hava hareketi (m/s). Yatay bileşen
        /// üretmez: bu sınıfın modellediği şey ısınma ve saptırmadır,
        /// meteorolojik rüzgâr değil (o <see cref="WindTuning.globalWind"/>).
        /// </summary>
        public float SampleVertical(Vector3 worldPos, Vector3 horizontalWind)
        {
            var td = T.terrainData;
            Vector3 o = T.transform.position;
            float u = (worldPos.x - o.x) / td.size.x;
            float v = (worldPos.z - o.z) / td.size.z;
            if (u < 0f || u > 1f || v < 0f || v > 1f) return 0f;

            float ground = T.SampleHeight(worldPos) + o.y;
            float agl = worldPos.y - ground;
            if (agl < 0f) return 0f;                       // yerin altinda

            // --- Su mu kara mi -----------------------------------------
            // Kiyida keskin bir sinir olmaz: cokelme kiyidan aciga dogru
            // kuvvetlenir. Keskin sinir oyuncuya "gorunmez duvar" hissi
            // verirdi ve ruzgarin OKUNABILIR olmasi ilkesine aykiri olurdu.
            if (ground < 0f)
            {
                float derinlik = Mathf.Clamp01(-ground / 12f);
                return waterSink * derinlik * TavanSonumu(worldPos.y);
            }

            // --- 1) Termik ----------------------------------------------
            float termik = Termik(td, u, v, agl, worldPos.y);

            // --- 2) Yamac kaldiraci -------------------------------------
            float yamac = YamacKaldiraci(td, u, v, agl, horizontalWind);

            // Ikisi ayni havayi iki kez saymaz: termik isinmadan, yamac
            // saptirmadan gelir ve gercekte de toplanirlar.
            return (termik + yamac) * TavanSonumu(worldPos.y);
        }

        float Termik(TerrainData td, float u, float v, float agl, float y)
        {
            float egim = td.GetSteepness(u, v);
            var n = td.GetInterpolatedNormal(u, v);

            // Yamacin baktigi yon (azimut, 0 = kuzey).
            float bakis = Mathf.Atan2(n.x, n.z) * Mathf.Rad2Deg;
            if (bakis < 0f) bakis += 360f;

            // Gunese ne kadar donuk? Guneye bakan yamac isinir, kuzeye
            // bakan golgede kalir. Fark 90 dereceyi gecince katki yok.
            float fark = Mathf.Abs(Mathf.DeltaAngle(bakis, sunAzimuthDeg));
            float yonel = Mathf.Cos(fark * Mathf.Deg2Rad);
            if (yonel <= 0f) return 0f;                    // golgeli yamac

            // Gunes ne kadar yuksek? Alcak gunes yamaci isitmaz.
            float gunes = Mathf.Sin(sunElevationDeg * Mathf.Deg2Rad);
            if (gunes <= 0f) return 0f;

            // Egim: duz zemin isinir ama havayi TOPLAMAZ; cok dik yamac
            // gunesi yalayarak alir. Ikisinin arasinda bir en iyi var.
            float egimPay = Mathf.Clamp01(
                1f - Mathf.Abs(egim - bestSlopeDeg) / slopeFalloffDeg);

            // Yerden yukseldikce termik toplanir; hemen zeminde henuz yok.
            float ramp = Mathf.Clamp01(agl / Mathf.Max(1f, rampMeters));

            return peakLift * yonel * gunes * egimPay * ramp;
        }

        float YamacKaldiraci(TerrainData td, float u, float v, float agl,
                             Vector3 wind)
        {
            var yatay = new Vector2(wind.x, wind.z);
            float hiz = yatay.magnitude;
            if (hiz < 0.5f) return 0f;

            float egim = td.GetSteepness(u, v);
            if (egim < 6f) return 0f;                      // duz zeminde yok

            var n = td.GetInterpolatedNormal(u, v);
            var bakisVek = new Vector2(n.x, n.z);
            if (bakisVek.sqrMagnitude < 1e-6f) return 0f;
            bakisVek.Normalize();

            // Yamac RUZGARA bakiyorsa kaldirac var: ruzgar yamaca carpar.
            // Ruzgarin GELDIGI yon -yatay.
            float karsi = Vector2.Dot(bakisVek, -yatay.normalized);
            if (karsi <= 0f) return 0f;

            // Yukseklikle soner: yamac kaldiraci sirtin biraz ustune kadar.
            float ust = Mathf.Clamp01(1f - agl / Mathf.Max(1f, ridgeReachMeters));

            return ridgeGain * hiz * karsi
                   * Mathf.Sin(egim * Mathf.Deg2Rad) * ust;
        }

        /// <summary>Bulut tabanına yaklaşırken kaldıraç söner.</summary>
        float TavanSonumu(float y)
        {
            if (y <= 0f) return 1f;
            float t = y / Mathf.Max(1f, ceilingMeters);
            return Mathf.Clamp01(1f - t * t);
        }
    }
}
