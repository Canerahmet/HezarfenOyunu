using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hezarfen.Arayuz;
using Hezarfen.Editor.Lighting;
using Hezarfen.Flight;
using Hezarfen.Player;
using Hezarfen.Sehir;
using Hezarfen.Streaming;
using Hezarfen.Tani;
using Hezarfen.Zaman;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Hezarfen.Editor.Pipeline
{
    /// <summary>
    /// <b>Şehri OYNANABİLİR hâle getirir.</b>
    ///
    /// ## Neden bu gerekti
    ///
    /// Faz 6 ve 7 bittiğinde 372 test yeşildi ve her sistem tek tek
    /// ölçülmüştü — zaman, rutin, aranma, ekonomi, görev, replik, uçuş
    /// dizisi, ışık, performans. Ama <b>hiçbiri bir sahnede bir araya
    /// gelmemişti</b>: `Faz1_Terrain` dünyayı taşıyordu (arazi, sur,
    /// landmark, iskele, ağaç) ve içinde ne oyuncu vardı ne saat.
    /// `FlightSlice` uçuşu taşıyordu ve içinde şehir yoktu.
    ///
    /// Yani oyun, testlerin hepsi geçerken bile <b>oynanamıyordu</b>.
    /// Testler parçaları ölçtü; kimse birleştirmedi. Projenin kendi
    /// kuralı bunu zaten söylüyordu — *üretilen ama görünmeyen bir öğe,
    /// olmayan bir öğedir* — ama kural sistemlere değil varlıklara
    /// uygulanmıştı.
    ///
    /// Bu komut montajı yapar ve <b>yeniden yapılabilir</b> kılar: sahne
    /// elle kurulsaydı, bir sonraki arazi yenilemesinde sessizce dağılırdı.
    /// </summary>
    public static class OyunSahnesiKur
    {
        /// <summary>Ortam sesi yatağını yükler; yoksa uyarır.</summary>
        private static AudioClip SesYatagi(string ad)
        {
            var k = AssetDatabase.LoadAssetAtPath<AudioClip>(
                $"Assets/_Project/Audio/Ortam/{ad}.wav");
            if (k == null)
                Debug.LogWarning($"[Hezarfen] Ses yatagi yok: {ad}. "
                    + "Once: python tools/audio/gen_ortam.py");
            return k;
        }

        /// <summary>
        /// <b>Şehirde insan var mı?</b>
        ///
        /// Caner (2026-08-30, oynadıktan sonra):
        ///
        /// > "simdilik npcleri kaldir haritaya odaklanalim. npcleri daha
        /// >  guzel bir sekilde uretip sonra ekleriz."
        ///
        /// Bu bir <b>silme</b> değil, bir <b>anahtar</b>. Kalabalığın
        /// arkasında duran her şey yerinde kalıyor: sokak grafı, meslek
        /// çizelgeleri, rutin, replik korpusu (5.088 satır), aranma
        /// sistemi, kayıt bağları ve bunları koruyan testler. Kapatılan
        /// tek şey, oyun sahnesinde gövde çizilmesi.
        ///
        /// Silmek ucuz görünürdü ve pahalı olurdu: gövdeler geri
        /// geldiğinde yeniden bağlanacak yedi ayrı bağlantı var ve her
        /// biri sessizce yanlış bağlanabilir. Bir bayrak, geri dönüşü
        /// <b>tek satıra</b> indiriyor.
        ///
        /// Geri açmak: burayı <c>true</c> yap, sonra
        /// <b>Hezarfen → Boru Hatti → Oyun sahnesini kur</b>.
        /// </summary>
        /// <summary>
        /// Kalabalık açık mı.
        ///
        /// ADR 0077 ile kapatılmıştı: kalabalık tek tipti ve o hâliyle
        /// şehri zenginleştirmek yerine kopya dizisi gösteriyordu.
        /// Şart, "NPC'ler daha güzel bir şekilde üretilip sonra
        /// eklenecek" idi.
        ///
        /// O gün geldi: <see cref="Hezarfen.Sehir.InsanDNA"/> her
        /// sakine tohumdan boy, yaş, giysi tonu ve tempo veriyor.
        /// Ölçüldü — 600 kişilik örnekte boy aralığı 0,35 m'den
        /// geniş, yaşlı genç'ten yavaş, giysi dört dönem boyasının
        /// dışına çıkmıyor.
        /// </summary>
        public const bool KalabalikVar = true;

        public const string DunyaSahnesi =
            "Assets/_Project/Scenes/Faz1_Terrain.unity";

        /// <summary>Sokak grafı varlığı — tek yol, iki yerde kullanılıyor.</summary>
        public const string GrafYolu = "Assets/_Project/Data/SG_Sehir.asset";

        /// <summary>Oyuncunun doğduğu yer: Galata Kulesi'nin dibi.</summary>
        public static readonly Vector3 BaslangicNoktasi =
            new Vector3(25f, 0f, 25f);

        [MenuItem("Hezarfen/Boru Hatti/Oyun sahnesini kur")]
        public static void KurMenu()
        {
            var sahne = EditorSceneManager.OpenScene(
                DunyaSahnesi, OpenSceneMode.Single);

            var rapor = new List<string>();

            // 1) ISIK: gecici takim GITMELI (ADR 0072). Dunya sahnesinde
            //    hala duruyordu — kalici pas yalniz sandik sahnesine
            //    kurulmustu.
            KaliciAydinlatma.Kur(out string isikRapor);
            rapor.Add("Isik: kalici pas kuruldu");

            // 2) OYUNCU
            var oyuncu = Oyuncu(out string oyuncuRapor);
            rapor.Add(oyuncuRapor);

            // 3) ZAMAN — vakitler gunesten hesaplanir; gunes sahnedeki.
            var zaman = Tekil<ZamanSistemi>("ZAMAN");
            zaman.yil = 1632;
            zaman.yilinGunu = 122;          // 1 Mayis 1632 (artik yil)
            zaman.saat = 9.0f;
            zaman.gunesiSur = true;
            zaman.Yenile();
            rapor.Add($"Zaman: {zaman.yil}, {zaman.yilinGunu}. gun, "
                      + $"saat {zaman.saat:F1}");

            // 4) SEHIR: sakinler
            var graf = AssetDatabase.LoadAssetAtPath<SokakGrafi>(GrafYolu);
            var meslekler = AssetDatabase.FindAssets("t:NPCMeslek")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<NPCMeslek>)
                .Where(m => m != null).ToList();
            var govde = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Art/Prefabs/PF_Hezarfen_Sivil.prefab");

            var sehir = Tekil<NPCYonetici>("SEHIR_NPC");
            sehir.graf = graf;
            sehir.meslekler = meslekler;
            sehir.govdePrefab = govde;
            sehir.oyuncu = oyuncu.transform;
            sehir.zaman = zaman;

            // NUFUS DEGERLERI BURADA YAZILIR — koddaki varsayilan YETMEZ.
            //
            // Sahnedeki bilesen serilestirilmis eski degerleri tasir;
            // NPCYonetici'nin alan varsayilanini degistirmek ZATEN
            // kurulmus bir sahneyi degistirmez. Bir kez tam olarak bu
            // oldu: sayilari kodda 9.000'e cikardim, sahne 1.200'de kaldi
            // ve olcum "hicbir sey degismedi" dedi.
            sehir.sakinSayisi = KalabalikVar ? NPCYonetici.VarsayilanSakin : 0;
            sehir.gorunurMesafe = NPCYonetici.VarsayilanGorunurMesafe;
            sehir.dilim = NPCYonetici.VarsayilanDilim;

            // KALABALIK KAPALIYSA BILESEN DE KAPANIR.
            //
            // Yalniz `sakinSayisi = 0` yazmak yetmez: `Update` yine her
            // kare kosar ve bir gun biri `Kur()` cagirdiginda sehir geri
            // gelir. Kapali demek kapali olmali.
            sehir.enabled = KalabalikVar;

            rapor.Add(KalabalikVar
                ? $"Sehir: {(graf == null ? "GRAF YOK" : graf.dugumler.Count + " dugum")}, "
                  + $"{meslekler.Count} meslek, {sehir.sakinSayisi} sakin"
                : "Sehir: KALABALIK KAPALI (Caner, 2026-08-30) — graf ve "
                  + $"meslekler bagli kaldi ({meslekler.Count} meslek)");

            // 5) KOLLUK
            var aranma = Tekil<AranmaSistemi>("ARANMA");
            aranma.zaman = zaman;
            aranma.sehir = sehir;
            aranma.oyuncu = oyuncu.transform;
            sehir.aranma = aranma;
            rapor.Add("Aranma: kuruldu");

            // 6) REPLIKLER — sehir konussun (Katman 2).
            var bark = Tekil<BarkGosterici>("BARK");
            bark.yonetici = sehir;
            bark.oyuncu = oyuncu.transform;
            bark.enabled = KalabalikVar;      // konusacak kimse yoksa susar
            rapor.Add(KalabalikVar ? "Replik: gosterici kuruldu"
                                   : "Replik: KAPALI (kalabalik yok)");

            // 5b) RUZGAR ALANI — OYUNUN KALBI, VE SAHNEDE HIC YOKTU.
            //
            // PLAN'in bir numarali tasarim diregi: "Ruzgari hissettir.
            // Ucus, oyunun kalbidir." Bes sinif bunun icin yazilmis —
            // `WindField`, `WindVolume`, `TerrainThermal`,
            // `UcusKamerasi`, `FlightHud` — ve BESI DE sahnede yoktu.
            // `GlideController` ruzgar alani bulamayinca
            // `tuning.globalWind`e dusuyor: her yerde, her irtifada,
            // her an sabit 9 m/s.
            //
            // Bedeli aritmetikle olculdu. Kule (52 m) ile Dogancilar
            // (46,6 m) arasi 3.336 m yatay, 5,4 m kot: gereken suzulme
            // orani 618:1. Kanat 11,56:1 veriyor. Sabit ruzgarla
            // gidilen 1.037 m, gereken 3.336 m. Yani oyunun FINALI
            // aritmetik olarak bitirilemezdi ve bunu hicbir test
            // sormamisti.
            //
            // Acigi kapatacak sey zaten yazili: termik. `TerrainThermal`
            // kaldiraci araziden turetiyor — guneye bakan yamacta
            // yukselir, su ustunde coker. Ucus rotasi da tam olarak
            // bunu istiyor: once Galata yamacinda yuksel, sonra Bogaz'i
            // gec.
            var ruzgarGo = GameObject.Find("RUZGAR");
            if (ruzgarGo == null) ruzgarGo = new GameObject("RUZGAR");
            var alan = ruzgarGo.GetComponent<WindField>();
            if (alan == null) alan = ruzgarGo.AddComponent<WindField>();
            alan.tuning = AssetDatabase.LoadAssetAtPath<WindTuning>(
                "Assets/_Project/Data/WindProfiles/WT_Faz0_Default.asset");
            alan.autoCollectVolumes = true;

            var araziGo = Object.FindAnyObjectByType<Terrain>();
            if (araziGo != null)
            {
                var termik = araziGo.GetComponent<TerrainThermal>();
                if (termik == null)
                    termik = araziGo.gameObject.AddComponent<TerrainThermal>();
                alan.terrainThermal = termik;
                rapor.Add($"Ruzgar: termik tavan {termik.ceilingMeters:F0} m, "
                          + $"tepe {termik.peakLift:F1} m/s");
            }
            else
            {
                rapor.Add("Ruzgar: arazi yok, TERMIK KURULAMADI");
            }

            // 5c) GOREV — sistemleri birbirine baglayan tel.
            //
            // `GorevUretici`, `Gorev`, `Kese`, `Ekonomi` yazilmis ve
            // test edilmisti; hicbirini OYUNDA cagiran yoktu. Faz 6
            // kapisi yine de yesildi cunku kapiyi gecen test gorevi
            // KENDISI oynuyordu. Bu bilesen o teli ceker: gorev uretir,
            // hedefi HUD'a verir, varisi olcer, akceyi oder.
            var gorevY = Tekil<GorevYonetici>("GOREV");
            gorevY.oyuncu = oyuncu.transform;
            gorevY.zaman = zaman;
            gorevY.graf = sehir.graf;
            rapor.Add("Gorev: yonetici kuruldu");

            // 5c-2) VAKIT BILDIRIMI — `VakitGirdi`'nin ILK abonesi.
            //
            // Olayin calisma zamaninda hicbir dinleyicisi yoktu; yani
            // `VakitHesabi`'nin butun dogrulugu — Hanefi ikindi, gercek
            // sapma, batistan kurulan ezani saat — oyuncuya kosede bir
            // yazi olarak ulasiyordu.
            var vakitB = Tekil<VakitBildirimi>("VAKIT");
            vakitB.zaman = zaman;
            vakitB.graf = sehir.graf;
            vakitB.oyuncu = oyuncu.transform;
            rapor.Add("Vakit: bildirim bagli");

            // 5d) PERME — karsiya gecis.
            //
            // Iskeleler ARAZI sahnesinde duruyor, semt sahnelerinde
            // degil; `EtkilesimKur` semtleri tariyor ve onlari hic
            // gormuyordu. Bir gecisin sahnesi, gectigi yerin sahnesi
            // degil.
            int permeSayisi = 0;
            foreach (var kok in sahne.GetRootGameObjects())
                foreach (var t in kok.GetComponentsInChildren<Transform>())
                {
                    // Onek ELLE YAZILMAZ: grafi kuran tablo sorulur.
                    // "PF_Iskele" oneki `PF_UskudarIskelesi`yi
                    // kacirmis ve Uskudar'i tek yonlu kapan yapmisti.
                    if (Hezarfen.Editor.Gis.SokakGrafiKur.TuruBul(t.name)
                        != SokakGrafi.Tur.Iskele) continue;
                    var pm = t.GetComponent<Perme>();
                    if (pm == null) pm = t.gameObject.AddComponent<Perme>();
                    pm.graf = sehir.graf;
                    pm.gorev = gorevY;
                    pm.zaman = zaman;
                    pm.Kur();
                    permeSayisi++;

                    // Iskeleye yaklasilabilmeli: etkilesim fizikten
                    // geciyor.
                    if (t.GetComponentInChildren<Collider>() == null)
                    {
                        var kutu = t.gameObject.AddComponent<BoxCollider>();
                        kutu.isTrigger = true;
                        kutu.size = new Vector3(6f, 3f, 6f);
                        kutu.center = new Vector3(0f, 1.5f, 0f);
                    }
                }
            rapor.Add($"Perme: {permeSayisi} iskele baglandi");

            // 5e) KULE KAPISI — oyunun adini tasiyan fiile giris.
            //
            // Kuleye cikilamiyordu: ic mekan yok, tirmanma mekanigi
            // yok, ve uçusu olcen tek arac oyuncuyu ISINLIYORDU. Yani
            // 3.336 m'lik final hicbir zaman bir oyuncunun
            // erisebilecegi bir sey olmadi.
            int kapiSayisi = 0;
            foreach (var kok in sahne.GetRootGameObjects())
                foreach (var t in kok.GetComponentsInChildren<Transform>())
                {
                    if (!t.name.StartsWith("SM_GalataTower")
                        && !t.name.StartsWith("PF_GalataKulesi")) continue;
                    if (t.GetComponent<KuleKapisi>() != null) continue;

                    var kapi = new GameObject("PF_KuleKapisi");
                    kapi.transform.SetParent(t, false);
                    kapi.transform.localPosition = new Vector3(0f, 1.2f, -6.5f);
                    var kk = kapi.AddComponent<KuleKapisi>();
                    var kutu = kapi.AddComponent<BoxCollider>();
                    kutu.isTrigger = true;
                    kutu.size = new Vector3(3f, 2.6f, 1.5f);
                    kutu.center = new Vector3(0f, 1.3f, 0f);
                    kapiSayisi++;
                    break;
                }
            rapor.Add($"Kule kapisi: {kapiSayisi}");

            // 6a) AY — gecenin TEK kaynagi.
            //
            // Gece karesi yakalandi ve tamamen siyahti: 78 KB'lik bir
            // PNG. Sebep tekti — sahnede bir ISIK var ve `ZamanSistemi`
            // onu gunes batinca kapatiyor. Karanlik ile siyah ayni sey
            // degil; dolunay disarida golge dusurur.
            var ayGo = GameObject.Find("AY");
            if (ayGo == null) ayGo = new GameObject("AY");
            var ayIsik = ayGo.GetComponent<Light>();
            if (ayIsik == null) ayIsik = ayGo.AddComponent<Light>();
            ayIsik.type = LightType.Directional;
            var ay = ayGo.GetComponent<AyIsigi>();
            if (ay == null) ay = ayGo.AddComponent<AyIsigi>();
            ay.zaman = zaman;
            ay.Uygula(zaman.yilinGunu, zaman.saat);
            rapor.Add($"Ay: evre {AyIsigi.Evre(zaman.yilinGunu):0.00}, "
                      + $"aydinlik {AyIsigi.Aydinlik(AyIsigi.Evre(zaman.yilinGunu)):0.00}");

            // 6b) SEHRIN HAVASI — baca dumani ve marti.
            //
            // Kalabaliktan bagimsiz: kalabalik kapatilsa bile bacalar
            // tuter. Bir sehri bos gostermeyen sey yalniz sokaktaki
            // insan degil, damdaki dumandir.
            var vfx = Tekil<SehirVFX>("SEHIR_VFX");
            vfx.oyuncu = oyuncu.transform;
            vfx.zaman = zaman;
            rapor.Add($"Hava olaylari: {vfx.dumanHavuzu} baca havuzu, "
                      + $"{vfx.martiSayisi} marti");

            // 7) HAVA — lodos; ucusu mumkun kilan sey bu.
            var hava = Tekil<HavaProfili>("HAVA");
            hava.ruzgar = Ruzgar.Lodos;
            hava.hiz = 8f;
            hava.gokVolume = Object.FindAnyObjectByType<UnityEngine.Rendering.Volume>();
            hava.Uygula();
            rapor.Add("Hava: lodos 8 m/s");

            // 8) SEMT AKISI — sehir oyuncunun etrafinda yuklenir.
            var semtKayit = AssetDatabase.LoadAssetAtPath<DistrictRegistry>(
                "Assets/_Project/Data/DistrictDefs/DistrictRegistry.asset");
            var akis = Tekil<DistrictStreamer>("SEMT_AKISI");
            akis.registry = semtKayit;
            akis.viewer = oyuncu.transform;
            rapor.Add(semtKayit == null ? "Semt akisi: KAYIT YOK"
                                    : "Semt akisi: kuruldu");

            // 9) PERDE 2 DILIMI — talim -> kule -> ucus -> inis -> tepki.
            var dilim = oyuncu.GetComponent<Perde2Dilimi>()
                        ?? oyuncu.AddComponent<Perde2Dilimi>();
            dilim.dizi = oyuncu.GetComponent<UcusDizisi>();
            dilim.oyuncu = oyuncu.transform;
            rapor.Add("Perde 2: dilim baglandi");

            // 10) KAYIT — neyin yazildigi ve nereye dondugu tek yerde.
            var kayit = Tekil<KayitBaglayici>("KAYIT");
            kayit.zaman = zaman;
            kayit.oyuncu = oyuncu.transform;
            kayit.aranma = aranma;
            kayit.sehir = sehir;
            rapor.Add("Kayit: baglayici kuruldu");

            // 11) HUD — tarih, ezani saat, aranma; ESC/F5/F9.
            var hud = Tekil<OyunHud>("HUD");
            hud.zaman = zaman;
            hud.kayit = kayit;
            hud.aranma = aranma;
            rapor.Add("HUD: ESC duraklat, F5 kaydet, F9 yukle");

            EditorSceneManager.MarkSceneDirty(sahne);
            EditorSceneManager.SaveScene(sahne);
            Debug.Log("[Hezarfen] OYUN SAHNESI KURULDU\n  "
                      + string.Join("\n  ", rapor));
        }

        /// <summary>Oyuncuyu kurar: yürüme + uçuş + kamera.</summary>
        private static GameObject Oyuncu(out string rapor)
        {
            string rapor2;
            var eski = GameObject.Find("OYUNCU");
            if (eski != null) Object.DestroyImmediate(eski);

            var go = new GameObject("OYUNCU");
            var arazi = Object.FindAnyObjectByType<Terrain>();
            float y = arazi != null
                ? arazi.SampleHeight(BaslangicNoktasi)
                  + arazi.transform.position.y
                : 0f;
            // DOGUM YERI SOKAGA OTURUR.
            //
            // Galata Kulesi'nin dibi dunya orijini, ama olculdu: oradan
            // en yakin sokak grafi dugumu **215 m** oteydi ve en yakin
            // sakin **227 m**. Gorunur mesafe 120 m oldugu icin oyuncu
            // her acilista bos bir meydanda doguyordu — "npc ler yok"
            // izleniminin yarisi buydu, nufusla ilgisi yoktu.
            //
            // Kule yine baslangic noktasi; yalnizca en yakin sokaga
            // kaydiriliyor ki oyuncu insanlarin arasinda uyansin.
            var baslangic = new Vector3(
                BaslangicNoktasi.x, y + 0.2f, BaslangicNoktasi.z);
            // SokakGrafi bir ScriptableObject; sahnede aranmaz, varlik
            // olarak yuklenir. Ilk yazimda FindAnyObjectByType kullandim
            // ve sessizce null dondu — dogum yeri hic kaydirilmadi.
            var grafObj = AssetDatabase.LoadAssetAtPath<SokakGrafi>(GrafYolu);
            if (grafObj != null && grafObj.dugumler.Count > 0)
            {
                // SEMTLERI GECICI OLARAK AC — yoksa hicbir sey "dolu"
                // gorunmez.
                //
                // Bu adim eksikti ve sonucu olculdu: aciklik puani her
                // adayda 8/8 cikiyor, en yakin dugum kazaniyor ve oyuncu
                // yine duvar dibinde doguyordu. Sebep, evlerin Faz1_Terrain'de
                // DEGIL semt sahnelerinde olmasi: editorde arazi sahnesi tek
                // basina bostur, butun isinlar serbest gecer.
                //
                // Fizik sorgusu ancak carpisticilar YUKLU iken bir sey
                // soyler. Semtler eklenerek acilir, secim yapilir, kapanir.
                var geciciSemtler = new List<UnityEngine.SceneManagement.Scene>();
                foreach (string sy in Directory.GetFiles(
                             "Assets/_Project/Scenes/Districts", "*.unity"))
                {
                    string temiz = sy.Replace("\\", "/");
                    try
                    {
                        geciciSemtler.Add(EditorSceneManager.OpenScene(
                            temiz, OpenSceneMode.Additive));
                    }
                    catch { /* acilamayan semt secimi engellemez */ }
                }
                Physics.SyncTransforms();

                // EN YAKIN DUGUM YETMEZ, "BOS" DUGUM DE YETMEZ.
                //
                // Iki deneme de olculdu ve ikisi de yetersizdi:
                //  1. En yakin dugum: oyuncu bir binanin TAS KAIDESI
                //     ustunde, sacagin altinda dogdu.
                //  2. Kapsul bos mu + tepede 3 m aciklik: ayni nokta
                //     gecti, cunku kapsul gercekten bostu — ama kamera
                //     0,95 m arkadaki duvara carpiyordu (kol 3,20 -> 0,70).
                //
                // Gereken sey bir NOKTANIN bos olmasi degil, cevresinin
                // ACIK olmasi. Sekiz yone 6 m isin atilir; kac tanesi
                // serbest, o dugumun puanidir. En yakin 250 aday arasindan
                // en acik olani secilir — boylece oyuncu meydanda ya da
                // genis sokakta uyanir, duvar dibinde degil.
                var adaylar = new List<Vector3>(grafObj.dugumler.Count);
                foreach (var d in grafObj.dugumler) adaylar.Add(d.konum);
                adaylar.Sort((a, b) =>
                    (a - baslangic).sqrMagnitude.CompareTo(
                        (b - baslangic).sqrMagnitude));

                Vector3? secilen = null;
                int enIyiPuan = -1;
                int bakilan = Mathf.Min(250, adaylar.Count);
                for (int i = 0; i < bakilan; i++)
                {
                    var aday = adaylar[i];
                    float yy = YuzeyKotu(arazi, aday);

                    // YUZEY ZEMIN OLMALI — DAM DA DEGIL, CESME DE DEGIL.
                    //
                    // Aciklik puani catiyi sever: bir damin uzerinde
                    // sekiz isin de serbesttir. Olculdu, secim oyuncuyu
                    // bir kahvehanenin damina koydu (kot 74,59 iken
                    // arazi 70,9) ve esik 1,5 m'ye cekildi.
                    //
                    // 1,5 m dami eledi ama SADIRVANI elemedi: kenari
                    // araziden 1,0 m yukarida ve oyuncu her acilista
                    // cesmenin ustunde doguyordu — yani oyunun ILK
                    // karesinde. Ayni kusuru tur aracinda da yasadik ve
                    // orada 0,35 m'ye cekildi; dogum yerinde eski deger
                    // kaldi.
                    //
                    // 0,35 m DENENDI VE COK SIKI CIKTI: hicbir aday
                    // gecmedi ve oyuncu kule dibine dustu — yani daha
                    // once duzeltilmis "bos meydanda dogma" kusuru geri
                    // geldi. Duzeltme, duzelttiginden fazlasini kirdi.
                    //
                    // Sebep, yuksekligin YANLIS SORU olmasi. Galata bir
                    // yamac; kaldirim ve kaide arazinin yarim metre
                    // ustunde olabiliyor ve bu mesru. Bir esik hem
                    // yamaci hem cesmeyi ayni sayiyla eleyemez.
                    //
                    // Dogru soru: **ustunde durdugum sey ne.** Isin
                    // zaten neye carptigini biliyor; ona sormak yeterli.
                    if (!ZemindeMi(yy, arazi, aday)) continue;

                    var ayak = new Vector3(aday.x, yy + 0.2f, aday.z);
                    var alt = ayak + Vector3.up * 0.35f;
                    var ust = ayak + Vector3.up * 1.55f;
                    if (Physics.CheckCapsule(alt, ust, 0.32f, ~0,
                                             QueryTriggerInteraction.Ignore))
                        continue;
                    // Tepede aciklik: sacak/teras altina dusme.
                    if (Physics.Raycast(ust, Vector3.up, 4f, ~0,
                                        QueryTriggerInteraction.Ignore))
                        continue;

                    int puan = 0;
                    var goz = ayak + Vector3.up * 1.4f;
                    for (int a = 0; a < 8; a++)
                    {
                        float rad = a * Mathf.PI * 0.25f;
                        var yon2 = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad));
                        if (!Physics.Raycast(goz, yon2, 6f, ~0,
                                             QueryTriggerInteraction.Ignore))
                            puan++;
                    }

                    // BASIN USTU DE SAYILIR.
                    //
                    // Aciklik yalniz YATAY olculuyordu ve dogum yeri
                    // ust uste uc turda bir cardagin altina dustu:
                    // sekiz yonde 5/8 acik, ama tepede bir kiris ve
                    // dort direk. Oyuncunun oyunda gordugu ILK kare,
                    // kirmizi direklerle kapali bir kadrajdi.
                    //
                    // Bes puan degerinde, cunku tek bir yatay yonden
                    // agir basmali: bir sokak dort duvarla cevrili
                    // olabilir, ama ustu kapaliysa orasi sokak degil.
                    if (!Physics.Raycast(goz, Vector3.up, 8f, ~0,
                                         QueryTriggerInteraction.Ignore))
                        puan += 5;
                    if (puan <= enIyiPuan) continue;
                    enIyiPuan = puan;
                    secilen = ayak;
                    if (puan == 13) break;    // tam acik, daha iyisi yok
                }

                if (secilen.HasValue)
                {
                    float uzak = Vector3.Distance(secilen.Value, baslangic);
                    baslangic = secilen.Value;
                    Debug.Log($"[Hezarfen] Dogum yeri: {uzak:F0} m otede, "
                              + $"aciklik {enIyiPuan}/13, kot {baslangic.y:F1}.");
                }
                else
                {
                    Debug.LogWarning("[Hezarfen] Acik sokak dugumu yok — "
                                     + "kule dibinde doguldu.");
                }

                foreach (var gs in geciciSemtler)
                    if (gs.IsValid()) EditorSceneManager.CloseScene(gs, true);
            }

            go.transform.position = baslangic;

            var cc = go.AddComponent<CharacterController>();
            cc.height = 1.70f; cc.radius = 0.30f;
            cc.center = new Vector3(0f, 0.85f, 0f);

            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 78f; rb.isKinematic = true; rb.useGravity = false;
            // UCUSTA HIZLI GIDILIR: ayrik carpisma ile 30 m/s'de kare
            // basina yarim metre atlanir ve ince bir cati delinir.
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            // UCUS KAPSULU — yerde KAPALI, havada ACIK.
            //
            // `CharacterController` de bir Collider'dir ve ucusa gecerken
            // kapatiliyordu (UcusDizisi.HavayaGec). Sonuc: ucus boyunca
            // oyuncunun uzerinde HICBIR carpistirici kalmiyordu. Rigidbody
            // hicbir seye carpmiyor, araziden ve binalardan geciyor,
            // yalnizca 0,55 m'lik bir Update isinina yakalanirsa iniyordu.
            // Yeterince hizli dusen (14 m'lik bir damdan atlamak yeter)
            // o isini de kacirip SONSUZA KADAR dusuyordu.
            var ucusKapsul = go.AddComponent<CapsuleCollider>();
            ucusKapsul.height = cc.height;
            ucusKapsul.radius = cc.radius;
            ucusKapsul.center = cc.center;
            ucusKapsul.enabled = false;

            var yurume = go.AddComponent<WalkController>();
            var suzulme = go.AddComponent<GlideController>();
            suzulme.enabled = false;
            // AERODINAMIK AYARI OLMADAN KANAT HICBIR SEY YAPMAZ.
            // `GlideController.FixedUpdate` ilk satirda `tuning == null`
            // ise geri doner; yani ucus, tasima ve suruklenmesi olmayan
            // bir SERBEST DUSUSTU. Animator ucus karisimini oynattigi icin
            // ekranda suzuluyormus gibi gorunuyordu.
            suzulme.tuning = AssetDatabase.LoadAssetAtPath<WindTuning>(
                "Assets/_Project/Data/WindProfiles/WT_Faz0_Default.asset");
            if (suzulme.tuning == null)
                Debug.LogError("[Hezarfen] WT_Faz0_Default yok — ucus "
                               + "aerodinamiksiz kalir.");

            // SES: hizin duyulmasi.
            //
            // `Runtime/Flight/` altinda hic ses kaynagi yoktu ve tek
            // ruzgar sesi IRTIFADAN besleniyordu — hava hizindan
            // degil. 300 m'de referans nesnesi olmayan bir sahnede
            // hizin oyuncuya ulasan tek kanali HUD'daki bir rakamdi.
            var ucusSes = go.AddComponent<UcusSesi>();
            ucusSes.yatak = AssetDatabase.LoadAssetAtPath<AudioClip>(
                "Assets/_Project/Audio/Ortam/SFX_Ortam_Ruzgar.wav");
            if (ucusSes.yatak == null)
                Debug.LogError("[Hezarfen] SFX_Ortam_Ruzgar yok — ucus "
                               + "sessiz kalir.");
            // GIRDI: bunlar olmadan havada W/A/S/D kanada hic ulasmaz.
            go.AddComponent<PlayerFlightInput>();
            var firlatma = go.AddComponent<FlightLaunch>();
            firlatma.launchOnStart = false;     // atlayisi UcusDizisi baslatir

            var dizi = go.AddComponent<UcusDizisi>();
            dizi.kapsul = cc;
            dizi.ucusKapsulu = ucusKapsul;
            dizi.govde = rb;
            dizi.suzulme = suzulme;
            dizi.yurume = yurume;
            dizi.firlatma = firlatma;

            // Kamera oyuncunun GOZUNDE. Sahnedeki bagimsiz kamera
            // kaldirilir; iki kamera olursa hangisinin cizdigi belirsiz.
            var eskiKam = GameObject.Find("Main Camera");
            if (eskiKam != null) Object.DestroyImmediate(eskiKam);

            var kamGo = new GameObject("Main Camera");
            kamGo.tag = "MainCamera";
            kamGo.transform.SetParent(go.transform, false);
            // Kamerayi KameraKipi yerlestirir (goz ya da omuz ustu);
            // burada bir yer yazmak ayni transforma iki sahip vermek olur.
            kamGo.transform.localPosition = Vector3.zero;
            var kam = kamGo.AddComponent<Camera>();

            // SEHIR 1 KM'DE BITIYORDU.
            //
            // Kameranin uzak kirpma duzlemi Unity'nin varsayilani olan
            // 1000 m'de duruyordu ve olculdu: dogum noktasindan
            // Suleymaniye 1.359 m, Ayasofya 1.984 m, Sultanahmet
            // 2.298 m, Kiz Kulesi 2.560 m, Uskudar Mihrimah 3.512 m.
            // Yani 39 nirenginin 34'u HIC cizilmiyordu. Oyuncu
            // Galata'da yarim kilometrelik bir kutuda yasiyor ve
            // sehrin geri kalaninin var oldugunu goremiyordu.
            //
            // Kirpan sey sis degildi: sis 1.400 m'de
            // (`KaliciAydinlatma`), yani sisin oteledigi seyi kamera
            // zaten kesmisti. Isaretsiz kesif nirengiyle yapilir ve
            // gorunmeyen nirengi yol gostermez.
            //
            // 6.000 m: ucus kamerasinin zaten kullandigi deger
            // (`HezarfenSpawner`). Kuleden Dogancilar 3.336 m, yani
            // ucusun iki ucu ayni karede gorunebilir.
            kam.farClipPlane = 6000f;
            kamGo.AddComponent<AudioListener>();

            // UCUS GOSTERGESI: gostergesiz bir ucus modeli ogrenilemez.
            //
            // `FlightHud` kendi belgesinde bunu yaziyor ve yalnizca
            // `FlightSlice` sahnesinde duruyordu; oyunda oyuncu neden
            // dustugunu ya da neden yukseldigini goremiyordu.
            var ucusHud = go.AddComponent<FlightHud>();
            // BAGLANMAMIS BIR GOSTERGE HIC CIZMEZ: `FlightHud.OnGUI`
            // ilk satirda `glider == null` ise geri donuyor. Bilesen
            // sahnede olup gostergenin gorunmemesi, hicbir hata
            // vermeyen bir eksikliktir — tam da testin tutmasi
            // gereken cins.
            ucusHud.glider = suzulme;

            // YERDE KAPALI.
            //
            // `FlightHud.OnGUI` yalniz `glider == null` ise geri
            // donuyordu; `glider.enabled`'a bakmiyordu. Yani oyuncu
            // sehirde YURURKEN ekraninda surekli HAVA HIZI, HUCUM
            // ACISI, YATIS ve "W/S: burun" yaziyordu — ucusa hic
            // girmemis bir oyuncuya, hicbiri anlamli olmayan dort
            // sayi.
            //
            // `UcusDizisi` havaya gecerken aciyor, inerken kapatiyor.
            ucusHud.enabled = false;

            // FENER: gece sokakta fenersiz dolasilmaz (RESEARCH 6).
            //
            // Ay disarida yetiyor ama dar sokakta yetmiyor: iki katli
            // ahsap ev cephesi gogun yarisini kapatir. Fener, hem o
            // bosluğu dolduruyor hem de donemin kendi kurali.
            go.AddComponent<Fener>();

            // ETKILESIM: kese oyuncuda, nisan kamerada.
            //
            // Ikisi ayri nesnede cunku ayri sorulara cevap veriyorlar.
            // Kese "neyi tasiyorum" der ve govdeye aittir; nisan
            // "neye bakiyorum" der ve GOZE aittir. Nisani govdeye
            // baglamak, omuz ustu kamerada oyuncunun baktigi seyle
            // uzandigi seyi ayirirdi.
            go.AddComponent<Envanter>();
            var uzan = go.AddComponent<EtkilesimAlgila>();
            uzan.bakis = kamGo.transform;

            // ORTAM SESI — dinleyiciyle ayni nesnede.
            //
            // Oyun bugune kadar tamamen sessizdi: sahnede tek bir
            // AudioSource yoktu. Ortam sesi bir noktadan gelmez, her
            // yerdedir; o yuzden kaynaklar dinleyiciye bagli ve 2B.
            // Yataklar sentezle uretiliyor (tools/audio/gen_ortam.py) —
            // indirilen ses yok, izlenecek lisans yok.
            var ses = kamGo.AddComponent<Hezarfen.City.OrtamSesi>();
            // ARAZI OZNITELIK KATMANI BAGLANIR.
            //
            // Sekiz `AO_D_*.asset` uretilmis ve calisma zamaninda tek
            // okuyucusu `OrtamSesi.katman`di — ve o alan HIC
            // atanmiyordu. Yani `SuUzakligi()` sonsuza kadar
            // `y * 3` vekiline dusuyordu: Galata sokaginda (y~20 m)
            // deniz sesi %97, kulenin serefesinde (98 m) %58. Yukseklik
            // denize uzakligin yerine geciyordu ve iliski TERSTI.
            //
            // Ustelik `OrtamSesiTests` katmani hic atamiyor, yani tam
            // olarak o vekil dali kilitliyordu — test, eksik baglantiyi
            // uc faz boyunca ortmustu.
            ses.katman = AssetDatabase.LoadAssetAtPath<Hezarfen.Gis.AraziOznitelik>(
                "Assets/_Project/Data/AO_D_Galata.asset");
            if (ses.katman == null)
                Debug.LogWarning("[Hezarfen] AO_D_Galata yok — ortam sesi "
                                 + "yukseklik vekiline duser.");

            ses.deniz = SesYatagi("SFX_Ortam_Deniz");
            ses.ruzgar = SesYatagi("SFX_Ortam_Ruzgar");
            ses.carsi = SesYatagi("SFX_Ortam_Carsi");
            ses.gece = SesYatagi("SFX_Ortam_Gece");
            var kamVeri = kamGo.AddComponent<UnityEngine.Rendering
                .HighDefinition.HDAdditionalCameraData>();

            // KENAR YUMUSATMA — TITREMENIN SEBEBI BUYDU.
            //
            // Caner (2026-08-29): *"modellerin kenar ve koselerinde
            // titremeler var... isiksal mi yoksa baska bir problem mi?"*
            // Isik degil: sahnedeki kameranin `antialiasing` degeri
            // SIFIRDI (None) ve kod tabaninda AA'ya dokunan tek satir
            // yoktu. Kiremit sirtlari, sur mazgallari ve minare
            // kenarlari gibi ince, yuksek kontrastli kenarlar her karede
            // baska pikseli orttugu icin kaynasiyordu.
            //
            // TAA secildi, SMAA degil: SMAA tek karelik bir kenar
            // filtresidir ve HAREKET eden ince geometride kaynamayi
            // durdurmaz — burada sikayet tam olarak harekette. TAA
            // kareleri biriktirdigi icin uzaktaki kiremit ve mazgal da
            // durulur.
            kamVeri.antialiasing = UnityEngine.Rendering.HighDefinition
                .HDAdditionalCameraData.AntialiasingMode
                .TemporalAntialiasing;
            kamVeri.TAAQuality = UnityEngine.Rendering.HighDefinition
                .HDAdditionalCameraData.TAAQualityLevel.High;
            // Titrek desen (dithering): gokyuzu ve sis gecislerindeki
            // bant izini kirar; AA ile ayni sikayetin ikinci yarisi.
            kamVeri.dithering = true;

            // GORUNUR GOVDE — ucuncu sahis kamerasi bir govde ister.
            // Birinci sahista gizlenmez, ShadowsOnly'ye duser: gunes
            // altinda golgesiz yuruyen bir adam dikkat cekerdi.
            var govde = GovdeTak(go, out string govdeNot);
            rapor2 = govdeNot;

            // KANAT — MODELLENDI, HIC EKRANA GIRMEDI.
            //
            // Uc kanat prefabi (`PF_Kanat_Katli/Acik/Kirik`) diskte
            // duruyor ve GUID taramasi **sifir referans** dondu: hicbir
            // sahnede, hicbir prefabta, hicbir kod satirinda. Yani
            // oyuncu kuleden atliyor ve ekranda kollarini iki yana
            // acmis entarili bir adam dusuyor — oyunun ADINI tasiyan
            // nesne hic gorunmuyordu.
            if (govde != null)
            {
                var kanatGo = new GameObject("KANAT");
                kanatGo.transform.SetParent(govde.transform, false);
                var kg = kanatGo.AddComponent<KanatGorseli>();
                kg.dizi = go.GetComponent<UcusDizisi>();
                kg.katli = KanatTak(kanatGo.transform, "PF_Kanat_Katli");
                kg.acik = KanatTak(kanatGo.transform, "PF_Kanat_Acik");
                kg.kirik = KanatTak(kanatGo.transform, "PF_Kanat_Kirik");
                kg.Uygula(UcusDizisi.Durum.Yerde);
                rapor2 += kg.katli != null ? " + kanat" : " + KANAT YOK";
            }

            // AYAK SESI — sehirde 40.000 sakin var, oyuncunun adimi yoktu.
            var adim = go.AddComponent<AdimSesi>();
            adim.ornekler = new[]
            {
                AssetDatabase.LoadAssetAtPath<AudioClip>(
                    "Assets/_Project/Audio/Ortam/SFX_Adim_1.wav"),
                AssetDatabase.LoadAssetAtPath<AudioClip>(
                    "Assets/_Project/Audio/Ortam/SFX_Adim_2.wav"),
                AssetDatabase.LoadAssetAtPath<AudioClip>(
                    "Assets/_Project/Audio/Ortam/SFX_Adim_3.wav"),
                AssetDatabase.LoadAssetAtPath<AudioClip>(
                    "Assets/_Project/Audio/Ortam/SFX_Adim_4.wav"),
            };
            if (adim.ornekler[0] == null)
                Debug.LogError("[Hezarfen] Adim ornekleri yok — "
                               + "tools/audio/gen_ortam.py kosulmali.");

            var kipler = go.AddComponent<KameraKipi>();
            kipler.govde = govde != null ? govde.transform : null;
            // Acilista OMUZ USTU: oyuncu once karakterini gormeli.
            kipler.kip = Bakis.UcuncuSahis;

            rapor = $"Oyuncu: Galata, ({go.transform.position.x:F0}, "
                    + $"{go.transform.position.y:F0}, "
                    + $"{go.transform.position.z:F0}); "
                    + $"kamera TAA/High; {rapor2}";
            return go;
        }

        /// <summary>
        /// Oyuncuya görünür karakteri takar.
        ///
        /// Prefab bulunamazsa <b>sessizce geçmez</b>: üçüncü şahıs kamerası
        /// gövdesiz kalırsa oyuncu boşluğun arkasından bakar ve bunun
        /// nedeni sahnede hiçbir yerde yazmaz.
        /// </summary>
        /// <summary>Kanat modelini gövdeye takar; yoksa null.</summary>
        private static GameObject KanatTak(Transform ebeveyn, string ad)
        {
            var pf = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"Assets/_Project/Art/Prefabs/{ad}.prefab");
            if (pf == null)
            {
                Debug.LogError($"[Hezarfen] {ad} bulunamadi.");
                return null;
            }
            var ornek = (GameObject)PrefabUtility.InstantiatePrefab(pf, ebeveyn);
            ornek.name = ad;
            // Sirtta: omuz hizasi, govdenin biraz arkasi.
            ornek.transform.localPosition = new Vector3(0f, 1.35f, -0.12f);
            ornek.transform.localRotation = Quaternion.identity;

            // KANAT GORULUR, CARPMAZ.
            //
            // Prefablar uc carpistirici tasiyor ve gorsel govdeye
            // takilan her carpistirici `CharacterController` ile
            // kavga eder — `OyunSahnesiTests` bunu ayni turda
            // yakaladi. Kanat bir gorsel; fizik gövdenin kapsulunde.
            foreach (var c in ornek.GetComponentsInChildren<Collider>(true))
                Object.DestroyImmediate(c, true);

            return ornek;
        }

        private static GameObject GovdeTak(GameObject oyuncu, out string not)
        {
            const string yol =
                "Assets/_Project/Art/Prefabs/PF_Hezarfen_Sivil.prefab";
            var pf = AssetDatabase.LoadAssetAtPath<GameObject>(yol);
            if (pf == null)
            {
                not = "GOVDE YOK (" + yol + " bulunamadi) — ucuncu sahis "
                      + "kamerasi bos bakar";
                Debug.LogWarning("[Hezarfen] " + not);
                return null;
            }

            var ornek = (GameObject)PrefabUtility
                .InstantiatePrefab(pf, oyuncu.transform);
            ornek.name = "Govde";
            ornek.transform.localPosition = Vector3.zero;
            ornek.transform.localRotation = Quaternion.identity;

            // Govdenin kendi carpisticilari CharacterController ile
            // kavga eder ve karakteri havaya firlatir; gorsel govde
            // yalnizca GORSELDIR.
            foreach (var c in ornek.GetComponentsInChildren<Collider>(true))
                Object.DestroyImmediate(c);

            not = "govde: PF_Hezarfen_Sivil";

            // ANIMATORU SUREN BILESEN.
            //
            // Govde animator'unu kimse surmuyordu: karakter durus pozunda
            // KAYIYORDU ve hiz arttikca kayma da hizlaniyordu — Caner'in
            // "kosmaya baslayinca problem oluyor" dedigi sey buydu.
            //
            // HezarfenAnimator hizi CharacterController'dan okuyup
            // karisim agacina veriyor; esikler WalkController'in
            // sabitlerinden turedigi icin ayaklar yerde kaymaz.
            var animator = ornek.GetComponentInChildren<Animator>(true);
            if (animator == null)
            {
                not += " (ANIMATOR YOK — karakter canlanmaz)";
                Debug.LogWarning("[Hezarfen] Govdede Animator yok.");
            }
            else
            {
                if (animator.runtimeAnimatorController == null)
                    Debug.LogWarning("[Hezarfen] Govde animatorunde controller "
                                     + "YOK — Boru Hatti -> Karakter "
                                     + "animatorunu bagla.");
                animator.applyRootMotion = false;

                var surucu = oyuncu.GetComponent<HezarfenAnimator>()
                             ?? oyuncu.AddComponent<HezarfenAnimator>();
                surucu.animator = animator;
                surucu.karakterKontrol = oyuncu.GetComponent<CharacterController>();
                surucu.suzulme = oyuncu.GetComponent<GlideController>();

                // Ayak IK animatorun KENDI nesnesine biner: Unity
                // OnAnimatorIK'yi Animator ile ayni GameObject'te arar.
                if (animator.GetComponent<AyakIK>() == null)
                    animator.gameObject.AddComponent<AyakIK>();
                not += " + animator + ayak IK";
            }

            return ornek;
        }

        /// <summary>
        /// Noktanın üstündeki <b>gerçek yüzeyin</b> kotu.
        ///
        /// Arazi kotu yetmiyor: sokakta arazinin ÜSTÜNDE kaldırım var,
        /// yapıların altında taş kaide var. Oyuncuyu arazi kotuna
        /// oturtmak onu kaldırımın içine gömüyordu — ölçüldü, bele kadar.
        ///
        /// Yukarıdan aşağı ışın atılır ve ilk çarpılan yüzey alınır;
        /// hiçbir şeye çarpmazsa araziye düşülür.
        /// </summary>
        /// <summary>
        /// Bu nokta üzerinde <b>durulabilir bir zemin</b> mi.
        ///
        /// ## Neden yükseklik yetmiyor
        ///
        /// Önce yükseklik eşiğiyle deneniyordu: 1,5 m damı eledi ama
        /// şadırvanı elemedi (kenarı +1,0 m) ve oyuncu her açılışta
        /// çeşmenin üstünde doğdu. 0,35 m'ye çekildi ve bu kez
        /// <b>hiçbir aday geçmedi</b> — Galata bir yamaç, kaldırım ve
        /// kaide arazinin yarım metre üstünde olabiliyor ve bu meşru.
        ///
        /// Tek bir sayı hem yamacı hem çeşmeyi eleyemiyor, çünkü ikisi
        /// aynı şeyi ölçmüyor. Doğru soru yükseklik değil <b>kimlik</b>:
        /// ışın zaten neye çarptığını biliyor.
        /// </summary>
        private static bool ZemindeMi(float yuzeyKotu, Terrain arazi,
                                      Vector3 nokta)
        {
            var bas = new Vector3(nokta.x, yuzeyKotu + 3f, nokta.z);
            if (!Physics.Raycast(bas, Vector3.down, out var vurus, 6f, ~0,
                                 QueryTriggerInteraction.Ignore))
                return true;                       // arazi, engel yok

            if (vurus.collider is TerrainCollider) return true;

            // IZIN LISTESI DEGIL, RET LISTESI.
            //
            // Once "yalniz sunlarin ustunde durulabilir" diye yazdim
            // (kaldirim, kaide, sokak, arazi) ve **yine hicbir aday
            // gecmedi**: carpistiriciyi tasiyan nesnenin adi bu
            // listedekilerden biri degil — semt sahnesinde parcalar
            // baska adlarla gruplanmis.
            //
            // Bir izin listesi, listelemedigi her seyi reddeder ve
            // dunyanin adlandirmasini tam bilmeden yazilamaz. Ret
            // listesi ise yalnizca bildigim seyi reddeder ve
            // bilmediklerimi serbest birakir — burada dogru olan bu,
            // cunku sorulan soru "bu yuzey nedir" degil, **"bu yuzey
            // ustune cikilmamasi gereken bir NESNE mi"**.
            var t = vurus.collider.transform;
            for (int derinlik = 0; t != null && derinlik < 6; derinlik++)
            {
                if (UstuneCikilmaz(t.name)) return false;
                t = t.parent;
            }
            return true;
        }

        /// <summary>
        /// Bu ad, üstünde durulmaması gereken bir nesneyi mi gösteriyor.
        ///
        /// Liste kısa ve <b>avlu eşyalarından</b> geliyor
        /// (<c>HayatDokusu.Esyalar</c>): şehirde 26.511 tanesi var ve
        /// oyuncunun ilk karesi hiçbirinin üstünde açılmamalı.
        /// Şadırvan ve çeşme ayrıca burada, çünkü sokak grafı düğümü
        /// tam onların yanında duruyor ve ışın onlara çarpıyor.
        /// </summary>
        private static bool UstuneCikilmaz(string ad)
        {
            if (string.IsNullOrEmpty(ad)) return false;
            return ad.StartsWith("PF_Sadirvan") || ad.StartsWith("PF_Cesme")
                || ad.StartsWith("PF_SuKupu") || ad.StartsWith("PF_Odunluk")
                || ad.StartsWith("PF_Cardak") || ad.StartsWith("PF_Kuyu")
                || ad.StartsWith("PF_Sepet") || ad.StartsWith("PF_Cit")
                || ad.StartsWith("PF_Sebze") || ad.StartsWith("PF_BahceAgaci");
        }

        private static float YuzeyKotu(Terrain arazi, Vector3 nokta)
        {
            var bas = new Vector3(nokta.x, nokta.y + 60f, nokta.z);
            if (Physics.Raycast(bas, Vector3.down, out var vurus, 200f, ~0,
                                QueryTriggerInteraction.Ignore))
                return vurus.point.y;
            return arazi != null
                ? arazi.SampleHeight(nokta) + arazi.transform.position.y
                : nokta.y;
        }

        /// <summary>Adı verilen tekil sistem nesnesini bulur ya da kurar.</summary>
        private static T Tekil<T>(string ad) where T : Component
        {
            var go = GameObject.Find(ad);
            if (go == null) go = new GameObject(ad);
            return go.GetComponent<T>() ?? go.AddComponent<T>();
        }
    }
}
