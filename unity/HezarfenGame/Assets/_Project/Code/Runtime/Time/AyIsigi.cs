using System;
using UnityEngine;

namespace Hezarfen.Zaman
{
    /// <summary>
    /// <b>Gecenin ışığı.</b>
    ///
    /// Gece karesi yakalandı ve <b>tamamen siyahtı</b> — 78 KB'lık bir
    /// PNG, yani neredeyse tek renk. Sebep ölçüldü ve tekti: sahnede
    /// bir tane ışık var, <see cref="ZamanSistemi"/> onu güneş ufkun
    /// altına inince kapatıyor (<c>gunesIsigi.enabled = alt > -0.10</c>).
    /// Kapanınca geriye hiçbir kaynak kalmıyor.
    ///
    /// ## "1632'de sokak lambası yoktu" doğru ama cevap değil
    ///
    /// Yoktu; İstanbul geceleri gerçekten karanlıktı ve gece dolaşan
    /// fener taşımak zorundaydı (RESEARCH §6 — ases ve fener). Ama
    /// karanlık ile <b>siyah</b> aynı şey değil. Açık havada dolunay
    /// ~0,25 lüks verir ve göz buna uyum sağlar: insan dolunayda
    /// gölgesini görür. Ekranda hiçbir şey görünmüyorsa bu tarihsel
    /// sadakat değil, eksik kaynaktır.
    ///
    /// ## Ay gerçekten hesaplanıyor
    ///
    /// Evre uydurulmuyor: sinodik ay 29,53 gün ve bilinen bir yeni ay
    /// tarihinden sayılıyor. Bunun bedeli bir çarpan; kazancı, bazı
    /// gecelerin gerçekten daha karanlık olması. Her gece aynı gümüş
    /// ışıkla aydınlanan bir şehir, gecenin kendisini anlamsızlaştırır.
    ///
    /// Ay <b>güneşin karşısında değil</b>: evreye göre güneşten
    /// açılanır. Dolunay güneşin tam karşısındadır (bu yüzden gece
    /// yarısı tepededir), hilal güneşe yakındır (bu yüzden gün
    /// batımında ufka yakın görünür ve erken batar).
    /// </summary>
    [AddComponentMenu("Hezarfen/Ay Isigi")]
    [RequireComponent(typeof(Light))]
    public class AyIsigi : MonoBehaviour
    {
        [Tooltip("Boşsa sahnede aranır.")]
        public ZamanSistemi zaman;

        [Tooltip("Dolunayda ışık şiddeti (lüks).")]
        public float dolunayLuks = 0.28f;

        [Tooltip("Yeni ayda bile kalan gök parıltısı (lüks).")]
        public float tabanLuks = 0.045f;

        private Light _isik;

        /// <summary>
        /// Işık, <b>istendiğinde</b> bulunur — <c>Awake</c>'te değil.
        ///
        /// Bu ders bu turda ikinci kez öğrenildi: Unity, sahne
        /// kurulumu sırasında (Editor kipi) <c>Awake</c> çağırmaz.
        /// <c>OyunSahnesiKur</c> bileşeni ekleyip hemen
        /// <see cref="Uygula"/> çağırınca alan boştu ve kurulum
        /// NullReferenceException ile düştü. Bir değerin var olması,
        /// onu kimin ne zaman uyandırdığına bağlı olmamalı.
        /// </summary>
        private Light Isik
        {
            get
            {
                if (_isik == null) _isik = GetComponent<Light>();
                if (_isik == null) _isik = gameObject.AddComponent<Light>();
                _isik.type = LightType.Directional;
                return _isik;
            }
        }

        /// <summary>Sinodik ay (gün).</summary>
        public const double SinodikAy = 29.530588;

        /// <summary>
        /// 1632'de bilinen bir yeni ay: 20 Ocak 1632 (Jülyen takvimi,
        /// yılın 20. günü). Sayım buradan yürür.
        /// </summary>
        public const int ReferansYeniAy = 20;

        private void Awake()
        {
            if (zaman == null) zaman = FindAnyObjectByType<ZamanSistemi>();
        }

