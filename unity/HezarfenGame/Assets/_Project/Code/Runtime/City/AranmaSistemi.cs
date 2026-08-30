using System;
using System.Collections.Generic;
using Hezarfen.Zaman;
using UnityEngine;

namespace Hezarfen.Sehir
{
    /// <summary>
    /// <b>Ases sistemi: fark edilme → uyarı → kovalamaca → yakalanma.</b>
    ///
    /// Plan Bölüm 11.1'in tarifi ve tek bir sert kuralı var:
    /// <b>şiddetsiz tasarım</b> — silahlı çatışma yok, tamamen
    /// kovalamaca ve saklanma. Bu bir kısıtlama değil, sistemin
    /// tanımı: burada <b>hasar diye bir kavram yoktur</b>. Yakalanmanın
    /// bedeli akçe, taşınan mal ve zamandır.
    ///
    /// Dönemin kaydı da böyle: subaşı, asesbaşı ve yeniçeri kolluğu gece
    /// dolaşanı sorar, yasak malı alır, mahalle imamı kefalet tutar
    /// (RESEARCH §6). Kimse kimseyi vurmaz.
    ///
    /// ## Aranma bir sayaç değil, bir GÖRÜLME
    ///
    /// İhlal tek başına aranma üretmez. Kimse görmediyse suç yoktur —
    /// bu hem oyun olarak doğru (saklanmanın anlamı var) hem tarih
    /// olarak: mahalleyi tutan şey kayıt ve kefalettir, her sokakta bir
    /// bekçi değil.
    ///
    /// ## Kaçış yolları
    ///
    /// Seviye, ases görüş alanının dışında zamanla söner. Kalabalık
    /// bunu <b>hızlandırır</b> — kalabalığa karışmak plan Bölüm 11.1'in
    /// saydığı kaçış yollarından biri ve şehrin kendi dokusunu bir
    /// mekaniğe çevirir: mahalle kalabalıksa saklanmak kolaydır.
    /// </summary>
    [DisallowMultipleComponent]
    public class AranmaSistemi : MonoBehaviour
    {
        /// <summary>Kolluğun oyuncuya karşı durumu.</summary>
        public enum Durum
        {
            /// <summary>Kimse bakmıyor.</summary>
            Temiz,
            /// <summary>Bir ases gördü; henüz bağırmadı.</summary>
            FarkEdildi,
            /// <summary>Uyarı bağırışı — "kim var orada?"</summary>
            Uyarildi,
            /// <summary>Kovalamaca.</summary>
            Kovalaniyor,
            /// <summary>Yakalandı — ceza kesilir.</summary>
            Yakalandi,
        }

        [Header("Bağlantılar")]
        public ZamanSistemi zaman;
        public NPCYonetici sehir;
        public Transform oyuncu;

        [Header("Algı")]
        [Tooltip("Ases oyuncuyu bu mesafeden görebilir (m). Gece daha kısa.")]
        public float gorusMesafesi = 26f;

        [Tooltip("Gece görüş bu oranla çarpılır — karanlıkta daha az görülür.")]
        [Range(0.2f, 1f)] public float geceGorusCarpani = 0.55f;

        [Header("Eşikler")]
        [Tooltip("Bu seviyenin üstünde uyarı bağırılır.")]
        [Range(0f, 1f)] public float uyariEsigi = 0.30f;

        [Tooltip("Bu seviyenin üstünde kovalamaca başlar.")]
        [Range(0f, 1f)] public float kovalamaEsigi = 0.60f;

        [Tooltip("Bu seviyede yakalanılır.")]
        [Range(0f, 1f)] public float yakalanmaEsigi = 1.0f;

        [Header("Sönümlenme")]
        [Tooltip("Görülmüyorken saniyede ne kadar düşer.")]
        public float sonumHizi = 0.07f;

        [Tooltip("Kalabalıkta sönümlenme bu kadar hızlanır (kişi başına).")]
        public float kalabalikPayi = 0.012f;

        [Tooltip("Kalabalık sayılırken bakılan yarıçap (m).")]
        public float kalabalikYaricapi = 18f;

        [Tooltip("Ceza kesildikten sonra bu kadar saniye yeniden " +
                 "durdurulmaz.")]
        public float cezaSonrasiMuafiyet = 25f;

        /// <summary>Aranma seviyesi (0-1).</summary>
        public float Seviye { get; private set; }

