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
            sehir.sakinSayisi = NPCYonetici.VarsayilanSakin;
            sehir.gorunurMesafe = NPCYonetici.VarsayilanGorunurMesafe;
            sehir.dilim = NPCYonetici.VarsayilanDilim;
            rapor.Add($"Sehir: {(graf == null ? "GRAF YOK" : graf.dugumler.Count + " dugum")}, "
                      + $"{meslekler.Count} meslek, {sehir.sakinSayisi} sakin");

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
            rapor.Add("Replik: gosterici kuruldu");

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

                    // YUZEY ZEMIN KATI OLMALI — CATI DEGIL.
                    //
                    // Aciklik puani catiyi sever: bir damin uzerinde sekiz
                    // isin de serbesttir. Olculdu, secim oyuncuyu bir
                    // kahvehanenin damina koydu (PF_Kahvehane_A, kot
                    // 74,59 iken arazi 70,9). Zeminden 1,5 m'den fazla
                    // yukaridaki her yuzey damdir ya da terastir.
                    float araziKotu = arazi != null
                        ? arazi.SampleHeight(aday) + arazi.transform.position.y
                        : yy;
                    if (yy - araziKotu > 1.5f) continue;

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
                    if (puan <= enIyiPuan) continue;
                    enIyiPuan = puan;
                    secilen = ayak;
                    if (puan == 8) break;      // tam acik, daha iyisi yok
                }

                if (secilen.HasValue)
                {
                    float uzak = Vector3.Distance(secilen.Value, baslangic);
                    baslangic = secilen.Value;
                    Debug.Log($"[Hezarfen] Dogum yeri: {uzak:F0} m otede, "
                              + $"aciklik {enIyiPuan}/8, kot {baslangic.y:F1}.");
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
            kamGo.AddComponent<Camera>();
            kamGo.AddComponent<AudioListener>();
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
                not += " + animator";
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