        private void LateUpdate()
        {
            // GUNESTEN SONRA: `ZamanSistemi.Yenile` gunesi Update'te
            // suruyor ve ay onun acisindan tureiyor. Ayni karede once
            // ay hesaplanirsa bir kare geriden gelir ve gun batiminda
            // ay yanlis yerde durur.
            if (zaman == null) return;
            Uygula(zaman.yilinGunu, zaman.saat);
        }

        /// <summary>
        /// Ay evresi 0..1: 0 yeni ay, 0,5 dolunay, 1 yine yeni ay.
        /// Saf ve statik — test sahne kurmadan sorabilsin diye.
        /// </summary>
        public static double Evre(int yilinGunu)
        {
            double g = (yilinGunu - ReferansYeniAy) % SinodikAy;
            if (g < 0) g += SinodikAy;
            return g / SinodikAy;
        }

        /// <summary>
        /// Evrenin aydınlatma çarpanı 0..1. Dolunayda 1, yeni ayda 0.
        ///
        /// Doğrusal değil: ayın parlaklığı evreyle doğrusal artmaz,
        /// dolunaya yakın hızla yükselir (ters ışık saçılması). Yarım
        /// ay, dolunayın yarısı kadar değil <b>onda biri</b> kadar
        /// aydınlatır — bu ölçülmüş bir gökbilim gerçeği, oyun ayarı
        /// değil. Üs bunu kabaca taklit ediyor.
        /// </summary>
        public static float Aydinlik(double evre)
        {
            // Dolunaya uzaklik: 0 = dolunay, 1 = yeni ay.
            double uzak = Math.Abs(evre - 0.5) * 2.0;
            double aydinlanan = 1.0 - uzak;              // 0..1
            return (float)Math.Pow(aydinlanan, 2.2);
        }

        /// <summary>Ayı bugünün tarihine ve saatine göre kurar.</summary>
        public void Uygula(int yilinGunu, float saat)
        {
            double evre = Evre(yilinGunu);
            float ay = Aydinlik(evre);

            // AYIN KONUMU: gunesin saat acisindan evre kadar kaymis.
            // Dolunay (evre 0,5) gunesin 180 derece karsisinda; yeni ay
            // (evre 0) gunesle ayni yerde.
            double aySaati = saat + 24.0 * evre;
            if (aySaati >= 24.0) aySaati -= 24.0;

            double d = VakitHesabi.Sapma(yilinGunu);
            double phi = VakitHesabi.IstanbulEnlem * Mathf.Deg2Rad;
            double h = (aySaati - 12.0) * 15.0 * Mathf.Deg2Rad;

            double sinAlt = Math.Sin(phi) * Math.Sin(d)
                          + Math.Cos(phi) * Math.Cos(d) * Math.Cos(h);
            double alt = Math.Asin(Math.Clamp(sinAlt, -1.0, 1.0));

            double cosAz = (Math.Sin(d) - Math.Sin(alt) * Math.Sin(phi))
                         / (Math.Cos(alt) * Math.Cos(phi));
            double az = Math.Acos(Math.Clamp(cosAz, -1.0, 1.0)) * Mathf.Rad2Deg;
            if (h > 0) az = 360.0 - az;

            transform.rotation = Quaternion.Euler(
                (float)(alt * Mathf.Rad2Deg), (float)(az + 180.0), 0f);

            // GUNDUZ AY DA GORUNUR AMA ISIK VERMEZ: gunes 0,25 luks'un
            // yuz bin kati. Gunduz aciksa yalnizca kare suresi yer.
            bool gunduz = zaman != null && !zaman.Gece;
            bool ufkunAltinda = alt < -0.05;

            float siddet = tabanLuks + (dolunayLuks - tabanLuks) * ay;
            if (ufkunAltinda) siddet = tabanLuks;   // gok parildisi kalir

            var isik = Isik;
            isik.intensity = gunduz ? 0f : siddet;
            isik.enabled = !gunduz;

            // Ay isigi MAVIDIR — ama gokyuzu mavi oldugu icin degil.
            // Purkinje kaymasi: az isikta goz cubuklarla gorur ve
            // cubuklar maviye kayik duyarlidir. Sinemanin "gece mavisi"
            // bir uzlasim degil, gozun kendi cevabi.
            isik.color = new Color(0.62f, 0.72f, 1.00f);
            isik.shadows = LightShadows.Soft;
        }
    }
}
