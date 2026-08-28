using System;
using UnityEngine;

namespace Hezarfen.Zaman
{
    /// <summary>
    /// <b>Namaz vakitleri — tablodan okunmaz, güneşten HESAPLANIR.</b>
    ///
    /// Oyun 1631'den 1633'e uzanan bir takvim taşıyor ve İstanbul'da
    /// vakitler mevsime göre saatlerce kayar: gün uzunluğu kışın 9,2,
    /// yazın 15,1 saat. Sabit bir tablo yazmak, oyunun yılın bir gününde
    /// doğru, geri kalanında yanlış olması demekti.
    ///
    /// Üstelik bu, dönemin kendi işidir. 1632'de vakitleri **muvakkit**
    /// hesaplardı — matematik ve astronomi eğitimli bir devlet görevlisi,
    /// selâtin camisinin avlusundaki muvakkithanede (RESEARCH.md §4.6f).
    /// Yani burada yapılan şey uydurma bir sistem değil, o adamın
    /// yaptığı hesabın aynısı.
    ///
    /// ## Saat GERÇEK GÜNEŞ saatidir — saat dilimi YOKTUR
    ///
    /// 1632'de saat dilimi diye bir şey yok; muvakkit güneşe bakar.
    /// Bu yüzden burada öğle **tanım gereği 12:00**'dir ve zaman
    /// denklemi (equation of time) hiç girmez — o düzeltme ancak ortalama
    /// güneş saatine geçerken gerekir. Modern bir saat dilimi eklemek,
    /// 20. yüzyıl kavramını 17. yüzyıla taşımak olurdu.
    ///
    /// ## Ezanî (alaturka) saat
    ///
    /// Osmanlı günü **gün batımında** başlar: akşam ezanı okununca saat
    /// <b>12:00</b>'ye kurulur ve gün 12'şer saatlik iki yarıma bölünür.
    /// Gün uzunluğu değiştikçe saatler her gün kaydırılır — bu yüzden
    /// muvakkitin işi süreklidir.
    ///
    /// Oyunun göstergesi bunu göstermeli: oyuncunun "gece yarısı"
    /// dediği an ezanî saatte 5-6 civarıdır ve bu yabancılık kasıtlıdır.
    ///
    /// ## İkindi HANEFÎ ölçüsüyle
    ///
    /// Osmanlı İstanbul'u Hanefî'dir: ikindi, cismin gölgesi **iki katı
    /// + öğle gölgesi** olduğunda girer. Şâfiî ölçüsü (bir kat) yaklaşık
    /// 40-50 dakika erken verir. Mezhep seçimi burada bir ayar değil
    /// **tarihî bir olgudur** ve NPC rutinlerinin saatini kaydırır.
    ///
    /// Kaynak: alaturka/ezanî saat ve muvakkit için RESEARCH.md §5.1;
    /// açı değerleri (fecr 18°, yatsı 17°) Diyanet ölçütüdür ve Osmanlı
    /// geleneğinden gelir.
    /// </summary>
    public static class VakitHesabi
    {
        /// <summary>Dünya orijini: Galata Kulesi tabanı (ADR 0007).</summary>
        public const double IstanbulEnlem = 41.025637;

        /// <summary>Fecr (sabah) için güneşin ufuk altı açısı (derece).</summary>
        public const double FecrAcisi = 18.0;

        /// <summary>Yatsı için güneşin ufuk altı açısı (derece).</summary>
        public const double YatsiAcisi = 17.0;

        /// <summary>
        /// Doğuş/batış için görünen yükseklik (derece).
        ///
        /// Sıfır değil: atmosfer ışığı kırar (~34') ve güneş bir nokta
        /// değil bir disktir (~16'). İkisi birlikte güneşi ufkun altındayken
        /// görünür kılar. Sıfır yazmak batışı ~3 dakika erkene alırdı — ve
        /// batış, ezanî saatin SIFIR NOKTASI olduğu için o hata bütün güne
        /// yayılırdı.
        /// </summary>
        public const double UfukDuzeltmesi = -0.833;

