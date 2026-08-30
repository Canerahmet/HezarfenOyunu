using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Hezarfen.Editor.Diagnostics
{
    /// <summary>
    /// <b>Yapılar yere değiyor mu — ölçer, düzeltmez.</b>
    ///
    /// Caner (2026-08-29, oynarken): *"bazi evler yere temas etmiyor,
    /// merdivenler yanlis yerlere koyulmus vs."*
    ///
    /// ## Neden önce ölçüm
    ///
    /// Yerleştirme kodunu okuyunca tasarım <b>doğru</b> görünüyor: ev ayak
    /// izinin en yüksek köşesine oturuyor ve altındaki boşluğu taş kaide
    /// dolduruyor (yamaç evinin gerçekte yapıldığı şey). Yani "kod yanlış"
    /// diyebileceğim bir satır yok — ama Caner boşluğu <b>görüyor</b>.
    /// Aradaki fark ancak sayıyla kapanır.
    ///
    /// Bu projede aynı tuzağa üç kez düşüldü: bir kez aydınlatma kusuru
    /// geometri hatası sanıldı, bir kez sayaç "3.175 ağaç çizildi" derken
    /// her ağacın yaprağı eksikti, bir kez de test kendi artığını saydı.
    /// <b>Render bir gözlemdir, kanıt değil.</b>
    ///
    /// ## Ne ölçülüyor
    ///
    /// Her yapının ayak izinin <b>iç %60</b>'ında bir ızgara noktasından
    /// aşağı ışın atılır ve yapının tabanı ile altındaki ilk yüzey (arazi,
    /// kaide, kaldırım) arasındaki düşü ölçülür. İç %60 kasıtlı: Osmanlı
    /// evinin <b>cumbası</b> zaten havada durur ve bu bir kusur değil,
    /// mimarîdir; bütün ayak izini taramak her cumbayı "havada ev" diye
    /// sayardı — yanlış cetvel, olmayan bir sorunu gösterirdi.
    /// </summary>
    public static class ZeminDenetimi
    {
        private const string TerrainScene =
            "Assets/_Project/Scenes/Faz1_Terrain.unity";
        private const string DistrictDir =
            "Assets/_Project/Scenes/Districts";

        /// <summary>Görünür sayılan en küçük boşluk (m).</summary>
        public const float GorunurBosluk = 0.30f;

        /// <summary>
        /// Ayak izinin taranan iç oranı.
        ///
        /// İlk denemede %60'tı ve <b>%57 yapıyı boşluklu</b> gösterdi — o
        /// kadarı olsa oyun oynanamazdı. Sebep cetveldi: ölçü kutusu
        /// çizicinin sınırlarıdır, yani <b>saçak ve cumbayı da kapsar</b>.
        /// Saçağın altı doğal olarak boştur; %60'ı hâlâ saçağın altına
        /// düşüyordu. %35 duvarın kendi içinde kalır.
        /// </summary>
        public const float IcOran = 0.35f;

        private sealed class Kayit
        {
            public string semt, ad;
            public Vector3 konum;
            public float bosluk;        // taban ile altindaki yuzey arasi
            public float egim;          // arazi egimi (derece)
            public bool carpisticiYok;
        }

        [MenuItem("Hezarfen/Denetim/Yapilar yere degiyor mu")]
        public static void Olc()
        {
            var rapor = new List<Kayit>();
            _tumEgimler.Clear();
            _tumSayim.Clear();
            int toplamYapi = 0;

            EditorSceneManager.OpenScene(TerrainScene, OpenSceneMode.Single);
            var arazi = Object.FindAnyObjectByType<Terrain>();
            if (arazi == null)
            {
                Debug.LogError("[Hezarfen] TR_Istanbul yok — once GIS/Terrain.");
                return;
            }

            // ANA SAHNENIN KENDI YAPILARI DA OLCULUR.
            //
            // Bu bir KOR NOKTAYDI: denetim yalnizca semt sahnelerini
            // tariyordu. Surlar (SUR_Kara, SUR_Galata), landmark'lar ve
            // iskeleler Faz1_Terrain'in KENDI kokleridir ve hicbiri hic
            // olculmedi. Yani "18.338 yapi, sifir bosluk" dedigim sey
            // sehrin en buyuk ve en cok goze carpan yapilarini
            // KAPSAMIYORDU — Caner havada ev gormeye devam etti ve
            // hakliydi.
            //
            // Ustelik dere yataklari yeni oyuldu: arazi degisti ve bu
            // yapilar semtler gibi yeniden uretilmiyor.
            {
                var ana = SceneManager.GetActiveScene();
                var gorulenAna = new HashSet<Transform>();
                foreach (var kok2 in ana.GetRootGameObjects())
                {
                    if (!AnaSahneOlculur(kok2.name)) continue;
                    foreach (var mf in kok2.GetComponentsInChildren<MeshFilter>(false))
                    {
                        var t = YapiKoku(mf.transform);
                        if (t == null || !gorulenAna.Add(t)) continue;
                        if (!Sayilir(t.name)) continue;
                        toplamYapi++;
                        var k = Yapiyi(t, "ANA:" + kok2.name, arazi);
                        if (k != null) rapor.Add(k);
                    }
                }
            }

            foreach (string yol in Directory
                         .GetFiles(DistrictDir, "*.unity")
                         .OrderBy(x => x))
            {
                string semt = Path.GetFileNameWithoutExtension(yol);
                // TEKNELER SUDA DURUR — SEMTE GORE DEGIL, KOKE GORE ELENIR.
                //
                // Once butun bir semt (`D_Tekneler`) atlaniyordu. Tekneler
                // artik kendi sularinin semtine yaziliyor (D_Halic,
                // D_Bogaz) ve o semtlerde iskele, sur, dukkan da var. Semti
                // atlamak simdi gercek yapilari da denetimden cikarirdi;
                // teknenin kendisini atlamak yeter (bkz. `TekneMi`).
                var sahne = EditorSceneManager.OpenScene(
                    yol.Replace('\\', '/'), OpenSceneMode.Additive);

                // TEKILLESTIR: bir evin birden cok MeshFilter'i vardir ve
                // ilk denemede her biri ayri bir "yapi" sayildi — 55.162
                // yapi, gercegin katbekat ustu. Sayinin kendisi de bir
                // olcumdur ve o sayi yanlisti.
                var gorulen = new HashSet<Transform>();
                foreach (var kok in sahne.GetRootGameObjects())
                    foreach (var mf in kok.GetComponentsInChildren<MeshFilter>(false))
                    {
                        var t = YapiKoku(mf.transform);
                        if (t == null || !gorulen.Add(t)) continue;
                        if (TekneMi(t)) continue;          // suda durur
                        if (!Sayilir(t.name)) continue;
                        toplamYapi++;
                        var k = Yapiyi(t, semt, arazi);
                        if (k != null) rapor.Add(k);
                    }

                EditorSceneManager.CloseScene(sahne, true);
            }

            Yaz(rapor, toplamYapi);
        }

        /// <summary>
        /// <b>Kaldırım ve merdivenler yerinde mi.</b>
        ///
        /// Caner (2026-08-29): *"merdivenler yanlis yerlere koyulmus vs."*
        ///
        /// Merdiven bu şehirde <b>elle konmaz</b>: kaldırım şeridi araziyi
        /// izler ve kot farkı bir rıht (0,17 m) biriktiğinde kendiliğinden
        /// basamaklanır (RESEARCH §4.1 — merdivenli sokaklar). Yani "yanlış
        /// yere konmuş merdiven" diye bir şey yok; olan şey, <b>yürünen
        /// yüzeyin araziden kopması</b>. Kopuş iki yönde de kusurdur:
        /// yüzey araziden yüksekte kalırsa basamak havada durur, alçakta
        /// kalırsa toprağa gömülür.
        ///
        /// Ölçülen: kaldırım mesh'inin köşe noktalarıyla o noktadaki arazi
        /// kotu arasındaki fark.
        /// </summary>
        [MenuItem("Hezarfen/Denetim/Kaldirim ve merdivenler yerinde mi")]
        public static void KaldirimOlc()
        {
            EditorSceneManager.OpenScene(TerrainScene, OpenSceneMode.Single);
            var arazi = Object.FindAnyObjectByType<Terrain>();
            if (arazi == null)
            {
                Debug.LogError("[Hezarfen] TR_Istanbul yok.");
                return;
            }
            float ay = arazi.transform.position.y;

            // OLCULEN SEY: BORDUR YERE INIYOR MU.
            //
            // Ilk denemem "yuzey arazinin kac metre ustunde" diye sordu ve
            // %22 sapma buldu. O sayi bir kusur DEGILDI: kaldirim araziyi
            // birebir izlemez, kesitin en yuksek noktasina oturur ve altta
            // kalan bosluk BORDURLE kapanir (evin tas kaidesiyle ayni
            // mantik). Yani yuzeyin arazinin ustunde olmasi tasarimin
            // kendisi.
            //
            // Gorunur kusur, bordurun yere INMEMESIdir. Onu olcmek icin
            // mesh'in koseleri 2 m'lik hucrelere bolunur ve her hucrede
            // EN ALCAK kose ile arazi karsilastirilir: o hucrede mesh'in
            // en asagi inen noktasi hala arazinin ustundeyse, orada
            // gercekten hava vardir.
            // HUCRE SOKAK GENISLIGI KADAR (4,6 m sokak — ADR 0016).
            //
            // 2 m'lik hucre kesiti ikiye boluyordu ve ortadaki BASAMAK
            // RIHTLARI ayri hucreye dusuyordu. Riht dikey yayilir, yani
            // "kenar hucresi" sayilir; oysa iki yani bordurle kapalidir
            // ve altinda gorunur bir bosluk yoktur. Hucre kesitin
            // tamamini kapsayinca rihtin yanindaki bordur ayni hucreye
            // duser ve en alcak nokta dogru okunur.
            const float Hucre = 5.0f;
            // NOT: kalan kuyruk (kenar hucrelerinin ~%5'i) iki kez
            // aciklanmaya calisildi ve iki aciklama da OLCUMLE yanlislandi:
            // once "ornekler arasi cukur" denildi (komsu en kucugu
            // eklendi — oran degismedi), sonra "basamak rihtlari" denildi
            // (hucre sokak genisligine cikarildi — yine degismedi).
            // Yani bu kuyruk gercek: %95 dogru oturuyor, %5 oturmuyor.
            // Sebebi henuz bilinmiyor ve bilinmedigi burada yaziyor.
            // Hucre basina EN ALCAK ve EN YUKSEK kose.
            //
            // Yukseklik farki neden lazim: seridin ORTASINDAKI hucrelerde
            // yalnizca yurunen yuzey vardir, asagi inen hicbir geometri
            // yoktur — ve orada gorunur bir bosluk da yoktur, cunku serit
            // iki yandan BORDURLE kapalidir. O hucreleri saymak, kapali
            // bir kutunun icini "hava" diye raporlamak olurdu.
            //
            // Kenar hucresinin isareti sudur: mesh orada DUSEY olarak
            // yayilir (bordur ya da basamak rihti). Duz bir hucrede
            // en alcak ile en yuksek kose ayni kottadir.
            var hucreler = new Dictionary<(int, int, string),
                                          (float min, float max, float zemin,
                                           Vector3 p)>();
            int meshSayisi = 0;

            // ANA SAHNE de taranir: yollar ve bostan duvarlari orada.
            foreach (var kok2 in SceneManager.GetActiveScene().GetRootGameObjects())
                foreach (var mf in kok2.GetComponentsInChildren<MeshFilter>(true))
                {
                    string mad2 = mf.gameObject.name;
                    if (!mad2.StartsWith("Kaldirim") && !mad2.StartsWith("Yol_")
                        && !mad2.StartsWith("BostanDuvarlari")) continue;
                    if (mf.sharedMesh == null) continue;
                    meshSayisi++;
                    var verts2 = mf.sharedMesh.vertices;
                    for (int i = 0; i < verts2.Length; i += 3)
                    {
                        var d2 = mf.transform.TransformPoint(verts2[i]);
                        var an2 = (Mathf.FloorToInt(d2.x / Hucre),
                                   Mathf.FloorToInt(d2.z / Hucre), "ANA");
                        if (hucreler.TryGetValue(an2, out var e2))
                        {
                            if (d2.y < e2.min)
                                hucreler[an2] = (d2.y, e2.max,
                                                 arazi.SampleHeight(d2) + ay, d2);
                            else if (d2.y > e2.max)
                                hucreler[an2] = (e2.min, d2.y, e2.zemin, e2.p);
                        }
                        else hucreler[an2] = (d2.y, d2.y,
                                              arazi.SampleHeight(d2) + ay, d2);
                    }
                }

            foreach (string yol in Directory.GetFiles(DistrictDir, "*.unity")
                                            .OrderBy(x => x))
            {
                string semt = Path.GetFileNameWithoutExtension(yol);
                var sahne = EditorSceneManager.OpenScene(
                    yol.Replace(Path.DirectorySeparatorChar, '/'),
                    OpenSceneMode.Additive);

                foreach (var kok in sahne.GetRootGameObjects())
                    foreach (var mf in kok.GetComponentsInChildren<MeshFilter>(true))
                    {
                        string mad = mf.gameObject.name;
                        if (!mad.StartsWith("Kaldirim")
                            && !mad.StartsWith("Yol_")
                            && !mad.StartsWith("BostanDuvarlari")) continue;
                        if (mf.sharedMesh == null) continue;
                        meshSayisi++;

                        var verts = mf.sharedMesh.vertices;
                        for (int i = 0; i < verts.Length; i += 3)
                        {
                            var d = mf.transform.TransformPoint(verts[i]);
                            var anahtar = (Mathf.FloorToInt(d.x / Hucre),
                                           Mathf.FloorToInt(d.z / Hucre),
                                           semt);
                            if (hucreler.TryGetValue(anahtar, out var eski))
                            {
                                if (d.y < eski.min)
                                    hucreler[anahtar] =
                                        (d.y, eski.max,
                                         arazi.SampleHeight(d) + ay, d);
                                else if (d.y > eski.max)
                                    hucreler[anahtar] =
                                        (eski.min, d.y, eski.zemin, eski.p);
                            }
                            else
                            {
                                hucreler[anahtar] =
                                    (d.y, d.y, arazi.SampleHeight(d) + ay, d);
                            }
                        }
                    }

                EditorSceneManager.CloseScene(sahne, true);
            }

            var farklar = new List<float>();
            var kotu = new List<(string semt, Vector3 nokta, float fark)>();
            int duzAtlanan = 0;
            foreach (var kv in hucreler)
            {
                // Duz hucre: seridin ic kismi, iki yandan bordurle kapali.
                if (kv.Value.max - kv.Value.min < 0.05f) { duzAtlanan++; continue; }

                float fark = kv.Value.min - kv.Value.zemin;
                farklar.Add(fark);
                if (fark > 0.3f)
                    kotu.Add((kv.Key.Item3, kv.Value.p, fark));
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("# Kaldırım ve merdiven denetimi");
            sb.AppendLine();
            sb.AppendLine("Merdiven bu şehirde **elle konmaz**: kaldırım şeridi");
            sb.AppendLine("araziyi izler ve kot farkı bir rıht (0,17 m) biriktiğinde");
            sb.AppendLine("kendiliğinden basamaklanır (RESEARCH §4.1). Yani \"yanlış");
            sb.AppendLine("yere konmuş merdiven\" diye bir şey yok; olabilecek kusur,");
            sb.AppendLine("**bordürün yere inmemesi** — yürünen yüzeyin altında hava");
            sb.AppendLine("kalması.");
            sb.AppendLine();
            sb.AppendLine($"Ölçülen kaldırım mesh'i: **{meshSayisi}**  ");
            sb.AppendLine($"{Hucre:0.#} m'lik hücre: **{hucreler.Count}**  ");
            sb.AppendLine($"Bunlardan **{duzAtlanan}** tanesi şeridin düz iç "
                          + "kısmı — ölçüme girmez, çünkü orada aşağı inen "
                          + "geometri de görünür bir kenar da yok.  ");
            sb.AppendLine($"Ölçülen kenar hücresi: **{farklar.Count}**");
            sb.AppendLine();

            if (farklar.Count > 0)
            {
                var f = farklar.OrderBy(x => x).ToList();
                sb.AppendLine("Her hücrede mesh'in **en alçak** noktası − arazi:");
                sb.AppendLine();
                sb.AppendLine($"medyan {f[f.Count / 2]:+0.00;-0.00} m · "
                              + $"p90 {f[(int)(f.Count * 0.9f)]:+0.00;-0.00} m · "
                              + $"p99 {f[(int)(f.Count * 0.99f)]:+0.00;-0.00} m · "
                              + $"en büyük {f[f.Count - 1]:+0.00;-0.00} m");
                sb.AppendLine();
                sb.AppendLine($"Hava kalan hücre (> 0,30 m): **{kotu.Count}** "
                              + $"(%{100f * kotu.Count / farklar.Count:0.0})");
                sb.AppendLine();
                sb.AppendLine("> Negatif sayı bordürün araziye **gömüldüğü** anlamına");
                sb.AppendLine("> gelir ve kusur değildir — kenarın açıkta kalmaması için");
                sb.AppendLine("> kasıtlı olarak gömülür.");

                if (kotu.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("## En kötü 30 hücre");
                    sb.AppendLine();
                    sb.AppendLine("| semt | boşluk (m) | konum |");
                    sb.AppendLine("|---|---:|---|");
                    foreach (var k in kotu.OrderByDescending(x => x.fark).Take(30))
                        sb.AppendLine($"| {k.semt} | {k.fark:0.00} | "
                                      + $"({k.nokta.x:0}, {k.nokta.y:0}, "
                                      + $"{k.nokta.z:0}) |");
                }
            }

            Directory.CreateDirectory("../../renders/denetim");
            File.WriteAllText("../../renders/denetim/kaldirim_denetimi.md",
                              sb.ToString());
            Debug.Log($"[Hezarfen] Kaldirim denetimi: {meshSayisi} mesh, "
                      + $"{farklar.Count} kenar hucresi, "
                      + $"{kotu.Count} boslukllu.");
        }

        /// <summary>
        /// Ana sahnenin bu kökü ölçülür mü — <b>RED listesi</b>, izin
        /// listesi değil.
        ///
        /// Burada bir izin listesi vardı (<c>SUR_</c>, <c>LANDMARK</c>,
        /// <c>ISKELE</c>, <c>GIS_</c>, <c>OKMEYDANI</c>) ve aynı hata
        /// <b>üçüncü kez</b> aynı biçimde oldu:
        ///
        /// 1. Denetim yalnız semt sahnelerini tarıyordu → surlar ve
        ///    landmark'lar hiç ölçülmedi; Ayasofya 2,85 m havadaydı.
        /// 2. İzin listesi eklendi → yeni gelen <c>KIRSAL_1632</c> kökü
        ///    (yollar ve bostan duvarları) listede olmadığı için yine
        ///    ölçülmedi.
        ///
        /// İzin listesi <b>sessizce</b> kaçırır: yeni bir kök eklendiğinde
        /// hiçbir şey bozulmaz, sadece görünmez olur. Red listesi
        /// <b>gürültülü</b> kaçırır: yeni kök ölçüme girer, yanlış
        /// pozitif verirse görürüz ve elemeyi yazarız.
        ///
        /// Yapı olmayan şeyler açıkça sayılıyor; geri kalan her şey ölçülür.
        /// </summary>
        /// <summary>
        /// Bu nesne bir tekne mi — tekne suda durur, yere değmez.
        ///
        /// Ada göre elemek ("Kayik" ile başlıyorsa") denendi ve yanlıştı:
        /// pereme ve mavna da tekne ve adları öyle başlamıyor. Kök nesne
        /// <see cref="Gis.BoatScatter.RootName"/> ise soru bitiyor.
        /// </summary>
        private static bool TekneMi(Transform t)
        {
            for (var k = t; k != null; k = k.parent)
                if (k.name == Gis.BoatScatter.RootName) return true;
            return false;
        }

        private static bool AnaSahneOlculur(string ad)
        {
            // Arazi, su, isik, gokyuzu ve sistem nesneleri yapi degildir.
            string[] disarida =
            {
                "TR_", "WATER_", "SUN_", "SkyAndFog", "AYDINLATMA",
                "DERELER_",              // su yuzeyi; yere degmez, degmemeli
                "AGAC_CIZICI", "ZAMAN", "SEHIR_NPC", "ARANMA", "BARK",
                "HAVA", "SEMT_AKISI", "KAYIT", "HUD", "OYUNCU",
                "Main Camera", "EventSystem", "Canvas",
            };
            foreach (string d in disarida)
                if (ad.StartsWith(d)) return false;
            return true;
        }

        /// <summary>
        /// Yapının kökü: prefab örneğinin en dış nesnesi. Alt parçaları tek
        /// tek ölçmek bir evi on kez sayardı.
        /// </summary>
        private static Transform YapiKoku(Transform t)
        {
            var kok = PrefabUtility.GetOutermostPrefabInstanceRoot(t.gameObject);
            if (kok != null) return kok.transform;

            // PREFAB OLMAYAN YAPILAR DA OLCULUR.
            //
            // Burada `null` donuluyordu ve bu, ayni kor noktanin DORDUNCU
            // bicimiydi:
            //   1. denetim yalniz semt sahnelerini tariyordu,
            //   2. ana sahnede izin listesi kullaniyordu,
            //   3. izin listesi yeni gelen KIRSAL_1632'yi kacirdi,
            //   4. red listesine cevrildi ama sayi DEGISMEDI (18.605),
            //      cunku yol ve bostan duvarlari uretilmis mesh'ler —
            //      prefab ORNEGI degiller ve bu satir onlari eliyordu.
            //
            // Sayinin degismemesi bir olcumdur: kapsam genisledi ama
            // hicbir yeni yapi girmedi, demek ki eleme baska yerdeydi.
            return t;
        }

        /// <summary>
        /// Ölçülen şey <b>yapı</b> mı. Kaide, kaldırım, ağaç ve arazi
        /// örtüsü ölçüme girmez: kaide zaten boşluğu KAPATAN şeydir, onu
        /// da ölçmek boşluğu iki kez saymak olurdu.
        /// </summary>
        private static bool Sayilir(string ad)
        {
            // BIRLESIK YUZEYLER YAPI DEGIL — BASKA CETVELLE OLCULUR.
            //
            // Kaide, kaldirim, yol ve bostan duvari kilometrelerce uzanan
            // TEK mesh'lerdir. Yapi cetveli ayak izinin ic kismini tarar;
            // 5 km genisliginde bir kutunun "ici" vadinin ustunde havada
            // bir noktadir ve olcum 14 m bosluk uydurur. Nitekim uydurdu.
            //
            // Dogru cetvel yuzey denetimindedir (Kaldirim ve merdivenler
            // yerinde mi): mesh 5 m'lik hucrelere bolunur ve her hucrede
            // EN ALCAK nokta araziyle karsilastirilir.
            if (ad.StartsWith("Kaide") || ad.StartsWith("Kaldirim")) return false;
            if (ad.StartsWith("Yol_") || ad.StartsWith("BostanDuvarlari"))
                return false;

            // AGAC VE MEZAR TASI OLCULMEZ — ve bu eleme BIR KEZ KACTI.
            //
            // Filtre `StartsWith("Cinar")` diyordu, prefab adi ise
            // `PF_Cinar_A`. Yani eleme hic calismadi ve cinarlarin %50'si
            // "havada" diye raporlandi. Sebep sudur: olcum yapinin ayak
            // izinin ic kismini tarar; bir agacin "ayak izi" TACIDIR ve
            // govdeden 2 m otedeki arazi yamacta 1,4 m asagida olabilir.
            // Agac yamaca dik durur, tacinin altini doldurmaz.
            //
            // Ayni sey mezar tasi icin de gecerli: yarim metrelik bir tas
            // egimli hazirede ne kaide ister ne alir.
            if (ad.Contains("Agac") || ad.Contains("Cinar")
                || ad.Contains("Servi") || ad.Contains("Mezar"))
                return false;

            // SUDA DURAN SEY YERE DEGMEZ — degmemeli.
            //
            // Ilk olcumde en kotu 40 yapinin hepsi kayikti: taban ile
            // DENIZ TABANI arasi 11,78 m. Bu bir kusur degil, kayigin
            // tanimi. Yanlis cetvel bir kez daha olmayan bir sorunu
            // listenin basina koydu.
            if (ad.Contains("Kayik") || ad.Contains("Tekne")
                || ad.Contains("Kadirga") || ad.Contains("Sandal"))
                return false;
            return true;
        }

        private static readonly HashSet<Transform> _kendi = new HashSet<Transform>();

        /// <summary>
        /// ÖLÇÜLEN her yapının eğimi — boşluklu olanlar da olmayanlar da.
        ///
        /// Karşılaştırma noktası olmadan "boşluklu evlerin eğimi medyan
        /// 25°" cümlesi hiçbir şey söylemez: mahallenin kendisi zaten
        /// yamaçtaysa o sayı sıradandır. Fark varsa sebep eğimdir.
        /// </summary>
        private static readonly List<float> _tumEgimler = new List<float>();

        /// <summary>
        /// Prefab adına göre ölçülen toplam sayı.
        ///
        /// "PF_AvluDuvar'ın 300'ü boşluklu" tek başına bir şey söylemez;
        /// sahnede 3.000 tane varsa oran %10, 320 tane varsa %94 ve ikinci
        /// durumda kusur o yapının YERLEŞTİRİCİSİNDEDİR. Oranı görmeden
        /// hangi kod yolunun bozuk olduğu bilinemez.
        /// </summary>
        private static readonly Dictionary<string, int> _tumSayim =
            new Dictionary<string, int>();

        private static Kayit Yapiyi(Transform t, string semt, Terrain arazi)
        {
            var ciziciler = t.GetComponentsInChildren<MeshRenderer>(false);
            if (ciziciler.Length == 0) return null;

            // AYAK IZI YAPININ KENDI EKSENINDE OLCULUR.
            //
            // Onceki hali `Renderer.bounds` kullaniyordu; o kutu DUNYA
            // eksenlerine hizalidir. Ev ise sokaga donuktur. 45 derece
            // donmus bir dikdortgenin dunya kutusu gercek ayak izinden
            // cok daha genistir ve o kutunun "ic %35"i pekala evin
            // DISINA, iki ev arasindaki bosluga dusebilir — orada kaide
            // yok, cunku orada yapi da yok. Yani olcum, olmayan bir
            // bosluk uydurur.
            //
            // Bu projede yanlis cetvel simdiye dek uc kez olmayan bir
            // sorunu gosterdi. Ayak izi artik yapinin kendi ekseninde.
            var yerel = YerelKutu(t, ciziciler);
            var kutu = new Bounds(t.TransformPoint(yerel.center), Vector3.zero);
            foreach (var r in ciziciler) kutu.Encapsulate(r.bounds);

            _kendi.Clear();
            foreach (var c in t.GetComponentsInChildren<Collider>(true))
                _kendi.Add(c.transform);

            bool carpisticiYok =
                t.GetComponentsInChildren<Collider>(true).Length == 0;

            // Ayak izinin ic kismindaki 3x3 izgara — YAPININ EKSENINDE.
            float ex = yerel.extents.x * IcOran, ez = yerel.extents.z * IcOran;
            float enBuyukDusu = 0f;
            bool olculdu = false;

            for (int i = -1; i <= 1; i++)
                for (int j = -1; j <= 1; j++)
                {
                    var yerelNokta = new Vector3(yerel.center.x + i * ex,
                                                 yerel.min.y,
                                                 yerel.center.z + j * ez);
                    var taban = t.TransformPoint(yerelNokta);
                    var p = taban + Vector3.up * 0.5f;
                    float? yuzey = AltindakiYuzey(p, t);
                    if (yuzey == null) continue;
                    olculdu = true;
                    float dusu = taban.y - yuzey.Value;
                    if (dusu > enBuyukDusu) enBuyukDusu = dusu;
                }

            if (!olculdu) return null;

            // SUDAKI YAPI YERE DEGMEZ — degmemeli.
            //
            // Iskele kaziklarin uzerinde durur, Kiz Kulesi bir kayaligin.
            // Ikisinin de altindaki "zemin" deniz TABANIdir ve 9-10 m
            // asagidadir. Bunu kusur saymak, kayiklari deniz tabanina
            // gore olcup "havada" demekle ayni hata olurdu — bu turda
            // bir kez yapildi.
            //
            // Eleme ADA gore degil KOTA gore: isim listesi eninde sonunda
            // birini kacirir (pereme kacirmisti).
            float zeminKotu = arazi != null
                ? arazi.SampleHeight(kutu.center) + arazi.transform.position.y
                : 1f;
            if (zeminKotu < 0.5f) return null;
            _tumEgimler.Add(Egim(arazi, kutu.center));
            _tumSayim.TryGetValue(t.name, out int adet);
            _tumSayim[t.name] = adet + 1;
            if (enBuyukDusu <= GorunurBosluk && !carpisticiYok) return null;

            return new Kayit
            {
                semt = semt,
                ad = t.name,
                konum = kutu.center,
                egim = Egim(arazi, kutu.center),
                bosluk = enBuyukDusu,
                carpisticiYok = carpisticiYok,
            };
        }

        /// <summary>
        /// Yapının <b>kendi ekseninde</b> ölçü kutusu.
        ///
        /// Her çizicinin mesh sınırları yapının yerel uzayına taşınır ve
        /// orada birleştirilir. Dünya kutusu kullanmak dönmüş bir yapıda
        /// gerçek ayak izini şişirir.
        /// </summary>
        private static Bounds YerelKutu(Transform kok, MeshRenderer[] ciziciler)
        {
            bool ilk = true;
            var sonuc = new Bounds();
            foreach (var r in ciziciler)
            {
                var mf = r.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;
                var mb = mf.sharedMesh.bounds;
                var m = kok.worldToLocalMatrix * r.transform.localToWorldMatrix;
                for (int k = 0; k < 8; k++)
                {
                    var kose = new Vector3(
                        (k & 1) == 0 ? mb.min.x : mb.max.x,
                        (k & 2) == 0 ? mb.min.y : mb.max.y,
                        (k & 4) == 0 ? mb.min.z : mb.max.z);
                    var yp = m.MultiplyPoint3x4(kose);
                    if (ilk) { sonuc = new Bounds(yp, Vector3.zero); ilk = false; }
                    else sonuc.Encapsulate(yp);
                }
            }
            return sonuc;
        }

        /// <summary>
        /// Noktanın altındaki ilk yüzeyin kotu — yapının KENDİ parçaları
        /// sayılmaz. Kendi zeminine çarpan bir ışın her yapıyı "yere
        /// oturmuş" gösterirdi; ölçtüğün şey değil, ölçme biçimin bozuk
        /// olurdu.
        /// </summary>
        private static float? AltindakiYuzey(Vector3 p, Transform yapi)
        {
            var vuruslar = Physics.RaycastAll(p, Vector3.down, 400f,
                                              ~0, QueryTriggerInteraction.Ignore);
            float enYuksek = float.MinValue;
            foreach (var v in vuruslar)
            {
                if (v.transform.IsChildOf(yapi)) continue;
                if (v.point.y > enYuksek) enYuksek = v.point.y;
            }
            return enYuksek > float.MinValue ? enYuksek : (float?)null;
        }

        /// <summary>
        /// Arazi eğimi (derece) — <b>iki rakip açıklamayı ayıran ölçü.</b>
        ///
        /// Boşluk yamaçta yoğunlaşıyorsa sebep yerleştirme matematiğidir
        /// (ayak izi köşeleri evin YAW'ı uygulanmadan, dünya eksenlerinde
        /// örnekleniyor — döndürülmüş bir evde yanlış köşeler ölçülür).
        /// Boşluk düz zeminde de aynıysa sebep başkadır: semtler bir
        /// araziye kurulup sonra arazi yeniden üretilmiş olur.
        ///
        /// Tahminle düzeltmeye kalkmak, bu projede üç kez yanlış şeyi
        /// düzeltmeye çalışmakla sonuçlandı.
        /// </summary>
        private static float Egim(Terrain t, Vector3 p)
        {
            if (t == null) return 0f;
            var yerel = p - t.transform.position;
            var boyut = t.terrainData.size;
            float u = Mathf.Clamp01(yerel.x / boyut.x);
            float v = Mathf.Clamp01(yerel.z / boyut.z);
            return t.terrainData.GetSteepness(u, v);
        }

        private static void Yaz(List<Kayit> rapor, int toplam)
        {
            var kultur = CultureInfo.InvariantCulture;
            var bosluklu = rapor.Where(k => k.bosluk > GorunurBosluk)
                                .OrderByDescending(k => k.bosluk).ToList();
            var carpismasiz = rapor.Where(k => k.carpisticiYok).ToList();

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("# Zemin denetimi — yapılar yere değiyor mu");
            sb.AppendLine();
            sb.AppendLine($"Ölçülen yapı: **{toplam}**  ");
            sb.AppendLine($"Görünür boşluğu olan (> {GorunurBosluk:0.00} m): "
                          + $"**{bosluklu.Count}** "
                          + $"(%{(toplam == 0 ? 0f : 100f * bosluklu.Count / toplam):0.0})  ");
            sb.AppendLine($"Çarpıştırıcısı olmayan: **{carpismasiz.Count}**");
            sb.AppendLine();

            if (bosluklu.Count > 0)
            {
                var b = bosluklu.Select(k => k.bosluk).OrderBy(x => x).ToList();
                var e = bosluklu.Select(k => k.egim).OrderBy(x => x).ToList();
                sb.AppendLine($"Boşluklu yapıların arazi eğimi: "
                              + $"medyan {e[e.Count / 2]:0.0}° · "
                              + $"p10 {e[(int)(e.Count * 0.1f)]:0.0}° · "
                              + $"p90 {e[(int)(e.Count * 0.9f)]:0.0}°");
                var hepsi = _tumEgimler.OrderBy(x => x).ToList();
                if (hepsi.Count > 0)
                    sb.AppendLine($"BÜTÜN yapıların arazi eğimi: "
                                  + $"medyan {hepsi[hepsi.Count / 2]:0.0}° · "
                                  + $"p90 {hepsi[(int)(hepsi.Count * 0.9f)]:0.0}° "
                                  + "← karşılaştırma noktası");
                sb.AppendLine();
                sb.AppendLine($"Boşluk: medyan {b[b.Count / 2]:0.00} m · "
                              + $"p90 {b[(int)(b.Count * 0.9f)]:0.00} m · "
                              + $"en büyük {b[b.Count - 1]:0.00} m");
                sb.AppendLine();
                sb.AppendLine("## Semte göre");
                sb.AppendLine();
                sb.AppendLine("| semt | boşluklu | en büyük (m) |");
                sb.AppendLine("|---|---:|---:|");
                foreach (var g in bosluklu.GroupBy(k => k.semt)
                                          .OrderByDescending(g => g.Count()))
                    sb.AppendLine($"| {g.Key} | {g.Count()} | "
                                  + $"{g.Max(k => k.bosluk):0.00} |");
                sb.AppendLine();
                sb.AppendLine("## Yapı türüne göre — hangi yerleştirici bozuk");
                sb.AppendLine();
                sb.AppendLine("| yapı | boşluklu | toplam | oran |");
                sb.AppendLine("|---|---:|---:|---:|");
                foreach (var g in bosluklu.GroupBy(k => k.ad)
                                          .OrderByDescending(g => g.Count())
                                          .Take(25))
                {
                    _tumSayim.TryGetValue(g.Key, out int hep);
                    float oran = hep == 0 ? 0f : 100f * g.Count() / hep;
                    sb.AppendLine($"| {g.Key} | {g.Count()} | {hep} | "
                                  + $"%{oran:0.0} |");
                }
                sb.AppendLine();
                sb.AppendLine("## En kötü 40");
                sb.AppendLine();
                sb.AppendLine("| semt | yapı | boşluk (m) | eğim (°) | konum |");
                sb.AppendLine("|---|---|---:|---:|---|");
                foreach (var k in bosluklu.Take(40))
                    sb.AppendLine($"| {k.semt} | {k.ad} | {k.bosluk:0.00} | "
                                  + $"{k.egim:0.0} | "
                                  + $"({k.konum.x:0}, {k.konum.y:0}, {k.konum.z:0}) |");
            }

            Directory.CreateDirectory("../../renders/denetim");
            string cikti = "../../renders/denetim/zemin_denetimi.md";
            File.WriteAllText(cikti, sb.ToString());
            Debug.Log($"[Hezarfen] Zemin denetimi: {toplam} yapi, "
                      + $"{bosluklu.Count} boslukllu, "
                      + $"{carpismasiz.Count} carpisticisiz.\n{cikti}");
        }
    }
}
