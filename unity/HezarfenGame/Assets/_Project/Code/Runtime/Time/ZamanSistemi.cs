using System;
using UnityEngine;

namespace Hezarfen.Zaman
{
    /// <summary>
    /// <b>Şehrin saati.</b> NPC rutinleri, ases devriyesi, fener
    /// zorunluluğu ve güneşin kendisi buradan okur.
    ///
    /// Plan Bölüm 11.1: *"gün döngüsü 5 vakit ezanla yapılanır"*. Yani
    /// zaman burada bir sayaç değil, bir **yapı**: gün beş vakte
    /// bölünmüştür ve şehir o bölünmeye göre yaşar. Kepenk öğle
    /// ezanıyla açılmaz, ikindiyle kapanmaz — vakitler şehrin ritmidir.
    ///
    /// ## Tek doğruluk kaynağı
    ///
    /// Güneşin açısı, gece bayrağı, hangi vakitteyiz — hepsi
    /// <b>tek bir</b> saatten türer. Işık sistemi kendi güneşini, NPC
    /// kendi saatini tutsaydı ikisi kaçınılmaz olarak ayrışırdı ve
    /// oyuncu güpegündüz ases devriyesine yakalanırdı. Bu projede aynı
    /// sayının iki sahibi olduğu her yerde er ya da geç iki değeri oldu.
    ///
    /// ## Tarih neden 1 Mayıs 1632
    ///
    /// Uçuşun günü kaynaklarda yok; mevsim SEÇİLDİ (T3, Caner
    /// 2026-08-21, ADR 0025): ilkbahar, çünkü oyunun birinci tasarım
    /// direği lodostur ve lodos yılın soğuk yarısının rüzgârıdır.
    /// İnceleme render'ları o günün 15:00'ini kullanır; oyun saati ise
    /// akar — ama aynı günden başlar ki ikisi aynı şehri göstersin.
    /// </summary>
    [DisallowMultipleComponent]
    public class ZamanSistemi : MonoBehaviour
    {
        [Header("Takvim")]
        [Tooltip("Miladî yıl. Oyun 1631'de başlar, 1633+'a uzanır.")]
        public int yil = 1632;

        [Tooltip("Yılın kaçıncı günü (1-365). 121 = 1 Mayıs — ADR 0025.")]
        [Range(1, 365)] public int yilinGunu = 121;

        [Header("Saat")]
        [Tooltip("Gerçek güneş saati (0-24). Öğle tanım gereği 12:00.")]
        [Range(0f, 24f)] public float saat = 15f;

        [Tooltip("Bir oyun günü kaç gerçek dakika sürer. 0 = zaman durur " +
                 "(inceleme ve test için).")]
        public float gunDakika = 24f;

        [Header("Güneş")]
        [Tooltip("Boşsa sahnedeki ilk yönlü ışık aranır.")]
        public Light gunesIsigi;

        [Tooltip("Güneşi bu bileşen sürsün mü. Kapalıysa ışık elle " +
                 "ayarlanır — inceleme render'ları böyle çalışır.")]
        public bool gunesiSur = true;

        /// <summary>Bugünün vakitleri — gün değişince yeniden hesaplanır.</summary>
        public VakitHesabi.Gun Bugun { get; private set; }

        /// <summary>Şu an içinde bulunduğumuz vakit.</summary>
        public VakitHesabi.Vakit Vakit { get; private set; }

        /// <summary>Gece mi — fener zorunluluğu ve ases buna bakar.</summary>
        public bool Gece => VakitHesabi.Gece(Bugun, saat);

        /// <summary>Ezanî saat (gün batımı = 12:00).</summary>
        public double EzaniSaat => VakitHesabi.Ezani(saat, Bugun.aksam);

        /// <summary>Ezanî saatin "3:24" biçimi — HUD bunu gösterir.</summary>
        public string EzaniYazi => VakitHesabi.EzaniYazi(EzaniSaat);

        /// <summary>Vakit değişince tetiklenir — ezan, NPC rutini, ases.</summary>
        public event Action<VakitHesabi.Vakit> VakitGirdi;

        /// <summary>Gün değişince tetiklenir (batıştan sonra, ezanî gün başı).</summary>
        public event Action<int> GunDegisti;

        private int _hesaplananGun = -1;

        private void Awake() => Yenile();

