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

            /// <summary>
            /// <b>Selâtin camisi — Cuma namazının kılındığı yer.</b>
            ///
            /// <see cref="Mescit"/>'ten ayrıdır ve ayrılığı bir kolaylık
            /// değil: mahalle mescidinin minberi yoktur, Cuma namazı orada
            /// kılınmaz. Cuma günü öğle vakti şehir mahalleye değil
            /// <b>buraya</b> akar (ADR 0071).
            ///
            /// Bu düğümler uydurulmaz: dünyada zaten duran, konumu
            /// katalogdan gelen selâtin camileridir.
            /// </summary>
            Cami = 15,
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

        /// <summary>
        /// <b>Gercek ev konumlari</b> — sakinin gecelediği yer.
        ///
        /// ## Neden ayri bir liste, neden dugum degil
        ///
        /// Grafta "ev" demek <c>PF_AvluKapi</c> demek ve sehirde 10.900
        /// eve karsilik yalnizca <b>142 avlu kapisi</b> var (Galata
        /// 2.651 ev / 34 kapi, Surici_Dogu 3.173 / 43). 40.000 sakin bu
        /// 142 noktaya bolusturulunce kapi basina 282 kisi dusuyor ve
        /// evde olanlarin hepsi ayni noktada duruyor. Turda izi acikca
        /// vardi: dort durakta 40 m icinde SIFIR kisi, bir durakta 272.
        /// Sehir "sayiya gore kalabalik, ekrana gore bos".
        ///
        /// Her evi graf dugumu yapmak dogru cozum gibi gorunuyor ama
        /// olculdu: kenar kurucu iki kez <b>O(n^2)</b> calisiyor
        /// (1.544 dugumde 2,4 milyon cift; 12.400 dugumde 154 milyon)
        /// ve <c>Yol()</c> her cagrida komsuluk dizisi ayiriyordu.
        /// Yol arama evin kapisina kadar gitmeli; kapidan eve son adim
        /// bir <b>nokta</b>, bir dugum degil.
        ///
        /// Bu yuzden ev konumlari grafin yaninda duruyor ve her evin
        /// hangi kapiya bagli oldugu URETIM aninda hesaplanip
        /// <see cref="evKapisi"/>ye yaziliyor — calisma zamaninda arama
        /// yok.
        /// </summary>
        public List<Vector3> evKonumlari = new();

        /// <summary>
        /// <see cref="evKonumlari"/> ile ayni sirada: o evin baglandigi
        /// avlu kapisi dugumu. Yol arama buraya kadar kosar.
        /// </summary>
        public List<int> evKapisi = new();

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
        /// Komsuluk listesi. `kayikVar=false` ise <b>yalniz yurunen</b>
        /// kenarlar sayilir — kara parcalarinin kendi icindeki bagliligi
        /// olcmek icin.
        ///
        /// ## Neden onbellek
        ///
        /// Bu islev her cagrildiginda dugum sayisi kadar liste ayiriyordu
        /// ve `Yol()` onu HER yol aramasinda cagiriyor. Yenileme kuyrugu
        /// kare basina 400 sakin isliyor; 1.544 dugumde bu, kare basina
        /// 600.000'den fazla liste ayirmasi demek — ve hepsi ayni cevabi
        /// uretiyor.
        ///
        /// Graf calisma zamaninda degismez: dugum ve kenar yalnizca
        /// uretim aninda yazilir. Onbellek bu yuzden sayilarla
        /// gecersizlestiriliyor; degisirse kendiliginden yeniden kurulur.
        ///
        /// <b>Dondurulen dizi paylasilir</b> — cagiran onu degistirmez.
        /// Bu depoda tek okur var (`Yol`, `EnBuyukBilesen`) ve ikisi de
        /// yalnizca okuyor.
        /// </summary>
        public List<int>[] Komsuluk(bool kayikVar = true)
        {
            int imza = dugumler.Count * 397 + kenarlar.Count;
            if (_komImza != imza) { _komEvet = null; _komHayir = null; }
            _komImza = imza;

            var onbellek = kayikVar ? _komEvet : _komHayir;
            if (onbellek != null) return onbellek;

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
            if (kayikVar) _komEvet = k; else _komHayir = k;
            return k;
        }

        [System.NonSerialized] private List<int>[] _komEvet;
        [System.NonSerialized] private List<int>[] _komHayir;
        [System.NonSerialized] private int _komImza = -1;

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

        // --------------------------------------------------------------
        // EN YAKIN DUGUM: IZGARA, DOGRUSAL TARAMA DEGIL
        //
        // `EnYakin` butun dugumleri tariyordu ve iki hot yolda birden
        // cagriliyor (`NPCYonetici.AjaniYenile`: biri cikis, biri hedef).
        // Yenileme kuyrugu kare basina 400 sakin isliyor; 1.544 dugumde
        // bu, kare basina ~1,2 milyon mesafe hesabi eder.
        //
        // Bu, her evi graf dugumu yapmanin onundeki asil engeldi.
        // Olculdu: sehirde 10.900 ev var ama grafta yalnizca 142 "ev"
        // (avlu kapisi), yani 40.000 sakin 142 noktaya yigiliyor —
        // kapi basina 282 kisi. Turda bunun izi acikca duruyordu: dort
        // durakta 40 m'de SIFIR kisi, bir durakta 272. Dugum sayisini
        // sekize katlamak dogrusal taramayi da sekize katlardi.
        //
        // Kova boyu 48 m: sokak dugumleri arasi mesafe bundan kucuk,
        // yani ilk halka cogu zaman doluyor ve arama bir avuc dugume
        // iniyor.
        private const float KovaBoyu = 48f;

        /// <summary>
        /// Aramanin acilabilecegi en cok halka.
        ///
        /// 220 x 48 m = 10,5 km, sehrin kosegeni. Bir baglanti degil
        /// KILIT: bos bir turden (sahnede hic sinagog yoksa) en yakini
        /// istenirse arama bir yerde durmali.
        /// </summary>
        private const int EnCokHalka = 220;

        [System.NonSerialized]
        private Dictionary<Tur, Dictionary<long, List<int>>> _izgara;
        [System.NonSerialized] private Dictionary<long, List<int>> _hepsi;
        [System.NonSerialized] private int _indekslenen = -1;

        private static long Anahtar(int cx, int cz)
            => ((long)cx << 32) ^ (uint)cz;

        /// <summary>
        /// Izgarayi kurar. Dugum sayisi degisince kendiliginden
        /// yenilenir; dugumler yalnizca uretim aninda eklendigi icin bu
        /// kontrol hem yeterli hem ucuz.
        /// </summary>
        private void Indeks()
        {
            if (_hepsi != null && _indekslenen == dugumler.Count) return;
            _hepsi = new Dictionary<long, List<int>>();
            _izgara = new Dictionary<Tur, Dictionary<long, List<int>>>();
            for (int i = 0; i < dugumler.Count; i++)
            {
                var k = dugumler[i].konum;
                long a = Anahtar(Mathf.FloorToInt(k.x / KovaBoyu),
                                 Mathf.FloorToInt(k.z / KovaBoyu));
                if (!_hepsi.TryGetValue(a, out var l))
                    _hepsi[a] = l = new List<int>();
                l.Add(i);

                var t = dugumler[i].tur;
                if (!_izgara.TryGetValue(t, out var g))
                    _izgara[t] = g = new Dictionary<long, List<int>>();
                if (!g.TryGetValue(a, out var l2))
                    g[a] = l2 = new List<int>();
                l2.Add(i);
            }
            _indekslenen = dugumler.Count;
        }

        /// <summary>Verilen noktaya en yakin dugum (-1 = graf bos).</summary>
        public int EnYakin(Vector3 p, Tur? tur = null)
        {
            Indeks();
            var g = _hepsi;
            if (tur.HasValue && !_izgara.TryGetValue(tur.Value, out g))
                return -1;

            int cx = Mathf.FloorToInt(p.x / KovaBoyu);
            int cz = Mathf.FloorToInt(p.z / KovaBoyu);

            int en = -1;
            float d2 = float.MaxValue;

            for (int r = 0; r < EnCokHalka; r++)
            {
                // DURMA KURALI. r halkasinin ic kenari, merkeze en az
                // (r-1)*kova kadar uzaktir; bulunan aday bundan yakinsa
                // daha oteye bakmanin anlami yok.
                if (en >= 0)
                {
                    float enAz = (r - 1) * KovaBoyu;
                    if (enAz > 0f && enAz * enAz > d2) break;
                }
                for (int dx = -r; dx <= r; dx++)
                {
                    for (int dz = -r; dz <= r; dz++)
                    {
                        // Yalniz halkanin KENARI; ici onceki turda
                        // tarandi.
                        if (r > 0 && Mathf.Abs(dx) != r
                            && Mathf.Abs(dz) != r) continue;
                        if (!g.TryGetValue(Anahtar(cx + dx, cz + dz),
                                           out var l)) continue;
                        foreach (int i in l)
                        {
                            float d = (dugumler[i].konum - p).sqrMagnitude;
                            if (d < d2) { d2 = d; en = i; }
                        }
                    }
                }
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
