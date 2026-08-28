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

        /// <summary>Görsel gövde — yalnız yakınken vardır.</summary>
        public Transform govde;

        /// <summary>Son ölçülen yatay hız (m/s) — animasyona verilir.</summary>
        public float hiz;

        /// <summary>Yürüyüş hızı; kişiden kişiye biraz değişir.</summary>
        public float yurumeHizi = 1.4f;

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
