using Hezarfen.Zaman;

namespace Hezarfen.Sehir
{
    /// <summary>
    /// <b>1632 İstanbul'unda suç neyse o.</b>
    ///
    /// Bu listenin uzunluğu değil <b>içeriği</b> önemli: hiçbiri şiddet
    /// içermiyor ve hiçbiri modern bir suç değil. Oyuncu kimseyi
    /// soymuyor, dövmüyor; <b>yasak bir yerde, yasak bir saatte, yasak
    /// bir şeyle</b> bulunuyor. Dönemin kolluk kaydı da böyle: subaşı ve
    /// ases kavga ayırır, gece dolaşanı sorar, yasak malı alır
    /// (RESEARCH §6).
    ///
    /// Ağırlıklar bir ceza cetveli değil, <b>fark edilme</b> ölçüsüdür:
    /// gece fenersiz dolaşan biri, sur burcuna tırmanandan daha az göze
    /// batar.
    /// </summary>
    public enum Ihlal
    {
        /// <summary>İhlal yok.</summary>
        Yok = 0,

        /// <summary>
        /// <b>Gece fenersiz dolaşmak.</b> RESEARCH §6: *"geceleri fenersiz
        /// dolaşmak yasak"*. Yalnız gece ve yalnız yasak yürürlükteyken
        /// (bkz. <see cref="Kronoloji.FenerZorunlu"/>) ihlaldir — gündüz
        /// fener taşımamak suç değildir.
        /// </summary>
        FenersizGece = 1,

        /// <summary>
        /// <b>Yasak kahve/tütün taşımak.</b> 2 Eylül 1633 fermanından
        /// sonra. Öncesinde kahve serbesttir ve kahvehaneler açıktır —
        /// oyunun kronolojik eşiği tam burada görünür.
        /// </summary>
        YasakMal = 2,

        /// <summary>
        /// <b>Yasak bölgeye tırmanmak.</b> Saray duvarı, sur burçları.
        /// En ağırı: burada mesele saat ya da eşya değil, <b>yer</b>.
        /// </summary>
        YasakBolge = 3,

        /// <summary>
        /// <b>Kolluktan kaçmak.</b> İhlalin kendisi değil, ona verilen
        /// tepkiyi ağırlaştıran şey. Kaçmayan sorulur; kaçan kovalanır.
        /// </summary>
        Kacmak = 4,
    }

    /// <summary>İhlallerin ağırlığı ve dönem kuralları.</summary>
    public static class IhlalKurali
    {
        /// <summary>
        /// Bu ihlal <b>bu tarihte</b> geçerli mi.
        ///
        /// Kronoloji burada bir süs değil: 1632'de kahve taşımak suç
        /// değildir ve oyuncunun aynı davranışı iki yıl sonra suç olur.
        /// Oyunun tarihi anlatmasının en doğrudan yolu bu — metin değil,
        /// aynı eylemin farklı sonucu.
        /// </summary>
        public static bool Gecerli(Ihlal i, int yil, int gun, bool gece)
        {
            return i switch
            {
                Ihlal.Yok => false,
                Ihlal.FenersizGece => gece && Kronoloji.FenerZorunlu(yil, gun),
                Ihlal.YasakMal => !Kronoloji.KahvehaneAcik(yil, gun),
                Ihlal.YasakBolge => true,
                Ihlal.Kacmak => true,
                _ => false,
            };
        }

        /// <summary>
        /// İhlalin <b>fark edilme</b> ağırlığı (0-1).
        ///
        /// Ceza değil dikkat: gece sokakta yürüyen biri, sur burcuna
        /// tırmanandan daha az göze batar. Yakalanınca ne olacağı ayrı
        /// bir soru (<see cref="AranmaSistemi"/>).
        /// </summary>
        public static float Agirlik(Ihlal i) => i switch
        {
            Ihlal.FenersizGece => 0.30f,
            Ihlal.YasakMal => 0.45f,
            Ihlal.YasakBolge => 0.85f,
            Ihlal.Kacmak => 0.60f,
            _ => 0f,
        };

        /// <summary>
        /// Yakalanınca ödenecek akçe.
        ///
        /// Akçe tek para birimidir (RESEARCH §6); Evliya "40 akçe yevmiye
        /// ile sipahi" der, yani günlük yevmiye o mertebede. Cezalar buna
        /// göre ölçeklendi: fenersiz gezmek yarım yevmiye, sur burcuna
        /// tırmanmak bir haftalık.
        ///
        /// <b>T2:</b> narh defterlerinde ceza cetveli yok; oran
        /// yevmiyeden türetildi ve buraya yazıldı.
        /// </summary>
        public static int Ceza(Ihlal i) => i switch
        {
            Ihlal.FenersizGece => 20,
            Ihlal.YasakMal => 60,
            Ihlal.YasakBolge => 280,
            Ihlal.Kacmak => 40,
            _ => 0,
        };

        /// <summary>Yakalanınca taşınan yasak mal alınır mı.</summary>
        public static bool MalaElKonur(Ihlal i) => i == Ihlal.YasakMal;
    }
}
