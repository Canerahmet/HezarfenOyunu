using UnityEngine;

namespace Hezarfen.Flight
{
    /// <summary>
    /// Uçuşun TÜM ayar katsayıları. Plan Bölüm 5'in şartı: "kod değişmeden ayar".
    /// Kodda sihirli sayı yok — buradaki her alan Inspector'dan oynanabilir ve
    /// farklı profiller (acemi / usta / rüzgârsız test) ayrı asset olarak tutulabilir.
    ///
    /// Varsayılanlar gerçek bir asılı planörün mertebesindedir (S≈15 m², m≈100 kg,
    /// en iyi süzülme ≈11:1, batış ≈1 m/s). Efsanenin istediği 33:1'i fizik sabitleriyle
    /// DEĞİL, rüzgâr akıntılarıyla kapatıyoruz (docs/RESEARCH.md ve plan Bölüm 2).
    /// </summary>
    [CreateAssetMenu(fileName = "WindTuning", menuName = "Hezarfen/Wind Tuning", order = 0)]
    public class WindTuning : ScriptableObject
    {
        [Header("Atmosfer ve aygıt")]
        [Tooltip("Hava yoğunluğu (kg/m^3). Deniz seviyesi ~1.225")]
        public float airDensity = 1.225f;

        [Tooltip("Kanat alanı S (m^2). Asılı planör tipik 15-16")]
        public float wingArea = 15f;

        [Tooltip("Pilot + aygıt kütlesi (kg)")]
        public float mass = 100f;

        [Header("Taşıma katsayısı CL(alpha)")]
        [Tooltip("Sıfır hücum açısındaki taşıma katsayısı")]
        public float cl0 = 0.15f;

        [Tooltip("CL'nin hücum açısına göre eğimi (1/radyan). İnce kanat teorisi ~2*pi")]
        public float clPerRadian = 5.0f;

        [Tooltip("Stall (perdövites) hücum açısı, derece")]
        public float stallAngleDeg = 15f;

        [Tooltip("Stall sonrası CL'nin ne kadar hızlı çöktüğü (1/derece)")]
        public float postStallClFalloff = 0.06f;

        [Header("Sürükleme katsayısı CD")]
        [Tooltip("Parazit sürükleme CD0 — hızla değişmeyen taban sürükleme")]
        public float cd0 = 0.030f;

        [Tooltip("İndüklenmiş sürükleme katsayısı k: CD = CD0 + k*CL^2. k = 1/(pi*AR*e)")]
        public float inducedDragK = 0.0624f;

        [Tooltip("Stall sonrası eklenen sürükleme (derece başına)")]
        public float postStallCdRise = 0.02f;

        [Header("Pilot kontrolü — ağırlık aktarımı")]
        [Tooltip("Pilotun komut edebileceği en küçük hücum açısı (derece). Burun aşağı = hız. " +
                 "Sıfır-taşıma açısının (~-1.7°) ALTINA inmemeli: negatif taşıma tuck'a ve takla atmaya yol açar.")]
        public float minCommandAlphaDeg = 1f;

        [Tooltip("Pilotun komut edebileceği en büyük hücum açısı (derece). Kararlılık momenti bunu " +
                 "kırptığı için ulaşılan gerçek açı daha düşüktür; stall'ın ÜSTÜNE çıkabilmeli ki hata mümkün olsun")]
        public float maxCommandAlphaDeg = 24f;

        [Header("Aerodinamik kararlılık")]
        [Tooltip("Pitch kararlılığı (rüzgâr gülü): hücum açısını sıfıra geri iter. " +
                 "Bu terim OLMAZSA aygıt en küçük bozulmada takla atar — bulundu ve testle sabitlendi.")]
        public float pitchStability = 0.8f;

        [Tooltip("Stall kırılması: stall açısı aşıldığında burnu AŞAĞI iten ek moment. " +
                 "Planın 'α eşiği aşılınca burun düşer' şartını sağlayan terim budur.")]
        public float stallBreakMoment = 1.5f;

        [Tooltip("Kararlılık momentlerinin en düşük etkinliği (0-1). Pilot kontrolü hava hızıyla " +
                 "zayıflar ama kararlılık zayıflamamalı: stall'da hız düşük olduğu için hızla " +
                 "ölçeklenen bir kurtarma momenti asla kurtaramaz. Gerçekte burnu düşüren şey " +
                 "aerodinamik değil ağırlık merkezidir — bu taban onu temsil eder.")]
        [Range(0f, 1f)]
        public float minStabilityAuthority = 0.45f;

        [Tooltip("Pitch (hücum açısı) otoritesi — ağırlık aktarımının ne kadar güçlü olduğu")]
        public float pitchAuthority = 2.2f;

        [Tooltip("Roll (yatış) otoritesi")]
        public float rollAuthority = 1.6f;

        [Tooltip("Tam yatış girdisinde ulaşılan bank açısı (derece). Girdi bank HIZI değil " +
                 "bank AÇISI komut eder — aksi halde sabit girdi aygıtı durmadan yuvarlar.")]
        public float maxBankAngleDeg = 55f;

        [Tooltip("Yalpa kararlılığı — yan kaymayı (sideslip) söndürür, rüzgâr gülü etkisi")]
        public float yawStability = 2.0f;

        [Tooltip("Açısal sönümleme — salınımları keser")]
        public float angularDamping = 1.8f;

        [Header("İniş ve kaza eşikleri")]
        [Tooltip("Bu dikey hızın üstünde temas = kaza (m/s)")]
        public float maxLandingSinkSpeed = 6f;

        [Tooltip("Bu açının üstünde eğik temas = kaza (derece)")]
        public float maxLandingTiltDeg = 40f;

        [Header("Rüzgâr (Faz 0 v0 — Görev 6'da WindField genişletecek)")]
        [Tooltip("Global lodos vektörü (m/s), dünya uzayında")]
        public Vector3 globalWind = new Vector3(0f, 0f, 0f);
    }
}