        /// <summary>
        /// <b>Kayıttan gelen aranma seviyesini geri koyar.</b>
        ///
        /// <see cref="Seviye"/> yalnız okunurdu ve kayıt dosyasında
        /// <c>aranmaSeviyesi</c> alanı vardı: yazılıyor, hiç okunmuyordu.
        /// Asesler kovalarken kaydeden oyuncu, yükleyince tertemiz
        /// uyanıyordu — HUD'daki aranma kutusu bile görünmüyordu, çünkü
        /// o <c>Seviye > 0,01</c> ile çiziliyor. Kaçış gerilimi kaydın
        /// içinde kayboluyordu.
        ///
        /// Ayrı bir metot, çünkü seviyeyi <b>oyun</b> içinde değiştiren
        /// tek şey suç ve zaman olmalı; bu kapı yalnız yükleme içindir.
        /// </summary>
        public void SeviyeyiGeriYukle(float seviye)
        {
            Seviye = Mathf.Clamp01(seviye);
            SuAn = Seviye <= 0.01f ? Durum.Temiz
                 : Seviye < 0.35f ? Durum.FarkEdildi
                 : Seviye < 0.70f ? Durum.Uyarildi : Durum.Kovalaniyor;
        }

        /// <summary>Şu anki durum.</summary>
        public Durum SuAn { get; private set; } = Durum.Temiz;

        /// <summary>Oyuncunun ödediği toplam ceza (akçe).</summary>
        public int OdenenCeza { get; private set; }

        /// <summary>Elden alınan yasak mal sayısı.</summary>
        public int ElKonanMal { get; private set; }

        /// <summary>Durum değişince tetiklenir — HUD, ses, müzik.</summary>
        public event Action<Durum> DurumDegisti;

        /// <summary>Yakalanınca tetiklenir: (ihlal, ceza).</summary>
        public event Action<Ihlal, int> Yakalandi;

        /// <summary>Oyuncunun şu an işlediği ihlal (dışarıdan set edilir).</summary>
        public Ihlal SuAnkiIhlal { get; set; } = Ihlal.Yok;

        /// <summary>Oyuncu yasak mal taşıyor mu.</summary>
        public bool YasakMalTasiyor { get; set; }

        /// <summary>Oyuncu fener taşıyor mu.</summary>
        public bool FenerVar { get; set; }

        /// <summary>Son ölçülen kalabalık — tanı ve test.</summary>
        public int YakindakiKalabalik { get; private set; }

        /// <summary>Son ölçülen en yakın ases mesafesi (m); yoksa -1.</summary>
        public float EnYakinAses { get; private set; } = -1f;

        private float _muafiyet;

        /// <summary>Ceza sonrası kalan muafiyet süresi (s) — tanı.</summary>
        public float Muafiyet => _muafiyet;

        private void Awake()
        {
            if (zaman == null) zaman = FindAnyObjectByType<ZamanSistemi>();
            if (sehir == null) sehir = FindAnyObjectByType<NPCYonetici>();
        }

        private void Update() => Adim(Time.deltaTime);

        /// <summary>
        /// Bir adım ilerlet. Test bunu doğrudan çağırır — zamanı
        /// beklemek yerine <b>süreyi vermek</b>, kovalamacayı gerçek
        /// zamanda oynamadan ölçmeyi sağlıyor.
        /// </summary>
        public void Adim(float dt)
        {
            if (oyuncu == null) return;

            bool gece = zaman != null && zaman.Gece;
            int yil = zaman != null ? zaman.yil : 1632;
            int gun = zaman != null ? zaman.yilinGunu : 121;

            // --- IHLAL: EN AGIRI secilir, ilk eslesen degil -----------
            //
            // Ilk yazimda sirayla bakiyordum ve fenersiz gece hep once
            // eslesiyordu: 1634'te gece tutun tasiyan biri yalnizca
            // "fenersiz" sayiliyor, tutun hic gorulmuyordu. Ihlaller bir
            // liste degil bir KUME; kolluk en agirina gore davranir.
            var ihlal = SuAnkiIhlal;
            void Aday(Ihlal i)
            {
                if (!IhlalKurali.Gecerli(i, yil, gun, gece)) return;
                if (IhlalKurali.Agirlik(i) > IhlalKurali.Agirlik(ihlal))
                    ihlal = i;
            }
            if (gece && !FenerVar) Aday(Ihlal.FenersizGece);
            if (YasakMalTasiyor) Aday(Ihlal.YasakMal);

            if (!IhlalKurali.Gecerli(ihlal, yil, gun, gece))
                ihlal = Ihlal.Yok;

            // --- ALGI: goren var mi ------------------------------------
            // GECE DEVRIYESI: karanlik saklar ama yatsidan sonra sokakta
            // daha cok goz vardir. Iki etki zit yonde ve ikisi de gercek;
            // net sonuc yine gunduzden dusuk (0,55 x 1,4 = 0,77).
            float menzil = gorusMesafesi * (gece ? geceGorusCarpani : 1f);
            if (zaman != null && Olaylar.DevriyeVar(zaman.Vakit))
                menzil *= Olaylar.DevriyeKatsayisi;
            EnYakinAses = EnYakinAsesMesafesi(menzil * 3f);
            YakindakiKalabalik = KalabalikSay();

            bool goruluyor = ihlal != Ihlal.Yok
                             && EnYakinAses >= 0f && EnYakinAses <= menzil;

            // CEZA SONRASI MUAFIYET.
            //
            // Ases seni sorar, cezani keser ve YOLUNA GONDERIR. Muafiyet
            // olmadan, hala fenersiz olan adam saniyede bir yeniden
            // yakalaniyordu — bir ceza dongusu. Test bunu "yakalandiktan
            // sonra seviye 0,44" diye bildirdi.
            //
            // Bu ayni zamanda oyuncuya cikis veriyor: cezayi odedin,
            // simdi fener bul ya da eve git.
            if (_muafiyet > 0f)
            {
                _muafiyet -= dt;
                goruluyor = false;
            }

            if (goruluyor)
            {
                // Yakinlik arttikca daha hizli fark edilir.
                float yakinlik = 1f - Mathf.Clamp01(EnYakinAses / menzil);
                Seviye += IhlalKurali.Agirlik(ihlal)
                          * (0.35f + 0.65f * yakinlik) * dt;
            }
            else
            {
                // KALABALIGA KARISMAK: sehrin dokusu bir mekanik.
                float sonum = sonumHizi
                              + YakindakiKalabalik * kalabalikPayi;
                Seviye -= sonum * dt;
            }
            Seviye = Mathf.Clamp01(Seviye);

            // --- DURUM --------------------------------------------------
            var yeni = Seviye >= yakalanmaEsigi ? Durum.Yakalandi
                     : Seviye >= kovalamaEsigi ? Durum.Kovalaniyor
                     : Seviye >= uyariEsigi ? Durum.Uyarildi
                     : Seviye > 0.01f ? Durum.FarkEdildi
                     : Durum.Temiz;

            if (yeni != SuAn)
            {
                SuAn = yeni;
                DurumDegisti?.Invoke(yeni);
                if (yeni == Durum.Yakalandi) Yakala(ihlal);
            }
        }

