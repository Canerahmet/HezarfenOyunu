using Hezarfen.Flight;
using NUnit.Framework;
using UnityEngine;

namespace Hezarfen.Tests
{
    /// <summary>
    /// Faz 0'ın sayısal kabul kriterlerini kilitler (docs/PLAN.md Bölüm 5):
    /// "Rüzgârsız düz süzülmede etkin oran 8-12:1 (asılı planör inandırıcılığı);
    ///  akıntılarla efsane mesafesi kapanıyor."
    ///
    /// Bu testler sahne açmaz, oyunu çalıştırmaz — saf matematik. Ayar yaparken
    /// saniyeler içinde geri bildirim verir.
    /// </summary>
    public class AerodynamicsTests
    {
        private WindTuning t;

        [SetUp]
        public void SetUp()
        {
            // Varsayilan degerlerle olusturulur; asset'ten bagimsiz test edilir ki
            // birinin Inspector'da oynamasi testi bozmasin.
            t = ScriptableObject.CreateInstance<WindTuning>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(t);
        }

        [Test]
        public void BestGlideRatio_IsInHangGliderBand()
        {
            var (ratio, alpha) = Aerodynamics.BestGlideRatio(t);

            Assert.GreaterOrEqual(ratio, 8f, $"Suzulme orani cok dusuk ({ratio:F1}:1) - tas gibi duser.");
            Assert.LessOrEqual(ratio, 12f, $"Suzulme orani cok yuksek ({ratio:F1}:1) - asili planor degil, planor olur.");
            Assert.Greater(alpha, 0f, "En iyi suzulme pozitif hucum acisinda olmali.");
            Assert.Less(alpha, t.stallAngleDeg, "En iyi suzulme stall'in ALTINDA kalmali.");
        }

        [Test]
        public void GlideRangeFrom100m_CannotReachLegendaryDistance()
        {
            // Tasarim niyeti: graybox KASTEN ulasilamaz. Efsane mesafesini
            // fizik sabitleri degil, ruzgar akintilari kapatacak (plan Bolum 2).
            float range = Aerodynamics.GlideRangeFrom(100f, t);

            Assert.Less(range, Core.GameUnits.LegendaryGlideDistanceMeters,
                "Ruzgarsiz sizulus efsane mesafesine ULASMAMALI - ulasirsa ruzgar sistemi gereksizlesir.");
            Assert.Greater(range, 800f, "100 m'den en az ~800 m gidilebilmeli.");
        }

        [Test]
        public void TrimSpeed_IsPlausibleForHangGlider()
        {
            var (_, bestAlpha) = Aerodynamics.BestGlideRatio(t);
            float v = Aerodynamics.TrimSpeed(bestAlpha, t);

            // Asili planor seyir hizi kabaca 35-70 km/s = 10-20 m/s
            Assert.GreaterOrEqual(v, 8f, $"Dengeli ucus hizi cok dusuk: {v:F1} m/s");
            Assert.LessOrEqual(v, 22f, $"Dengeli ucus hizi cok yuksek: {v:F1} m/s");
        }

        [Test]
        public void SinkRate_IsPlausibleForHangGlider()
        {
            var (_, bestAlpha) = Aerodynamics.BestGlideRatio(t);
            float sink = Aerodynamics.SinkRate(bestAlpha, t);

            // Iyi bir asili planor ~1 m/s batar.
            Assert.Greater(sink, 0.5f, $"Batis hizi gercekci degil (cok yavas): {sink:F2} m/s");
            Assert.Less(sink, 2.5f, $"Batis hizi cok yuksek: {sink:F2} m/s");
        }

        [Test]
        public void Stall_CollapsesLift()
        {
            float justBelow = Aerodynamics.LiftCoefficient(t.stallAngleDeg - 0.5f, t);
            float wellBeyond = Aerodynamics.LiftCoefficient(t.stallAngleDeg + 10f, t);

            Assert.Less(wellBeyond, justBelow,
                "Stall sonrasi tasima COKMELI - burun dusmezse stall bir ceza olmaz.");
        }

        [Test]
        public void Stall_IncreasesDrag()
        {
            float aBelow = t.stallAngleDeg - 0.5f;
            float aBeyond = t.stallAngleDeg + 10f;

            float cdBelow = Aerodynamics.DragCoefficient(aBelow, Aerodynamics.LiftCoefficient(aBelow, t), t);
            float cdBeyond = Aerodynamics.DragCoefficient(aBeyond, Aerodynamics.LiftCoefficient(aBeyond, t), t);

            Assert.Greater(cdBeyond, cdBelow, "Stall sonrasi surukleme ARTMALI.");
        }

        [Test]
        public void GlideRatio_DegradesOutsideBestAlpha()
        {
            var (best, bestAlpha) = Aerodynamics.BestGlideRatio(t);

            Assert.Less(Aerodynamics.GlideRatioAt(bestAlpha - 5f, t), best,
                "Cok hizli ucmak (dusuk alpha) oranı kotulestirmeli.");
            Assert.Less(Aerodynamics.GlideRatioAt(bestAlpha + 5f, t), best,
                "Cok yavas ucmak (yuksek alpha) orani kotulestirmeli.");
        }

        [Test]
        public void PilotCanCommandBeyondStall()
        {
            // Hata mumkun olmali: pilot stall'a girebilmeli, yoksa ustalik diye bir sey kalmaz.
            Assert.Greater(t.maxCommandAlphaDeg, t.stallAngleDeg,
                "Pilot stall acisinin USTUNE cikabilmeli.");
        }
    }
}
