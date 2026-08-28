using System;
using System.Collections.Generic;
using Hezarfen.Zaman;
using UnityEngine;

namespace Hezarfen.Sehir
{
    /// <summary>
    /// <b>Dinamik olaylar.</b> Plan Bölüm 11.1: mahalle yangını, pazar
    /// kurulumu, Cuma kalabalığı, gece devriye yoğunlaşması.
    ///
    /// Bir olayın "dinamik" olması rastgele olması demek değil: hepsi
    /// <b>takvimden ve vakitten</b> doğuyor. Cuma her hafta gelir, yangın
    /// belgeli bir günde çıkar, devriye her gece yoğunlaşır. Rastgelelik
    /// olsaydı oyuncu bir örüntü öğrenemez ve şehir bir zar atışı olurdu.
    /// </summary>
    public enum OlayTuru
    {
        Yok = 0,

        /// <summary>
        /// <b>Cuma.</b> Namaz mahalle mescidine değil selâtin camisine
        /// akar; sokaklar öğleye doğru boşalır, cami çevresi dolar.
        /// Haftanın tek özel günü.
        /// </summary>
        Cuma = 1,

        /// <summary>
        /// <b>Cibali yangını — 26 Ağustos 1633, Cuma.</b>
        ///
        /// Bir gemi kalafatçısının ateşinden çıktı. Kâtip Çelebi şehrin
        /// beşte birinin yandığını yazar; başka kaynaklar dörtte biri
        /// ya da beşte dördü der — <b>oran tartışmalı, olayın kendisi
        /// değil</b> (RESEARCH §6).
        ///
        /// <b>Tulumba YOK:</b> ilk teşkilat 1720'lerdedir. 1632 söndürme
        /// yöntemi yıkıcılarla ateş hattı kesmek, su taşımak, bina
        /// yıkmaktır. Yangın kuleleri de henüz yok.
        /// </summary>
        Yangin = 2,

        /// <summary>Pazar kurulumu — çarşı sabahı.</summary>
        Pazar = 3,

        /// <summary>Gece devriye yoğunlaşması — ases sayısı artar.</summary>
        GeceDevriyesi = 4,
    }

    /// <summary>
    /// <b>Şehirde bugün ne oluyor.</b>
    ///
    /// Bu sınıf olay <b>yaratmaz</b>, takvime bakıp <b>söyler</b>. Aynı
    /// tarih hep aynı olayı verir — oyuncu Cuma'nın ne demek olduğunu
    /// öğrenebilsin diye.
    /// </summary>
    public static class Olaylar
    {
        /// <summary>
        /// Yangının söndürülme yöntemleri — <b>tulumba yok</b>.
        ///
        /// Bu bir liste değil bir kısıt: 1632'de tulumba teşkilatı
        /// kurulmamıştır (ilk teşkilat Gerçek Davud, 1720'ler). Oyuncunun
        /// yapabileceği şey su taşımak, yıkıcılara katılmak ve ateş
        /// hattını kesmek.
        /// </summary>
        public static readonly string[] SondurmeYollari =
        {
            "su tasima",
            "yikicilarla ates hatti kesme",
            "bina yikimi",
        };

        /// <summary>Bugün olan olaylar.</summary>
        public static List<OlayTuru> Bugun(int yil, int gun,
                                           VakitHesabi.Vakit vakit)
        {
            var liste = new List<OlayTuru>();

            if (Kronoloji.Cuma(yil, gun)) liste.Add(OlayTuru.Cuma);

            if (yil == Kronoloji.CibaliYanginiYil
                && gun == Kronoloji.CibaliYanginiGun)
                liste.Add(OlayTuru.Yangin);

            // Pazar sabahi: gunes vaktinde carsi kurulur.
            if (vakit == VakitHesabi.Vakit.Gunes) liste.Add(OlayTuru.Pazar);

            // Gece devriyesi: yatsidan sonra. Yasak yururlukteyken daha
            // yogun ama devriye 1632'de de vardir.
            if (vakit == VakitHesabi.Vakit.Yatsi)
                liste.Add(OlayTuru.GeceDevriyesi);

            return liste;
        }

