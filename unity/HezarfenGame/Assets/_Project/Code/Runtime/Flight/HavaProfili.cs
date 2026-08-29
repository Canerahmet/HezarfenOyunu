using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Hezarfen.Flight
{
    /// <summary>
    /// <b>Rüzgâr gülü — Osmanlı adlarıyla.</b>
    ///
    /// Adlar uydurulmadı: RESEARCH §4.6 menzil atışı kaynaklarından
    /// (Kaya &amp; Şahin, HÜTAD; Karagülle &amp; Koç). Okmeydanı'nda
    /// menzile uygun rüzgârlar tek tek sayılıyor ve <b>yıldız ile karayel
    /// meydanda nadir eser</b>.
    ///
    /// Yön, rüzgârın <b>geldiği</b> azimuttur — denizcilik ve okçuluk
    /// geleneğinde rüzgâr geldiği yerle anılır. Kaynak bunu açıkça
    /// söylüyor: *"ok azimutu = rüzgârın geldiği azimut + 180°"*.
    /// </summary>
    public enum Ruzgar
    {
        /// <summary>Kuzey. Meydanda nadir.</summary>
        Yildiz = 0,
        /// <summary>Kuzeydoğu.</summary>
        Poyraz = 45,
        /// <summary>Doğu.</summary>
        Gundogusu = 90,
        /// <summary>Güneydoğu.</summary>
        Kesisleme = 135,
        /// <summary>Güney.</summary>
        Kible = 180,
        /// <summary>
        /// Güneybatı — <b>oyunun birinci tasarım direği</b> (PLAN §0).
        /// Uçuşu mümkün kılan arkadan rüzgâr budur.
        /// </summary>
        Lodos = 225,
        /// <summary>Kuzeybatı. Meydanda nadir.</summary>
        Karayel = 315,
    }

    /// <summary>
    /// <b>Hava profili: tek rüzgâr, her yerde aynı rüzgâr.</b>
    /// PLAN Bölüm 12: *"lodoslu hava profili (bulut hızı, dalga, ağaç
    /// savrulması senkron)"*.
    ///
    /// ## "Senkron" bir süs değil, bir KISIT
    ///
    /// Bulutu bir yöne, dalgayı başka yöne, ağacı üçüncü yöne savuran bir
    /// sahne oyuncuya rüzgârın nereden estiğini <b>söyleyemez</b>. Ve bu
    /// oyunda rüzgâr bir efekt değil <b>ana mekanik</b>: Hezarfen'in
    /// uçuşunu mümkün kılan şey lodostur, oyuncu da onu okumak zorunda.
    /// Gökyüzü yalan söylerse oyuncu yanlış yöne atlar.
    ///
    /// Bu yüzden burada tek bir vektör var ve <b>hepsi ondan türüyor</b>:
    /// uçuş fiziği (<see cref="WindTuning.globalWind"/>), bulut, dalga ve
    /// ağaç. İkinci bir rüzgâr alanı yok — *bir sayının iki sahibi varsa
    /// er ya da geç iki değeri olur.*
    /// </summary>
    [ExecuteAlways]
    [AddComponentMenu("Hezarfen/Hava Profili")]
    public class HavaProfili : MonoBehaviour
    {
        [Header("Rüzgâr")]
        [Tooltip("Hangi rüzgâr esiyor — Osmanlı adlarıyla.")]
        public Ruzgar ruzgar = Ruzgar.Lodos;

        [Tooltip("Rüzgâr hızı (m/s). Lodos fırtınası 15-20 m/s'ye çıkar.")]
        [Range(0f, 25f)] public float hiz = 8f;

        [Header("Bağlantılar (boşsa sahnede aranır)")]
        public WindTuning ayar;
        public WaterSurface su;
        public Volume gokVolume;

        [Header("Türetme katsayıları")]
        [Tooltip("Bulut hızı = rüzgâr hızı × bu. Bulutlar yerden hızlıdır.")]
        public float bulutKati = 1.6f;

        [Tooltip("Dalga = rüzgâr hızı × bu.")]
        public float dalgaKati = 1.0f;

        /// <summary>
        /// Ağaç savrulması için küresel shader vektörü:
        /// <c>xyz</c> = yön, <c>w</c> = hız.
        /// </summary>
        public static readonly int RuzgarKimlik =
            Shader.PropertyToID("_HZ_Ruzgar");

        /// <summary>Rüzgârın gittiği yön (birim vektör, dünya uzayı).</summary>
        public Vector3 Yon
        {
            get
            {
                // Enum GELDIGI azimutu tutar; ruzgar ters yone GIDER.
                float a = ((int)ruzgar + 180f) * Mathf.Deg2Rad;
                return new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
            }
        }

        /// <summary>Rüzgâr vektörü (m/s) — her şeyin kaynağı.</summary>
        public Vector3 Vektor => Yon * hiz;

        private void OnEnable() => Uygula();
        private void OnValidate() => Uygula();

        /// <summary>Tek vektörü bütün dinleyicilere dağıtır.</summary>
        public void Uygula()
        {
            if (ayar == null)
            {
                var f = FindAnyObjectByType<WindField>();
                if (f != null) ayar = f.tuning;
            }
            if (su == null) su = FindAnyObjectByType<WaterSurface>();

            Vector3 v = Vektor;

            // 1) UCUS FIZIGI — rüzgârın tek kaynağı (WindField buradan okur).
            if (ayar != null) ayar.globalWind = v;

            // 2) AGAC SAVRULMASI — küresel shader vektörü.
            //
            // Kanca burada; ağaç malzemesi henüz bunu OKUMUYOR (ağaçlar
            // dokusuz katı geometri, ADR 0019, ve savrulma bir vertex
            // shader'ı ister). Kancayı şimdi koymak, shader geldiğinde
            // rüzgârın ikinci bir sahibi doğmasın diye.
            Shader.SetGlobalVector(RuzgarKimlik,
                new Vector4(Yon.x, Yon.y, Yon.z, hiz));

            // 3) DALGA — su rüzgârla kabarır ve rüzgârın yönüne gider.
            if (su != null)
            {
                su.largeWindSpeed = hiz * dalgaKati * 3.6f;   // km/h ister
                su.largeOrientationValue = Azimut();
            }

            // 4) BULUT — gökyüzü de aynı yöne akar.
            if (gokVolume != null && gokVolume.sharedProfile != null
                && gokVolume.sharedProfile.TryGet(out VolumetricClouds bulut))
            {
                // HDRP bulut rüzgârı kendi sarmalayıcısını kullanır
                // (WindParamaterValue): sayı doğrudan atanmaz, ÖZEL mod
                // ile verilir. Yoksa bulut, göğün kendi varsayılanına
                // döner ve rüzgârdan bağımsız akar.
                bulut.globalWindSpeed.overrideState = true;
                bulut.globalWindSpeed.value =
                    new WindParameter.WindParamaterValue {
                        mode = WindParameter.WindOverrideMode.Custom,
                        customValue = hiz * bulutKati * 3.6f,
                    };
                bulut.orientation.overrideState = true;
                bulut.orientation.value =
                    new WindParameter.WindParamaterValue {
                        mode = WindParameter.WindOverrideMode.Custom,
                        customValue = Azimut(),
                    };
            }
        }

        /// <summary>Rüzgârın gittiği azimut (derece, kuzeyden saat yönü).</summary>
        public float Azimut() => Mathf.Repeat((int)ruzgar + 180f, 360f);
    }
}
