using Hezarfen.Zaman;
using UnityEngine;

namespace Hezarfen.Sehir
{
    /// <summary>
    /// <b>Rutinin tek sahibi:</b> `(meslek, vakit, tohum, yıl, gün) → hedef`.
    ///
    /// ## Neden ayrı bir sınıf
    ///
    /// <see cref="NPCMeslek.Hedef"/> çizelgeyi okur ve orada durur — bu
    /// doğru, çünkü çizelge bir veridir ve takvimi bilmesi gerekmez. Ama
    /// hedefin **takvime göre değişmesi** gerekir: kahvehane kapalıysa
    /// oraya kimse gitmez, Cuma günü öğle namazı mescide değil camiye
    /// akar.
    ///
    /// Bu düzeltmeler önce iki ayrı yerde yazılıydı —
    /// <see cref="SehirGunu"/> (simülasyon) ve <c>NPCYonetici</c> (canlı
    /// şehir) — ve her biri kuralın kendi kopyasını taşıyordu.
    /// <b>Bir sayının iki sahibi varsa er ya da geç iki değeri olur:</b>
    /// simülasyonun ölçtüğü gün ile oyuncunun yürüdüğü gün ayrışırdı ve
    /// bu ayrışma sessiz olurdu, çünkü testler simülasyona bakar.
    ///
    /// Artık ikisi de buraya sorar. Ölçülen gün, oynanan gündür.
    ///
    /// ## Hâlâ SAF
    ///
    /// Ajanın durumu yok; hedef yalnızca vakitten, tohumdan ve takvimden
    /// çıkar. Yani şehrin bütün bir günü hiç çizilmeden simüle edilip
    /// sayılabilir (ADR 0070).
    /// </summary>
    public static class Rutin
    {
        /// <summary>
        /// Bu kişi bu vakitte nereye gider — takvim uygulanmış hâliyle.
        /// </summary>
        public static SokakGrafi.Tur Hedef(NPCMeslek meslek,
                                           VakitHesabi.Vakit v,
                                           int tohum, int yil, int gun)
        {
            if (meslek == null) return SokakGrafi.Tur.Ev;
            var hedef = meslek.Hedef(v, tohum);

            // --- CUMA (ADR 0071) ---------------------------------------
            //
            // Cuma namazi mahalle mescidinde KILINMAZ: minberi olan bir
            // camide, cemaatle kilinir. Cuma iki sey yapar — toplar ve
            // cogaltir.
            if (v == VakitHesabi.Vakit.Ogle && Kronoloji.Cuma(yil, gun))
            {
                if (hedef == SokakGrafi.Tur.Mescit) return SokakGrafi.Tur.Cami;

                if (Cekilir(meslek, v, SokakGrafi.Tur.Mescit,
                            Olaylar.CumaKatsayisi, tohum, 7717))
                    return SokakGrafi.Tur.Cami;
            }

            // --- CARSI SABAHI -----------------------------------------
            //
            // Gunes vakti kepenkler acilir ve yuk iskeleden carsiya akar;
            // esnafin gunu burada baslar. Cuma ogle vaktindedir, bu gunes
            // vaktinde — ikisi ayni kisiyi ayni anda cekmez.
            if (Olaylar.PazarVar(v)
                && hedef != SokakGrafi.Tur.Dukkan
                && Cekilir(meslek, v, SokakGrafi.Tur.Dukkan,
                           Olaylar.PazarKatsayisi, tohum, 5501))
                return SokakGrafi.Tur.Dukkan;

            // --- KRONOLOJI ---------------------------------------------
            //
            // Kapali bir binaya kimse gitmez. 2 Eylul 1633 fermanindan
            // sonra kahvehane yok; o hedefi secen kisi eve doner.
            if (hedef == SokakGrafi.Tur.Kahvehane
                && !Kronoloji.KahvehaneAcik(yil, gun))
                return SokakGrafi.Tur.Ev;

            return hedef;
        }

        /// <summary>
        /// Bu kişi bu vakitte dışarıda mı — gece ölçümü buna bakar.
        ///
        /// Cuma camiye giden kişi <b>dışarıdadır</b>: mahalleden camiye
        /// yürünür ve avluda beklenir. Zaten Cuma'nın görünür yanı da bu
        /// — sokakların öğleye doğru tek yöne dolması.
        /// </summary>
        public static bool Disarida(NPCMeslek meslek, VakitHesabi.Vakit v,
                                    int tohum, int yil, int gun)
        {
            if (meslek == null) return false;
            var hedef = Hedef(meslek, v, tohum, yil, gun);
            if (hedef == SokakGrafi.Tur.Cami) return true;
            if (hedef == SokakGrafi.Tur.Ev) return false;
            return meslek.Disarida(v, tohum);
        }

        /// <summary>
        /// Bu kişi, temel hedefi o olmadığı hâlde <paramref name="tur"/>'e
        /// çekiliyor mu.
        ///
        /// Temel payı `p` olan bir hedefte olay günü payın `p·k` olmasını
        /// istiyoruz. Oraya gitmeyenlerin `q = p(k−1)/(1−p)` kadarı da o
        /// gün gider; sonuç `p + (1−p)q = p·k`. Katsayı <b>tek bir yerde</b>
        /// yazılı ve ölçüm onu doğruluyor — elle ayarlanmış bir sayı değil.
        /// </summary>
        private static bool Cekilir(NPCMeslek meslek, VakitHesabi.Vakit v,
                                    SokakGrafi.Tur tur, float katsayi,
                                    int tohum, int tuz)
        {
            float p = meslek.Olasilik(v, tur);
            if (p <= 0f || p >= 1f) return false;
            float q = p * (katsayi - 1f) / (1f - p);
            return Zar(tohum, tuz) < q;
        }

        /// <summary>Deterministik zar: aynı (tohum, tuz) hep aynı sayı.</summary>
        private static float Zar(int tohum, int tuz)
        {
            uint h = (uint)(tohum * 2654435761u + tuz * 2246822519u);
            h ^= h >> 13; h *= 3266489917u; h ^= h >> 16;
            return (h & 0xFFFFFF) / (float)0x1000000;
        }
    }
}