        /// <summary>Hanefî ikindi gölge katsayısı. Şâfiî'de 1.</summary>
        public const double IkindiGolgeKati = 2.0;

        /// <summary>Beş vakit + doğuş. Sıra gün içindeki sıradır.</summary>
        public enum Vakit
        {
            /// <summary>Fecr — imsak; gece biter.</summary>
            Sabah = 0,
            /// <summary>Güneş doğar; sabah vakti çıkar.</summary>
            Gunes = 1,
            /// <summary>Zuhr — güneş tepeyi geçince.</summary>
            Ogle = 2,
            /// <summary>Asr — Hanefî gölge ölçüsüyle.</summary>
            Ikindi = 3,
            /// <summary>Mağrib — gün batımı. <b>Ezanî saat burada 12:00.</b></summary>
            Aksam = 4,
            /// <summary>İşâ — şafak tamamen kaybolunca.</summary>
            Yatsi = 5,
        }

        /// <summary>Bir günün altı vakti, gerçek güneş saati cinsinden.</summary>
        [Serializable]
        public struct Gun
        {
            public double sabah, gunes, ogle, ikindi, aksam, yatsi;

            /// <summary>Gün uzunluğu (saat) — doğuştan batışa.</summary>
            public double GunUzunlugu => aksam - gunes;

            public double this[Vakit v] => v switch
            {
                Vakit.Sabah => sabah,
                Vakit.Gunes => gunes,
                Vakit.Ogle => ogle,
                Vakit.Ikindi => ikindi,
                Vakit.Aksam => aksam,
                _ => yatsi,
            };
        }

        /// <summary>
        /// Güneşin sapması (radyan) — yılın kaçıncı gününe göre.
        ///
        /// Ortalama anomali + merkez denklemi kullanılıyor, Cooper'ın
        /// tek terimli yaklaşımı değil: Cooper 0,5°'ye kadar sapar ve
        /// 41° enlemde bu, batışı birkaç dakika kaydırır. Batış ezanî
        /// saatin sıfır noktası olduğu için o hata bütün güne yayılır.
        /// </summary>
        public static double Sapma(int yilinGunu)
        {
            double n = yilinGunu - 1;
            double m = Mod360(357.5291 + 0.98560028 * n) * Mathf.Deg2Rad;
            double c = 1.9148 * Math.Sin(m)
                     + 0.0200 * Math.Sin(2 * m)
                     + 0.0003 * Math.Sin(3 * m);
            double lam = Mod360(280.4665 + 0.98564736 * n + c) * Mathf.Deg2Rad;
            return Math.Asin(Math.Sin(23.4397 * Mathf.Deg2Rad) * Math.Sin(lam));
        }

        /// <summary>
        /// Güneşin verilen yüksekliğe geldiği **saat açısı** (derece).
        ///
        /// Kutup yazı/kışı gibi güneşin o yüksekliğe hiç ulaşmadığı
        /// durumlarda `null` döner. İstanbul'da 41° enlemde bu olmaz ama
        /// fonksiyon enlem alıyor ve sessizce NaN döndürmek, hatayı
        /// vakitlerin içine gömmek olurdu.
        /// </summary>
        public static double? SaatAcisi(double sapmaRad, double yukseklikDeg,
                                        double enlemDeg = IstanbulEnlem)
        {
            double phi = enlemDeg * Mathf.Deg2Rad;
            double a = yukseklikDeg * Mathf.Deg2Rad;
            double c = (Math.Sin(a) - Math.Sin(phi) * Math.Sin(sapmaRad))
                     / (Math.Cos(phi) * Math.Cos(sapmaRad));
            if (c > 1.0 || c < -1.0) return null;
            return Math.Acos(c) * Mathf.Rad2Deg;
        }