        /// <summary>
        /// Yakalanma. <b>Hasar yok</b> — akçe, mal ve zaman.
        ///
        /// Bu metotta bir sağlık değişkeni, bir vuruş, bir ölüm yoktur ve
        /// olmayacaktır. Şiddetsizlik hem döneme hem yaş derecelendirmesine
        /// uygun, ayrıca dövüş sistemi yazmamak üretim maliyetini de
        /// düşürüyor (plan Bölüm 11.1).
        /// </summary>
        private void Yakala(Ihlal ihlal)
        {
            int ceza = IhlalKurali.Ceza(ihlal);
            OdenenCeza += ceza;

            // EL KOYMA, seni durduran ihlale degil USTUNDEKINE bakar.
            //
            // Gece fenersiz yakalanan adamin cebindeki tutun de alinir;
            // ases once durdurur, sonra arar. Ilk yazimda el koymayi
            // tetikleyen ihlale baglamistim ve gece yakalanan kacakci
            // maliyla birlikte serbest kaliyordu.
            bool yasakYururlukte = !Kronoloji.KahvehaneAcik(
                zaman != null ? zaman.yil : 1632,
                zaman != null ? zaman.yilinGunu : 121);
            if (YasakMalTasiyor && yasakYururlukte)
            {
                ElKonanMal++;
                YasakMalTasiyor = false;
                ceza += IhlalKurali.Ceza(Ihlal.YasakMal);
                OdenenCeza += IhlalKurali.Ceza(Ihlal.YasakMal);
            }
            Yakalandi?.Invoke(ihlal, ceza);

            // Ceza kesildi, mesele kapandi: oyuncu serbest.
            Seviye = 0f;
            SuAnkiIhlal = Ihlal.Yok;
            _muafiyet = cezaSonrasiMuafiyet;
        }

        /// <summary>En yakın ases mesafesi; menzilde yoksa -1.</summary>
        private float EnYakinAsesMesafesi(float menzil)
        {
            if (sehir == null || sehir.Sakinler == null) return -1f;
            float en = float.MaxValue;
            float m2 = menzil * menzil;
            foreach (var a in sehir.Sakinler)
            {
                if (a.meslek == null) continue;
                if (a.meslek.tip != NPCMeslek.Tip.Ases
                    && a.meslek.tip != NPCMeslek.Tip.Yeniceri) continue;
                float d2 = (a.konum - oyuncu.position).sqrMagnitude;
                if (d2 < m2 && d2 < en) en = d2;
            }
            return en < float.MaxValue ? Mathf.Sqrt(en) : -1f;
        }

        /// <summary>Yakındaki sakin sayısı — kalabalığa karışmak için.</summary>
        private int KalabalikSay()
        {
            if (sehir == null || sehir.Sakinler == null) return 0;
            int n = 0;
            float r2 = kalabalikYaricapi * kalabalikYaricapi;
            foreach (var a in sehir.Sakinler)
                if ((a.konum - oyuncu.position).sqrMagnitude <= r2) n++;
            return n;
        }
    }
}
