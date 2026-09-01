using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Hezarfen.Editor.Lighting
{
    /// <summary>
    /// <b>Kalıcı ışık pası</b> — Faz 7'nin ilk işi.
    ///
    /// PLAN Bölüm 12: *"Faz 7 başlarken yapılacak İLK iş: geçici aydınlatma
    /// takımını SİLMEK. Kalıcı ışık pası bunun ÜSTÜNE kurulmaz, YERİNE
    /// kurulur."*
    ///
    /// ## Geçici takım neyi taklit ediyordu
    ///
    /// Eksik olan tek terim vardı: <b>sıçrama</b>. HDRP'de gerçek zamanlı
    /// küresel aydınlatma yoktur; ışık pişirilmediği sürece bir duvar
    /// yalnızca güneşi ve göğü görür, karşı duvardan dönen ışığı görmez.
    /// Dar bir Osmanlı sokağında gölgeyi asıl dolduran şey odur. Geçici
    /// takım bunu iki <b>gölgesiz</b> dolgu ışığı ve şişirilmiş bir gök
    /// çarpanıyla taklit ediyordu — fizikî değil, kadraja göre ayarlanmış.
    ///
    /// ## Kalıcı çözüm o terimi TAKLİT etmiyor, HESAPLIYOR
    ///
    /// İki katman, ikisi de gerçek:
    ///
    /// <list type="number">
    /// <item><b>Adaptive Probe Volumes (APV)</b> — temel. Sahne bir prob
    ///   ızgarasıyla kaplanır ve sıçrama <b>pişirilir</b>. Her donanım
    ///   kademesinde açık; şehir aktığında problar da akar. Bir sokağın
    ///   gölgesini gerçekten karşı cephe doldurur.</item>
    /// <item><b>SSGI</b> — üstüne. Ekranda görünen yüzeylerden gelen anlık
    ///   sıçrama; probların yakalayamadığı ince ayrıntıyı (cumba altı,
    ///   kemer içi) tamamlar. PLAN'ın dediği gibi <b>kademeli</b>:
    ///   Performant'ta kapalı, Balanced ve High'da açık.</item>
    /// </list>
    ///
    /// ## Poz artık elle çekilmiyor
    ///
    /// Geçici takım pozu 14,5'ten 13,0 EV'ye çekiyordu, çünkü sahne toptan
    /// az pozlanmıştı — sıçrama eksik olduğu için. Terim yerine gelince o
    /// düzeltmeye gerek kalmaz: poz <b>fizikî</b> değerine döner ve
    /// otomatik poz gün boyu (öğle 15 EV, alacakaranlık 6 EV) kendi
    /// ayarlar. Gözün yaptığı da budur.
    /// </summary>
    public static class KaliciAydinlatma
    {
        public const string RigName = "AYDINLATMA_Kalici";
        public const string ProbeName = "APV_Sehir";

        private const string ProfileDir = "Assets/_Project/Settings";
        private const string ProfilePath =
            ProfileDir + "/VP_Kalici_Aydinlatma.asset";

        /// <summary>
        /// Fizikî öğle pozu (EV100). Geçici takım bunu 13,0'a çekiyordu;
        /// sıçrama terimi geri gelince gerçek değerine dönüyor.
        /// </summary>
        public const float OgleEV = 14.5f;

        /// <summary>
        /// Otomatik pozun <b>alt</b> sınırı (EV100).
        ///
        /// 6,0 ile başladı ve adı doğruydu — 6 EV alacakaranlıktır.
        /// Ama sınır alacakaranlıkta durunca <b>gece hiç açılmadı</b>:
        /// ay ışığı 0,24 lüks, yani kabaca −3 EV; kamera 6 EV'de
        /// kilitliyken o sahne siyahtır. Gece karesi ölçüldü, 78 KB'lık
        /// tek renk bir PNG çıktı ve sebebi ışığın yokluğu sanıldı.
        /// Işık eklendi, kare 367 KB'a çıktı ve <b>hâlâ</b> karanlıktı:
        /// eksik olan ikinci parça buydu.
        ///
        /// −1,0 seçildi ve <b>ölçülmedi</b> — bedeli buydu: sınır hiç
        /// ısırmadı ve gece gündüzle aynı parlaklıkta pozlandı.
        /// Aritmetik açık yazılırsa görülür. Dolunay 0,55 lüks
        /// (<see cref="AyIsigi.dolunayLuks"/>); albedosu 0,30 olan bir
        /// sıva duvarın parlaklığı L = ρ·E/π ≈ 0,053 cd/m², yani
        /// EV100 = log₂(L·8) ≈ <b>−1,25</b>. Sınır −1,0'da dururken
        /// kelepçe topu topu <b>çeyrek durak</b> ısırıyordu; histogram
        /// geceyi de orta griye çekiyor ve ay ile fener için harcanan
        /// bütün tur ekranda görünmez oluyordu.
        ///
        /// 2,0: gecenin kendi pozundan <b>3,25 durak koyu</b>. Gerçekçi
        /// gece bundan da koyudur (−3 EV çıplak gözle uyum ister) ama
        /// oyuncu yirmi dakika karanlığa alışamaz; 3,25 durak, biçimin
        /// okunduğu ama gecenin gece kaldığı yer. Sinemanın gece-gündüz
        /// farkı da bu mertebededir.
        ///
        /// Bu sayı artık <c>AydinlatmaProfiliTests</c>'te bir kapı:
        /// bir daha sessizce gevşeyemez.
        /// </summary>
        public const float GeceEV = 2.0f;

        /// <summary>
        /// Gölgenin görüldüğü en uzak mesafe (m).
        ///
        /// HDRP varsayılanı 150 m'ydi ve profilde bu ayar <b>hiç
        /// yoktu</b> — 11 override'ın hiçbiri ona dokunmuyordu. 320 m,
        /// dört kaskadla bölündüğünde yakın kaskadın dokunum
        /// yoğunluğunu düşürür ama atlas maliyeti sabit kalır; beklenen
        /// bedel yarım milisaniye mertebesinde ve <b>ölçülecek</b>.
        /// </summary>
        public const float GolgeMesafesi = 320f;

        /// <summary>Alacakaranlık pozu — artık yalnız belge değeri.</summary>
        public const float AlacakaranlikEV = 6.0f;

        /// <summary>
        /// Film grain şiddeti. <b>Çok hafif</b> — planın vurgusu.
        ///
        /// Ölçülebilir bir sınırı var: grain, okunabilirlik ölçüsünü
        /// (<see cref="SokakOkunabilirligi"/>) <b>yukarı</b> çekebilir,
        /// çünkü o ölçü komşu ortalamasından sapmayı sayıyor ve grain tam
        /// olarak odur. Yani ağır bir grain testi sahte biçimde geçirirdi.
        /// Değer bu yüzden ölçülerek seçildi, gözle değil.
        /// </summary>
        public const float GrainSiddeti = 0.12f;

        /// <summary>
        /// Haliç sabahının sisi (m). Sis bir efekt değil bir <b>yer</b>
        /// bilgisi: Haliç sabahları basar ve Galata'dan bakınca suriçi
        /// siluetini yumuşatan şey odur.
        /// </summary>
        public const float SisMesafesi = 1400f;

        // --------------------------------------------------------------
        [MenuItem("Hezarfen/Aydinlatma/Kalici isik pasini kur")]
        public static void KurMenu()
        {
            int n = Kur(out string rapor);
            Debug.Log($"[Hezarfen] Kalici isik pasi kuruldu ({n} parca).\n{rapor}");
            EditorSceneManager.MarkSceneDirty(
                EditorSceneManager.GetActiveScene());
        }

        [MenuItem("Hezarfen/Aydinlatma/Kalici isik pasini kaldir")]
        public static void KaldirMenu()
        {
            bool vardi = Kaldir();
            Debug.Log(vardi ? "[Hezarfen] Kalici isik pasi kaldirildi."
                            : "[Hezarfen] Kalici isik pasi zaten yoktu.");
            EditorSceneManager.MarkSceneDirty(
                EditorSceneManager.GetActiveScene());
        }

        public static bool Kaldir()
        {
            var eski = GameObject.Find(RigName);
            if (eski == null) return false;
            Object.DestroyImmediate(eski);
            return true;
        }

        /// <summary>
        /// Kalıcı pası kurar. <b>Geçici takımı da siler</b> — ikisi bir
        /// arada durursa sıçrama iki kere sayılır ve sahne patlar.
        /// </summary>
        public static int Kur(out string rapor)
        {
            Kaldir();

            // Gecici takim ARTIK YOK (ADR 0072, PLAN Bolum 12). Eski
            // sahnelerde kok nesnesi kalmis olabilir; adiyla temizlenir.
            // Sinifa bagimlilik birakmiyoruz — takim silindi.
            var gecici = GameObject.Find("GECICI_Aydinlatma");
            bool geciciVardi = gecici != null;
            if (geciciVardi) Object.DestroyImmediate(gecici);

            var kok = new GameObject(RigName);
            int n = 0;

            // 1) GLOBAL VOLUME — poz, sis, SSGI.
            var volumeGo = new GameObject("VOL_Kalici");
            volumeGo.transform.SetParent(kok.transform, false);
            var volume = volumeGo.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10f;   // gecici profilin ustunde
            volume.sharedProfile = Profil();
            n++;

            // 2) PROB HACMI — sicrama terimi burada PISIRILIR.
            //
            // MODE.GLOBAL DEGIL, ve bu bir ayrinti degil.
            //
            // Ilk yazimda `Mode.Global` vardi ("sahnenin tamamini kapla")
            // ve pisirme bitmedi: editor onbes dakika donduktan sonra tek
            // satir cikti bile uretmemisti. Sebep olculdu — sokak sahnesi
            // ARAZIYI de iceriyor, yani "sahnenin tamami" 10 km x 10 km
            // demek. APV'ye 100 km2'lik bos yamaci prob prob pisirtmeye
            // calismisim.
            //
            // Problar oyuncunun YURUDUGU yere aittir: binalarin arasina,
            // sokaga, avluya. Bos yamacta sicrama diye bir sorun zaten
            // yok — orada gokyuzu her seyi goruyor.
            var probeGo = new GameObject(ProbeName);
            probeGo.transform.SetParent(kok.transform, false);
            var pv = probeGo.AddComponent<ProbeVolume>();
            pv.mode = ProbeVolume.Mode.Local;

            var sinir = YapiSinirlari();
            probeGo.transform.position = sinir.center;
            pv.size = sinir.size;
            n++;

            rapor = (geciciVardi
                        ? "Gecici takim SILINDI (ustune degil yerine).\n"
                        : "Gecici takim zaten yoktu.\n")
                    + $"Poz: fiziki {OgleEV} EV (otomatik, alt sinir "
                    + $"{AlacakaranlikEV}).\n"
                    + $"Sis: {SisMesafesi} m (Halic sabahi).\n"
                    + $"Prob hacmi: {_probYazi}\n"
                    + "Sicrama: APV (pisirilmeli) + SSGI (kademeli).\n"
                    + "SONRAKI ADIM: Hezarfen -> Aydinlatma -> Problari pisir";
            return n;
        }

        /// <summary>En büyük prob hacmi kenarı (m) — pişirme bütçesi.</summary>
        public const float EnBuyukProbKenari = 600f;

        private static string _probYazi = "";

        /// <summary>
        /// Prob hacminin kaplayacağı kutu: <b>araziyi saymaz</b>.
        ///
        /// Sıçrama terimi binalar arasında gerekli; boş yamaçta gökyüzü
        /// zaten her şeyi görüyor. Araziyi katmak, prob ızgarasını on
        /// kilometreye yaymak ve pişirmeyi bitmez hâle getirmek demekti —
        /// bir kez denendi, bitmedi.
        /// </summary>
        public static Bounds YapiSinirlari()
        {
            bool ilk = true;
            var kutu = new Bounds(Vector3.zero, Vector3.one);
            foreach (var r in Object.FindObjectsByType<MeshRenderer>(
                         FindObjectsSortMode.None))
            {
                if (r.GetComponentInParent<Terrain>() != null) continue;
                // Su duzlemi ve benzeri devasa tek parcalar da sayilmaz.
                if (r.bounds.size.x > EnBuyukProbKenari * 2f) continue;
                if (ilk) { kutu = r.bounds; ilk = false; }
                else kutu.Encapsulate(r.bounds);
            }
            if (ilk) kutu = new Bounds(Vector3.zero, Vector3.one * 60f);

            // Kenari yine de sinirla: bir semt 600 m'yi asarsa prob hacmi
            // semt basina BOLUNMELI, tek hacme sisirilmemeli.
            var b = kutu.size;
            b.x = Mathf.Min(b.x + 12f, EnBuyukProbKenari);
            b.z = Mathf.Min(b.z + 12f, EnBuyukProbKenari);
            b.y = Mathf.Min(b.y + 12f, 120f);   // catilarin biraz ustu
            kutu.size = b;

            _probYazi = $"{b.x:F0} x {b.y:F0} x {b.z:F0} m @ "
                + $"({kutu.center.x:F0}, {kutu.center.y:F0}, "
                + $"{kutu.center.z:F0})  [arazi HARIC]";
            return kutu;
        }

        /// <summary>
        /// Bileşeni bulur ya da kurar — <b>ve diske yazar</b>.
        ///
        /// `VolumeProfile.Add&lt;T&gt;()` bileşeni yalnızca bellekte kurar;
        /// `AddObjectToAsset` olmadan profil dosyası BOŞ kalır. Bir kez
        /// öyle oldu ve sonuç sessizdi: menü "kuruldu" dedi, sahne üç
        /// durak karardı, ölçüm 2,62'den 0,55'e düştü. Bu deyim zaten
        /// geçici takımda vardı; yeniden yazarken kullanmadım.
        /// </summary>
        private static T Ensure<T>(VolumeProfile profil) where T : VolumeComponent
        {
            if (profil.TryGet(out T mevcut) && mevcut != null) return mevcut;
            var c = profil.Add<T>(true);
            c.hideFlags = HideFlags.HideInHierarchy;
            if (AssetDatabase.Contains(profil))
                AssetDatabase.AddObjectToAsset(c, profil);
            return c;
        }

        /// <summary>
        /// Pozu <b>sabitler</b> — yalnız çevrimdışı kare yakalama için.
        ///
        /// Oyunda poz otomatiktir ve doğrusu odur. Ama inceleme paketi tek
        /// bir kare render eder ve otomatik poz uyum sağlamaya vakit
        /// bulamaz: aynı sahne iki koşumda iki farklı parlaklık verirdi.
        /// Ölçüm tekrarlanabilir olmalı, o yüzden yakalama sırasında poz
        /// çivilenir — ve iş bitince <see cref="OtomatikPoz"/> ile geri
        /// alınır.
        /// </summary>
        public static void SabitPoz(float ev)
        {
            var poz = Ensure<Exposure>(Profil());
            poz.mode.overrideState = true;
            poz.mode.value = ExposureMode.Fixed;
            poz.fixedExposure.overrideState = true;
            poz.fixedExposure.value = ev;
            EditorUtility.SetDirty(poz);
            AssetDatabase.SaveAssets();
        }

        /// <summary>Pozu oyundaki hâline — otomatiğe — döndürür.</summary>
        public static void OtomatikPoz()
        {
            var poz = Ensure<Exposure>(Profil());
            poz.mode.overrideState = true;
            poz.mode.value = ExposureMode.AutomaticHistogram;
            EditorUtility.SetDirty(poz);
            AssetDatabase.SaveAssets();
        }

        /// <summary>Kalıcı Volume profili — yoksa üretilir.</summary>
        public static VolumeProfile Profil()
        {
            // MEVCUT PROFIL ERKEN DONMEZ.
            //
            // Once "bilesen varsa oldugu gibi dondur" diyordu ve yeni bir
            // bilesen (film grain, ton egrisi) eklendiginde o kod HIC
            // KOSMADI: profil uc bilesenle duruyor, menu "kuruldu" diyor,
            // eklenen sey ortada yok. Sessiz, cunku hata vermiyor.
            //
            // `Ensure<T>` zaten fikirsizdir — varsa bulur, yoksa kurar.
            // O yuzden her cagrida HEPSI gecilir ve profil kendini onarir.
            var profil = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            if (profil == null)
            {
                Directory.CreateDirectory(ProfileDir);
                profil = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profil, ProfilePath);
            }

            // BILESENLER ALT-VARLIK OLARAK KAYDEDILMELI.
            //
            // `VolumeProfile.Add<T>()` bileseni yalnizca BELLEKTE kurar;
            // diske yazilmasi icin ayrica `AddObjectToAsset` gerekir. Ilk
            // yazimda bu yoktu ve sonuc SESSIZDI: dosya olustu, menu
            // "kuruldu" dedi, ama profilin icinde hicbir sey yoktu. Poz
            // gecici takimin 13,0 EV'sini kaybedince sahne karardi ve
            // olcum 2,62'den 0,55'e dustu. Once sicramadan suphelendim;
            // oysa profil bostu.

            // --- POZ: otomatik, fiziki sinirlar icinde ------------------
            //
            // Sabit bir EV, gun boyu donen bir gunesle calismaz: ogleyin
            // dogru olan deger alacakaranlikta sahneyi karartir. Goz de
            // uyum saglar; kamera da saglasin.
            var poz = Ensure<Exposure>(profil);
            poz.mode.overrideState = true;
            poz.mode.value = ExposureMode.AutomaticHistogram;
            poz.limitMin.overrideState = true;
            poz.limitMin.value = GeceEV;
            poz.limitMax.overrideState = true;
            poz.limitMax.value = OgleEV + 2.0f;
            poz.adaptationMode.overrideState = true;
            poz.adaptationMode.value = AdaptationMode.Progressive;
            // Gozun uyumu ani degildir: karanlik bir hana girince bir an
            // hicbir sey gorulmez, sonra acilir. Hizli uyum bu duyguyu
            // tumden siler.
            poz.adaptationSpeedDarkToLight.overrideState = true;
            poz.adaptationSpeedDarkToLight.value = 1.2f;
            poz.adaptationSpeedLightToDark.overrideState = true;
            poz.adaptationSpeedLightToDark.value = 0.6f;

            // OLCUM EKRANIN MERKEZINDEN ALINMAZ — BU BIR UCUS OYUNU.
            //
            // Merkez agirlikli olcum yerde makul, havada felaket:
            // burnu kaldirinca ekranin merkezi GOKYUZU olur, poz
            // gokyuzune gore ayarlanir ve sehir kararir; dalisa gecince
            // merkez zemin olur ve ekran patlar. Oyuncu 86 saniyelik bir
            // suzulus boyunca bu pompalamayi izliyordu.
            //
            // Yordamsal maske dokusuz calisir: agirlik merkezi ekranin
            // biraz ALTINA (0,42) alinir, cunku ucusta bakilan sey
            // asagidaki sehirdir, ustteki bosluk degil.
            poz.meteringMode.overrideState = true;
            poz.meteringMode.value = MeteringMode.ProceduralMask;
            poz.proceduralCenter.overrideState = true;
            poz.proceduralCenter.value = new Vector2(0.5f, 0.42f);
            poz.proceduralRadii.overrideState = true;
            poz.proceduralRadii.value = new Vector2(0.4f, 0.4f);
            poz.proceduralSoftness.overrideState = true;
            poz.proceduralSoftness.value = 0.5f;

            // --- GOLGE MESAFESI ----------------------------------------
            //
            // Profilde `HDShadowSettings` HIC YOKTU, yani HDRP
            // varsayilani (150 m) gecerliydi. Kule serefesinden Halic'e
            // bakan oyuncu icin bu sunu demek: Suleymaniye, Ayasofya,
            // sur hatti ve butun Surici **hicbir golge dusurmuyor**.
            // Sehir kesilmis karton gibi okunuyordu.
            //
            // Ortam ortme (1,2 m) ve temas golgesi (0,15 m) bu olcekte
            // yardim edemez — ikisi de yakin olcek araci. 320 m: yaya
            // gozunden bir mahalle derinligi, ucustan bir siluet.
            var golge = Ensure<HDShadowSettings>(profil);
            golge.maxShadowDistance.overrideState = true;
            golge.maxShadowDistance.value = GolgeMesafesi;
            golge.cascadeShadowSplitCount.overrideState = true;
            golge.cascadeShadowSplitCount.value = 4;

            // KASKADLAR ARASI BANT: SERT HALKA OLMASIN.
            //
            // 320 m ve 0,05/0,15/0,3 bolunmeleriyle sinirlar 16, 48 ve
            // 96 m'ye dusuyor; bant sifir oldugu icin yururken golge
            // cozunurlugu o mesafelerde SERT HALKA halinde degisiyor.
            // Gecen tur mesafeyi 150'den 320'ye cektim ve bandi
            // koymadim — mesafeyi uzatmak halkalari daha gorunur
            // yapti.
            golge.cascadeShadowBorder0.overrideState = true;
            golge.cascadeShadowBorder0.value = 0.10f;
            golge.cascadeShadowBorder1.overrideState = true;
            golge.cascadeShadowBorder1.value = 0.10f;
            golge.cascadeShadowBorder2.overrideState = true;
            golge.cascadeShadowBorder2.value = 0.10f;

            // --- SSGI: PROFILDEN CIKARILIR (ADR 0086) ------------------
            //
            // Profil tam donanimli bir `GlobalIllumination` bileseni
            // tasiyordu — `enable: 1`, `fullResolutionSS: 1`,
            // `m_MaxRaySteps: 32` — ve etkin ardisik duzen
            // (`HDRP Balanced`) `supportSSGI: 0` ile onu HIC
            // DERLEMIYOR. Yani "11 override diskte" diye sayilan
            // katmanlardan biri tamamen oluydu ve bir sonraki okuyan
            // "GI acik" diye okuyacakti.
            //
            // Silmek aciktan yegdir: dolayli aydinlatma zaten APV'den
            // geliyor ve APV GERCEKTEN pisirilmis (98 MB CellData
            // diskte). Bir profil, tasidigi seyi yapmiyorsa yalan
            // soyluyor demektir.
            var gi = profil.components.Find(
                c => c is UnityEngine.Rendering.HighDefinition.GlobalIllumination);
            if (gi != null)
            {
                profil.Remove<UnityEngine.Rendering.HighDefinition.GlobalIllumination>();
                Object.DestroyImmediate(gi, true);
            }

            // --- SIS: Halic sabahi -------------------------------------
            var sis = Ensure<Fog>(profil);
            sis.enabled.overrideState = true;
            sis.enabled.value = true;
            sis.meanFreePath.overrideState = true;
            sis.meanFreePath.value = SisMesafesi;
            sis.baseHeight.overrideState = true;
            sis.baseHeight.value = 0f;          // deniz seviyesi
            sis.maximumHeight.overrideState = true;
            sis.maximumHeight.value = 220f;     // ucus kotunun ustu

            // ATMOSFER 64 METREDE BITIYORDU.
            //
            // Volumetrik sis hacmi kameradan yalnizca **64 m**
            // uzaniyordu (HDRP varsayilani); otesinde analitik sise
            // dusuluyor. Yani isik huzmesi, gunesin sisteki halesi,
            // minareler arasindan gecen isik — hepsi 64 m'nin
            // icinde, ve ucusun TAMAMI o mesafenin disinda. Kuleden
            // atladigin an atmosfer bitiyordu.
            //
            // Bedeli sifir: `m_FogControlMode` Balance ve butce
            // `m_VolumetricFogBudget` ile sabit; `depthExtent` ayni
            // froxel izgarasinin DUNYADAKI dagilimini degistirir,
            // sayisini degil. Dilim dagilimi kameraya yaklastirilir
            // (0,40) ki yakin hassasiyet kaybolmasin.
            sis.depthExtent.overrideState = true;
            sis.depthExtent.value = 900f;
            sis.sliceDistributionUniformity.overrideState = true;
            sis.sliceDistributionUniformity.value = 0.40f;

            // ILERI SACILMA: GUNESE BAKINCA HALE.
            //
            // `anisotropy = 0` izotropik sacilma demek, yani gunese
            // dogru bakmakla sirtini donmek arasinda fark yok. Gercek
            // atmosferde fark buyuktur ve mesafeyi okunur kilan sey
            // odur. 0,45 ileri sacilmali bir Henyey-Greenstein terimi;
            // olcum gurultusunun altinda bir maliyet.
            sis.anisotropy.overrideState = true;
            sis.anisotropy.value = 0.45f;
            sis.multipleScatteringIntensity.overrideState = true;
            sis.multipleScatteringIntensity.value = 0.15f;
            sis.enableVolumetricFog.overrideState = true;
            sis.enableVolumetricFog.value = true;

            // --- SSGI: sicramanin ustune, kademeli ---------------------
            var ssgi = Ensure<GlobalIllumination>(profil);
            ssgi.enable.overrideState = true;
            ssgi.enable.value = true;

            // --- POST: gravür esintisi, ÇOK hafif ---------------------
            //
            // PLAN Bölüm 12: *"hafif film grain + ton eğrisi (dönem gravür
            // esintisi ÇOK hafif)"* — vurgu planın kendisinde.
            //
            // Neden ağır olmamalı: Lorck ve Grelot gravürleri referans
            // olarak duruyor ama oyun bir gravür DEĞİL. Ağır bir grain ya
            // da sert bir eğri, üç fazdır ölçerek kurduğumuz malzeme ve
            // ışık farklarını bir dokunun altına gömerdi — Balat'ın
            // bilerek koyu paleti ile gölgedeki sıva aynı kirli griye
            // düşerdi.
            var grain = Ensure<FilmGrain>(profil);
            grain.type.overrideState = true;
            grain.type.value = FilmGrainLookup.Thin1;
            grain.intensity.overrideState = true;
            grain.intensity.value = GrainSiddeti;
            // Karanlıkta grain daha görünürdür; tepki eğrisi onu kısar.
            grain.response.overrideState = true;
            grain.response.value = 0.8f;

            var ton = Ensure<Tonemapping>(profil);
            ton.mode.overrideState = true;
            // ACES DEGIL: filmik ve kontrastli, gravür değil sinema
            // hissi verir. Nötr, dizginlenmiş bir eğri gravürün düz
            // tonlamasına daha yakın.
            ton.mode.value = TonemappingMode.Neutral;

            EditorUtility.SetDirty(profil);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(ProfilePath);

            // KURDUGUNU DOGRULA. Bos bir profil sessizce kabul edilirse
            // sahne kararir ve sebep gorunmez.
            if (profil.components.Count == 0)
                Debug.LogError("[Hezarfen] Kalici profil BOS kaldi — poz, "
                               + "sis ve SSGI yazilamadi.");
            return profil;
        }

        // --------------------------------------------------------------
        /// <summary>
        /// Prob ızgarasını pişirir — <b>sıçrama terimi burada doğar</b>.
        ///
        /// <c>Lightmapping.Bake()</c> DEĞİL: o çağrı sahnenin her şeyini
        /// pişirir (ışık haritaları dahil), eşzamanlıdır ve editörü
        /// kilitler — ilk denemede editör dakikalarca yanıt vermedi ve
        /// hangi işin sürdüğü bile görünmedi. Burada yalnızca <b>problar</b>
        /// pişiriliyor ve iş asenkron: editör yaşamaya devam eder.
        /// </summary>
        [MenuItem("Hezarfen/Aydinlatma/Problari pisir")]
        public static void PisirMenu()
        {
            var pv = Object.FindAnyObjectByType<ProbeVolume>();
            if (pv == null)
            {
                Debug.LogError("[Hezarfen] Sahnede prob hacmi yok — once "
                               + "'Kalici isik pasini kur'.");
                return;
            }
            Debug.Log("[Hezarfen] Problar pisiriliyor (yalniz APV)...");
            AdaptiveProbeVolumes.BakeAsync();
        }
    }
}
