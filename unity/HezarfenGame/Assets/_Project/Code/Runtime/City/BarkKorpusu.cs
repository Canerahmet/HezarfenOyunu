using System;
using System.Collections.Generic;
using Hezarfen.Zaman;
using UnityEngine;

namespace Hezarfen.Sehir
{
    /// <summary>Bir repliğin ne işe yaradığı.</summary>
    public enum BarkTuru
    {
        Satis = 0, Dedikodu = 1, Selam = 2, Is = 3, Uyari = 4, Dua = 5,
    }

    /// <summary>
    /// <b>Tek bir ambiyans repliği.</b> Alanlar
    /// <c>tools/content/gen_bark_korpusu.py</c>'nin yazdığı JSON'la birebir.
    /// </summary>
    [Serializable]
    public class Replik
    {
        public string id;
        public string metin;

        /// <summary><see cref="NPCMeslek.Tip"/> indeksi.</summary>
        public int meslek;

        /// <summary><see cref="VakitHesabi.Vakit"/> üzerinde bit maskesi.</summary>
        public int vakit;

        /// <summary><see cref="BarkTuru"/> indeksi.</summary>
        public int tur;

        /// <summary>0 farketmez, 1 yalnız aranırken, 2 yalnız temizken.</summary>
        public int aranma;

        /// <summary>Bu tarihten önce söylenmez (0 = sınır yok).</summary>
        public int enErkenYil, enErkenGun;

        /// <summary>Bu tarihten sonra söylenmez (0 = sınır yok).</summary>
        public int enGecYil, enGecGun;

        /// <summary>Neye dayanıyor — her repliğin bir kaynağı var.</summary>
        public string kaynak;
    }

    [Serializable]
    public class ReplikListesi { public Replik[] replikler; }

    /// <summary>
    /// <b>Katman 2 — offline üretilmiş ambiyans repliği korpusu.</b>
    /// PLAN Bölüm 11.3.
    ///
    /// ## Çalışma zamanında hiçbir şey üretilmez
    ///
    /// CLAUDE.md kuralı: *"Çalışma zamanında bulut LLM çağrısı YOK (v1.0).
    /// NPC içerikleri offline üretilir ve statik gemiye konur."* Bu sınıf
    /// metin **yazmaz**, hazır metinlerden **seçer**. Maliyet sıfır,
    /// gecikme sıfır, moderasyon riski sıfır ve tamamı QA edilebilir.
    ///
    /// ## Bağlama duyarlılık koşullu seçimle
    ///
    /// Plan bunu şöyle tarif ediyor: *"Bağlama duyarlılık, koşullu seçimle
    /// sağlanır (saat/hava/aranma durumu/perde → uygun replik havuzu)."*
    /// Burada süzgeç dört şey: <b>kim</b> (meslek), <b>ne vakit</b>,
    /// <b>hangi tarihte</b> (kronoloji) ve <b>oyuncu araniyor mu</b>.
    ///
    /// Kronolojinin süzgeçte olması bir ayrıntı değil: 1632'de "akşam
    /// kahvehanede miyiz?" sıradan bir cümledir, 1634'te kapatılmış bir
    /// yerden söz etmektir. Aynı şehir, aynı korpus, farklı yıl.
    /// </summary>
    public static class BarkKorpusu
    {
        public const string Yol = "Bark/bark_korpusu";

        private static Replik[] _hepsi;

        /// <summary>Korpustaki bütün replikler.</summary>
        public static Replik[] Hepsi
        {
            get { if (_hepsi == null) Yukle(); return _hepsi; }
        }

        /// <summary>Korpusu yükler (bir kez).</summary>
        public static void Yukle()
        {
            var metin = Resources.Load<TextAsset>(Yol);
            if (metin == null)
            {
                Debug.LogError($"[Hezarfen] Bark korpusu yok: Resources/{Yol} "
                               + "— once tools/content/gen_bark_korpusu.py");
                _hepsi = Array.Empty<Replik>();
                return;
            }
            var liste = JsonUtility.FromJson<ReplikListesi>(metin.text);
            _hepsi = liste?.replikler ?? Array.Empty<Replik>();
        }

