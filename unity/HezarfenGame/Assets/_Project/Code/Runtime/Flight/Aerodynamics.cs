using UnityEngine;

namespace Hezarfen.Flight
{
    /// <summary>
    /// Saf aerodinamik matematik — Unity sahnesine, Rigidbody'ye, zamana bağlı DEĞİL.
    /// Bu ayrım bilinçli: uçuşun kabul kriteri (rüzgârsız süzülme oranı 8-12:1)
    /// böylece sahne açmadan, oyunu çalıştırmadan, saniyeler içinde test edilebiliyor.
    ///
    /// Model basitleştirilmiştir (plan Bölüm 5): CL alpha ile doğrusal artar, stall'da
    /// çöker; CD parazit + indüklenmiş iki terimden oluşur.
    /// </summary>
    public static class Aerodynamics
    {
        /// <summary>Yerçekimi ivmesi (m/s^2). Unity'nin Physics.gravity'sinden bağımsız tutuldu ki test deterministik olsun.</summary>
        public const float Gravity = 9.81f;

        /// <summary>Taşıma katsayısı CL, hücum açısının (derece) fonksiyonu.</summary>
        public static float LiftCoefficient(float alphaDeg, WindTuning t)
        {
            float linear = t.cl0 + t.clPerRadian * (alphaDeg * Mathf.Deg2Rad);

            float overStall = Mathf.Abs(alphaDeg) - t.stallAngleDeg;
            if (overStall <= 0f)
                return linear;

            // Stall: taşıma çöker. Burun düşer, hız toplanır, pilot toparlayabilir.
            float collapse = Mathf.Max(0f, 1f - t.postStallClFalloff * overStall);
            return linear * collapse;
        }

        /// <summary>Sürükleme katsayısı CD. Parazit + indüklenmiş (+ stall cezası).</summary>
        public static float DragCoefficient(float alphaDeg, float cl, WindTuning t)
        {
            float cd = t.cd0 + t.inducedDragK * cl * cl;

            float overStall = Mathf.Abs(alphaDeg) - t.stallAngleDeg;
            if (overStall > 0f)
                cd += t.postStallCdRise * overStall;

            return cd;
        }

        /// <summary>Belirli bir hücum açısındaki süzülme oranı (L/D).</summary>
        public static float GlideRatioAt(float alphaDeg, WindTuning t)
        {
            float cl = LiftCoefficient(alphaDeg, t);
            float cd = DragCoefficient(alphaDeg, cl, t);
            if (cd <= Mathf.Epsilon) return 0f;
            return cl / cd;
        }

        /// <summary>
        /// Stall öncesi ulaşılabilen EN İYİ süzülme oranı ve o andaki hücum açısı.
        /// Kabul kriteri bu değere bakar (plan Bölüm 5: 8-12:1).
        /// </summary>
        public static (float ratio, float alphaDeg) BestGlideRatio(WindTuning t)
        {
            float best = 0f;
            float bestAlpha = 0f;

            // 0.1 derece adımlarla tara — analitik çözüm yerine tarama, çünkü
            // stall kesintisi fonksiyonu parçalı yapıyor.
            for (float a = t.minCommandAlphaDeg; a <= t.stallAngleDeg; a += 0.1f)
            {
                float r = GlideRatioAt(a, t);
                if (r > best)
                {
                    best = r;
                    bestAlpha = a;
                }
            }

            return (best, bestAlpha);
        }

        /// <summary>
        /// Verilen hücum açısında dengeli (taşıma = ağırlık) uçuş hızı, m/s.
        /// L = 0.5*rho*v^2*S*CL = m*g  =>  v = sqrt(2*m*g / (rho*S*CL))
        /// </summary>
        public static float TrimSpeed(float alphaDeg, WindTuning t)
        {
            float cl = LiftCoefficient(alphaDeg, t);
            if (cl <= Mathf.Epsilon) return float.PositiveInfinity;

            float denom = t.airDensity * t.wingArea * cl;
            return Mathf.Sqrt(2f * t.mass * Gravity / denom);
        }

        /// <summary>Dengeli süzülüşte batış hızı (m/s, pozitif = aşağı).</summary>
        public static float SinkRate(float alphaDeg, WindTuning t)
        {
            float ratio = GlideRatioAt(alphaDeg, t);
            if (ratio <= Mathf.Epsilon) return float.PositiveInfinity;
            return TrimSpeed(alphaDeg, t) / ratio;
        }

        /// <summary>Belirli yükseklikten rüzgârsız katedilebilecek yatay mesafe (m).</summary>
        public static float GlideRangeFrom(float altitudeMeters, WindTuning t)
        {
            return altitudeMeters * BestGlideRatio(t).ratio;
        }

        /// <summary>Dinamik basınç q = 0.5*rho*v^2.</summary>
        public static float DynamicPressure(float speed, WindTuning t)
        {
            return 0.5f * t.airDensity * speed * speed;
        }
    }
}
