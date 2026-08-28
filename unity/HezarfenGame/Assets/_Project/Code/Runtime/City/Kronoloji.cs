using UnityEngine;

namespace Hezarfen.Sehir
{
    /// <summary>
    /// <b>Dönemin eşikleri.</b> Aynı şehir, aynı çizelge, farklı yıl —
    /// farklı davranış.
    ///
    /// Oyun 1631'den 1633+'a uzanıyor ve bu aralıkta İstanbul'un günlük
    /// hayatını değiştiren belgeli olaylar var. Onları bir yerde toplamak
    /// bir kolaylık değil bir <b>doğruluk aracı</b>: NPC rutini "kahvehaneye
    /// git" derken kahvehanenin o tarihte açık olup olmadığını sormak
    /// zorunda, yoksa oyun 1634'te yıkılmış bir binaya adam gönderir.
    ///
    /// Tarihler <c>docs/RESEARCH.md</c> §6'dan; her biri ferman ya da
    /// arşiv kaydına dayanıyor.
    /// </summary>
    public static class Kronoloji
    {
        /// <summary>
        /// <b>Kahvehane yasağı: 2 Eylül 1633.</b>
        ///
        /// TDV "Kahve": 20 Safer 1043'te (26 Ağustos 1633) Cibali'de
        /// başlayan yangının kahvehanelerde tütün içenler yüzünden çıktığı
        /// haberi üzerine, 27 Safer 1043 (**2 Eylül 1633**) tarihli
        /// fermanla kahvehaneler kapatıldı; yalnız Eyüp ve civarında 120
        /// kahve dükkânı yıktırıldı (BA, A.DVN, nr. 25/47). Yasak IV.
        /// Murad'ın ölümüne (1640) kadar sert uygulandı.
        ///
        /// Oyunun çekirdek yılı 1632'de kahvehaneler <b>açık</b>; bu,
        /// kronolojideki kritik eşiktir.
        /// </summary>
        public const int KahveYasagiYil = 1633;

        /// <summary>2 Eylül = yılın 245. günü.</summary>
        public const int KahveYasagiGun = 245;

        /// <summary>
        /// <b>Cibali yangını: 26 Ağustos 1633.</b> Bir gemi kalafatçısının
        /// ateşinden çıktı. Kâtip Çelebi şehrin beşte birinin yandığını
        /// yazar (başka kaynaklar dörtte biri/beşte dördü der — oran
        /// tartışmalı, olayın kendisi değil).
        ///
        /// <b>Tulumba 1632'de YOK</b>: ilk teşkilat 1720'lerdedir. Söndürme
        /// yöntemi yıkıcılarla ateş hattı kesmek, su taşımak, bina yıkmak.
        /// </summary>
        public const int CibaliYanginiYil = 1633;

        /// <summary>26 Ağustos = yılın 238. günü.</summary>
        public const int CibaliYanginiGun = 238;

        /// <summary>Kahvehaneler bu tarihte açık mı.</summary>
        public static bool KahvehaneAcik(int yil, int yilinGunu)
        {
            if (yil < KahveYasagiYil) return true;
            if (yil > KahveYasagiYil) return false;
            return yilinGunu < KahveYasagiGun;
        }

        /// <summary>
        /// Gece fenersiz dolaşmak yasak mı.
        ///
        /// RESEARCH §6: *"geceleri fenersiz dolaşmak yasak"* — kolluk
        /// (subaşı, asesbaşı, yeniçeri) bunu uygular. Yasak IV. Murad'ın
        /// sıkı dönemiyle belirginleşir; oyun çerçevesinde kahve/tütün
        /// yasağıyla <b>aynı sertleşmenin</b> parçası sayılıyor.
        ///
        /// Bu bir T2 okuması: yasağın kendisi belgeli, kesin başlangıç
        /// tarihi değil. O yüzden aynı eşiğe bağlandı ve <b>burada
        /// yazıyor</b> — sessizce varsayılmadı.
        /// </summary>
        public static bool FenerZorunlu(int yil, int yilinGunu)
            => !KahvehaneAcik(yil, yilinGunu);

        /// <summary>Bu tarihi okunur bir metne çevirir — HUD ve kodeks.</summary>
        public static string Tarih(int yil, int yilinGunu)
        {
            int[] gun = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
            string[] ay = { "Ocak", "Şubat", "Mart", "Nisan", "Mayıs",
                            "Haziran", "Temmuz", "Ağustos", "Eylül",
                            "Ekim", "Kasım", "Aralık" };
            int g = Mathf.Clamp(yilinGunu, 1, 365);
            for (int i = 0; i < 12; i++)
            {
                if (g <= gun[i]) return $"{g} {ay[i]} {yil}";
                g -= gun[i];
            }
            return $"{yilinGunu}. gün {yil}";
        }
    }
}