        /// <summary>
        /// Cuma günü öğle vaktinde <b>mescit yerine cami</b>.
        ///
        /// Cuma namazı mahalle mescidinde kılınmaz: minberi olan bir
        /// camide, cemaatle kılınır.
        ///
        /// Burada ilk yazımda <c>Mabet</c> yazıyordu ve testi <b>geçti</b>
        /// — çünkü test eşlemeyi yalnızca kendisiyle kıyaslıyordu. Oysa
        /// grafta <see cref="SokakGrafi.Tur.Mabet"/> <b>kilise/sinagog</b>
        /// demektir (ADR 0018): kod, Cuma cemaatini kiliseye yolluyordu.
        /// Sayı doğruydu, adres yanlıştı (ADR 0071).
        /// </summary>
        public static SokakGrafi.Tur CumaHedefi(SokakGrafi.Tur normal)
            => normal == SokakGrafi.Tur.Mescit ? SokakGrafi.Tur.Cami : normal;

        /// <summary>
        /// Cuma öğlesinde mescide gitme olasılığı bu kadar artar.
        ///
        /// Cuma namazı <b>zorunlu</b> bir toplu ibadettir; sıradan bir
        /// öğle namazından belirgin daha kalabalıktır ve bu oyunun
        /// haftalık ritmi.
        /// </summary>
        public const float CumaKatsayisi = 1.8f;

        /// <summary>Gece devriyesinde ases görüşü bu kadar artar.</summary>
        public const float DevriyeKatsayisi = 1.4f;

        /// <summary>
        /// Şu an gece devriyesi yürüyor mu — <b>ases sayısı</b> artıyor mu.
        ///
        /// Karanlık saklar (<c>geceGorusCarpani</c>) ama yatsıdan sonra
        /// sokakta daha çok göz vardır: subaşı, asesbaşı ve yeniçeri
        /// kolluğu gece devriyesi yürütür (RESEARCH §6). İki etki zıt
        /// yönde ve ikisi de gerçek; net sonuç yine gündüzden düşük.
        /// </summary>
        public static bool DevriyeVar(VakitHesabi.Vakit v)
            => v == VakitHesabi.Vakit.Yatsi;

        /// <summary>
        /// Şu an çarşı kuruluyor mu.
        ///
        /// Güneş vakti: kepenkler açılır, yük iskeleden çarşıya akar.
        /// Esnafın günü burada başlar.
        /// </summary>
        public static bool PazarVar(VakitHesabi.Vakit v)
            => v == VakitHesabi.Vakit.Gunes;

        /// <summary>
        /// Çarşı sabahı dükkâna/hana gitme olasılığı bu kadar artar.
        ///
        /// <see cref="CumaKatsayisi"/> ile aynı mantık, aynı formül; ikisi
        /// de <b>T2</b>: kurulumun olduğu belgeli, oranı değil.
        /// </summary>
        public const float PazarKatsayisi = 1.35f;

        /// <summary>Yangın hangi mahallede — Cibali, Haliç kıyısı.</summary>
        public static bool YanginBugun(int yil, int gun)
            => yil == Kronoloji.CibaliYanginiYil
               && gun == Kronoloji.CibaliYanginiGun;

        /// <summary>
        /// Yangına müdahale ödülü: <b>mahalle itibarı</b>, akçe değil.
        ///
        /// Yangına koşan adam para almaz; mahalle onu tanır. Plan
        /// Bölüm 11.2 bu arketipin ödülünü "mahalle itibarı" diye
        /// veriyor ve bu bir denge tercihi değil, olayın ne olduğu.
        /// </summary>
        public static Odul YanginOdulu => Odul.MahalleItibari | Odul.Kodeks;
    }
}