        private void OnValidate()
        {
            if (!Application.isPlaying) Yenile();
        }

        private void Update()
        {
            if (gunDakika > 0.001f)
            {
                // Oyun gunu = `gunDakika` gercek dakika. 24 saat / (d*60 s).
                saat += Time.deltaTime * 24f / (gunDakika * 60f);
                while (saat >= 24f)
                {
                    saat -= 24f;
                    yilinGunu = yilinGunu % 365 + 1;
                    if (yilinGunu == 1) yil++;
                    GunDegisti?.Invoke(yilinGunu);
                }
            }
            Yenile();
        }

        /// <summary>Vakitleri, vakit geçişini ve güneşi günceller.</summary>
        public void Yenile()
        {
            if (yilinGunu != _hesaplananGun)
            {
                Bugun = VakitHesabi.Hesapla(yilinGunu);
                _hesaplananGun = yilinGunu;
            }

            var yeni = VakitHesabi.SuAnki(Bugun, saat);
            if (yeni != Vakit)
            {
                Vakit = yeni;
                VakitGirdi?.Invoke(yeni);
            }

            if (gunesiSur) GunesiYerlestir();
        }

        /// <summary>
        /// Güneşi gerçek konumuna koyar.
        ///
        /// Yükseklik ve azimut, vakitlerle **aynı** sapmadan türer. Işığı
        /// ayrı bir eğriyle sürmek, güneşin battığı an ile akşam ezanının
        /// okunduğu anın ayrışması demekti — ve o iki an aynı olmak
        /// zorunda, çünkü ezanî saatin sıfır noktası odur.
        /// </summary>
        private void GunesiYerlestir()
        {
            if (gunesIsigi == null)
            {
                foreach (var l in FindObjectsByType<Light>(FindObjectsInactive.Exclude))
                    if (l.type == LightType.Directional) { gunesIsigi = l; break; }
                if (gunesIsigi == null) return;
            }

            double d = VakitHesabi.Sapma(yilinGunu);
            double phi = VakitHesabi.IstanbulEnlem * Mathf.Deg2Rad;
            double h = (saat - 12.0) * 15.0 * Mathf.Deg2Rad;   // saat acisi

            double sinAlt = Math.Sin(phi) * Math.Sin(d)
                          + Math.Cos(phi) * Math.Cos(d) * Math.Cos(h);
            double alt = Math.Asin(Math.Clamp(sinAlt, -1.0, 1.0));

            double cosAz = (Math.Sin(d) - Math.Sin(alt) * Math.Sin(phi))
                         / (Math.Cos(alt) * Math.Cos(phi));
            double az = Math.Acos(Math.Clamp(cosAz, -1.0, 1.0)) * Mathf.Rad2Deg;
            if (h > 0) az = 360.0 - az;        // ogleden sonra bati yarisinda

            // ISIK GUNESIN BULUNDUGU YERE DEGIL, ORADAN GELDIGI YONE BAKAR.
            //
            // Yonlu isigin `forward`u isigin GITTIGI yondur. Gunes azimut
            // `az`de duruyorsa isik `az + 180`e dogru gider. Once `az`
            // yaziliyordu ve bu, gunesi tam 180 derece TERS yerlestirdi:
            // sahne 1632'nin 122. gunu, saat 09:00'da basliyor, gercek
            // azimut 110 (dogu-guneydogu) — ama golgeler gunesin BATI-
            // KUZEYBATIDA oldugunu soyluyordu. Sabah gunesi batidan
            // doguyordu ve hicbir test bunu okumuyordu.
            gunesIsigi.transform.rotation = Quaternion.Euler(
                (float)(alt * Mathf.Rad2Deg), (float)(az + 180.0), 0f);
            gunesIsigi.enabled = alt > -0.10;   // ufkun cok altinda kapan
        }

        /// <summary>Saati doğrudan bir vakte kurar — test ve sahne kurulumu.</summary>
        public void VakteAtla(VakitHesabi.Vakit v)
        {
            if (yilinGunu != _hesaplananGun)
            {
                Bugun = VakitHesabi.Hesapla(yilinGunu);
                _hesaplananGun = yilinGunu;
            }
            saat = (float)Bugun[v] + 0.02f;    // vaktin hemen icine
            Yenile();
        }
    }
}