        /// <summary>Bir günün bütün vakitleri (gerçek güneş saati).</summary>
        public static Gun Hesapla(int yilinGunu, double enlem = IstanbulEnlem)
        {
            double d = Sapma(yilinGunu);
            var g = new Gun();

            // Ogle: gercek gunes saatinde TANIM GEREGI 12:00. Gelenekte
            // ezan, gunes tepeyi GECTIKTEN sonra okunur; birkac dakikalik
            // pay o yuzden.
            g.ogle = 12.0 + 4.0 / 60.0;

            double? wGunes = SaatAcisi(d, UfukDuzeltmesi, enlem);
            double? wFecr = SaatAcisi(d, -FecrAcisi, enlem);
            double? wYatsi = SaatAcisi(d, -YatsiAcisi, enlem);

            g.gunes = wGunes.HasValue ? 12.0 - wGunes.Value / 15.0 : 6.0;
            g.aksam = wGunes.HasValue ? 12.0 + wGunes.Value / 15.0 : 18.0;
            g.sabah = wFecr.HasValue ? 12.0 - wFecr.Value / 15.0 : g.gunes - 1.2;
            g.yatsi = wYatsi.HasValue ? 12.0 + wYatsi.Value / 15.0 : g.aksam + 1.2;

            // IKINDI (Hanefi): cismin golgesi = 2 x boy + ogle golgesi.
            // Ogle golgesi enlem ile sapmanin farkindan gelir; ekinoksta
            // Istanbul'da zaten cismin boyundan uzundur.
            double golgeOgle = Math.Abs(Math.Tan(enlem * Mathf.Deg2Rad - d));
            double yukseklik = Math.Atan(1.0 / (IkindiGolgeKati + golgeOgle))
                               * Mathf.Rad2Deg;
            double? wIkindi = SaatAcisi(d, yukseklik, enlem);
            g.ikindi = wIkindi.HasValue ? 12.0 + wIkindi.Value / 15.0
                                        : (g.ogle + g.aksam) * 0.5;
            return g;
        }

        /// <summary>
        /// Gerçek güneş saatini **ezanî** saate çevirir.
        ///
        /// Gün batımı 12:00'dir. Bir saat sonra 13:00 (gösterimde 1),
        /// gece yarısı 5-6 civarı. Yabancılık kasıtlı: oyuncunun
        /// saatiyle şehrin saati aynı şey değil.
        /// </summary>
        public static double Ezani(double gunesSaati, double batis)
        {
            double t = (gunesSaati - batis + 12.0) % 24.0;
            return t < 0 ? t + 24.0 : t;
        }

        /// <summary>Ezanî saati "3:24" gibi 12'lik gösterime çevirir.</summary>
        public static string EzaniYazi(double ezani)
        {
            int h = (int)ezani % 12;
            if (h == 0) h = 12;
            int m = (int)Math.Round((ezani - Math.Floor(ezani)) * 60.0);
            if (m == 60) { m = 0; h = h % 12 + 1; }
            return $"{h}:{m:00}";
        }

        /// <summary>
        /// Bu saatte hangi vakit içindeyiz.
        ///
        /// Yatsıdan sonra ve fecrden önce gece vardır; o aralık
        /// <see cref="Vakit.Yatsi"/> sayılır — çünkü vakit "hangi
        /// namazın vakti içindeyiz" sorusunun cevabıdır ve gece boyunca
        /// yatsı vakti sürer.
        /// </summary>
        public static Vakit SuAnki(in Gun g, double gunesSaati)
        {
            double t = gunesSaati;
            if (t < g.sabah) return Vakit.Yatsi;     // gece yarisindan sonra
            if (t < g.gunes) return Vakit.Sabah;
            if (t < g.ogle) return Vakit.Gunes;
            if (t < g.ikindi) return Vakit.Ogle;
            if (t < g.aksam) return Vakit.Ikindi;
            if (t < g.yatsi) return Vakit.Aksam;
            return Vakit.Yatsi;
        }

        /// <summary>Gece mi — fener zorunluluğu ve ases devriyesi buna bakar.</summary>
        public static bool Gece(in Gun g, double gunesSaati)
            => gunesSaati < g.gunes || gunesSaati >= g.aksam;

        private static double Mod360(double x)
        {
            double r = x % 360.0;
            return r < 0 ? r + 360.0 : r;
        }
    }
}
