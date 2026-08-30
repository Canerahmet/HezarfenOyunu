using System.Collections.Generic;
using UnityEngine;

namespace Hezarfen.Tests
{
    /// <summary>
    /// <b>Orta duruş ölçümü — basan ayağın hızı, tek ve ortak tanım.</b>
    ///
    /// Ayak kaymasını ölçmenin zor yanı hareketin kendisi değil,
    /// <b>ayağın ne zaman yerde olduğuna</b> karar vermek. Bu oturumda üç
    /// tanım denendi ve üçü de kendi yoluyla yanıldı:
    ///
    /// | tanım | ne oldu |
    /// |---|---|
    /// | "ayak 6 cm'den alçaksa basıyor" | sallanmanın başı ve sonu o pencereden hızla geçiyor; kayma %15 fazla ölçüldü |
    /// | "alçak ayağın hızının üst çeyreği" | sallanma anları karışıyor; koşuda yürümeden yavaş sonuç |
    /// | "en alçak kotu ayrı bir pencerede bul" | koşuda pencereler örtüşmedi, sıfır örnek |
    ///
    /// Şüpheye yer bırakmayan tek an, ayağın <b>o koşudaki en alçak
    /// noktasına</b> yakın olduğu karelerdir: orada ayak kesinlikle
    /// yerdedir ve orta duruştadır. Eşik, temas, yükseklik tahmini yok.
    ///
    /// Kritik ayrıntı: en alçak kot, ölçümün <b>kendi</b> örneklerinden
    /// bulunur. Ayrı bir pencerede aramak, iki pencere örtüşmediğinde
    /// sessizce sıfır örnek verir — koşuda tam bu oldu.
    ///
    /// Aynı tanım iki yerde kullanılıyor: kapıyı tutan
    /// <see cref="AyakKaymasiTests"/> ve sayıyı üreten
    /// <see cref="KlipYerHiziOlcumu"/>. İki farklı tanım olsaydı biri
    /// ötekini hiçbir zaman doğrulayamazdı.
    /// </summary>
    public static class OrtaDurus
    {
        /// <summary>Bu kadar yukarısı artık orta duruş değildir (m).</summary>
        public const float Pencere = 0.01f;

        /// <summary>
        /// Son ölçümün dağılımı — bir eşik tartışıldığında sayıya
        /// bakılabilsin diye. Ortalama tek başına, kuyruğun nerede
        /// olduğunu söylemez.
        /// </summary>
        public static string Dagilim { get; private set; } = "-";

        /// <summary>Bir karede kaydedilen ham örnek.</summary>
        public struct Ornek
        {
            /// <summary>Alçak ayağın köke göre yüksekliği (m).</summary>
            public float yukseklik;

            /// <summary>
            /// O ayağın dünyadaki hızının <b>yürüyüş ekseni</b> üzerindeki
            /// bileşeni (m/s). Büyüklük DEĞİL.
            ///
            /// Büyüklük almak ölçüme yanal salınımı da katıyordu: basan
            /// ayak duruş genişliği değiştikçe yana da gider ve o hareket
            /// kayma değildir. Ölçülen kalıntının bir bölümü tam olarak
            /// buydu — yürümede ~0,12 m/s'lik bir taban, klip ile
            /// kontrolcü tam uyuşurken bile kaybolmuyordu.
            /// </summary>
            public float hiz;
        }

        /// <summary>
        /// Kaydedilen örneklerden orta duruş hızının ortancasını çıkarır.
        /// Yeterli örnek yoksa <c>-1</c>.
        /// </summary>
        public static float Hiz(List<Ornek> ornekler, out int sayi)
        {
            sayi = 0;
            if (ornekler == null || ornekler.Count < 4) return -1f;

            float enAlcak = float.MaxValue;
            foreach (var o in ornekler) enAlcak = Mathf.Min(enAlcak, o.yukseklik);

            var secilen = new List<float>();
            foreach (var o in ornekler)
                if (o.yukseklik <= enAlcak + Pencere) secilen.Add(o.hiz);

            sayi = secilen.Count;
            if (sayi < 3) return -1f;
            secilen.Sort();
            Dagilim = $"min {secilen[0]:0.00} / %25 "
                + $"{secilen[secilen.Count / 4]:0.00} / ort "
                + $"{secilen[secilen.Count / 2]:0.00} / %75 "
                + $"{secilen[secilen.Count * 3 / 4]:0.00} / max "
                + $"{secilen[secilen.Count - 1]:0.00}  (n={sayi}, "
                + $"enAlcak={enAlcak:0.000})";
            return secilen[secilen.Count / 2];
        }
    }
}
