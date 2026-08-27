using UnityEngine;

namespace Hezarfen.Flight
{
    /// <summary>
    /// Rüzgârı görünür kılar (plan Görev 6: "görünür rüzgâr partikülleri").
    ///
    /// Neden zorunlu: rüzgâr bu oyunda bir efekt değil ana mekanik. Göremediği bir
    /// şeyde oyuncu ustalaşamaz — yükselticiyi bulmak "şansa" dönüşür ve plan Bölüm 2'nin
    /// "okunabilir ve ustalık isteyen" şartı çöker.
    ///
    /// Partikül sistemi oyuncuyu takip eder; şerit hızı ve yoğunluğu o noktadaki
    /// rüzgârdan sürülür. Yükselen hava mavi, çökelen hava kırmızıya kayar.
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    [AddComponentMenu("Hezarfen/Wind Visualizer")]
    public class WindVisualizer : MonoBehaviour
    {
        [Tooltip("Boşsa sahnede aranır.")]
        public WindField windField;

        [Tooltip("Takip edilecek nesne (oyuncu). Boşsa sahnedeki GlideController aranır.")]
        public Transform followTarget;

        [Tooltip("Takip yüksekliği ofseti (m) — partiküller oyuncunun etrafını sarsın.")]
        public Vector3 followOffset = Vector3.zero;

        [Tooltip("Bu hızın altındaki rüzgâr gösterilmez (m/s).")]
        public float minVisibleSpeed = 0.4f;

        [Tooltip("Bu hızda yayılım doruğa çıkar (m/s).")]
        public float maxVisibleSpeed = 8f;

        [Tooltip("Doruktaki saniyelik partikül sayısı.")]
        public float maxEmissionRate = 90f;

        [Tooltip("Yükselen hava rengi.")]
        public Color liftColor = new Color(0.45f, 0.85f, 1f, 0.55f);

        [Tooltip("Çökelen hava rengi.")]
        public Color sinkColor = new Color(1f, 0.45f, 0.35f, 0.55f);

        [Tooltip("Yatay/nötr hava rengi.")]
        public Color neutralColor = new Color(0.85f, 0.9f, 0.95f, 0.35f);

        private ParticleSystem ps;

        private void Awake()
        {
            ps = GetComponent<ParticleSystem>();

            var main = ps.main;
            // Dunya uzayinda simule et: partikuller oyuncuyla birlikte SURUKLENMEZ,
            // havada asili kalir. Hareket hissinin kaynagi budur.
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            if (windField == null) windField = FindAnyObjectByType<WindField>();

            if (followTarget == null)
            {
                var glider = FindAnyObjectByType<GlideController>();
                if (glider != null) followTarget = glider.transform;
            }
        }

        private void LateUpdate()
        {
            if (followTarget != null)
            {
                transform.position = followTarget.position + followOffset;
            }

            if (windField == null || ps == null) return;

            Vector3 wind = windField.Sample(transform.position);
            float speed = wind.magnitude;

            var emission = ps.emission;

            if (speed < minVisibleSpeed)
            {
                emission.rateOverTime = 0f;
                return;
            }

            float t = Mathf.InverseLerp(minVisibleSpeed, maxVisibleSpeed, speed);
            emission.rateOverTime = Mathf.Lerp(6f, maxEmissionRate, t);

            // Partikuller rüzgârla birlikte aksin
            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(wind.x);
            vel.y = new ParticleSystem.MinMaxCurve(wind.y);
            vel.z = new ParticleSystem.MinMaxCurve(wind.z);

            // Dikey bilesen rengi surusun: yukselen mavi, cokelen kirmizi.
            var main = ps.main;
            float vertical = Mathf.Clamp(wind.y / 4f, -1f, 1f);
            Color c = vertical >= 0f
                ? Color.Lerp(neutralColor, liftColor, vertical)
                : Color.Lerp(neutralColor, sinkColor, -vertical);
            main.startColor = new ParticleSystem.MinMaxGradient(c);
        }
    }
}
