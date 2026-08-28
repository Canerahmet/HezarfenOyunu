using System.Collections.Generic;
using Hezarfen.Zaman;
using UnityEngine;

namespace Hezarfen.Sehir
{
    /// <summary>
    /// <b>Şehrin bir gününü çizmeden simüle eder ve SAYAR.</b>
    ///
    /// Rutin saf bir işlev olduğu için (`(vakit, tohum) → hedef`) bir
    /// günü oynatmak için ne model, ne animasyon, ne kare gerekiyor.
    /// Bunun karşılığı şu: *"öğle ezanında mescide akış oluyor mu"*,
    /// *"yatsıdan sonra sokaklar boşalıyor mu"* soruları bir görüş değil
    /// bir <b>sayı</b>.
    ///
    /// Plan Bölüm 11.3 bu katmanı şöyle tarif ediyor: *"Şehri yaşatan
    /// asıl katman budur — açık dünya hissinin büyük kısmı rutin ve
    /// tepkilerden gelir, diyalogdan değil."* Rutin görünmüyorsa yoktur;
    /// görünüp görünmediğini de ancak ölçüm söyler.
    /// </summary>
    public static class SehirGunu
    {
        /// <summary>Bir sakin: meslek, ev, tohum.</summary>
        public struct Sakin
        {
            public NPCMeslek meslek;
            public int evDugum;
            public int tohum;
        }

        /// <summary>Bir vaktin ölçümü.</summary>
        public struct Olcum
        {
            public VakitHesabi.Vakit vakit;
            public int toplam;
            public int disarida;
            public int mescitte;
            public int ulasilamaz;
            public Dictionary<SokakGrafi.Tur, int> hedefler;

            public float DisariOrani => toplam > 0 ? disarida / (float)toplam : 0f;
            public float MescitOrani => toplam > 0 ? mescitte / (float)toplam : 0f;
        }

        /// <summary>
        /// Şehre sakin dağıtır.
        ///
        /// Ev düğümleri (avlu kapıları) sakinlerin çıkış noktası. Meslek,
        /// <see cref="NPCMeslek.pay"/> oranlarına göre seçilir — yani
        /// şehrin çoğu esnaf ve çocuk, birkaçı ases. Bu oranlar T2:
        /// 1632'nin meslek dağılımı sayıyla kayıtlı değil, ama bir şehrin
        /// dörtte birinin bekçi olmadığı da kesin.
        /// </summary>
        public static List<Sakin> Sakinler(SokakGrafi graf,
                                           IList<NPCMeslek> meslekler,
                                           int adet, int tohum = 1632)
        {
            var liste = new List<Sakin>();
            if (graf == null || meslekler == null || meslekler.Count == 0)
                return liste;

            var evler = new List<int>();
            for (int i = 0; i < graf.dugumler.Count; i++)
                if (graf.dugumler[i].tur == SokakGrafi.Tur.Ev) evler.Add(i);
            if (evler.Count == 0) return liste;

            float toplamPay = 0f;
            foreach (var m in meslekler) toplamPay += Mathf.Max(0f, m.pay);
            if (toplamPay <= 0f) return liste;

            var rng = new System.Random(tohum);
            for (int i = 0; i < adet; i++)
            {
                float r = (float)rng.NextDouble() * toplamPay;
                NPCMeslek secilen = meslekler[meslekler.Count - 1];
                foreach (var m in meslekler)
                {
                    if (r <= m.pay) { secilen = m; break; }
                    r -= m.pay;
                }
                liste.Add(new Sakin
                {
                    meslek = secilen,
                    evDugum = evler[rng.Next(evler.Count)],
                    tohum = rng.Next(int.MaxValue),
                });
            }
            return liste;
        }

        /// <summary>
        /// Bir vakti ölçer: kim nerede, kaçı dışarıda, kaçı hedefine
        /// ulaşamıyor.
        ///
        /// `yil`/`gun` kronolojiye bakmak için: 2 Eylül 1633'ten sonra
        /// kahvehane yok ve o hedefi seçen kişi eve döner (Kronoloji).
        /// Aynı çizelge, farklı yıl, farklı şehir.
        /// </summary>
        public static Olcum Olc(SokakGrafi graf, IList<Sakin> sakinler,
                                VakitHesabi.Vakit v, int yil, int gun)
        {
            var o = new Olcum
            {
                vakit = v,
                hedefler = new Dictionary<SokakGrafi.Tur, int>(),
            };
            if (graf == null || sakinler == null) return o;

            var kom = graf.Komsuluk(kayikVar: false);
            var bilesen = Bilesenler(graf, kom);

            foreach (var s in sakinler)
            {
                if (s.meslek == null) continue;
                o.toplam++;

                // Takvim RUTININ ICINDE uygulanir (ADR 0071). Burada
                // ikinci bir kopyasi YOKTUR: simulasyonun olctugu gun,
                // oyuncunun yurudugu gun olsun diye.
                var hedef = Rutin.Hedef(s.meslek, v, s.tohum, yil, gun);
                bool disarida = Rutin.Disarida(s.meslek, v, s.tohum, yil, gun);

                if (!o.hedefler.ContainsKey(hedef)) o.hedefler[hedef] = 0;
                o.hedefler[hedef]++;
                if (disarida) o.disarida++;
                if (hedef == SokakGrafi.Tur.Mescit) o.mescitte++;

                // Hedefe YURUYEREK gidilebiliyor mu — ayni bilesende
                // o turden dugum var mi. Yoksa NPC yerinde doner.
                if (hedef == SokakGrafi.Tur.Ev) continue;
                if (!AyniBilesende(graf, bilesen, s.evDugum, hedef))
                    o.ulasilamaz++;
            }
            return o;
        }

        /// <summary>Bütün günü ölçer — altı vakit.</summary>
        public static List<Olcum> Gun(SokakGrafi graf, IList<Sakin> sakinler,
                                      int yil, int gun)
        {
            var liste = new List<Olcum>();
            foreach (VakitHesabi.Vakit v in
                     System.Enum.GetValues(typeof(VakitHesabi.Vakit)))
                liste.Add(Olc(graf, sakinler, v, yil, gun));
            return liste;
        }

        private static int[] Bilesenler(SokakGrafi graf, List<int>[] kom)
        {
            var etiket = new int[graf.dugumler.Count];
            for (int i = 0; i < etiket.Length; i++) etiket[i] = -1;
            int c = 0;
            var yigin = new Stack<int>();
            for (int s = 0; s < etiket.Length; s++)
            {
                if (etiket[s] >= 0) continue;
                yigin.Push(s); etiket[s] = c;
                while (yigin.Count > 0)
                {
                    int v = yigin.Pop();
                    foreach (int w in kom[v])
                        if (etiket[w] < 0) { etiket[w] = c; yigin.Push(w); }
                }
                c++;
            }
            return etiket;
        }

        private static bool AyniBilesende(SokakGrafi graf, int[] bilesen,
                                          int ev, SokakGrafi.Tur hedef)
        {
            if (ev < 0 || ev >= bilesen.Length) return false;
            int b = bilesen[ev];
            for (int i = 0; i < graf.dugumler.Count; i++)
                if (graf.dugumler[i].tur == hedef && bilesen[i] == b)
                    return true;
            return false;
        }
    }
}
