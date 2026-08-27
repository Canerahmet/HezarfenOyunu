using UnityEngine;

namespace Hezarfen.Flight
{
    /// <summary>
    /// Kalkış: aygıta başlangıç hava hızı verir.
    /// Gerçekte pilot yamaçtan koşarak atlar; v0'da o koşuyu simüle etmiyoruz —
    /// prototipin amacı süzülüş hissi, kalkış koreografisi değil (o Faz 5'te
    /// karakter animasyonuyla gelir).
    ///
    /// Varsayılan hız, <see cref="WindTuning"/>'den hesaplanan en iyi süzülme
    /// hızıdır; yani aygıt dengeli uçuşta doğar, ilk saniyeyi düşerek harcamaz.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(GlideController))]
    [AddComponentMenu("Hezarfen/Flight Launch")]
    public class FlightLaunch : MonoBehaviour
    {
        [Tooltip("Açıksa kalkış hızı WindTuning'den hesaplanır (en iyi süzülme hızı).")]
        public bool useTrimSpeed = true;

        [Tooltip("useTrimSpeed kapalıysa kullanılacak hız (m/s).")]
        public float manualLaunchSpeed = 12.5f;

        [Tooltip("Sahne başlarken otomatik fırlat.")]
        public bool launchOnStart = true;

        private void Start()
        {
            if (launchOnStart) Launch();
        }

        /// <summary>Aygıtı burnu yönünde kalkış hızıyla başlatır.</summary>
        public void Launch()
        {
            var rb = GetComponent<Rigidbody>();
            var glide = GetComponent<GlideController>();

            float speed = manualLaunchSpeed;

            if (useTrimSpeed && glide != null && glide.tuning != null)
            {
                var best = Aerodynamics.BestGlideRatio(glide.tuning);
                float trim = Aerodynamics.TrimSpeed(best.alphaDeg, glide.tuning);
                if (!float.IsInfinity(trim) && !float.IsNaN(trim)) speed = trim;
            }

            // KRİTİK: kalkış hızı HAVA hızıdır, yer hızı değil.
            // Rüzgâr eklenmezse kuyruk rüzgârında aygıt hava hızsız doğar ve taş gibi
            // düşer (9 m/s lodosta 12.4 m/s yer hızı = 3.4 m/s hava hızı). Gerçek
            // planörcülerin rüzgâra KARŞI kalkmasının sebebi de budur.
            rb.linearVelocity = SampleLaunchWind(glide) + transform.forward * speed;
        }

        private Vector3 SampleLaunchWind(GlideController glide)
        {
            var field = glide != null && glide.windField != null
                ? glide.windField
                : FindAnyObjectByType<WindField>();

            if (field != null) return field.Sample(transform.position);
            return glide != null && glide.tuning != null ? glide.tuning.globalWind : Vector3.zero;
        }
    }
}
