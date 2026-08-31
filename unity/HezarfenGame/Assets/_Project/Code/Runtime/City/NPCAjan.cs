using System.Collections.Generic;
using UnityEngine;

namespace Hezarfen.Sehir
{
    /// <summary>
    /// <b>Bir sakinin durumu.</b> Görsel değil — konum, yol, hedef.
    ///
    /// Bu sınıf MonoBehaviour DEĞİL ve bu kasıtlı: şehirde iki bin sakin
    /// var ve çoğu her an oyuncudan kilometrelerce uzakta. Onlara birer
    /// GameObject vermek, hiç görülmeyecek iki bin Transform'u her karede
    /// Unity'nin döngüsünde taşımak demekti.
    ///
    /// Sakin <b>her zaman</b> yaşar; yalnızca <b>görünmesi</b> kademelidir
    /// (<see cref="NPCYonetici"/>). Bu ayrım önemli: oyuncu bir mahalleye
    /// girdiğinde insanlar orada belirmez, zaten oradadırlar — sadece
    /// artık çiziliyorlardır. Tersi, dünyayı oyuncunun etrafında dönen
    /// bir sahne yapardı.
    /// </summary>
    public class NPCAjan
    {
        public NPCMeslek meslek;
        public int evDugum;
        public int tohum;

        /// <summary>Şu anki dünya konumu (görsel olsa da olmasa da doğru).</summary>
        public Vector3 konum;

        /// <summary>İzlenen yol (düğüm indeksleri) ve kaçıncı adımda olduğu.</summary>
        public List<int> yol = new();
        public int adim;

        /// <summary>Hedef düğüm; -1 = hedefsiz (evinde bekliyor).</summary>
        public int hedefDugum = -1;

        /// <summary>
        /// Bu karede çizilecek mi — <see cref="NPCYonetici"/> önce karar
        /// verir, sonra bırakır, en son alır. Karar bayrağı olmadan
        /// bırakma ile alma iç içe geçiyor ve havuz bütçeyi aşıyordu.
        /// </summary>
        public bool gorunmeli;

        /// <summary>Görsel gövde — yalnız yakınken vardır.</summary>
        public Transform govde;

        /// <summary>Son ölçülen yatay hız (m/s) — animasyona verilir.</summary>
        public float hiz;

        /// <summary>Yürüyüş hızı; kişiden kişiye biraz değişir.</summary>
        public float yurumeHizi = 1.4f;

        /// <summary>
        /// Gövdenin sokak eksenine göre <b>yanal sapması</b> (m).
        ///
        /// Sakin sokak grafının düğüm ve kenarları üzerinde yürüyor; hepsi
        /// eksenin tam üstünde durunca <b>üst üste yığılıyorlar</b>.
        /// Ölçüldü: 9.000 sakin yalnız <b>3.070 ayrı noktada</b>, bir
        /// noktada 33 kişiye kadar. Şehir bu yüzden boş görünüyordu —
        /// otuz kişi iç içe durunca uzaktan tek kişi eder.
        ///
        /// Sapma <see cref="tohum"/>'dan türer, yani <b>deterministiktir</b>:
        /// aynı tohum aynı şehri verir (ADR 0070'in saf işlev kuralı).
        /// İlk denemede ±1,6 m'ydi ve yetmedi: doğum yerinde 20 m
        /// yarıçapta <b>254 sakin</b> toplanıyor ve hepsi omuz omuza tek
        /// bir küme oluyordu — kalabalık değil, yığın. Graf 1.544 düğüm,
        /// nüfus 40.000; düğüm başına ~26 kişi düşüyor ve onlar düğümün
        /// üstünde değil, çevresindeki alana yayılmalı.
        ///
        /// ±6 m yanal, ±12 m boylamsal: meydanda kalabalık, sokakta sıra
        /// olur. Duvara girenler olabilir — kabul: bir kalabalığın
        /// içinden geçmek, yığının içinden geçmekten iyidir.
        /// </summary>
        //: SAPMA SOKAKTAN GENIS OLAMAZ.
        //
        // ±6 m yaziyordu ve sokak 7,2 m geniş (ADR 0075) — yani sapma
        // sokagin kendisinden genis. "Duvara girenler olabilir — kabul"
        // diye yazilmisti ve hic olculmemisti; karelerde bir govde tas
        // duvarin icinden cikiyor.
        //
        // ±2,8 m sokak eninin yarisinin biraz alti: kalabaligin
        // dagilmasina yeter, duvara girmeye yetmez. Meydanda daha
        // genis bir dagilim istenirse dogru yol sapmayi buyutmek
        // degil, meydani ayri isaretlemek.
        public float Sapma => ((tohum * 2654435761u) % 1000u) / 1000f
                              * 5.6f - 2.8f;

