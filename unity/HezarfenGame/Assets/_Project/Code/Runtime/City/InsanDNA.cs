using UnityEngine;

namespace Hezarfen.Sehir
{
    /// <summary>
    /// <b>Bir insanın tohumdan türeyen özellikleri.</b>
    ///
    /// Kalabalık kapatılmadan önceki hâlinde 40.000 sakin vardı ve
    /// hepsi aynı gövdeydi: aynı boy, aynı renk, aynı yürüyüş. Bir
    /// sokakta yirmi kişi görünce göz bunu bir topluluk değil, bir
    /// **kopya dizisi** olarak okur — evlerde tam bu kusuru ölçüp
    /// 26 varyanttan 201'e çıkardık; insanda 1 varyant vardı.
    ///
    /// ## Neden mesh değil, DNA
    ///
    /// Evde çözüm 201 tekil ağdı çünkü ev durur ve LOD'u vardır.
    /// İnsan yürür, animasyonludur ve aynı anda 60 tanesi görünür;
    /// 60 tekil karakter ağı bellek ve deri hesabı demek. Onun yerine
    /// **tek gövde + tohumdan türeyen ayarlar**: ölçek, ton, tempo.
    /// Bu, evlerdeki <see cref="Hezarfen.City.EvTonu"/> ile aynı fikir, insana
    /// uygulanmış hâli.
    ///
    /// ## Sayılar nereden
    ///
    /// Boy dağılımı 17. yüzyıl Akdeniz erkeği için ~1,66 m ortalama,
    /// 6 cm standart sapma (iskelet çalışmalarının verdiği aralık;
    /// bugünün ortalamasından ~8 cm kısa). Karakterimiz 1,70 m
    /// (<c>KarakterTests</c> ölçek sözleşmesi), yani ölçek çarpanı
    /// bunun etrafında gezinir. **T2**: dağılım gerçek, birey değil.
    ///
    /// Yaş: şehir nüfusu genç — çocuk ve yaşlı azınlıkta, çalışan
    /// yetişkin çoğunlukta. Yaş yalnız görünüşü değil <b>hızı</b> da
    /// belirler; yaşlı adam sokakta yavaş yürür ve bu, kalabalığın
    /// ritmini tek başına değiştirir.
    /// </summary>
    public readonly struct InsanDNA
    {
        /// <summary>Gövde ölçek çarpanı (1,0 = 1,70 m).</summary>
        public readonly float olcek;

        /// <summary>Yürüme hızı (m/s).</summary>
        public readonly float hiz;

        /// <summary>Giysi tonunun renk çarpanı.</summary>
        public readonly Color ton;

        /// <summary>0 çocuk … 1 yaşlı.</summary>
        public readonly float yas;

        /// <summary>Animasyon faz kayması — herkes aynı adımda olmasın.</summary>
        public readonly float faz;

        /// <summary>Karakterin sözleşmedeki boyu (m).</summary>
        public const float TabanBoy = 1.70f;

        /// <summary>Dönem erkeğinin ortalama boyu (m).</summary>
        public const float OrtalamaBoy = 1.66f;

        /// <summary>Boy standart sapması (m).</summary>
        public const float BoySapma = 0.062f;

        private InsanDNA(float olcek, float hiz, Color ton, float yas, float faz)
        {
            this.olcek = olcek;
            this.hiz = hiz;
            this.ton = ton;
            this.yas = yas;
            this.faz = faz;
        }

        /// <summary>
        /// Tohumdan bir insan üretir. <b>Saf fonksiyon</b>: aynı tohum
        /// her zaman aynı insanı verir, çağrı sırasından bağımsız.
        /// Kalabalık akışa alınıp yeniden kurulduğunda aynı kişinin
        /// aynı kişi kalmasını bu sağlar.
        /// </summary>
        public static InsanDNA Uret(int tohum)
        {
            uint h = (uint)tohum;
            float a = Karma(ref h);
            float b = Karma(ref h);
            float c = Karma(ref h);
            float d = Karma(ref h);
            float e = Karma(ref h);

            // Yaş: çalışan yetişkine yığılmış. Üs, dağılımı gençliğe
            // kaydırır — şehir nüfusu piramittir, dikdörtgen değil.
            //
            // Üs 1,8 ile başladı ve ölçüm reddetti: ortalama boy
            // 1,48 m çıktı, yani sokakta **%38 çocuk** vardı. Bir
            // liman şehri çocuk doludur ama üçte biri değildir.
            // 1,2 ile çocuk oranı ~%25'e iner.
            float yas = Mathf.Pow(a, 1.2f);

            // Boy: normale yakın (iki tekdüzenin toplamı). Çocuk ve
            // yaşlı kısalır; ikisi de aynı ucu paylaşmaz, çocuk çok
            // daha kısadır.
            float normal = (b + c - 1.0f);                 // -1..1, tepe 0
            float boy = OrtalamaBoy + normal * BoySapma * 1.9f;
            // Çocuk ölçeği 0,62 ile başlıyordu — 1,03 m, yani dört
            // yaşında. Sokakta görünen çocuk çoğunlukla daha büyüktür;
            // bebek kucakta taşınır, sokakta yürümez.
            if (yas < 0.16f) boy *= Mathf.Lerp(0.74f, 0.97f, yas / 0.16f);
            else if (yas > 0.82f) boy *= Mathf.Lerp(1.0f, 0.965f,
                                                    (yas - 0.82f) / 0.18f);

            // Hız: yaşla düşer, boyla hafif artar (uzun adım).
            float hiz = 1.42f
                      * Mathf.Lerp(1.06f, 0.72f, Mathf.Pow(yas, 1.6f))
                      * Mathf.Lerp(0.94f, 1.06f, Mathf.InverseLerp(1.45f, 1.85f, boy));

            // Ton: dönemin boya dünyası dar. Kök boya kırmızısı, ceviz
            // kahvesi, çivit mavisi, ham keten. Doygunluk düşük tutulur —
            // pahalı boya zengin işidir, sokak solgundur.
            float doygun = 0.10f + 0.28f * d;
            float parlak = 0.55f + 0.35f * e;
            float tonAci = TonAcisi(d, e);
            Color ton = Color.HSVToRGB(tonAci, doygun, parlak);

            return new InsanDNA(boy / TabanBoy, hiz, ton, yas, Karma(ref h));
        }

        /// <summary>
        /// Giysi rengi tonu (0-1). Rastgele bir çember değil, dört
        /// boya ailesinden biri: kök kırmızısı, ceviz, çivit, ham keten.
        /// Rastgele ton seçmek 17. yüzyıl sokağına yeşil ve mor sokar.
        /// </summary>
        private static float TonAcisi(float a, float b)
        {
            float u = (a + b) * 0.5f;
            if (u < 0.34f) return Mathf.Lerp(0.02f, 0.05f, u / 0.34f);      // kök kırmızı
            if (u < 0.62f) return Mathf.Lerp(0.07f, 0.10f, (u - 0.34f) / 0.28f); // ceviz
            if (u < 0.86f) return Mathf.Lerp(0.58f, 0.64f, (u - 0.62f) / 0.24f); // çivit
            return Mathf.Lerp(0.10f, 0.13f, (u - 0.86f) / 0.14f);           // ham keten
        }

        /// <summary>xorshift — ucuz ve tohumdan bağımsız yayılan.</summary>
        private static float Karma(ref uint h)
        {
            h ^= h << 13;
            h ^= h >> 17;
            h ^= h << 5;
            return (h & 0xFFFFFF) / (float)0xFFFFFF;
        }
    }
}
