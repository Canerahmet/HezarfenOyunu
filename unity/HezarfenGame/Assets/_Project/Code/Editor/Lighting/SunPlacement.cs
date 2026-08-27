using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Hezarfen.Editor.Lighting
{
    /// <summary>
    /// Güneşi <b>tarih ve saatten</b> yerleştirir — elle döndürülmüş bir açıdan
    /// değil.
    ///
    /// ## Neden bu dosya var
    ///
    /// Sahnedeki güneş **imkânsız bir yerdeydi**: ışık 205°'ye doğru yol
    /// alıyordu, yani güneş 25° azimutta — kuzeykuzeydoğuda. 41° kuzey
    /// enleminde güneş oraya hiçbir gün, hiçbir saat gelmez. Yükseklik (42°)
    /// makuldü ve kimse alt tarafını sorgulamadı; gölgeler "bir yöne" düşüyordu
    /// ve kare makul görünüyordu.
    ///
    /// Bu, tam da projenin kaçınmaya çalıştığı hata türü: gözle doğrulanan,
    /// ölçülmeyen bir sayı. Güneş artık bir <b>hesabın çıktısı</b>:
    /// enlem/boylam + tarih + saat → yükseklik ve azimut. Yanlış bir güneş
    /// kurmak artık yanlış bir TARİH yazmayı gerektirir, ki o da göze çarpar.
    ///
    /// ## Seçilen an ve gerekçesi
    ///
    /// Uçuşun günü kaynaklarda yok (RESEARCH.md §4.4(f)). Mevsim **ilkbahar**
    /// seçildi (ADR 0025): oyunun birinci tasarım direği lodostur ve lodos
    /// yılın soğuk yarısının rüzgârıdır; yaz sonuna hâkim olan poyraz
    /// kuzeydoğudandır.
    ///
    /// 1 Mayıs, güneş saati <b>15:00</b> → yükseklik <b>43,2°</b>, azimut
    /// <b>249,6°</b> (batı-güneybatı). Yükseklik eskisinin neredeyse aynısı
    /// (42°), yani aydınlatma kalibrasyonu (ADR 0023, poz 13,0 EV) korunur;
    /// değişen yalnızca pusula yönüdür.
    ///
    /// ## Saat neden ÖĞLEDEN SONRA
    ///
    /// Aynı yüksekliği sabah 09:00 da verir (azimut 110°, doğu-güneydoğu) —
    /// ve bu, oyunun ana uçuşu için yanlış taraftır. Hezarfen Galata'dan
    /// Üsküdar'a, yani **doğuya** uçar; sabah güneşi bütün uçuş boyunca
    /// gözüne gelir ve önündeki şehir kontr-ışıkta silüete iner. Öğleden
    /// sonra güneş arkada kalır: hedef kıyı, Kız Kulesi ve iniş alanı önde
    /// ve aydınlıktır.
    ///
    /// Lodosla da uyumlu: rüzgâr güneybatıdan, güneş batı-güneybatıdan.
    /// İkisi de arkadan gelir.
    /// </summary>
    public static class SunPlacement
    {
        // Dünya orijini: Galata Kulesi tabanı (ADR 0007).
        public const double LatitudeDeg = 41.025637;
        public const double LongitudeDeg = 28.974017;

        /// <summary>Seçilen an — yıl gün sayısı (1 Mayıs) ve güneş saati.</summary>
        public const int DayOfYear = 121;       // 1 Mayıs
        public const float SolarHour = 15.0f;   // güneş saati (öğle = 12)

        public const string SunName = "SUN_Directional";

        [MenuItem("Hezarfen/Aydinlatma/Gunesi tarihten yerlestir")]
        public static void PlaceMenu()
        {
            var sun = Find();
            if (sun == null)
            {
                Debug.LogError($"[Hezarfen] {SunName} bulunamadi.");
                return;
            }

            Solar(DayOfYear, SolarHour, LatitudeDeg, out double alt, out double azi);
            Apply(sun, alt, azi);

            Debug.Log($"[Hezarfen] Gunes yerlestirildi: 1 Mayis, gunes saati "
                      + $"{SolarHour:F1} -> yukseklik {alt:F1} derece, azimut {azi:F1} derece. "
                      + $"Isik yonu Y = {(azi + 180.0) % 360.0:F1}.");
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        public static Light Find()
        {
            foreach (var l in UnityEngine.Object.FindObjectsByType<Light>())
                if (l.type == LightType.Directional && l.name == SunName) return l;
            foreach (var l in UnityEngine.Object.FindObjectsByType<Light>())
                if (l.type == LightType.Directional && l.shadows != LightShadows.None)
                    return l;
            return null;
        }

        public static void Apply(Light sun, double altDeg, double aziDeg)
        {
            // Unity'de yönlü ışığın forward'ı ışığın YOL ALDIĞI yöndür; güneş
            // azimutunun tam KARŞISI. Bu işareti ters yazmak, güneşi gökyüzünün
            // öbür ucuna koyar ve fark yalnızca gölge yönünden anlaşılır.
            sun.transform.rotation = Quaternion.Euler((float)altDeg,
                                                      (float)((aziDeg + 180.0) % 360.0),
                                                      0f);
        }

        /// <summary>
        /// Güneşin yüksekliği ve azimutu (derece; azimut kuzeyden doğuya).
        ///
        /// Standart küresel astronomi; deklinasyon Cooper yaklaşımıyla
        /// (yıl içinde ±0,5° hata — bir oyun sahnesi için fazlasıyla yeterli,
        /// ve önemli olan zaten <b>mümkün</b> bir konum üretmesi).
        /// </summary>
        public static void Solar(int dayOfYear, double solarHour, double latDeg,
                                 out double altDeg, out double aziDeg)
        {
            double dec = 23.45 * Math.Sin(2.0 * Math.PI * (284 + dayOfYear) / 365.0)
                         * Math.PI / 180.0;
            double lat = latDeg * Math.PI / 180.0;
            double H = (solarHour - 12.0) * 15.0 * Math.PI / 180.0;   // saat açısı

            double sinAlt = Math.Sin(lat) * Math.Sin(dec)
                            + Math.Cos(lat) * Math.Cos(dec) * Math.Cos(H);
            sinAlt = Math.Clamp(sinAlt, -1.0, 1.0);
            double alt = Math.Asin(sinAlt);

            double cosAz = (Math.Sin(dec) - sinAlt * Math.Sin(lat))
                           / Math.Max(Math.Cos(alt) * Math.Cos(lat), 1e-9);
            double az = Math.Acos(Math.Clamp(cosAz, -1.0, 1.0));

            altDeg = alt * 180.0 / Math.PI;
            // Öğleden ÖNCE güneş doğuda, sonra batıda. `Acos` bu ayrımı
            // yapamaz (0-180 döner); saat açısının işareti yapar.
            aziDeg = H < 0 ? az * 180.0 / Math.PI : 360.0 - az * 180.0 / Math.PI;
        }

        /// <summary>
        /// Verilen azimut, bu enlemde yıl boyunca hiç görülebilir mi?
        ///
        /// Güneşin azimutu gün doğumunda en kuzeye ulaşır; o an yüksekliği 0
        /// olduğuna göre <c>cos(A) = sin(δ)/cos(φ)</c>. En büyük deklinasyon
        /// (+23,45°) en kuzey gün doğumunu verir. Bunun kuzeyinde kalan hiçbir
        /// azimutta güneş bulunamaz.
        /// </summary>
        public static double NorthernmostAzimuth(double latDeg)
        {
            double lat = latDeg * Math.PI / 180.0;
            double c = Math.Sin(23.45 * Math.PI / 180.0) / Math.Cos(lat);
            return Math.Acos(Math.Clamp(c, -1.0, 1.0)) * 180.0 / Math.PI;
        }

        /// <summary>
        /// Güneşin verilen yüksekliğe indiği <b>öğleden sonraki</b> güneş saati.
        ///
        /// Neden hesaplanıyor, elle yazılmıyor: "gün batımı" bir saat değil bir
        /// <b>yükseklik</b>tir ve tarihe göre kayar. Sabit bir saat yazsaydım,
        /// mevsim kararı (ADR 0025) bir gün değiştiğinde inceleme paketi
        /// sessizce başka bir ışıkta üretilirdi.
        ///
        /// Güneşin ufka <b>değdiği</b> an (0°) kare için işe yaramaz: ışık
        /// neredeyse yok ve sahne kendi gölgesine iner. Çağıran, yükseklik
        /// ister; ikili arama onu verir. Öğle ile gece yarısı arasında
        /// yükseklik tekdüze azalır, arama bu yüzden güvenlidir.
        /// </summary>
        public static float AfternoonHourAtAltitude(int dayOfYear, double latDeg,
                                                    double targetAltDeg)
        {
            double lo = 12.0, hi = 24.0;
            for (int i = 0; i < 60; i++)
            {
                double mid = 0.5 * (lo + hi);
                Solar(dayOfYear, mid, latDeg, out double alt, out _);
                if (alt > targetAltDeg) lo = mid; else hi = mid;
            }
            return (float)(0.5 * (lo + hi));
        }

        /// <summary>Işığın Y dönüşünden güneşin azimutunu geri okur.</summary>
        public static double AzimuthOf(Light sun) =>
            (sun.transform.eulerAngles.y + 180.0) % 360.0;
    }
}
