using System;
using System.Collections.Generic;
using Hezarfen.Zaman;
using UnityEngine;

namespace Hezarfen.Sehir
{
    /// <summary>
    /// <b>Bir mesleğin günlük çizelgesi.</b>
    ///
    /// Plan Bölüm 11.3: *"Şehri yaşatan asıl katman budur — açık dünya
    /// hissinin büyük kısmı rutin ve tepkilerden gelir, diyalogdan
    /// değil."*
    ///
    /// ## Çizelge vakte bağlıdır, saate değil
    ///
    /// "Sabah 7'de dükkânı aç" yazmak 20. yüzyıl cümlesidir. 1632'de gün
    /// beş vakitle bölünür ve vakitler mevsime göre saatlerce kayar
    /// (<see cref="VakitHesabi"/>). Esnaf sabah ezanıyla kalkar, yatsıdan
    /// sonra sokakta kalmaz — kışın da yazın da. Çizelgeyi saate bağlamak,
    /// aralık ayında kepenkleri karanlıkta açtırırdı.
    ///
    /// ## Rutin SAF bir işlevdir
    ///
    /// `(vakit, tohum) → hedef türü`. Ajanın kendi durumu yok; nereye
    /// gideceği yalnızca vakitten ve kendi tohumundan çıkar.
    ///
    /// Bunun bedeli var — NPC dün ne yaptığını hatırlamaz — ama karşılığı
    /// büyük: şehrin bütün bir gününü <b>hiç çizmeden</b> simüle edip
    /// sayabiliyoruz. "Öğlende mescide akış oluyor mu", "yatsıdan sonra
    /// sokaklar boşalıyor mu" sorularının cevabı bir görüş değil bir sayı.
    /// </summary>
    [CreateAssetMenu(menuName = "Hezarfen/NPC Meslek", fileName = "NM_")]
    public class NPCMeslek : ScriptableObject
    {
        /// <summary>Meslek tipleri — plan Bölüm 11.3'ün listesi.</summary>
        public enum Tip
        {
            Esnaf, Hamal, Kayikci, Yeniceri, Ases,
            SuSaticisi, Dilenci, Cocuk, Imam, Medreseli,
        }

        [Serializable]
        public struct Adim
        {
            public VakitHesabi.Vakit vakit;
            public SokakGrafi.Tur hedef;

            [Tooltip("0-1: bu adımın gerçekleşme olasılığı. 1 = her gün.")]
            [Range(0f, 1f)] public float olasilik;

            [Tooltip("Bu adım açık havada mı geçer — gece ölçümü buna bakar.")]
            public bool disarida;
        }

        public Tip tip = Tip.Esnaf;

        [Tooltip("Şehirde bu meslekten kaç kişi olsun (oran, 0-1).")]
        [Range(0f, 1f)] public float pay = 0.1f;

        public List<Adim> cizelge = new();

        /// <summary>
        /// Bu vakitteki hedef. Bulunamazsa <see cref="SokakGrafi.Tur.Ev"/>.
        ///
        /// Tohum kişiyi ayırır: aynı meslekten iki kişi aynı vakitte hep
        /// aynı yere gitmez. Ama tohum sabittir, yani aynı kişi aynı gün
        /// aynı şeyi yapar — rutin rastgelelik değil <b>düzendir</b>.
        /// </summary>
        public SokakGrafi.Tur Hedef(VakitHesabi.Vakit v, int tohum)
        {
            // Deterministik: ayni (vakit, tohum) hep ayni sonucu verir.
            uint h = (uint)(tohum * 2654435761u + (int)v * 40503u);
            h ^= h >> 13; h *= 2246822519u; h ^= h >> 16;
            float r = (h & 0xFFFFFF) / (float)0x1000000;

            foreach (var a in cizelge)
            {
                if (a.vakit != v) continue;
                if (r <= a.olasilik) return a.hedef;
                r -= a.olasilik;
            }
            return SokakGrafi.Tur.Ev;
        }

        /// <summary>Bu vakitte dışarıda mı olur (gece ölçümü için).</summary>
        public bool Disarida(VakitHesabi.Vakit v, int tohum)
        {
            var hedef = Hedef(v, tohum);
            foreach (var a in cizelge)
                if (a.vakit == v && a.hedef == hedef) return a.disarida;
            return false;
        }
    }
}