        /// <summary>İleri-geri sapma — sıra hâlinde dizilmesinler.</summary>
        public float Boylamsal => ((tohum * 40503u) % 997u) / 997f
                                  * 24f - 12f;

        /// <summary>
        /// Bu vakit ne söylüyor — <see cref="BarkKorpusu"/>'ndan seçilmiş
        /// replik. Vakit değişince yenilenir.
        ///
        /// Ajanda durmasının sebebi görünürlükten bağımsız olması: sakin
        /// her zaman yaşar, yalnızca çizilmesi kademelidir. Repliği
        /// gövdeye bağlasaydık, oyuncu yaklaşınca herkes aynı anda
        /// konuşmaya başlardı.
        /// </summary>
        public Replik replik;

        /// <summary>
        /// Hedefi en son hangi vakit için yenilendi (−1 = hiç).
        ///
        /// Yenileme kareye yayıldığı için (bkz. <c>NPCYonetici</c>'nin
        /// <c>yenilemeButcesi</c>'si) bir sakinin sırası gelmemiş
        /// olabilir. Görünür olan sakinlerin beklemesi kabul edilemez:
        /// oyuncunun yanındaki adam repliksiz kalır ve <b>şehir susar</b>.
        /// Bu damga, "sırasını bekle" ile "şimdi yenile" ayrımını verir.
        /// </summary>
        public int vakitDamgasi = -1;

        /// <summary>Yolun sonuna geldi mi.</summary>
        public bool Vardi => yol.Count == 0 || adim >= yol.Count;

        /// <summary>
        /// Yolu bir adım ilerletir.
        ///
        /// `dt` gerçek zaman değil <b>geçen süre</b>: uzaktaki sakinler
        /// seyrek güncellenir ve o zaman `dt` büyük gelir. Hareketi
        /// kareye değil süreye bağlamak, uzakta yavaşlayan bir şehri
        /// önler — oyuncu geri döndüğünde herkes olması gereken yerdedir.
        /// </summary>
        public void Ilerle(SokakGrafi graf, float dt)
        {
            if (graf == null || Vardi) { hiz = 0f; return; }

            float kalan = yurumeHizi * dt;
            var oncekiKonum = konum;

            while (kalan > 0f && adim < yol.Count)
            {
                Vector3 hedef = graf.dugumler[yol[adim]].konum;
                float d = Vector3.Distance(konum, hedef);
                if (d <= kalan)
                {
                    konum = hedef;
                    kalan -= d;
                    adim++;
                }
                else
                {
                    konum = Vector3.MoveTowards(konum, hedef, kalan);
                    kalan = 0f;
                }
            }

            hiz = dt > 1e-4f
                ? new Vector2(konum.x - oncekiKonum.x,
                              konum.z - oncekiKonum.z).magnitude / dt
                : 0f;
        }

        /// <summary>Yeni bir yol ver ve baştan başlat.</summary>
        public void YolaKoy(List<int> yeniYol, int hedef)
        {
            yol = yeniYol ?? new List<int>();
            adim = 0;
            hedefDugum = hedef;
        }
    }
}
