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

        /// <summary>Artık yıl mı (Gregoryen kural).</summary>
        public static bool ArtikYil(int yil)
            => (yil % 4 == 0 && yil % 100 != 0) || yil % 400 == 0;

        /// <summary>O yılın gün sayısı.</summary>
        public static int YilUzunlugu(int yil) => ArtikYil(yil) ? 366 : 365;

        /// <summary>Ay/gün → yılın kaçıncı günü.</summary>
        public static int YilinGunu(int yil, int ay, int gun)
        {
            int[] u = AyUzunluklari(yil);
            int n = gun;
            for (int i = 0; i < ay - 1 && i < 12; i++) n += u[i];
            return n;
        }

        private static int[] AyUzunluklari(int yil) => new[]
        { 31, ArtikYil(yil) ? 29 : 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

        /// <summary>
        /// Bu tarihi okunur bir metne çevirir — HUD ve kodeks.
        ///
        /// <b>Artık yıl sayılır.</b> Önce sabit 365 günlük bir tablo
        /// vardı ve 1632 artık yıl olduğu için mayıstan sonraki her tarih
        /// bir gün kayıyordu — sessizce, çünkü kimse "13 Ağustos mu 14
        /// Ağustos mu" diye bakmaz. Oysa oyunun kronolojik eşikleri gün
        /// hassasiyetinde (2 Eylül 1633 fermanı).
        /// </summary>
        public static string Tarih(int yil, int yilinGunu)
        {
            int[] u = AyUzunluklari(yil);
            string[] ay = { "Ocak", "Şubat", "Mart", "Nisan", "Mayıs",
                            "Haziran", "Temmuz", "Ağustos", "Eylül",
                            "Ekim", "Kasım", "Aralık" };
            int g = Mathf.Clamp(yilinGunu, 1, YilUzunlugu(yil));
            for (int i = 0; i < 12; i++)
            {
                if (g <= u[i]) return $"{g} {ay[i]} {yil}";
                g -= u[i];
            }
            return $"{yilinGunu}. gün {yil}";
        }

        /// <summary>Haftanın günleri — Cuma oyunun ritmindeki tek özel gün.</summary>
        public enum Gun { Pazar, Pazartesi, Sali, Carsamba, Persembe, Cuma, Cumartesi }

        /// <summary>
        /// Haftanın günü — <b>gerçek gün sayımıyla</b>.
        ///
        /// Çapa: <b>1 Ocak 1632 Perşembe</b>. Buradan itibaren gerçek gün
        /// sayılır (artık yıllar dahil), yani oyunun içindeki Cuma tarihin
        /// Cumasıdır. Kaydın doğruladığı bir ayrıntı da buradan çıkıyor:
        /// <b>Cibali yangını (26 Ağustos 1633) da kahvehane fermanı
        /// (2 Eylül 1633) da Cuma günüdür</b> — bir hafta arayla, iki
        /// Cuma.
        /// </summary>
        public static Gun HaftaGunu(int yil, int yilinGunu)
        {
            long gun = 0;
            if (yil >= 1632)
                for (int y = 1632; y < yil; y++) gun += YilUzunlugu(y);
            else
                for (int y = yil; y < 1632; y++) gun -= YilUzunlugu(y);
            gun += yilinGunu - 1;
            // 1 Ocak 1632 = Persembe (index 4).
            int i = (int)(((gun + 4) % 7 + 7) % 7);
            return (Gun)i;
        }

        /// <summary>Cuma mı — namaz selâtin camisine akar.</summary>
        public static bool Cuma(int yil, int yilinGunu)
            => HaftaGunu(yil, yilinGunu) == Gun.Cuma;
    }
}