        /// <summary>Testler ve yeniden üretim için önbelleği boşaltır.</summary>
        public static void Unut() => _hepsi = null;

        /// <summary>
        /// Bu replik bu bağlamda söylenebilir mi.
        /// </summary>
        public static bool Uygun(Replik r, NPCMeslek.Tip meslek,
                                 VakitHesabi.Vakit vakit,
                                 int yil, int gun, bool araniyor)
        {
            if (r == null) return false;
            if (r.meslek != (int)meslek) return false;
            if ((r.vakit & (1 << (int)vakit)) == 0) return false;
            if (r.aranma == 1 && !araniyor) return false;
            if (r.aranma == 2 && araniyor) return false;

            // KRONOLOJI: olmamis bir seyden bahsedilmez, kapanmis bir
            // yerden de. Sinir 0 ise o yonde sinir yoktur.
            if (r.enErkenYil > 0 && Once(yil, gun, r.enErkenYil, r.enErkenGun))
                return false;
            if (r.enGecYil > 0 && !Once(yil, gun, r.enGecYil, r.enGecGun))
                return false;
            return true;
        }

        private static bool Once(int yil, int gun, int eYil, int eGun)
            => yil < eYil || (yil == eYil && gun < eGun);

        /// <summary>
        /// Bu bağlamda söylenebilecek replikler.
        /// </summary>
        //: (meslek, vakit, yil, gun, aranma) -> havuz. Bkz. `Havuz`.
        private static readonly Dictionary<(int, int, int, int, bool),
                                           List<Replik>> _havuzlar = new();

        /// <summary>
        /// Bir bağlama uyan repliklerin listesi — <b>bellekte tutulur.</b>
        ///
        /// Önce her çağrıda 5.088 repliğin tamamı baştan taranıyordu ve
        /// çağıran <see cref="Sec"/>, vakit değişiminde <b>40.000 sakinin
        /// her biri için</b> bir kez çağrılıyordu: 203 milyon süzgeç
        /// çağrısı ve 40.000 kısa ömürlü liste, hepsi tek karede. Oyuncu
        /// için bu, her ezanda birkaç saniyelik donma demekti.
        ///
        /// Bağlam sayısı küçüktür (meslek × vakit × aranma), yani sonuç
        /// tekrar tekrar hesaplanan ama <b>hiç değişmeyen</b> bir şeydi.
        /// </summary>
        public static List<Replik> Havuz(NPCMeslek.Tip meslek,
                                         VakitHesabi.Vakit vakit,
                                         int yil, int gun, bool araniyor)
        {
            var anahtar = ((int)meslek, (int)vakit, yil, gun, araniyor);
            if (_havuzlar.TryGetValue(anahtar, out var hazir)) return hazir;

            var liste = new List<Replik>();
            foreach (var r in Hepsi)
                if (Uygun(r, meslek, vakit, yil, gun, araniyor)) liste.Add(r);
            _havuzlar[anahtar] = liste;
            return liste;
        }

        /// <summary>Korpus yeniden yüklenirse önbellek de düşer.</summary>
        public static void OnbellegiBosalt() => _havuzlar.Clear();

        /// <summary>
        /// Bir replik seçer; havuz boşsa <c>null</c>.
        ///
        /// Seçim <b>deterministiktir</b>: aynı (tohum, bağlam) hep aynı
        /// repliği verir. Rutin de böyle çalışıyor (ADR 0070) ve sebebi
        /// aynı — şehir bir zar atışı değil bir düzen olsun diye. Aynı
        /// adam aynı vakitte aynı şeyi söyler; oyuncu onu tanıyabilir.
        /// </summary>
        public static Replik Sec(NPCMeslek.Tip meslek,
                                 VakitHesabi.Vakit vakit,
                                 int yil, int gun, bool araniyor, int tohum)
        {
            var havuz = Havuz(meslek, vakit, yil, gun, araniyor);
            if (havuz.Count == 0) return null;
            uint h = (uint)(tohum * 2654435761u + (int)vakit * 40503u
                            + (int)meslek * 2654435761u);
            h ^= h >> 13; h *= 2246822519u; h ^= h >> 16;
            return havuz[(int)(h % (uint)havuz.Count)];
        }
    }
}
