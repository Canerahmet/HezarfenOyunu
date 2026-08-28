using System.Collections.Generic;
using System.Linq;
using System.Text;
using Hezarfen.Sehir;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Hezarfen.Editor.Gis
{
    /// <summary>
    /// <b>Sokak grafını sahnelerden okur.</b>
    ///
    /// Faz 4 şehri kurarken sokak omurgalarını hesapladı ve attı — o
    /// kayıt yok. Yeniden üretip kaydetmek 1,5 milyon satırlık sahne
    /// diff'i ve LFS'e kalıcı ikinci bir kopya demekti. Onun yerine
    /// sahnede zaten duran <b>yerler</b> okunuyor: mescit, çeşme, fırın,
    /// dükkân, avlu kapısı.
    ///
    /// ## Kenar bir varsayım değil, bir ÖLÇÜMDÜR
    ///
    /// İki düğümü "yakın oldukları için" bağlamak yetmez: aralarında
    /// dik bir yamaç varsa o yol yürünmez ve NPC duvara yürür. Her aday
    /// kenar boyunca arazi örneklenir ve <b>eğim</b> ölçülür. Galata bir
    /// yamaçtır; bu şehirde yürünebilirlik gerçekten bir soru.
    /// </summary>
    public static class SokakGrafiKur
    {
        private const string Cikti = "Assets/_Project/Data/SG_Sehir.asset";

        /// <summary>Bir düğümün bağlanabileceği en uzak komşu (m).</summary>
        private const float MaxKenar = 95f;

        /// <summary>Düğüm başına en fazla kaç komşu denenecek.</summary>
        private const int KomsuSayisi = 5;

        /// <summary>
        /// Mahalleler arası bağ için en uzak aday (m).
        ///
        /// İlk yazımda tek bir yarıçap vardı (95 m) ve graf **paramparça**
        /// çıktı: en büyük bileşen 1530 düğümün 23'ü, yani %2. Sebebi
        /// yapısaldı — bir mahalle sıkı bir cep, mahalleler arası mesafe
        /// o yarıçapın çok üstünde. Her düğüm kendi cebine bağlanıp
        /// orada kalıyordu ve şehir 130 ada oldu.
        ///
        /// Doğru yapı iki katmanlı: cep içinde SIK, cepler arasında
        /// SEYREK ama uzun. Osmanlı mahallesi zaten böyledir — içeride
        /// çıkmaz sokaklar, dışarıya birkaç geçit.
        /// </summary>
        private const float MaxUzakKenar = 1200f;

        /// <summary>
        /// İskeleye giden kenarın en uzak adayı (m).
        ///
        /// İskele kıyıdadır ve en yakın mahalle uzakta olabilir — Eyüp'ün
        /// iskelesi semtin düğüm merkezinden 928 m, Üsküdar'ınki daha da
        /// tenha bir noktada. 1200 m sınırıyla ikisi de <b>yalnız kayıkla</b>
        /// erişilebilir kaldı; yani NPC iskeleye yürüyemiyor ama kayığa
        /// binebiliyor, ki bu saçmadır.
        ///
        /// Ayrı bir sınır, çünkü ayrı bir şey: mahalleler arası geçit ile
        /// kıyıya inen yol aynı ölçüyle sınırlanmaz.
        /// </summary>
        private const float MaxIskeleKenar = 2400f;

        /// <summary>
        /// İskele kenarında geçilebilen en geniş su açıklığı (m).
        ///
        /// Bir iskele güvertesi en çok ~34 m'dir (Üsküdar'ınki). Haliç
        /// ağzı 700 m. Bu sayı ikisini ayırır: iskeleye uzanan tahtayı
        /// geçmek serbest, boğazı geçmek değil.
        /// </summary>
        private const float MaxSuAcikligi = 50f;

        /// <summary>
        /// Yürünebilir en dik eğim (derece).
        ///
        /// `CharacterController.slopeLimit` 55° ama o **tırmanılabilir**
        /// sınır; rutin yürüyüşü için değil. Galata'nın merdivenli
        /// sokakları 30°'yi geçer, o yüzden düz yürüyüş sınırından
        /// (~20°) yüksek tutuldu — ama 55° yazmak NPC'yi kayalıktan
        /// yürütürdü.
        /// </summary>
        private const float MaxEgim = 34f;

        /// <summary>
        /// Kısa kenarlarda izin verilen eğim (derece) — <b>merdiven</b>.
        ///
        /// Galata bir yamaçtır ve yokuş sokakları merdivenlidir; insanlar
        /// oralardan yürür. Tek bir 34° sınırı merdiveni "geçilmez" sayıp
        /// Galata'yı ikiye böldü (%85 bağlı, 53 düğümlük bir cep koptu).
        ///
        /// Ama sınırı topluca gevşetmek NPC'yi kayalıktan yürütürdü.
        /// Ayrımı yapan şey <b>uzunluk</b>: kısa ve dik = merdiven,
        /// uzun ve dik = uçurum. Bir kilometrelik merdiven yoktur.
        /// </summary>
        private const float MerdivenEgimi = 46f;

        /// <summary>Bu uzunluğun altındaki kenar merdiven sayılabilir (m).</summary>
        private const float MerdivenBoyu = 60f;

        /// <summary>Kenar boyunca her kaç metrede bir örnek alınacak.</summary>
        private const float OrnekAralik = 12f;

        /// <summary>En az bu kadar örnek (kısa kenarlar için).</summary>
        private const int MinOrnek = 6;

        /// <summary>
        /// Karanın bittiği kot (m). `y=0` deniz seviyesidir (ADR 0007).
        ///
        /// Bu denetim olmadan graf **suyun üstünden** kenar kurar ve
        /// NPC Haliç'i yürüyerek geçer. İlk yazımda yalnızca eğime
        /// bakıyordum ve hiçbir aday reddedilmedi — çünkü Haliç'in
        /// tabanı yumuşak eğimlidir, dik değil. Eğim testi yanlış soruyu
        /// soruyordu: mesele dikleşme değil, <b>su</b>.
        ///
        /// 1632'de Haliç'te köprü yok ve bu bir eksik değil ulaşım
        /// mekaniğidir (RESEARCH §6) — karşıya kayıkla geçilir.
        /// </summary>
        /// Değer 0,6 m'den 0,15 m'ye indirildi: aradaki fark **kıyı
        /// şeridi**. Deniz seviyesiyle 0,6 m arasında kalan düz kumsal ve
        /// rıhtım önü, 0,6 eşiğinde "su" sayılıyordu ve iskeleye giden
        /// kenarlar oradan geçtiği için reddediliyordu — iskeleler
        /// yürünemez kaldı. Karayı denizden ayıran şey deniz seviyesidir;
        /// yarım metrelik pay, kıyıyı denize katıyordu.
        private const float KaraKotu = 0.15f;

        private static readonly (string onek, SokakGrafi.Tur tur)[] Eslesme =
        {
            ("PF_Mescit", SokakGrafi.Tur.Mescit),
            ("PF_AvluKapi", SokakGrafi.Tur.Ev),
            ("PF_Cesme", SokakGrafi.Tur.Cesme),
            ("PF_Sadirvan", SokakGrafi.Tur.Cesme),
            ("PF_Firin", SokakGrafi.Tur.Firin),
            ("PF_Dukkan", SokakGrafi.Tur.Dukkan),
            ("PF_Kahvehane", SokakGrafi.Tur.Kahvehane),
            ("PF_Bozahane", SokakGrafi.Tur.Bozahane),
            ("PF_Hamam", SokakGrafi.Tur.Hamam),
            ("PF_Han", SokakGrafi.Tur.Han),
            ("PF_Medrese", SokakGrafi.Tur.Medrese),
            ("PF_Mektep", SokakGrafi.Tur.Mektep),
            ("PF_Kilise", SokakGrafi.Tur.Mabet),
            ("PF_Sinagog", SokakGrafi.Tur.Mabet),
            ("PF_Turbe", SokakGrafi.Tur.Turbe),
            ("PF_Iskele", SokakGrafi.Tur.Iskele),
            ("PF_UskudarIskelesi", SokakGrafi.Tur.Iskele),

            // CUMA CAMILERI (ADR 0071).
            //
            // Bunlar mahalle dokusundan gelmez; landmark olarak DUNYAYA
            // ZATEN YERLESTIRILMIS, konumu katalogdan gelen selatin
            // camileridir. Graf onlari yalnizca TANIR.
            //
            // `PF_YeniCamiHarabe` bu listede YOKTUR: 1632'de yarim kalmis
            // bir harabedir ("Zulmiyye"), cemaati yoktur.
            ("PF_Suleymaniye", SokakGrafi.Tur.Cami),
            ("PF_Ayasofya", SokakGrafi.Tur.Cami),
            ("PF_Sultanahmet", SokakGrafi.Tur.Cami),
            ("PF_FatihCamii", SokakGrafi.Tur.Cami),
            ("PF_Beyazit", SokakGrafi.Tur.Cami),
            ("PF_UskudarMihrimah", SokakGrafi.Tur.Cami),
            ("PF_DogancilarCamii", SokakGrafi.Tur.Cami),
            ("PF_HudayiTekkesi", SokakGrafi.Tur.Cami),
            ("PF_ArapCamii", SokakGrafi.Tur.Cami),
        };

        [MenuItem("Hezarfen/GIS/Sokak grafini kur")]
        public static void Kur()
        {
            var terrainGo = GameObject.Find("TR_Istanbul");
            var terrain = terrainGo != null ? terrainGo.GetComponent<Terrain>() : null;
            if (terrain == null)
            {
                Debug.LogError("[Hezarfen] TR_Istanbul yok — once arazi "
                               + "sahnesini ac (Faz1_Terrain).");
                return;
            }

            var graf = ScriptableObject.CreateInstance<SokakGrafi>();
            var sb = new StringBuilder("SOKAK GRAFI");

            // Semt sahnelerini SIRAYLA yukle: hepsini birden acmak 15 MB'lik
            // sahnelerin tamamini bellege alirdi.
            string[] semtler = System.IO.Directory.Exists(
                "Assets/_Project/Scenes/Districts")
                ? System.IO.Directory.GetFiles(
                    "Assets/_Project/Scenes/Districts", "*.unity")
                : new string[0];

            foreach (string yol in semtler.OrderBy(p => p))
            {
                string ad = System.IO.Path.GetFileNameWithoutExtension(yol);
                var sahne = EditorSceneManager.OpenScene(
                    yol.Replace('\\', '/'), OpenSceneMode.Additive);
                int once = graf.dugumler.Count;
                Topla(sahne, ad, graf);
                sb.AppendLine($"  {ad}: {graf.dugumler.Count - once} dugum");
                EditorSceneManager.CloseScene(sahne, true);
            }

            // Arazi sahnesindeki landmark'lar (iskele gibi) da girer.
            int oncekiSayi = graf.dugumler.Count;
            for (int i = 0; i < SceneManager.sceneCount; i++)
                Topla(SceneManager.GetSceneAt(i), "TERRAIN", graf);
            sb.AppendLine($"  arazi sahnesi: {graf.dugumler.Count - oncekiSayi} dugum");

            var (red, uzun) = Bagla(graf, terrain);
            int kayik = KayikBagla(graf);
            sb.AppendLine($"  {kayik} kayik kenari (iskeleler arasi)");
            sb.AppendLine($"  {graf.dugumler.Count} dugum, {graf.kenarlar.Count} "
                          + $"kenar ({uzun} mahalleler arasi, {red} aday "
                          + "egimden reddedildi)");

            foreach (SokakGrafi.Tur t in System.Enum.GetValues(typeof(SokakGrafi.Tur)))
            {
                int n = graf.Say(t);
                if (n > 0) sb.AppendLine($"    {t,-12} {n}");
            }

            // BILESEN RAPORU SEMT SEMT.
            //
            // Tek bir "%36 bagli" sayisi burada teshis DEGIL: 1632'de
            // Halic'te kopru yok ve Bogaz'i yuruyerek gecemezsin. Yani
            // grafin kara parcalarina bolunmus olmasi bir hata degil
            // TARIHSEL OLGUdur; ulasim kayikladir (RESEARCH BOLUM 6:
            // "Halic'te kopru olmamasi bir eksik degil, ulasim
            // mekanigidir").
            //
            // Sorulmasi gereken soru "graf bagli mi" degil, "HER SEMT
            // KENDI ICINDE bagli mi". Ilk yazimda toplam orana bakiyordum
            // ve o sayi iki ayri seyi -- gercek kopukluk ile denizi --
            // tek bir yuzdede karistiriyordu.
            var kom = graf.Komsuluk(kayikVar: false);
            var etiket = new int[graf.dugumler.Count];
            for (int i = 0; i < etiket.Length; i++) etiket[i] = -1;
            int bilesenSayisi = 0;
            var yigin = new Stack<int>();
            for (int s0 = 0; s0 < etiket.Length; s0++)
            {
                if (etiket[s0] >= 0) continue;
                yigin.Push(s0); etiket[s0] = bilesenSayisi;
                while (yigin.Count > 0)
                {
                    int v = yigin.Pop();
                    foreach (int w in kom[v])
                        if (etiket[w] < 0) { etiket[w] = bilesenSayisi; yigin.Push(w); }
                }
                bilesenSayisi++;
            }

            sb.AppendLine($"  {bilesenSayisi} bilesen (kara parcasi + kopukluk)");
            foreach (var grup in graf.dugumler
                         .Select((d, i) => (d, i))
                         .GroupBy(x => x.d.semt)
                         .OrderByDescending(g => g.Count()))
            {
                var sayim = grup.GroupBy(x => etiket[x.i])
                                .Select(g => g.Count())
                                .OrderByDescending(x => x).ToList();
                int toplam = grup.Count();
                float o = toplam > 0 ? sayim[0] / (float)toplam : 0f;
                sb.AppendLine($"    {grup.Key,-16} {toplam,4} dugum, "
                              + $"{sayim.Count} parca, en buyugu {sayim[0]} ({o:P0})");
                // KOPUK PARCA NEREDE. Bir yuzde "bozuk" der; koordinat
                // NEYIN bozuk oldugunu soyler. Kopukluk gercek bir
                // cografi engelse (koy, vadi) duzeltilecek sey graf
                // degil ULASIM MEKANIGIDIR — kayik, ADR 0069.
                if (sayim.Count > 1)
                {
                    var enBuyukEtiket = grup.GroupBy(x => etiket[x.i])
                        .OrderByDescending(g2 => g2.Count()).First().Key;
                    foreach (var parca in grup.GroupBy(x => etiket[x.i])
                                 .Where(g2 => g2.Key != enBuyukEtiket)
                                 .OrderByDescending(g2 => g2.Count())
                                 .Take(3))
                    {
                        var kk = parca.Select(x => graf.dugumler[x.i].konum).ToList();
                        var mrk = kk.Aggregate(Vector3.zero, (a2, b2) => a2 + b2)
                                  / kk.Count;
                        float uzak = kk.Max(k2 => Vector3.Distance(k2, mrk));
                        sb.AppendLine($"      kopuk {parca.Count(),4} dugum "
                                      + $"merkez ({mrk.x:0},{mrk.y:0},{mrk.z:0}) "
                                      + $"yayilim {uzak:0} m");
                    }
                }
            }

            int bilesen = graf.EnBuyukBilesen();
            sb.AppendLine($"  en buyuk bilesen: {bilesen}");

            System.IO.Directory.CreateDirectory("Assets/_Project/Data");
            var eski = AssetDatabase.LoadAssetAtPath<SokakGrafi>(Cikti);
            if (eski != null)
            {
                // Var olan varligin USTUNE yaz: GUID korunur.
                EditorUtility.CopySerialized(graf, eski);
                EditorUtility.SetDirty(eski);
            }
            else AssetDatabase.CreateAsset(graf, Cikti);
            AssetDatabase.SaveAssets();

            sb.AppendLine($"  -> {Cikti}");
            // Olcut SEMT ICI baglilik: her semtin dugumlerinin en az
            // %90'i tek parca olmali. Semtler arasi kopukluk denizdir
            // ve kayik agiyla kapanir.
            //
            // IKI ESIK, cunku iki ayri sey var:
            //
            //  * %50'nin altinda bir semt GERCEKTEN bozuktur — orada
            //    NPC'lerin cogu hedefine gidemez.
            //  * %50-%90 arasi bir cep, cografyanin kendisi olabilir.
            //    Olcum bunu gosterdi: Galata'nin kopuk 53 dugumu kulenin
            //    1330 m batisinda, 479 m guneyinde, 519 m yayilimda —
            //    yani KASIMPASA. Galata'dan bir dere vadisiyle ayrilan,
            //    tersaneye ait ayri yerlesim. Oraya zorla bir yaya kenari
            //    cakmak, olmayan bir koprü uydurmak olurdu.
            //
            // Cevabi graf degil ULASIM MEKANIGI verir (kayik agi) — Halic'te
            // kopru olmamasinin bir eksik degil mekanik olmasi gibi
            // (RESEARCH BOLUM 6).
            bool bozuk = false, cep = false;
            foreach (var grup in graf.dugumler.Select((d, i) => (d, i))
                         .GroupBy(x => x.d.semt))
            {
                if (grup.Key == "TERRAIN") continue;
                int enBuyuk = grup.GroupBy(x => etiket[x.i]).Max(g => g.Count());
                float o2 = enBuyuk / (float)grup.Count();
                if (o2 < 0.50f) bozuk = true;
                else if (o2 < 0.90f) cep = true;
            }
            bool saglam = !bozuk;
            if (!saglam)
                Debug.LogError("[Hezarfen] " + sb
                    + "Bir semt KENDI ICINDE kopuk: oradaki NPC'ler "
                    + "hedeflerine gidemez ve yerinde doner. Bu 'yapay zeka "
                    + "bozuk' gibi gorunur ama bozuk olan HARITADIR.");
            else if (cep)
                Debug.LogWarning("[Hezarfen] " + sb
                    + "Bir semtte cografyanin ayirdigi cep var; yaya "
                    + "kenariyla zorlanmadi. Kayik agi baglayacak.");
            else Debug.Log("[Hezarfen] " + sb);
        }

        /// <summary>
        /// İskeleleri birbirine <b>kayık</b> kenarlarıyla bağlar.
        ///
        /// Haliç ve Boğaz tek bir su kütlesidir, yani her iskeleden her
        /// iskeleye kayık gider. Ama kenar yürüyen kenardan AYRI
        /// işaretlenir: kayık akçe ister, iskelede beklemek ister, ve
        /// bunu bilmeyen bir yol arama NPC'yi suyun üstünde yürütür.
        ///
        /// 1632'de Haliç'te köprü yoktur ve bu bir eksik değil ulaşım
        /// mekaniğidir (RESEARCH BOLUM 6).
        /// </summary>
        private static int KayikBagla(SokakGrafi graf)
        {
            var iskeleler = new List<int>();
            for (int i = 0; i < graf.dugumler.Count; i++)
                if (graf.dugumler[i].tur == SokakGrafi.Tur.Iskele)
                    iskeleler.Add(i);

            int n = 0;
            for (int a = 0; a < iskeleler.Count; a++)
                for (int b = a + 1; b < iskeleler.Count; b++)
                {
                    int i = iskeleler[a], j = iskeleler[b];
                    float d = Vector3.Distance(graf.dugumler[i].konum,
                                               graf.dugumler[j].konum);
                    graf.kenarlar.Add(new SokakGrafi.Kenar
                    { a = i, b = j, uzunluk = d, kayik = true });
                    n++;
                }
            return n;
        }

        private static void Topla(Scene sahne, string semt, SokakGrafi graf)
        {
            if (!sahne.IsValid() || !sahne.isLoaded) return;
            foreach (var kok in sahne.GetRootGameObjects())
                foreach (var t in kok.GetComponentsInChildren<Transform>(true))
                {
                    var tur = TuruBul(t.name);
                    if (tur == SokakGrafi.Tur.Bilinmeyen) continue;
                    graf.dugumler.Add(new SokakGrafi.Dugum
                    {
                        konum = t.position,
                        tur = tur,
                        semt = semt,
                    });
                }
        }

        private static SokakGrafi.Tur TuruBul(string ad)
        {
            // UZUN onek once: "PF_UskudarIskelesi" hem "PF_Iskele"ye hem
            // kendisine uyuyor gibi gorunebilir; siralamak yerine en uzun
            // eslesmeyi seciyoruz ki ekleme sirasi anlam tasimasin.
            SokakGrafi.Tur en = SokakGrafi.Tur.Bilinmeyen;
            int enUzun = 0;
            foreach (var (onek, tur) in Eslesme)
                if (ad.StartsWith(onek, System.StringComparison.Ordinal)
                    && onek.Length > enUzun)
                { enUzun = onek.Length; en = tur; }
            return en;
        }

        /// <summary>
        /// Düğümleri **iki katmanda** bağlar ve bağlılığı garanti eder.
        ///
        /// 1. <b>Cep içi</b>: her düğüm en yakın birkaç komşusuna.
        /// 2. <b>Cepler arası</b>: farklı bileşenlerdeki en kısa
        ///    çiftler, uzunluğa göre sırayla (Kruskal). Bu, grafın
        ///    bağlı olmasını bir umut değil bir <b>sonuç</b> yapar.
        ///
        /// Döner: `(egimden_red, uzun_kenar)`.
        /// </summary>
        private static (int red, int uzun) Bagla(SokakGrafi graf, Terrain terrain)
        {
            var n = graf.dugumler;
            var kenar = new HashSet<(int, int)>();
            var bul = new int[n.Count];
            for (int i = 0; i < n.Count; i++) bul[i] = i;

            int Kok(int x) { while (bul[x] != x) { bul[x] = bul[bul[x]]; x = bul[x]; } return x; }
            bool Birlestir(int a, int b)
            {
                int ra = Kok(a), rb = Kok(b);
                if (ra == rb) return false;
                bul[ra] = rb;
                return true;
            }
            void Ekle(int i, int j, float d)
            {
                var k = i < j ? (i, j) : (j, i);
                if (!kenar.Add(k)) return;
                graf.kenarlar.Add(new SokakGrafi.Kenar
                { a = k.Item1, b = k.Item2, uzunluk = d });
                Birlestir(i, j);
            }

            int red = 0;

            // --- 1) CEP ICI --------------------------------------------------
            for (int i = 0; i < n.Count; i++)
            {
                var adaylar = new List<(int j, float d)>();
                for (int j = 0; j < n.Count; j++)
                {
                    if (i == j) continue;
                    float d = Vector3.Distance(n[i].konum, n[j].konum);
                    if (d <= MaxKenar) adaylar.Add((j, d));
                }
                adaylar.Sort((a, b) => a.d.CompareTo(b.d));

                int eklendi = 0;
                foreach (var (j, d) in adaylar)
                {
                    if (eklendi >= KomsuSayisi) break;
                    var anahtar = i < j ? (i, j) : (j, i);
                    if (kenar.Contains(anahtar)) { eklendi++; continue; }
                    if (!Yurunebilir(terrain, n[i].konum, n[j].konum,
                                     Iskele(n[i], n[j])))
                    { red++; continue; }
                    Ekle(i, j, d);
                    eklendi++;
                }
            }

            // --- 2) CEPLER ARASI (Kruskal) -----------------------------------
            //
            // Yalnizca FARKLI bilesenlerdeki ciftler aday; uzunluga gore
            // sirali eklenir. Boylece sehir bagli hale gelirken en kisa
            // gecitler secilir — ve secim keyfi degil, olculebilir.
            var uzak = new List<(int i, int j, float d)>();
            for (int i = 0; i < n.Count; i++)
                for (int j = i + 1; j < n.Count; j++)
                {
                    float d = Vector3.Distance(n[i].konum, n[j].konum);
                    float sinir = Iskele(n[i], n[j]) ? MaxIskeleKenar
                                                     : MaxUzakKenar;
                    if (d > MaxKenar && d <= sinir)
                        uzak.Add((i, j, d));
                }
            uzak.Sort((a, b) => a.d.CompareTo(b.d));

            int uzunEklendi = 0;
            foreach (var (i, j, d) in uzak)
            {
                if (Kok(i) == Kok(j)) continue;
                if (!Yurunebilir(terrain, n[i].konum, n[j].konum,
                                 Iskele(n[i], n[j])))
                { red++; continue; }
                Ekle(i, j, d);
                uzunEklendi++;
            }
            return (red, uzunEklendi);
        }

        /// <summary>
        /// Uçlardan biri iskele mi — su kuralı orada geçerli değildir.
        ///
        /// Bir iskele <b>tanım gereği</b> karayla suyun sınırındadır ve
        /// güvertesi suyun üstüne uzanır. Su kuralını ona da uygulamak
        /// iskeleye giden HER kenarı reddetmek demekti — graf bunu
        /// gösterdi: altı iskelenin altısı da kendi başına bir bileşendi,
        /// yani <b>hiçbirine yürünemiyordu</b>. Faz 3'ten beri sahnede
        /// duran Üsküdar iskelesi dahil.
        ///
        /// Kural doğruydu, kapsamı yanlıştı.
        /// </summary>
        private static bool Iskele(SokakGrafi.Dugum a, SokakGrafi.Dugum b)
            => a.tur == SokakGrafi.Tur.Iskele || b.tur == SokakGrafi.Tur.Iskele;

        /// <summary>
        /// İki nokta arası yürünebilir mi — <b>su</b> ve <b>eğim</b> ölçülür.
        ///
        /// Örnek sayısı uzunlukla artar. Sabit sayıda örnek almak uzun
        /// kenarlarda aldatıcıydı: 1 km'lik bir kenarda sekiz örnek 125
        /// metrede bir bakmak demek ve arada koca bir koy sığar.
        /// </summary>
        private static bool Yurunebilir(Terrain terrain, Vector3 a, Vector3 b,
                                        bool suyaIzin = false)
        {
            float toplam = Vector3.Distance(a, b);
            if (toplam < 0.1f) return true;
            int ornek = Mathf.Max(MinOrnek,
                                  Mathf.CeilToInt(toplam / OrnekAralik));
            float egimSiniri = toplam <= MerdivenBoyu ? MerdivenEgimi : MaxEgim;
            Vector3 o = terrain.transform.position;
            float once = terrain.SampleHeight(a) + o.y;
            float yatay = toplam / ornek;
            float suAcikligi = 0f;
            for (int i = 1; i <= ornek; i++)
            {
                Vector3 p = Vector3.Lerp(a, b, i / (float)ornek);
                float h = terrain.SampleHeight(p) + o.y;
                // Iskele kenarinda ARAZI olculmez: yurunen sey guvertedir.
                // Ama SU ACIKLIGI olculur — ve bu sinir olmadan muafiyet
                // Halic'in uzerinde bir yaya koprusu acti.
                //
                // Once muafiyeti kosulsuz yazdim: iskeleye giden kenar
                // her seyden muaf. Sonuc, Eminonu iskelesinin hem
                // Surici'ne hem Galata'ya baglanmasi ve NPC'lerin
                // Halic'i YURUYEREK gecmesi oldu — tam da su kuralinin
                // engellemek icin var oldugu sey. Bilesen sayisi ucten
                // ikiye dustu ve bu bir iyilesme gibi GORUNDU.
                //
                // Dogru sinir mesafe degil, ustunden gecilen SU: bir
                // iskele guvertesi en cok 34 m'dir; Halic agzi 700 m.
                if (h < KaraKotu)
                {
                    if (!suyaIzin) return false;
                    suAcikligi += yatay;
                    if (suAcikligi > MaxSuAcikligi) return false;
                    once = h;
                    continue;
                }
                suAcikligi = 0f;
                float egim = Mathf.Atan2(Mathf.Abs(h - once),
                                         Mathf.Max(0.01f, yatay))
                             * Mathf.Rad2Deg;
                if (egim > egimSiniri) return false;
                once = h;
            }
            return true;
        }
    }
}
