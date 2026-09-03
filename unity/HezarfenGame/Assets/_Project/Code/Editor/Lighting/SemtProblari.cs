using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Hezarfen.Editor.Lighting
{
    /// <summary>
    /// <b>Her semtin kendi prob hacmi</b> — şehrin sıçrama ışığı burada
    /// doğar.
    ///
    /// ## Ölçüm: şehrin dolaylı ışığı yoktu
    ///
    /// Turun üstten çekilen denetim karesinde Sûriçi sokağı simsiyahtı.
    /// Ama içindeki insanlar aydınlıktı ve karanlık bölge <b>her yerde
    /// tıpatıp aynı rengi</b> okuyordu — üstte (37,1 / 16,0 / 0,2),
    /// ortada (36,6 / 15,0 / 0,2), altta (36,4 / 14,7 / 0,2). Gerçek bir
    /// gölge altındaki yüzeye göre değişir; sabit bir renk gölge değil,
    /// <b>hiç ışık almayan yüzey + sis</b> demektir.
    ///
    /// Aynı yerin göz hizasındaki karesi ise normaldi: gölgedeki kaldırım
    /// mavi/kırmızı 0,63, gölgedeki sıva 0,86. <b>Aynı yer, aynı saniye,
    /// iki kamera, iki sonuç.</b> Farkı yaratan tek şey açıydı — yani
    /// dolaylı ışık <b>ekran uzayından</b> geliyordu (SSGI). Göz
    /// hizasında gökyüzü ve güneşli duvarlar ekranda; yukarıdan dar
    /// sokağa bakınca değiller, ve ışık sıfıra düşüyordu.
    ///
    /// Sebep fırının kaydında yazılıydı: pişirme kümesi <b>tek sahne</b>
    /// içeriyordu (<c>singleSceneMode: 1</c>, tek GUID) ve o sahne
    /// binaların olduğu sahne değil, taban sahneydi. Şehir sekiz semt
    /// sahnesinde yaşıyor (35 + 41 + 34 + 27 MB…), taban sahne 1,1 MB.
    /// 2.829.507 prob pişti ve hepsi <b>boş bir 729 × 648 m yamanın</b>
    /// üstündeydi.
    ///
    /// ## Hacim: semtin kendi sınırı, ama yürünen bant kadar yüksek
    ///
    /// Önceki hâl kutuyu <c>YapiSinirlari()</c> ile hesaplıyor ve 600
    /// m'ye kırpıyordu; kendi yorumu da *"bir semt 600 m'yi aşarsa prob
    /// hacmi semt başına BÖLÜNMELİ"* diyordu. Bölme hiç yapılmadı —
    /// ders yazılıydı, iş bağlanmamıştı.
    ///
    /// Önce <c>Mode.Global</c> denendi: hacim sahnenin kendi sınırından
    /// türer ve her pişirmede yeniden hesaplanır, yani eskiyecek bir
    /// sayı kalmaz. Doğru fikirdi ama sınır **tam** sınırdı: minarenin
    /// tepesi, tepenin sırtı, çatının üstü. Prob geometrinin çevresine
    /// konur, yani oralara da konuyordu — ve orada gökyüzü zaten her
    /// şeyi görüyor; sıçrama terimi ölçüm gürültüsü kadar.
    ///
    /// Ölçüm bunun bedelini gösterdi: şehrin tamamı bir oturuşta
    /// pişmiyor (GPU çöküyor, CPU 172 dakikada bitmiyor). Yüksekliği
    /// kırpmak denendi ve <b>elendi</b> — gerekçesi aşağıda, kodun
    /// yanında: tek bir kutu yamacı izleyemez. Çözüm bu yüzden hacmi
    /// küçültmek değil, pişirmeyi <b>semt semt</b> bölmek oldu
    /// (<c>KaliciAydinlatma.TopluPisirSemt</c>).
    ///
    /// ## Neden 3 m aralık
    ///
    /// Sıçrama ışığı <b>alçak frekanslıdır</b>: bir duvarın yansıttığı
    /// ışık metrelerce yumuşak değişir. 1 m aralık küçük bir odada
    /// anlamlı, 10 km'lik bir şehirde yalnız bellek yer. Aralığı üçe
    /// katlamak prob sayısını yaklaşık dokuzda bire indirir ve
    /// kazanılan yerle şehrin <b>tamamı</b> pişebilir. Ölçü sahibi tek:
    /// <see cref="ProbAraligi"/>.
    /// </summary>
    public static class SemtProblari
    {
        public const string SemtDizini = "Assets/_Project/Scenes/Districts";

        /// <summary>
        /// Problar arası en az uzaklık (m) — tek sahibi burası.
        ///
        /// <b>3 m'ydi ve ölçüm kabul etmedi.</b> Sekiz semtin hepsi
        /// yüklüyken — kısmi pişirmenin şartı bu, hücre ızgarası küme
        /// çapında — yerleştirme 67.180.350 prob sınırını aşıyor.
        /// Denemeler <c>-hezarfenYerlesimDene</c> ile dakikalar içinde
        /// koştu: <b>3 m geçmedi, 4 m geçti, 6 m geçti.</b> 4 seçildi;
        /// prob sayısı 3'e göre 2,4 kat azalıyor ve sınırın altında
        /// kalan en ince aralık o.
        ///
        /// Sıçrama ışığı alçak frekanslıdır — bir duvarın yansıttığı
        /// ışık metrelerce yumuşak değişir — yani 3 ile 4 arasındaki
        /// fark gözde değil, bellekte.
        /// </summary>
        public const float ProbAraligi = 4f;

        // YURUNEN BANTLA SINIRLAMA DENENDI VE ELENDI.
        //
        // Fikir dogruydu: minarenin tepesine prob koymak, gokyuzunun
        // zaten yaptigi isi ikinci kez odemektir. Uygulamasi degildi —
        // tek bir kutunun alt siniri sahnenin EN ALCAK noktasidir ve
        // Galata bir YAMAC: zeminden 24 m, tepedeki her seyi keserdi.
        //
        // Bir kutu bir tepeyi izleyemez. Bunu yapmanin dogru yolu
        // semti izgaraya bolup her hucreyi kendi zeminine oturtmak;
        // o ayri bir isin konusu ve olculmeden yazilmayacak. Sayi
        // yazmadan once bunun kaydi burada duruyor ki ayni fikir
        // ikinci kez ayni hatayla denenmesin.

        /// <summary>Hacmin yapı sınırının dışına taştığı pay (m).</summary>
        public const float SinirPayi = 8f;

        /// <summary>
        /// Bir çiziciyi "yapı" saymanın üst sınırı (m, yatayda).
        /// Bundan büyüğü deniz, arazi ya da fon düzlemidir.
        /// </summary>
        public const float EnBuyukYapi = 1000f;

        /// <summary>Taban sahne — prob hacmi burada YAŞAMAZ.</summary>
        public const string TabanSahne =
            "Assets/_Project/Scenes/Faz1_Terrain.unity";

        /// <summary>
        /// Bir semt sahnesinin <b>kendi</b> yapı sınırı.
        ///
        /// Yalnız o sahnenin köklerinden dolaşır — <c>Mode.Global</c>'in
        /// yaptığı gibi yüklü olan her şeyi toplamaz. Arazi dışarıda:
        /// 15 km'lik bir arazinin çevresine prob koymak, 67 milyonluk
        /// sınırı tek başına aşan şeydi.
        /// </summary>
        public static Bounds SemtSiniri(UnityEngine.SceneManagement.Scene s)
            => SemtSiniri(s, out _);

        public static Bounds SemtSiniri(UnityEngine.SceneManagement.Scene s,
                                        out List<string> disarida)
        {
            bool ilk = true;
            var b = new Bounds();
            disarida = new List<string>();
            foreach (var kok in s.GetRootGameObjects())
                foreach (var r in kok.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (!r.enabled) continue;
                    if (r.GetComponent<MeshFilter>() == null) continue;
                    if (r.GetComponentInParent<Terrain>() != null) continue;

                    // MANZARA YUZEYI YAPI DEGILDIR.
                    //
                    // Ilk olcumde D_Bogaz'in hacmi 7929 x 17 x 15284 m
                    // cikti: 17 m yuksekliginde, 15 km uzunlugunda bir
                    // dilim. Bu bir semt degil, DENIZ — bir su duzlemi
                    // butun haritayi boydan boya geciyor. D_Halic ayni.
                    //
                    // Bunlarin cevresine prob koymak iki kez yanlis:
                    // acik suyun sicratacak bir seyi yok, ve sinirin
                    // kendisi 67 milyonluk prob sinirini tek basina
                    // yiyor.
                    //
                    // Esik OLCULDU. Once 300 m yazildi (Suleymaniye
                    // kulliyesi kabaca 200 m) ve yanlis seyi eledi:
                    // sehir blok blok BIRLESTIRILMIS cizicilerden
                    // olusuyor — `Kaideler`, `Kaldirim`,
                    // `BahceDuvarlari` 300-340 m arasi kutular ve
                    // hepsi gercek yapi. Iki topluluk olculdu: birlesik
                    // bloklar ~340 m'de bitiyor, harita boyu yuzeyler
                    // kilometrelerde. 1000 m ikisinin arasinda duruyor.
                    var o = r.bounds.size;
                    if (o.x > EnBuyukYapi || o.z > EnBuyukYapi)
                    {
                        if (disarida.Count < 12)
                            disarida.Add($"{r.name} ({o.x:0}x{o.z:0} m)");
                        continue;
                    }

                    if (ilk) { b = r.bounds; ilk = false; }
                    else b.Encapsulate(r.bounds);
                }
            return ilk ? new Bounds() : b;
        }

        /// <summary>Semt sahnelerinin yolları (alfabetik).</summary>
        public static List<string> Semtler()
        {
            if (!Directory.Exists(SemtDizini)) return new List<string>();
            return Directory.GetFiles(SemtDizini, "D_*.unity")
                .Select(y => y.Replace('\\', '/'))
                .OrderBy(y => y).ToList();
        }

        /// <summary>Projedeki pişirme kümesi (tek olmalı).</summary>
        public static ProbeVolumeBakingSet Kume()
        {
            foreach (string guid in AssetDatabase.FindAssets(
                         "t:ProbeVolumeBakingSet"))
            {
                var s = AssetDatabase.LoadAssetAtPath<ProbeVolumeBakingSet>(
                    AssetDatabase.GUIDToAssetPath(guid));
                if (s != null) return s;
            }
            return null;
        }

        /// <summary>
        /// <b>Pişirme kümesine bağlanmamış semtler</b> — testin okuduğu
        /// ölçü.
        ///
        /// Sahne dosyasını açmadan cevaplanabilir: kümenin sahne GUID
        /// listesi diskte duruyor. Bağlı olmayan bir semtin probu hiç
        /// pişmez ve orada dolaylı ışık yoktur.
        /// </summary>
        public static List<string> BaglanmamisSemtler()
        {
            var kume = Kume();
            var eksik = new List<string>();
            if (kume == null)
            {
                eksik.Add("Projede hic ProbeVolumeBakingSet yok.");
                return eksik;
            }
            var bagli = new HashSet<string>(kume.sceneGUIDs);
            foreach (string yol in Semtler())
            {
                string guid = AssetDatabase.AssetPathToGUID(yol);
                if (!bagli.Contains(guid))
                    eksik.Add(Path.GetFileNameWithoutExtension(yol));
            }
            return eksik;
        }

        /// <summary>
        /// <b>Pişmiş hücre sayısı</b> — fırının diske ne yazdığının
        /// ölçüsü.
        ///
        /// Bu ölçü bir kusurdan doğdu. <c>D_Okmeydani</c> 11,7 dakika
        /// pişti ve koşum <i>"APV pişti ve kaydedildi"</i> dedi; küme
        /// varlığında ise <c>m_Values: []</c> yazıyordu — <b>sıfır
        /// hücre</b>. Sebebi kaydın içindeydi: <i>"the number of APV
        /// probes exceeds the current system limit of 67.180.350"</i>.
        /// Yerleştirme daha başlarken düşmüş, ama <c>Lightmapping</c>
        /// yine de bir tur dönmüş ve bekleyici bunu bitiş saymıştı.
        ///
        /// Yani bekleyici, işin <b>başladığını</b> görüyordu ama
        /// <b>ürününü</b> görmüyordu. Ölçü olmadan "başarılı" diyen bir
        /// fırın, bu turda üçüncü kez aynı tuzağı kuruyor.
        /// </summary>
        public static int HucreSayisi()
        {
            var kume = Kume();
            if (kume == null) return -1;
            var so = new SerializedObject(kume);
            var anahtarlar = so.FindProperty("cellDescs.m_Keys");
            return anahtarlar == null ? -1 : anahtarlar.arraySize;
        }

        /// <summary>
        /// <b>Pişmiş APV verisini siler</b> — hücre yerleşimi
        /// değiştiğinde gerekir.
        ///
        /// Prob hacimleri değişince (dünya boyu <c>Global</c> kutulardan
        /// semt boyu <c>Local</c> kutulara) hücre ızgarası da değişti,
        /// ama kümede eski ızgarayla pişmiş hücreler duruyordu. Unity
        /// bunu tek satırla söylüyor ve <b>sessizce sonucu atıyor</b>:
        /// <i>"You are partially baking the set with an incompatible
        /// cell layout."</i>
        ///
        /// Eski veri durdukça her kısmi pişirme kaybolur. Bu yüzden
        /// yeni ızgarayla ilk koşum eskisini siler.
        /// </summary>
        public static string PismisVeriyiSil()
        {
            var kume = Kume();
            if (kume == null) return "kume yok";
            int silinen = 0;
            string dizin = System.IO.Path.GetDirectoryName(
                AssetDatabase.GetAssetPath(kume))?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(dizin) && Directory.Exists(dizin))
            {
                foreach (string y in Directory.GetFiles(dizin, "*.bytes"))
                {
                    string varlik = y.Replace('\\', '/');
                    if (AssetDatabase.DeleteAsset(varlik)) silinen++;
                }
            }

            var so = new SerializedObject(kume);
            foreach (string ad in new[]
                     {
                         "cellDescs.m_Keys", "cellDescs.m_Values",
                         "m_SerializedPerSceneCellList",
                     })
            {
                var p = so.FindProperty(ad);
                if (p != null && p.isArray) p.ClearArray();
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(kume);
            AssetDatabase.SaveAssets();
            return $"{silinen} .bytes silindi, hucre listesi bosaltildi.";
        }

        /// <summary>
        /// <b>Sanal kaydırmayı kapatır</b> — GPU'yu pişirmenin dışında
        /// tutar.
        ///
        /// Sanal kaydırma (virtual offset), geometrinin içinde kalan
        /// probları dışarı iten bir <b>GPU</b> geçişidir. Fırın CPU'ya
        /// alınmıştı (7,25 GB'lik sahne girdisi 8 GB'lik karta
        /// sığmıyordu), ama bu geçiş yine karta gidiyordu ve ölçüm
        /// bunun bedelini gösterdi — <c>D_Bogaz</c> koşumu tek satırla
        /// öldü:
        ///
        /// <code>
        /// d3d12: Unrecoverable GPU device error!
        ///   UnityEditor.Lightmapping/VirtualOffsetBake:Update
        /// d3d12: upload buffer was too small … Requested: 100761624
        /// </code>
        ///
        /// Yani 100 MB'lık bir istek 20 MB'lık bir tampona
        /// yazılmaya çalışıldı. İşi CPU'ya vermek, işin
        /// <b>tamamını</b> vermek demekmiş.
        ///
        /// Bedeli ne: duvarın içinde kalan birkaç prob dışarı
        /// itilmeyecek ve orada geçersiz kalacak. APV'nin kendi
        /// geçerlilik karışımı (<c>validityThreshold</c>) bu probları
        /// zaten dışarıda bırakıyor; kaybedilen şey biraz doğruluk,
        /// kazanılan şey pişirmenin <b>bitmesi</b>.
        /// </summary>
        public static bool SanalKaydirmayiKapat()
        {
            var kume = Kume();
            if (kume == null) return false;
            var so = new SerializedObject(kume);
            var p = so.FindProperty(
                "settings.virtualOffsetSettings.useVirtualOffset");
            if (p == null) return false;
            if (!p.boolValue) return true;
            p.boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(kume);
            AssetDatabase.SaveAssets();
            return true;
        }

        /// <summary>
        /// <b>Hücre yerleşimini dondurur</b> — kısmi pişirmenin
        /// gerçek anahtarı.
        ///
        /// APV'nin hücre ızgarası küme çapındadır ve her pişirmede
        /// o an <b>yüklü</b> prob hacimlerinden yeniden hesaplanır.
        /// Tek bir semt yüklüyken ızgara başka çıkıyor ve Unity kısmi
        /// pişirmeyi <i>"incompatible cell layout"</i> diyerek sessizce
        /// atıyor.
        ///
        /// Bunun bir çaresi bütün semtleri yüklemekti ve ölçüm bedelini
        /// gösterdi: en küçük semt %6,2'ye dokuz dakikada geldi ve
        /// ilerleme hızı düşüyordu — tek semt için otuz saatin üstünde.
        /// Sebep açık: ışık hesabı bütün şehrin geometrisine karşı
        /// koşuyor.
        ///
        /// <c>freezePlacement</c> ızgarayı kümede saklı olana sabitler.
        /// Bir kez bütün semtlerle yerleştirme yapılıp dondurulduktan
        /// sonra her semt tek başına pişirilebilir ve sonuç aynı
        /// ızgaraya oturur.
        ///
        /// Bedeli: bir semt pişerken komşu semtin geometrisi sahnede
        /// olmaz, yani semt <b>sınırındaki</b> sıçrama komşudan gelen
        /// ışığı görmez. Semtler coğrafi olarak ayrı bölgeler ve taban
        /// (arazi) her koşumda yüklü; hata sınırda kalır. Alternatifi
        /// hiç pişmemesiydi.
        /// </summary>
        public static bool YerlesimiDondur(bool donuk)
        {
            var kume = Kume();
            if (kume == null) return false;
            var so = new SerializedObject(kume);
            var p = so.FindProperty("freezePlacement");
            if (p == null) return false;
            p.boolValue = donuk;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(kume);
            AssetDatabase.SaveAssets();
            return true;
        }

        /// <summary>
        /// <b>Pişmiş verinin imzası</b> — hücre sayısı, veri
        /// dosyalarının toplam boyu ve en son yazılma anı.
        ///
        /// Neden sayı yetmiyor: <c>cellDescs</c> <b>küme çapında</b>
        /// bir listedir. İkinci semt hiçbir şey yazmadığı hâlde koşum
        /// "5 hücre" gördü ve başarılı döndü — çünkü o 5 hücreyi
        /// <b>birinci</b> semt yazmıştı. Bir denetim, başkasının işiyle
        /// karşılanabiliyorsa denetim değildir.
        /// </summary>
        public static string PismisVeriImzasi()
        {
            var kume = Kume();
            if (kume == null) return "kume yok";
            string dizin = System.IO.Path.GetDirectoryName(
                AssetDatabase.GetAssetPath(kume))?.Replace('\\', '/');
            long bayt = 0;
            long an = 0;
            if (!string.IsNullOrEmpty(dizin) && Directory.Exists(dizin))
            {
                foreach (string y in Directory.GetFiles(dizin, "*.bytes"))
                {
                    var f = new FileInfo(y);
                    bayt += f.Length;
                    long t = f.LastWriteTimeUtc.Ticks;
                    if (t > an) an = t;
                }
            }
            return $"{HucreSayisi()} hucre, {bayt} bayt, {an}";
        }

        /// <summary>
        /// Prob aralığını kümeye yazar — <b>yalnız ölçüm koşumları
        /// için</b>.
        ///
        /// Sayının sahibi <see cref="ProbAraligi"/>'dir ve öyle kalır.
        /// Bu yol, "hangi aralık 67 milyon prob sınırının altına
        /// sığıyor" sorusunu her seferinde kod derleyip yeniden
        /// başlatmadan denemek içindir; bulunan sayı sabite yazılır.
        /// </summary>
        public static void AraligiYaz(float aralik)
        {
            var kume = Kume();
            if (kume == null) return;
            var so = new SerializedObject(kume);
            var p = so.FindProperty("minDistanceBetweenProbes");
            if (p != null) p.floatValue = aralik;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(kume);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Toplu kip girişi: kurar, sahneleri kaydeder, çıkar.
        /// <c>-executeMethod Hezarfen.Editor.Lighting.SemtProblari.KurToplu</c>
        /// </summary>
        public static void KurToplu()
        {
            int n = Kur(out string rapor);
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            Debug.Log($"[Hezarfen] Semt problari: {n} semt hazir.\n{rapor}");
            EditorApplication.Exit(0);
        }

        [MenuItem("Hezarfen/Aydinlatma/Semt problarini kur")]
        public static void KurMenu()
        {
            int n = Kur(out string rapor);
            Debug.Log($"[Hezarfen] Semt problari: {n} semt hazir.\n{rapor}\n"
                      + "SONRAKI ADIM: Hezarfen -> Aydinlatma -> Problari pisir");
        }

        /// <summary>
        /// Semt sahnelerini pişirme kümesine bağlar ve her birine
        /// kendi prob hacmini koyar. Sahneler <b>zaten açık olmalı</b>
        /// (toplu pişirme onları ek olarak açar); açık değilse burada
        /// açılır.
        /// </summary>
        public static int Kur(out string rapor)
        {
            var satirlar = new List<string>();
            var kume = Kume();
            if (kume == null)
            {
                rapor = "Pisirme kumesi bulunamadi.";
                return 0;
            }

            // ARALIK VE COK SAHNE KIPI — kumenin kendi alanlari.
            var so = new SerializedObject(kume);
            var aralik = so.FindProperty("minDistanceBetweenProbes");
            if (aralik != null) aralik.floatValue = ProbAraligi;
            var tek = so.FindProperty("singleSceneMode");
            if (tek != null) tek.boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(kume);
            satirlar.Add($"Prob araligi {ProbAraligi:0.#} m, cok sahne kipi acik.");
            satirlar.Add("Sanal kaydirma: "
                         + (SanalKaydirmayiKapat() ? "KAPALI" : "BULUNAMADI"));

            // TABAN SAHNEDEKI DUNYA BOYU HACIM KALDIRILIR.
            //
            // Taban sahnede `mode: 2` (Global) bir prob hacmi vardi ve
            // orada yasayan tek buyuk sey ARAZI: 15 km x 15 km. APV
            // problari isiga katilan geometrinin cevresine koyar; 3 m
            // aralikla bir arazi yuzeyi tek basina 27 milyon prob eder
            // ve 67 milyonluk sinir bunun ustune sehri koyunca asilir.
            //
            // Arazinin sicrama isigina KATILMASI icin hacme gerek yok —
            // katilim `ContributeGI` bayragiyla olur ve isik yolu yine
            // arazi zeminine carpar. Gerekli olan tek sey, probun
            // SEHRIN oldugu yerde bulunmasi.
            {
                var ts = EditorSceneManager.GetSceneByPath(TabanSahne);
                if (!ts.isLoaded)
                    ts = EditorSceneManager.OpenScene(
                        TabanSahne, OpenSceneMode.Additive);
                var dunya = ts.GetRootGameObjects()
                    .SelectMany(g => g.GetComponentsInChildren<ProbeVolume>(true))
                    .ToList();
                foreach (var pv0 in dunya)
                    Object.DestroyImmediate(pv0.gameObject);
                if (dunya.Count > 0)
                {
                    EditorSceneManager.MarkSceneDirty(ts);
                    satirlar.Add($"Taban sahne: {dunya.Count} dunya boyu "
                                 + "prob hacmi KALDIRILDI.");
                }
            }

            int n = 0;
            foreach (string yol in Semtler())
            {
                string ad = Path.GetFileNameWithoutExtension(yol);
                var sahne = EditorSceneManager.GetSceneByPath(yol);
                if (!sahne.isLoaded)
                    sahne = EditorSceneManager.OpenScene(
                        yol, OpenSceneMode.Additive);

                string guid = AssetDatabase.AssetPathToGUID(yol);
                bool eklendi = kume.TryAddScene(guid);

                // HACIM SEMTIN KENDI SAHNESINDE YASAR.
                //
                // APV verisi sahne sahne saklanir; hacim baska bir
                // sahnede olsaydi semt akisla gidip geldiginde probu
                // gelmezdi.
                var pv = sahne.GetRootGameObjects()
                    .SelectMany(g => g.GetComponentsInChildren<ProbeVolume>(true))
                    .FirstOrDefault();
                bool yeni = pv == null;
                if (yeni)
                {
                    var go = new GameObject($"PV_{ad}");
                    EditorSceneManager.MoveGameObjectToScene(go, sahne);
                    pv = go.AddComponent<ProbeVolume>();
                }
                // HACIM SEMTIN KENDI SINIRI — VE ARTIK GERCEKTEN OYLE.
                //
                // ONCE `Mode.Global` YAZILDI VE OLCUM ONU ELEDI.
                //
                // Gerekce mantikliydi: "hacim sahnenin sinirindan
                // turer, her pisirmede yeniden hesaplanir, eskiyecek
                // sayi kalmaz". Ama `Global` SAHNENIN degil, YUKLU
                // OLAN HER SEYIN sinirini alir — ve bu kurulum sekiz
                // semti birlikte aciyor. Sonuc pisirme kumesinin kendi
                // varliginda yaziliydi:
                //
                //   m_Extent: {x: 7776, y: 364.5, z: 7897.5}
                //
                // Yani her semtin "kendi" hacmi 15,5 km x 0,73 km x
                // 15,8 km, ve SEKIZI DE AYNI KUTU. Bedeli de kayitta:
                // "the number of APV probes exceeds the current system
                // limit of 67.180.350". Yerlestirme daha basta dustu,
                // sifir hucre yazildi, kosum yine "basarili" dedi.
                //
                // Dogru olcu semtin KENDI cizicileridir. Kutu onlarin
                // birlesiminden turer; her pisirmede yeniden hesaplanir,
                // yani `Global`in vaat ettigi "eskimeyen sayi" korunur
                // ama olculen sey dogru sey olur.
                var b = SemtSiniri(sahne, out var disarida);
                if (b.size == Vector3.zero)
                {
                    satirlar.Add($"{ad}: cizici yok, hacim atlandi.");
                    continue;
                }
                // PAY: prob duvarin DISINDA da olmali, yoksa cephenin
                // onundeki hava karanlik kalir. 8 m, sokak genisligi
                // mertebesinde.
                b.Expand(SinirPayi * 2f);
                pv.transform.position = b.center;
                pv.mode = ProbeVolume.Mode.Local;
                pv.size = b.size;
                pv.overridesSubdivLevels = false;
                EditorUtility.SetDirty(pv);
                EditorSceneManager.MarkSceneDirty(sahne);
                n++;
                satirlar.Add($"{ad}: hacim {(yeni ? "KURULDU" : "vardi")} "
                             + $"{b.size.x:0}x{b.size.y:0}x{b.size.z:0} m, "
                             + $"kumeye {(eklendi ? "EKLENDI" : "zaten bagliydi")}.");
                if (disarida.Count > 0)
                    satirlar.Add($"  disarida: {string.Join(", ", disarida)}");
            }

            AssetDatabase.SaveAssets();
            rapor = string.Join("\n", satirlar);
            return n;
        }
    }
}
