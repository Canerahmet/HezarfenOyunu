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

        /// <summary>Kadın mı — gövde arketipini bu ve <see cref="yas"/> seçer.</summary>
        public readonly bool kadin;

        /// <summary>
        /// <b>Ten çarpanı</b> — kişiden kişiye değişen deri rengi.
        /// Paletin ten rengiyle çarpılır; 0,62-1,20 arası açıklık ve
        /// hafif bir sıcaklık kayması taşır.
        /// </summary>
        public readonly Color ten;

        /// <summary>
        /// <b>Kafa oranı</b> — gövdeye göre başın büyüklüğü ve biçimi.
        ///
        /// Uzaktan bir insanı ötekinden ayıran şey renk değil silüettir
        /// ve silüetin en okunur oranı baş/gövdedir: aynı boyda iki
        /// kişiden başı büyük olan daha genç, küçük olan daha uzun
        /// görünür. Ölçüm bunu doğruladı — yedi arketip ve dokuz renk
        /// üretildikten sonra bile kalabalıkta ayrışma tonlardan
        /// geliyordu; silüet hâlâ arketip başına tekti.
        ///
        /// Aralık dar ve bilerek: yetişkinde baş boyu 1/7,5 ile 1/8,3
        /// arasında gezinir, yani ±%5. Daha fazlası insan değil karikatür
        /// olur. <c>x</c> ve <c>z</c> ayrı: yüz yuvarlak ya da uzun olur.
        /// </summary>
        public readonly Vector3 kafa;

        /// <summary>
        /// Bu insanın hedef boyu (m).
        ///
        /// <see cref="olcek"/> bunun 1,70 m'ye bölünmüş hâliydi ve tek
        /// gövde varken ikisi aynı şeydi. Yedi arketip gelince ayrıldılar:
        /// oğlan gövdesinin kendi boyu 1,24 m ve onu 1,70'lik bir çarpanla
        /// ölçeklemek çocuğu 0,9 m'lik bir cüceye çevirirdi. Ölçek artık
        /// gövdenin <b>kendi</b> tabanına göre hesaplanıyor
        /// (<see cref="SakinGovde.tabanBoy"/>), o yüzden asıl sayı budur.
        /// </summary>
        public readonly float boy;

        /// <summary>Karakterin sözleşmedeki boyu (m).</summary>
        public const float TabanBoy = 1.70f;

        /// <summary>Dönem erkeğinin ortalama boyu (m).</summary>
        public const float OrtalamaBoy = 1.66f;

        /// <summary>Boy standart sapması (m).</summary>
        public const float BoySapma = 0.062f;

        private InsanDNA(float olcek, float hiz, Color ton, float yas,
                         float faz, bool kadin, float boy, Color ten,
                         Vector3 kafa)
        {
            this.olcek = olcek;
            this.hiz = hiz;
            this.ton = ton;
            this.yas = yas;
            this.faz = faz;
            this.kadin = kadin;
            this.ten = ten;
            this.kafa = kafa;
            this.boy = boy;
        }

        /// <summary>
        /// Yaşın bandı: 0 çocuk, 1 genç, 2 yetişkin, 3 yaşlı.
        /// <see cref="SakinGovde.BandDizini"/> ile aynı ölçek.
        ///
        /// Sınırlar <see cref="Uret"/>'in kendi eşikleriyle aynı yerde:
        /// çocuk 0,16'nın altı (boy çarpanı orada bükülüyor), yaşlı
        /// 0,82'nin üstü (orada da). Üçüncü bir yerde ayrı sayı yazmak,
        /// gövdeyle boyun farklı yaşlarda değişmesi demek olurdu.
        /// </summary>
        public int Band
        {
            get
            {
                if (yas < 0.16f) return 0;
                if (yas < 0.34f) return 1;
                if (yas > 0.82f) return 3;
                return 2;
            }
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

            // CINSIYET: sehrin yarisi kadin.
            //
            // Sokakta hic kadin yoktu ve bunu bir oyuncu yazdi. Sebebi
            // ideoloji degil eksiklikti: tek bir govde vardi ve o govde
            // erkekti. Payi 0,48 tutuyorum — 17. yy Istanbul'unda erkek
            // nufus, tasradan gelen bekar isci akini yuzunden bir miktar
            // fazladir; bu bir tahmin degil, kaynaklarin tekrar tekrar
            // soyledigi bir dengesizlik (T2).
            //
            // `d` daha once yalniz TON icin kullaniliyordu; ayri bir
            // karma cekmiyorum ki eski tohumlarin boyu ve hizi
            // degismesin — kaydedilmis bir oyunda ayni kisi ayni kisi
            // kalmali.
            bool kadin = Karma(ref h) < 0.48f;

            // TEN: aciklik ve sicaklik — kisiden kisiye DEGISIR.
            //
            // Ten rengi bugune kadar hic degismiyordu: palette tek bir
            // SKIN vardi ve `ton` yalniz kumasa uygulaniyordu. Yani yedi
            // govde ve dokuz kumas rengi uretildikten sonra bile
            // sehirdeki herkesin TENI ayniydi — ve ten, bir kalabalikta
            // en cok degisen seydir.
            //
            // Us 0,85 dagilimi hafifce ACIGA kaydirir; Akdeniz'in kuzey
            // kiyisinda beklenen budur (T2: dagilim gercek, birey degil).
            // Carpan paletin ten rengiyle CARPILIYOR, yani tenin ne
            // oldugunu hala palet soyluyor; DNA yalnizca ne kadar acik.
            //
            // Mavi kanal ayrica kisiliyor: koyu ten yalnizca daha
            // karanlik degil daha SICAKTIR. Tek bir degeri uc kanala
            // birden uygulamak griye kayan, cansiz bir ten verir.
            float tenAcik = Mathf.Pow(Karma(ref h), 0.85f);
            float tenSicak = Karma(ref h);
            float tv = Mathf.Lerp(0.62f, 1.20f, tenAcik);
            var ten = new Color(
                tv * (1.00f + 0.06f * (tenSicak - 0.5f)),
                tv * (0.99f - 0.02f * (tenSicak - 0.5f)),
                tv * (0.94f - 0.10f * (tenSicak - 0.5f)
                      - 0.08f * (1f - tenAcik)),
                1f);

            // KAFA ORANI: buyukluk ve bicim.
            //
            // `kafaBoy` genel olcek (bas/govde orani), `kafaEn` yuzun
            // yuvarlakligi. Ikisi ayri karmadan geliyor ki buyuk ve
            // uzun bir yuz de, kucuk ve yuvarlak bir yuz de olabilsin.
            // Cocukta taban zaten buyuk basli (olculdu: 1/6,25), o
            // yuzden burada YASA GORE bir kaydirma yok — arketip onu
            // zaten soyluyor.
            float kafaBoy = 0.95f + 0.10f * Karma(ref h);
            float kafaEn = 0.96f + 0.08f * Karma(ref h);
            var kafa = new Vector3(kafaBoy * kafaEn, kafaBoy,
                                   kafaBoy * (1.96f - kafaEn) * 0.5f);
            // Donem kadini erkekten ~12 cm kisadir (1,54 / 1,66 = 0,928).
            // Ayni oran arketip boylarinda da var (`sakin_kit`), cunku
            // ikisi de ayni yerden okundu.
            if (kadin) boy *= 0.928f;
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

            return new InsanDNA(boy / TabanBoy, hiz, ton, yas, Karma(ref h),
                                kadin, boy, ten, kafa);
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
