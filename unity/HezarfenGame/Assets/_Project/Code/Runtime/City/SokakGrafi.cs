using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hezarfen.Sehir
{
    /// <summary>
    /// <b>Şehrin gezilebilir iskeleti.</b> NPC rutinleri, ases devriyesi
    /// ve kayık ağı bu graf üzerinde yürür.
    ///
    /// ## Neden NavMesh değil
    ///
    /// Unity'nin NavMesh'i **serbest yüzey** verir: ajan iki nokta
    /// arasında yürünebilir her yerden geçer. 12 248 evlik bir şehirde
    /// bu hem pahalıdır hem yanlıştır — 17. yüzyıl İstanbul'unda insanlar
    /// avludan avluya değil <b>sokaktan</b> yürür, ve avlu duvarı bir
    /// engel değil bir <b>mahremiyet sınırıdır</b> (ADR 0062'de mahalle
    /// dokusunun kurulduğu ilke).
    ///
    /// Graf ayrıca rutinin dilini konuşur: NPC "12 metre kuzeye git"
    /// demez, "öğle ezanında mescide, sonra dükkâna" der. Düğümler yer
    /// değil <b>yerler</b>dir.
    ///
    /// ## Düğümler sahneden okunur, üretilmez
    ///
    /// Mescit, çeşme, fırın, dükkân — hepsi Faz 4'te zaten yerleştirildi
    /// ve sahnede duruyor. Grafı üretim sırasında kaydetmek daha ucuz
    /// olurdu ama o kayıt yoktu; sahneleri yeniden üretmek 1,5 milyon
    /// satırlık bir diff ve LFS'e kalıcı ikinci bir kopya demekti
    /// (CLAUDE.md, yeniden üretim gürültüsü). Sahneyi <b>okumak</b>
    /// hem ucuz hem de tek doğruluk kaynağını sahnede bırakıyor.
    /// </summary>
    [CreateAssetMenu(menuName = "Hezarfen/Sokak Grafi", fileName = "SG_Sehir")]
    public class SokakGrafi : ScriptableObject
    {
        /// <summary>
        /// Düğüm türü — NPC rutini buna göre hedef seçer.
        ///
        /// Sıra önemsiz ama <b>eklenen tür sona eklenir</b>: değerler
        /// serileştirilmiş varlıklarda saklıdır ve araya sokmak var olan
        /// bütün düğümlerin türünü kaydırır.
        /// </summary>
        public enum Tur
        {
            Bilinmeyen = 0,
            /// <summary>Mahalle mescidi — beş vakit buraya akış olur.</summary>
            Mescit = 1,
            /// <summary>Avlu kapısı = ev. NPC'nin gecelediği yer.</summary>
            Ev = 2,
            /// <summary>Çeşme/şadırvan — su ve buluşma.</summary>
            Cesme = 3,
            /// <summary>Fırın — sabah yoğun.</summary>
            Firin = 4,
            /// <summary>Dükkân — kepenk açma/kapama rutininin yeri.</summary>
            Dukkan = 5,
            /// <summary>Kahvehane — <b>1633 Eylül'ünden sonra kapalı</b>.</summary>
            Kahvehane = 6,
            Bozahane = 7,
            Hamam = 8,
            /// <summary>Han — hamal ve tüccar.</summary>
            Han = 9,
            Medrese = 10,
            /// <summary>Mektep — çocukların sabah gittiği yer.</summary>
            Mektep = 11,
            /// <summary>Kilise/sinagog — gayrimüslim mahallesinin merkezi.</summary>
            Mabet = 12,
            Turbe = 13,
            /// <summary>İskele — kayık ağının düğümü.</summary>
            Iskele = 14,
        }

        [Serializable]
        public struct Dugum
        {
            public Vector3 konum;
            public Tur tur;
            public string semt;
        }

        [Serializable]
        public struct Kenar
        {
            public int a, b;
            public float uzunluk;

            /// <summary>
            /// Bu kenar <b>kayıkla</b> geçilir — yürünmez.
            ///
            /// 1632'de Haliç'te köprü yok ve Boğaz'ı yürüyerek geçemezsin;
            /// karşıya kayık ve peremeyle gidilir ve iskeleler tarifelidir
            /// (RESEARCH §6). Bu yüzden ayrım bir bayrak değil bir
            /// <b>mekanik</b>: kayık kenarı akçe ister, iskelede beklemek
            /// ister, ve gece işlemeyebilir.
            ///
            /// Yol arama bunu bilmeden çalışırsa NPC suyun üstünde yürür.
            /// </summary>
            public bool kayik;
        }

        public List<Dugum> dugumler = new();
        public List<Kenar> kenarlar = new();

        /// <summary>Kaç düğüm hangi türden — inceleme ve test okur.</summary>
        public int Say(Tur t)
        {
            int n = 0;
            foreach (var d in dugumler) if (d.tur == t) n++;
            return n;
        }

        /// <summary>
        /// Komşuluk listesi. `kayikVar=false` ise <b>yalnız yürünen</b>
        /// kenarlar sayılır — kara parçalarının kendi içindeki bağlılığı
        /// ölçmek için.
        /// </summary>
        public List<int>[] Komsuluk(bool kayikVar = true)
        {
            var k = new List<int>[dugumler.Count];
            for (int i = 0; i < k.Length; i++) k[i] = new List<int>();
            foreach (var e in kenarlar)
            {
                if (!kayikVar && e.kayik) continue;
                if (e.a < 0 || e.b < 0 || e.a >= k.Length || e.b >= k.Length)
                    continue;
                k[e.a].Add(e.b);
                k[e.b].Add(e.a);
            }
            return k;
        }

        /// <summary>
        /// En büyük bağlı bileşendeki düğüm sayısı.
        ///
        /// Bağlantısızlık sessiz bir hatadır: NPC hedefine gidemez,
        /// yerinde döner ve "yapay zekâ bozuk" gibi görünür. Oysa bozuk
        /// olan haritadır.
        /// </summary>
        public int EnBuyukBilesen(bool kayikVar = true)
        {
            if (dugumler.Count == 0) return 0;
            var kom = Komsuluk(kayikVar);
            var gorildi = new bool[dugumler.Count];
            int enIyi = 0;
            var yigin = new Stack<int>();
            for (int s = 0; s < dugumler.Count; s++)
            {
                if (gorildi[s]) continue;
                int n = 0;
                yigin.Push(s);
                gorildi[s] = true;
                while (yigin.Count > 0)
                {
                    int v = yigin.Pop();
                    n++;
                    foreach (int w in kom[v])
                        if (!gorildi[w]) { gorildi[w] = true; yigin.Push(w); }
                }
                if (n > enIyi) enIyi = n;
            }
            return enIyi;
        }

        /// <summary>Verilen noktaya en yakın düğüm (-1 = graf boş).</summary>
        public int EnYakin(Vector3 p, Tur? tur = null)
        {
            int en = -1;
            float d2 = float.MaxValue;
            for (int i = 0; i < dugumler.Count; i++)
            {
                if (tur.HasValue && dugumler[i].tur != tur.Value) continue;
                float d = (dugumler[i].konum - p).sqrMagnitude;
                if (d < d2) { d2 = d; en = i; }
            }
            return en;
        }

        /// <summary>
        /// A* ile yol: düğüm indeksleri listesi (boşsa yol yok).
        ///
        /// Dijkstra değil A*: sezgisel kuş uçuşu mesafedir ve şehir
        /// grafında düğüm sayısı binlerce olduğu için fark ölçülebilir.
        /// Sezgisel gerçek maliyeti asla aşmaz (kenarlar en az kuş uçuşu
        /// kadar uzundur), yani bulunan yol en kısadır.
        /// </summary>
        public List<int> Yol(int bas, int son, bool kayikVar = true)
        {
            var yol = new List<int>();
            if (bas < 0 || son < 0 || bas >= dugumler.Count
                || son >= dugumler.Count) return yol;
            if (bas == son) { yol.Add(bas); return yol; }

            var kom = Komsuluk(kayikVar);
            int n = dugumler.Count;
            var g = new float[n];
            var f = new float[n];
            var geldi = new int[n];
            var kapali = new bool[n];
            for (int i = 0; i < n; i++)
            {
                g[i] = float.MaxValue;
                f[i] = float.MaxValue;
                geldi[i] = -1;
            }
            g[bas] = 0f;
            f[bas] = Vector3.Distance(dugumler[bas].konum, dugumler[son].konum);

            var acik = new List<int> { bas };
            while (acik.Count > 0)
            {
                int en = 0;
                for (int i = 1; i < acik.Count; i++)
                    if (f[acik[i]] < f[acik[en]]) en = i;
                int v = acik[en];
                acik.RemoveAt(en);
                if (v == son) break;
                kapali[v] = true;

                foreach (int w in kom[v])
                {
                    if (kapali[w]) continue;
                    float yeni = g[v] + Vector3.Distance(
                        dugumler[v].konum, dugumler[w].konum);
                    if (yeni >= g[w]) continue;
                    geldi[w] = v;
                    g[w] = yeni;
                    f[w] = yeni + Vector3.Distance(
                        dugumler[w].konum, dugumler[son].konum);
                    if (!acik.Contains(w)) acik.Add(w);
                }
            }

            if (geldi[son] < 0 && bas != son) return yol;
            for (int v = son; v >= 0; v = geldi[v]) yol.Add(v);
            yol.Reverse();
            return yol;
        }
    }
}
