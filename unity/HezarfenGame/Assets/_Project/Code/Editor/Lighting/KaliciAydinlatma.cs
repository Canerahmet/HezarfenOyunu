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

        /// <summary>
        /// Ölü <c>GlobalIllumination</c> bileşenini profilden çıkarır.
        ///
        /// Etkin ardışık düzen (<c>HDRP Balanced</c>) <c>supportSSGI: 0</c>
        /// ile onu hiç derlemiyor; profilde durması "GI açık" diye
        /// okunan bir yalandır. Dolaylı aydınlatma APV'den geliyor ve
        /// APV gerçekten pişirilmiş.
        ///
        /// <b>Alt-varlık da silinir.</b> Yalnız listeden çıkarmak
        /// dosyada yetim bir nesne bırakır ve <c>components</c> sayısı
        /// ile diskteki alt-varlık sayısı ayrışır — bu depoda o
        /// ayrışmayı yakalayan bir test zaten var.
        /// </summary>
        private static void SsgiyiKaldir(VolumeProfile profil)
        {
            var gi = profil.components.Find(c => c is GlobalIllumination);
            if (gi == null) return;
            profil.components.Remove(gi);
            AssetDatabase.RemoveObjectFromAsset(gi);
            Object.DestroyImmediate(gi, true);
            EditorUtility.SetDirty(profil);
        }

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

            // 3) FIRININ GOKYUZU — problar gogu buradan ogrenir.
            string gokRapor = GokAyari();
            n++;

            rapor = (geciciVardi
                        ? "Gecici takim SILINDI (ustune degil yerine).\n"
                        : "Gecici takim zaten yoktu.\n")
                    + $"Poz: fiziki {OgleEV} EV (otomatik, alt sinir "
                    + $"{AlacakaranlikEV}).\n"
                    + $"Sis: {SisMesafesi} m (Halic sabahi).\n"
                    + $"Prob hacmi: {_probYazi}\n"
                    + gokRapor + "\n"
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

            Bulutlar(profil);

            EditorUtility.SetDirty(profil);
                        // SILME EN SONDA — CUNKU BIR SEYI SILIP SONRA GERI EKLEDIM.
            //
            // Ilk denemede bu blok burada duruyordu ve elli satir
            // asagida `Ensure<GlobalIllumination>` onu **geri
            // ekliyordu**. Tur raporuna "profilden silindi" diye
            // yazdim; diskte iki `GlobalIllumination` satiri duruyordu
            // ve bir yorumcu bunu bularak beni duzeltti.
            //
            // Bu, bu oturumun tekrar eden kusurunun en sade hali:
            // yaptigimi ölçmedim. Silme artik profilin kurulmasi
            // BITTIKTEN sonra kosuyor ve alt-varlik da diskten
            // kaldiriliyor — bellekten kaldirmak yetmez, dosyada kalir.
            SsgiyiKaldir(profil);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(ProfilePath);

            // KURDUGUNU DOGRULA. Bos bir profil sessizce kabul edilirse
            // sahne kararir ve sebep gorunmez.
            if (profil.components.Count == 0)
                Debug.LogError("[Hezarfen] Kalici profil BOS kaldi — poz, "
                               + "sis ve SSGI yazilamadi.");
            return profil;
        }

        /// <summary>
        /// <b>Hacimsel bulutlar</b> — uçuş oyununun asıl manzarası.
        /// </summary>
        /// <remarks>
        /// HDRP'nin bulut desteği projede <b>kapalıydı</b>
        /// (<c>supportVolumetricClouds: 0</c>) ve gökyüzü düz bir
        /// gradyandı. Yerde yürüyen bir oyun için bu bir eksiklik bile
        /// sayılmayabilir; <b>süzülen</b> bir oyun için gökyüzü zeminin
        /// yarısı kadar önemlidir — yüksekliği, hızı ve yönü okutan
        /// tek şey odur. 800 m'de düz mavi bir gökte süzülmek, hiç
        /// hareket etmemek gibi görünüyordu.
        ///
        /// ## Ayarlar neden bunlar
        ///
        /// <c>Sparse</c>: Mayıs sabahı İstanbul'u. Kapalı bir gök
        /// (<c>Overcast</c>) hem tarihsel olarak keyfî olurdu hem de
        /// termal görselleştirmesini (ADR 0084) okunmaz yapardı —
        /// oyuncunun yükselen havayı bulut tabanından okuması gerekiyor.
        ///
        /// <b>Gölge AÇIK.</b> Bedeli var ama asıl kazanç orada: bulut
        /// gölgesi şehrin üstünden geçtiğinde yükseklik ve hız
        /// hissedilir hâle gelir. Gölgesiz bulut bir duvar kâğıdıdır.
        ///
        /// Adım sayıları (32/8) HDRP'nin varsayılanının altında ve bu
        /// bilinçli: kare bütçesi 16,7 ms ve bu oyunun kalabalığı da
        /// var. Ölçüm neyin ödendiğini söyler — bu blok eklendikten
        /// sonra kare 7,6 ms'den ne olduysa, tur raporuna o yazılır.
        /// </remarks>
        private static void Bulutlar(VolumeProfile profil)
        {
            var b = Ensure<VolumetricClouds>(profil);
            b.enable.overrideState = true;
            b.enable.value = true;

            b.cloudControl.overrideState = true;
            b.cloudControl.value = VolumetricClouds.CloudControl.Simple;
            // `cloudPreset` bir VolumeParameter DEGIL, duz bir enum
            // alani — ustune `.value` yazmak derlenmez.
            b.cloudPreset = VolumetricClouds.CloudPresets.Sparse;

            // TABAN KOTU UCUS ZARFINDAN TURER, ZEVKTEN DEGIL.
            //
            // Kule serefesi 35 m, en uzun sizulus ~200 m'ye cikiyor
            // (UcusDizisi olcumleri). Bulut tabani 1.200 m: oyuncu asla
            // bulutun icine girmez — girseydi kamera beyaza gomulur ve
            // sehir kaybolurdu. Yeterince yakin ki olcek versin,
            // yeterince uzak ki engel olmasin.
            b.bottomAltitude.overrideState = true;
            b.bottomAltitude.value = 1200f;
            b.altitudeRange.overrideState = true;
            b.altitudeRange.value = 1800f;

            b.shadows.overrideState = true;
            b.shadows.value = true;

            b.numPrimarySteps.overrideState = true;
            b.numPrimarySteps.value = 32;
            b.numLightSteps.overrideState = true;
            b.numLightSteps.value = 8;

            // Zamansal birikim: gurultuyu karelere yayar. Yuksek deger
            // ucusta iz birakir (kamera hizli doner), dusuk deger
            // gurultulu olur. 0,90 ikisinin arasi.
            b.temporalAccumulationFactor.overrideState = true;
            b.temporalAccumulationFactor.value = 0.90f;
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
            Debug.Log("[Hezarfen] " + GokAyari());
            Debug.Log("[Hezarfen] Problar pisiriliyor (yalniz APV)...");
            AdaptiveProbeVolumes.BakeAsync();
        }

        /// <summary>
        /// Toplu kipten prob pişirme — <b>başlatır ve BEKLER</b>.
        ///
        /// Gölgeler ölçüldü ve ışık almadıkları görüldü: sokakta güneşli
        /// yüzey 196/255, gölge 36/15/0. Pozu iki durak açınca güneş
        /// 247'ye çıktı, gölge 41'de kaldı — yani gölge <b>az pozlanmış
        /// değil, aydınlatılmamış</b>. Statik geometrinin dolaylı ışığı
        /// APV'den geliyor (<c>lightProbeSystem: 1</c>) ve diskteki fırın
        /// 31 Ağustos'tan kalma; şehir o günden beri defalarca yeniden
        /// kuruldu.
        ///
        /// <c>BakeAsync</c> adı gibi çalışır: çağrı hemen döner. Toplu
        /// kipte <c>-quit</c> ile birlikte bu, "fırınladım" deyip hiçbir
        /// şey fırınlamamak olurdu — bu depoda aynı sınıf hata
        /// (<c>-quit</c> ile <c>-runTests</c>) yıllarca testleri hiç
        /// koşturmamıştı. Burada bitiş beklenir.
        ///
        /// Süre sınırı var, çünkü sonsuz bekleyen bir toplu koşum hata
        /// verenden kötüdür: kimse ne olduğunu bilmez.
        /// </summary>
        public static void TopluPisir()
        {
            EditorSceneManager.OpenScene(ProbeSahnesi);

            // SEHIR TABAN SAHNEDE DEGIL, SEMTLERDE.
            //
            // Firin yalniz taban sahneyi aciyordu ve pisirme kumesi tek
            // sahne iceriyordu (`singleSceneMode: 1`). Sonuc olculdu:
            // 2.829.507 prob pisti ve hepsi bos bir 729 x 648 m yamanin
            // ustundeydi; binalarin oldugu 35-41 MB'lik sekiz semt
            // sahnesine hic prob dusmedi. Sehrin dolayli isigi bu yuzden
            // yalnizca SSGI'dan geliyordu ve ustten bakan denetim
            // karesinde sokak simsiyah cikiyordu.
            //
            // Semtler ek olarak acilir; `SemtProblari.Kur` her birine
            // kendi `Mode.Global` hacmini koyar ve kumeye baglar.
            foreach (string semt in SemtProblari.Semtler())
                EditorSceneManager.OpenScene(semt, OpenSceneMode.Additive);
            Debug.Log("[Hezarfen] " + (SemtProblari.Kur(out string semtRapor) > 0
                          ? semtRapor : "Semt bulunamadi."));

            // PROB, ISIGA KATILAN GEOMETRININ CEVRESINE KONUR.
            //
            // Sehrin tamami "Contribute GI" isaretsizdi: D_Surici_Dogu
            // 498 nesne, hepsi 0; D_Galata 401 nesne, hepsi 0. Yani
            // firin her seferinde katilan hicbir sey bulamadi ve
            // 2.829.507 probu bos yamaca pisirdi — ve "basarili" dedi.
            Debug.Log("[Hezarfen] GI katilimi: "
                      + GIKatilimi.Kat(out string giRapor) + " cizici\n"
                      + giRapor);

            var pv = Object.FindAnyObjectByType<ProbeVolume>();
            if (pv == null)
            {
                Debug.LogError("[Hezarfen] Sahnede prob hacmi yok.");
                EditorApplication.Exit(1);
                return;
            }
            // PISIRILECEK BIR SEY OLMASI ICIN BAKED GI ACIK OLMALI.
            //
            // Sahnenin `m_LightingSettings` alani BOSTU (fileID: 0), yani
            // Unity'nin varsayilan ayarlari geciyordu ve orada "Baked
            // Global Illumination" kapali. `BakeAsync` bu durumda hicbir
            // sey yapmaz ve hicbir sey de soylemez — olculdu: 120 saniye
            // boyunca `Lightmapping.isRunning` hep false kaldi ve
            // diskteki firin 31 Agustos tarihiyle oldugu gibi durdu.
            //
            // Ayar bir varliga yazilir ve sahneye baglanir; boylece bir
            // dahaki sefere "acik miydi" diye sorulmaz.
            AydinlatmaAyari();

            Debug.Log("[Hezarfen] " + GokAyari());
            EditorSceneManager.SaveOpenScenes();

            Debug.Log("[Hezarfen] APV pisirme basladi (toplu kip).");
            _pisirmeBasi = System.DateTime.UtcNow;
            _sonIlerleme = 0f;
            _ilerlemeAni = 0.0;
            AdaptiveProbeVolumes.BakeAsync();
            EditorApplication.update += PisirmeyiBekle;
        }

        private const string AyarYolu =
            "Assets/_Project/Settings/LS_Hezarfen.lighting";

        /// <summary>
        /// Sahnenin aydınlatma ayarı — <b>Baked GI açık</b>.
        ///
        /// APV'nin pişirebilmesi için "Baked Global Illumination" açık
        /// olmak zorunda. Sahnede atanmış bir ayar yoktu ve varsayılanda
        /// kapalı; ölçüm bunu gösterdi (pişirme hiç başlamadı).
        ///
        /// Örnek sayısı düşük tutuldu: bu bir ışık haritası pişirmesi
        /// değil, yalnızca prob ızgarası. Şehir 9,6 × 7,8 km ve yüksek
        /// örnekleme burada saatlere mal olur, karşılığında gölgedeki
        /// gök ışığını daha doğru yapmaz — o ışık zaten yumuşak.
        /// </summary>
        private static void AydinlatmaAyari()
        {
            var ls = AssetDatabase.LoadAssetAtPath<LightingSettings>(AyarYolu);
            if (ls == null)
            {
                ls = new LightingSettings { name = "LS_Hezarfen" };
                const string dizin = "Assets/_Project/Settings";
                if (!AssetDatabase.IsValidFolder(dizin))
                    AssetDatabase.CreateFolder("Assets/_Project",
                                               "Settings");
                AssetDatabase.CreateAsset(ls, AyarYolu);
            }
            ls.bakedGI = true;
            ls.realtimeGI = false;
            // GPU DEGIL CPU — VE SEBEBI OLCULDU.
            //
            // Toplu pisirme iki kez COKTU: sureç hicbir sey yazmadan
            // yok oldu, gunlukte hata yok. Sayilar sebebi soyluyor —
            // gunlukteki tek satir "Transformed OOTS snapshot into
            // LightBaker scene input ... Size: 7251.37MB" ve bu
            // makinenin karti 8 GB. Yani sahne girdisi tek basina
            // VRAM'in tamamina yakin; ilerlemeli GPU firinini
            // calistiracak yer kalmiyor.
            //
            // Sistem bellegi 32 GB (olculdu), yani CPU firininin yeri
            // var. Yavas ama BITIYOR; bitmeyen bir firindan yavas bir
            // firin iyidir. Sehir kucultulmedi, prob araligi
            // seyreltilmedi: kusur kalite ayarinda degil, isin yanlis
            // yere verilmesindeydi.
            ls.lightmapper = LightingSettings.Lightmapper.ProgressiveCPU;
            // ORNEK SAYISI ISIN BOYUNA GORE SECILIR.
            //
            // 128 dolayli ornek + 2 sicrama, bir oda icin makul
            // sayilardir; 10 km'lik bir sehir icin degil. Olculdu:
            // GPU firini VRAM'e sigmayip coktu, CPU firini ise UC
            // SAATTE bitmedi. Ikisi de ayni seyi soyluyor — is,
            // makineye gore fazla.
            //
            // Neyi kesecegimi secerken prob IZGARASINI korudum:
            // araligi 3 m'den 6 m'ye cikarmak 7,2 m'lik bir sokaga
            // enine tek prob birakirdi ve sokak tam da isigin
            // olculmesi gereken yer. Kesilen sey ORNEKLEME: prob bir
            // kuresel harmonik, yani zaten agir ortalamasi alinmis bir
            // sey. 32 ornek onda gurultu birakmaz; ayni sayi bir isik
            // haritasinda leke yapardi.
            //
            // Sicrama 2 -> 1: acik hava sehrinde ikinci sicrama, ilkin
            // yaninda olcum gurultusu kadar kalir. Kapali ic mekan
            // gelince bu sayi yeniden sorulur.
            // ORNEK 32 -> 16, VE BU DA OLCULDU.
            //
            // D_Galata 4 m aralikla altmis dakikada **%21,9**'a geldi
            // ve dakikada %0,02 kazaniyordu — uc saatlik sinirin
            // altinda bitmesi mumkun degildi. Ayni ayarlarla D_Eyup 26
            // dakikada bitmisti; Galata'da 2.325 ev var, Eyup'ta bir
            // avuc. Yani maliyet PROB sayisindan degil GEOMETRIDEN
            // geliyor ve orneklemeyi yarilamak dogrudan yarilar.
            //
            // Fırında ISIK YOK (bkz. dosyanın basindaki not): hesap
            // gokyuzu gorunurlugu ve gok sicramasi. Bu, isik haritasi
            // degil bir kuresel harmonik — 16 ornek onda leke birakmaz.
            ls.directSampleCount = 16;
            ls.indirectSampleCount = 16;
            ls.maxBounces = 1;
            ls.ao = false;                 // AO ekranda zaten var (SSAO)
            EditorUtility.SetDirty(ls);
            Lightmapping.lightingSettings = ls;
            AssetDatabase.SaveAssets();
            Debug.Log("[Hezarfen] Aydinlatma ayari: Baked GI ACIK "
                      + $"({ls.indirectSampleCount} dolayli ornek).");
        }

        /// <summary>
        /// <b>Fırının gökyüzü</b> — problara gök ışığını veren şey.
        ///
        /// ## Ölçüm
        ///
        /// APV pişirildikten sonra turun kareleri ölçüldü ve gölgedeki
        /// zemin <c>(36, 15, 0)</c> çıktı: <b>mavi kanal sıfır</b>.
        /// Açık gök altındaki bir gölge güneşten daha mavidir, daha az
        /// değil; mavinin hiç olmaması "karanlık" demek değil, <b>gök
        /// hiç katkı vermiyor</b> demektir. Aynı turda Galata sokağının
        /// gölgesi 0,76 mavi/kırmızı oranı taşıyordu — yani kusur pozda
        /// ya da tonlamada değil, yerdeydi.
        ///
        /// Sebep sahnede yazılıydı: <c>StaticLightingSky</c> nesnesi
        /// vardı, <c>m_Profile</c> alanı <c>{fileID: 0}</c>. Fırın
        /// 2.829.507 probu <b>gökyüzüsüz</b> pişirdi; şehrin dolaylı
        /// ışığı olarak yalnız güneşin kiremitten ve sıvadan sıçrayan
        /// sıcak payı kaldı. Bu depoda dördüncü kez aynı sınıf kusur:
        /// nesne var, sayı büyük, taşıması gereken şey bağlanmamış.
        ///
        /// Bir önceki turda bu karanlığı ölçüp <i>"kusur değil, üstü
        /// kapalı sokak"</i> demiştim. Ölçtüğüm şey parlaklıktı ve
        /// parlaklık bu iki durumu ayırmıyor; ayıran ölçü <b>rengin
        /// mavisi</b>.
        ///
        /// ## Neden bulut fırına girmiyor
        ///
        /// Bulut gölgesi gezer; pişirilirse şehrin üstüne kalıcı bir
        /// leke olarak çakılır. Gök statik, bulut gerçek zamanlı.
        /// </summary>
        public static string GokAyari()
        {
            var sls = Object.FindAnyObjectByType<StaticLightingSky>();
            if (sls == null)
                sls = new GameObject("StaticLightingSky")
                      .AddComponent<StaticLightingSky>();

            var gokProfili = AssetDatabase.LoadAssetAtPath<VolumeProfile>(
                Gis.SkyProfileBuilder.ProfilePath);
            if (gokProfili == null)
                return "Gok profili YOK ("
                       + Gis.SkyProfileBuilder.ProfilePath
                       + ") — once 'Hezarfen > GIS > Faz1 gokyuzu "
                       + "profilini uret'.";

            // SIRA ONEMLI: `profile` atamasi HDRP icinde benzersiz
            // kimligi SIFIRLIYOR (paket kaynaginda yazili: "Changing the
            // volume is considered a destructive operation"). Kimlik
            // once yazilirsa profil onu siler ve firin yine goksuz
            // kosar.
            sls.profile = gokProfili;
            sls.staticLightingSkyUniqueID =
                SkySettings.GetUniqueID(typeof(PhysicallyBasedSky));
            sls.staticLightingCloudsUniqueID = 0;

            EditorUtility.SetDirty(sls);
            EditorSceneManager.MarkSceneDirty(
                EditorSceneManager.GetActiveScene());
            return $"Firin gokyuzu: {gokProfili.name} "
                   + $"(kimlik {sls.staticLightingSkyUniqueID}).";
        }

        /// <summary>
        /// Fırının gökyüzü <b>bağlı mı</b> — testin okuduğu ölçü.
        ///
        /// İki koşul birden: profil atanmış olacak <b>ve</b> kimlik o
        /// profilde gerçekten bulunan, etkin bir gökyüzünü gösterecek.
        /// Kimliği sıfır olan bir profil, profilsiz bir kimlik kadar
        /// karanlık pişirir.
        /// </summary>
        public static bool GokBagli(out string neden)
        {
            var sls = Object.FindAnyObjectByType<StaticLightingSky>();
            if (sls == null)
            {
                neden = "Sahnede StaticLightingSky yok.";
                return false;
            }
            if (sls.profile == null)
            {
                neden = "StaticLightingSky profilsiz — firin goksuz kosar.";
                return false;
            }
            if (sls.staticLightingSkyUniqueID == 0)
            {
                neden = "StaticLightingSky kimligi 0 — profil bagli ama "
                        + "gok secilmemis.";
                return false;
            }
            if (!sls.profile.TryGet<PhysicallyBasedSky>(out var gok)
                || !gok.active)
            {
                neden = $"Profil ({sls.profile.name}) etkin bir "
                        + "PhysicallyBasedSky tasimiyor.";
                return false;
            }
            neden = $"{sls.profile.name} / kimlik "
                    + $"{sls.staticLightingSkyUniqueID}";
            return true;
        }

        /// <summary>
        /// <b>Tek semtin problarini pisirir</b> — bütün şehri bir
        /// oturuşta pişiremeyen makine için.
        ///
        /// ## Neden gerekti
        ///
        /// Şehrin tamamı iki kez pişmedi: GPU fırını 8 GB'lık karta
        /// sığmayıp çöktü, CPU fırını üç saatte bitmedi. İş bölünebilir
        /// ve bölünmesi gereken yer belli — <b>semt</b>: oyun zaten
        /// semt semt akıtıyor, APV verisi de sahne sahne saklanıyor.
        ///
        /// ## Nasıl
        ///
        /// APV, yüklü sahnelerden hangilerinin pişeceğini
        /// <c>partialBakeSceneList</c> ile öğrenir ve listede olmayan
        /// sahnelerin hücrelerini <b>korur</b>
        /// (<c>ProbeGIBaking.Serialization</c>). Alan <c>internal</c>;
        /// yansımayla yazılıyor ve bu bilerek kaydediliyor: paket
        /// sürümü değişip alan kaybolursa kısmi pişirme sessizce
        /// TAM pişirmeye döner ve yine sığmaz. O yüzden bulunamazsa
        /// koşum durur, devam etmez.
        ///
        /// Semt adı komut satırından gelir:
        /// <c>-hezarfenSemt D_Galata</c>.
        /// </summary>
        public static void TopluPisirSemt()
        {
            string semt = KomutSatiri("-hezarfenSemt");
            if (string.IsNullOrEmpty(semt))
            {
                Debug.LogError("[Hezarfen] -hezarfenSemt <ad> verilmedi.");
                EditorApplication.Exit(1);
                return;
            }
            string yol = $"{SemtProblari.SemtDizini}/{semt}.unity";
            if (!System.IO.File.Exists(yol))
            {
                Debug.LogError($"[Hezarfen] Semt yok: {yol}");
                EditorApplication.Exit(1);
                return;
            }

            // BUTUN SEMTLER ACILIR — PISEN YALNIZ BIRI.
            //
            // Once yalniz taban + hedef semt aciliyordu ve olculdu:
            // ikinci semtin pisirmesi 10,5 dakika kostu, "basarili"
            // dondu, ve diskteki hucre dosyalari BIRINCI semtin saatini
            // tasimaya devam etti. Sebep kaydin icindeydi:
            //
            //   You are partially baking the set with an incompatible
            //   cell layout.
            //
            // APV'nin hucre izgarasi KUME capindadir ve o an YUKLU olan
            // prob hacimlerinden turer. Her kosumda baska bir semt
            // yuklu olunca izgara da baska cikiyor; kismi pisirme,
            // uyusmayan bir izgaraya yazamayacagi icin sonucu atiyor.
            //
            // Yani `partialBakeSceneList` "yalniz sunu YUKLE" demek
            // degil, "yalniz sunu PISIR" demek. Sahnelerin hepsi acik
            // olmali ki izgara her kosumda ayni cikssin; kazanc,
            // isik hesabinin bir semte inmesinden gelir.
            // `-hezarfenDonuk`: IZGARA DONMUS, YALNIZ HEDEF SEMT
            // YUKLENIR.
            //
            // Butun semtleri yuklemek izgarayi her kosumda ayni yapar
            // ama isik hesabini butun sehrin geometrisine karsi
            // kosturur; olculdu: en kucuk semt dokuz dakikada %6,2 ve
            // hiz dusuyordu — tek semt icin otuz saatin ustu.
            // `freezePlacement` izgarayi sabitleyince o yuke gerek
            // kalmiyor (bkz. `SemtProblari.YerlesimiDondur`).
            bool donuk = System.Array.IndexOf(
                System.Environment.GetCommandLineArgs(),
                "-hezarfenDonuk") >= 0;
            EditorSceneManager.OpenScene(ProbeSahnesi);
            if (donuk)
            {
                EditorSceneManager.OpenScene(yol, OpenSceneMode.Additive);
            }
            else
            {
                foreach (string s2 in SemtProblari.Semtler())
                    if (!EditorSceneManager.GetSceneByPath(s2).isLoaded)
                        EditorSceneManager.OpenScene(
                            s2, OpenSceneMode.Additive);
            }
            // SEMT KURULUMU VE GI BAYRAKLARI BURADA KOSMAZ.
            //
            // Ikisi de butun semtleri ACAR (`SemtProblari.Kur`,
            // `GIKatilimi.Kat`) ve bu isin amaci tam olarak onu
            // yapmamak. Ikisi de daha once kostu ve sonuclari
            // sahnelere KAYDEDILDI; bosta kostuklarinda zaten "0"
            // diyorlar. Burada yalniz dogrulanir.
            if (System.Array.IndexOf(
                    System.Environment.GetCommandLineArgs(),
                    "-hezarfenTemizle") >= 0)
                Debug.Log("[Hezarfen] Eski firin verisi: "
                          + SemtProblari.PismisVeriyiSil());

            var _pv = Object.FindAnyObjectByType<ProbeVolume>();
            if (_pv == null)
            {
                Debug.LogError($"[Hezarfen] {semt} icinde prob hacmi yok"
                               + " — once 'Semt problarini kur'.");
                EditorApplication.Exit(1);
                return;
            }
            AydinlatmaAyari();
            Debug.Log("[Hezarfen] " + GokAyari());
            EditorSceneManager.SaveOpenScenes();

            // SANAL KAYDIRMA HER KOSUMDA KAPATILIR.
            //
            // Gerekcesi `SemtProblari.SanalKaydirmayiKapat`ta: o GPU
            // gecisi D_Bogaz kosumunu d3d12 aygit hatasiyla oldurdu.
            // Kurulum isini burada tekrar etmek bedava ve kurulum
            // kosulmadan pisirmeye girilirse kosum yine olmez.
            Debug.Log("[Hezarfen] Sanal kaydirma: "
                      + (SemtProblari.SanalKaydirmayiKapat()
                         ? "kapali" : "BULUNAMADI"));

            // OLCUM ANAHTARLARI.
            //
            // `-hezarfenAralik <m>`: prob araligini kumeye yazar. Sayinin
            // sahibi `SemtProblari.ProbAraligi`; bu anahtar yalniz
            // "hangi aralik 67 milyon prob sinirinin altina siger"
            // sorusunu denemek icin var.
            //
            // `-hezarfenYerlesimDene`: yerlestirme gecer gecmez pisirmeyi
            // iptal edip cikar. Bir deneme boyle birkac dakika surer,
            // saatler degil.
            string _aralik = KomutSatiri("-hezarfenAralik");
            if (!string.IsNullOrEmpty(_aralik)
                && float.TryParse(_aralik,
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out float _ar))
            {
                SemtProblari.AraligiYaz(_ar);
                Debug.Log($"[Hezarfen] Prob araligi DENEME icin {_ar:0.##} m.");
            }
            _yerlesimDenemesi = System.Array.IndexOf(
                System.Environment.GetCommandLineArgs(),
                "-hezarfenYerlesimDene") >= 0;

            if (!KismiPisirmeyiAyarla(new[] { ProbeSahnesi, yol }))
            {
                Debug.LogError("[Hezarfen] Kismi pisirme alani bulunamadi "
                               + "(AdaptiveProbeVolumes.partialBakeSceneList). "
                               + "Paket surumu degismis olabilir; tam "
                               + "pisirme bu makinede sigmiyor, o yuzden "
                               + "devam edilmiyor.");
                EditorApplication.Exit(4);
                return;
            }

            _imzaOnce = SemtProblari.PismisVeriImzasi();
            Debug.Log($"[Hezarfen] APV pisirme basladi — YALNIZ {semt}. "
                      + $"Onceki firin: {_imzaOnce}");
            _pisirmeBasi = System.DateTime.UtcNow;
            _sonIlerleme = 0f;
            _ilerlemeAni = 0.0;
            _pisirmeGoruldu = false;
            // YEDEK YOL BURADA KAPALI.
            //
            // `PisirmeyiBekle` 20 saniyede baslamayan bir APV cagrisi
            // gorunce klasik `Lightmapping.BakeAsync`e dusuyor — ve o
            // cagri KISMI listeyi tanimaz, butun sehri pisirmeye
            // kalkar. Yani yedek yol, bu islevin varlik sebebini
            // ortadan kaldirirdi.
            _yedekDenendi = true;
            _sonBildirim = 0.0;
            // KLASIK CAGRI — VE BU KISMI PISIRMEYI BOZMAZ.
            //
            // Ilk yazimda `AdaptiveProbeVolumes.BakeAsync()` vardi ve
            // olculdu: 120 saniye boyunca `Lightmapping.isRunning`
            // false kaldi, pisirme HIC baslamadi. Bu depoda zaten
            // yazili bir gercek — o cagri toplu kipte baslamiyor;
            // sehrin tamamini pisiren yol da ancak klasik cagriya
            // duserek calisiyordu.
            //
            // `Lightmapping.BakeAsync()` AYNI APV yolundan geciyor
            // (yigin izi: Lightmapping.BakeAsync -> OnBakeStarted ->
            // PrepareBaking) ve `partialBakeSceneList` orada okunuyor.
            // Yani klasik cagri kismi listeyi tanir; tam pisirmeye
            // donmez.
            // YERLESTIRME HATASI **ESZAMANLI** DUSER.
            //
            // Yigin izi bunu soyluyor: `Lightmapping.BakeAsync` ->
            // `Internal_CallBakeStartedFunctions` -> `OnBakeStarted` ->
            // `PrepareBaking` -> `DoProbePlacement`. Yani prob
            // yerlestirme, cagri daha DONMEDEN kosuyor; bir sinir
            // asilirsa hata tam burada, asenkron pisirme hic
            // baslamadan dusuyor.
            //
            // Bunu duymadigimiz icin `D_Okmeydani` 11,7 dakika bos
            // dondu ve kosum "pisti ve kaydedildi" dedi.
            _apvHatasi = null;
            Application.logMessageReceived += ApvKaydiniDinle;
            Lightmapping.BakeAsync();
            if (_apvHatasi != null)
            {
                Application.logMessageReceived -= ApvKaydiniDinle;
                Debug.LogError("[Hezarfen] APV YERLESTIRME DUSTU — "
                               + "pisirme hic baslamadi:\n" + _apvHatasi);
                Lightmapping.Cancel();
                EditorApplication.Exit(6);
                return;
            }
            if (_yerlesimDenemesi)
            {
                Application.logMessageReceived -= ApvKaydiniDinle;
                Lightmapping.Cancel();
                // YERLESIM GECTIYSE IZGARA DONDURULUR.
                //
                // Deneme kipi zaten butun semtler yukluyken kosuyor,
                // yani bu, izgaranin sehrin TAMAMINI kapsadigi tek an.
                // Burada dondurulmazsa sonraki her kismi pisirme kendi
                // izgarasini uretir ve sonucu atilir.
                bool d = SemtProblari.YerlesimiDondur(true);
                Debug.Log($"[Hezarfen] YERLESIM GECTI ({semt}). Izgara "
                          + (d ? "DONDURULDU" : "DONDURULAMADI")
                          + " — deneme kipinde pisirme iptal edildi.");
                EditorApplication.Exit(d ? 0 : 8);
                return;
            }
            EditorApplication.update += PisirmeyiBekle;
        }

        /// <summary>
        /// APV'nin kendi hata kaydını dinler.
        ///
        /// Neden kaydı dinliyoruz: prob yerleştirmenin başarısız
        /// olduğunu söyleyen tek yer o. <c>Lightmapping</c> bir dönüş
        /// değeri vermiyor, <c>isRunning</c> yine de bir süre true
        /// oluyor ve fırın "bitti" diyor — üretimi sıfır olsa bile.
        /// </summary>
        private static void ApvKaydiniDinle(string mesaj, string iz,
                                            LogType tur)
        {
            if (tur != LogType.Error && tur != LogType.Exception) return;
            if (_apvHatasi != null) return;
            if (mesaj.IndexOf("Adaptive Probe Volume",
                    System.StringComparison.OrdinalIgnoreCase) < 0
                && mesaj.IndexOf("APV",
                    System.StringComparison.Ordinal) < 0)
                return;
            _apvHatasi = mesaj;
        }

        private static string _apvHatasi;
        private static string _imzaOnce;
        private static bool _yerlesimDenemesi;

        private static string KomutSatiri(string anahtar)
        {
            var a = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < a.Length - 1; i++)
                if (a[i] == anahtar) return a[i + 1];
            return null;
        }

        /// <summary>
        /// <c>partialBakeSceneList</c>'i verilen sahnelerle doldurur.
        /// Alan <c>internal</c> olduğu için yansıma; bulunamazsa
        /// <c>false</c> döner ve çağıran durur.
        /// </summary>
        private static bool KismiPisirmeyiAyarla(string[] sahneler)
        {
            var t = typeof(AdaptiveProbeVolumes);
            var f = t.GetField("partialBakeSceneList",
                System.Reflection.BindingFlags.NonPublic
                | System.Reflection.BindingFlags.Static);
            if (f == null) return false;

            var kume = new System.Collections.Generic.HashSet<string>();
            foreach (string y in sahneler)
            {
                string g = AssetDatabase.AssetPathToGUID(y);
                if (!string.IsNullOrEmpty(g)) kume.Add(g);
            }
            f.SetValue(null, kume);
            return true;
        }

        // FIRINDA ISIK YOK — VE BU BILINCLI.
        //
        // Pisirme kaydi bunu tek satirda soyluyor:
        //
        //   Extracted OOTS snapshot with 11260 instances, 404 geometries,
        //   28 materials, **0 lights**
        //
        // Sahnedeki iki yonlu isik da `m_Lightmapping: 4` — Realtime
        // Only — ve oyle KALMALI: gunesi `ZamanSistemi.GunesiYerlestir`
        // saate gore donduruyor. Sabit bir gunesi pisirmek, gunun her
        // saatinde yanlis yonden gelen bir sicrama demek olurdu.
        //
        // Yani APV burada GOKYUZU sicramasini tasir, gunesinkini degil;
        // gunesin sicramasi ekran uzayindan (SSGI) gelir. Golgenin
        // rengi olcusu de tam bunu olcuyor: gok mavidir, gokten
        // sicrayan isik alan bir golge mavi/kirmizi oraninda yukselir.
        //
        // Bu not, ileride birinin "firinda isik yok" diye gunesi
        // Baked/Mixed yapmasini engellemek icin burada: o degisiklik
        // gun dongusunu bozar.

        private const string ProbeSahnesi =
            "Assets/_Project/Scenes/Faz1_Terrain.unity";

        private static System.DateTime _pisirmeBasi;
        private static double _sonBildirim;

        /// <summary>
        /// Pişirmenin gerçekten <b>başladığı görüldü mü</b>.
        ///
        /// İlk yazımda beklemek yalnızca <c>Lightmapping.isRunning</c>'e
        /// bakıyordu ve o, çağrıdan hemen sonraki karede hâlâ
        /// <c>false</c>: koşum "APV pişti ve kaydedildi (0,0 dk)" yazıp
        /// çıktı, diskteki dosyalar 31 Ağustos tarihiyle olduğu gibi
        /// kaldı. Yani araç işini yapmadığını değil, YAPTIĞINI bildirdi.
        ///
        /// Bir beklemenin, beklediği şeyin başladığını görmeden bittiğine
        /// karar vermesi bekleme değildir.
        /// </summary>
        /// <summary>
        /// Pişirmenin bekleneceği en uzun süre (sn).
        ///
        /// 60 dakikaydı ve o sayı, şehrin GI'ya <b>katılmadığı</b>
        /// zamandan kalmaydı: fırın hiçbir şey bulamadığı için 1,7
        /// dakikada bitiyordu. 105.192 çizici katılınca ölçüldü —
        /// pişirme 35 dakikayı geçti ve sürüyordu. Bir sınır, ölçülen
        /// işten kısa olduğunda koruma değil kayıp üretir: iptal eder ve
        /// bir saatlik işi çöpe atar.
        /// </summary>
        /// <summary>
        /// 3 saatti ve o sayı da ölçümün gerisinde kaldı: <c>D_Galata</c>
        /// on iki dakikada %19,8'de ve ilerleme dakikada %0,1. Bir üst
        /// sınır, ölçülen işten kısa olduğunda koruma değil <b>kayıp</b>
        /// üretir — üç saatlik bir fırını iptal edip hiçbir şey
        /// yazmadan çıkar.
        ///
        /// Asıl koruma artık süre değil <see cref="TakilmaSiniri"/>:
        /// ilerleme durursa dakikalar içinde biliniyor. Süre sınırı
        /// yalnız son çare.
        /// </summary>
        private const double EnCokPisirme = 6.0 * 3600.0;

        /// <summary>
        /// İlerleme bu kadar süre kıpırdamazsa pişirme <b>takılmış</b>
        /// sayılır (sn).
        ///
        /// 25 dakika: ölçülen fırında iki ilerleme adımı arasındaki en
        /// uzun boşluk birkaç dakikaydı; 25 onun katı ve üç saatlik
        /// süre sınırının onda biri. Amaç yavaş bir fırını kesmek
        /// değil, <b>ölmüş</b> bir fırını saatler sonra değil dakikalar
        /// sonra bildirmek.
        /// </summary>
        private const double TakilmaSiniri = 25.0 * 60.0;

        /// <summary>
        /// <b>İlk</b> ilerleme adımı için tanınan süre (sn).
        ///
        /// Takılma sınırı 25 dakikaydı ve o sayı, pişirmenin hemen
        /// ilerlemeye başladığı bir kuruluma göre seçilmişti. Kısmi
        /// pişirme <b>sekiz semt yüklüyken</b> koşuyor ve prob
        /// yerleştirme bütün şehir için yapılıyor: o aşama boyunca
        /// <c>buildProgress</c> sıfırda durur ve hiçbir şey bozuk
        /// değildir. 25 dakikalık sınır burada sağlıklı bir fırını
        /// keserdi.
        ///
        /// Yani sayı değil, <b>ne zaman saydığı</b> yanlıştı: ilk
        /// adıma kadar bekleme uzun, ondan sonra kısa. Üst sınır yine
        /// <see cref="EnCokPisirme"/>.
        /// </summary>
        private const double IlkIlerlemeSiniri = 75.0 * 60.0;

        private static float _sonIlerleme;
        private static double _ilerlemeAni;

        private static bool _pisirmeGoruldu;
        private static bool _yedekDenendi;

        private static void PisirmeyiBekle()
        {
            double gecen = (System.DateTime.UtcNow - _pisirmeBasi)
                .TotalSeconds;
            // ILERLIYOR MU — SURE DEGIL, ILERLEME OLCULUR.
            //
            // Bekleyici yalnizca gecen dakikayi yaziyordu ve o sayi bir
            // sey soylemiyor: 90 dakika "ilerliyor" ile 90 dakika
            // "takildi" ayni satiri uretiyordu. Bir turda tam bunu
            // yasadik — firin uc saat kostu ve durumu hakkinda tek
            // bilgi gecen zamandi.
            //
            // `Lightmapping.buildProgress` isin oranini veriyor. Ondan
            // hem YUZDE hem VARIS TAHMINI cikar; ikisi de gecen
            // dakikadan fazlasini soyler.
            // ILERLEME OKUMASI SACMALAYABILIR — VE OLCULDU.
            //
            // `Lightmapping.buildProgress` ikinci evrede
            // **%44.366.093,8** dondu. Bir oran degil, coplukten okunan
            // bir sayi. Bunun bedeli tek satirda: takilma denetimi
            // "ilerledi" sanip esigi oraya tasir, sonra hicbir gercek
            // ilerleme o sayiyi gecemez ve SAGLIKLI bir firin takilmis
            // sayilarak kesilir.
            //
            // Aralik disi okuma ilerleme degil BILGI YOKLUGUDUR: eldeki
            // son gecerli deger korunur, sayac ilerletilmez.
            float _ham = Lightmapping.buildProgress;
            bool ilerlemeGecerli = _ham >= 0f && _ham <= 1f
                                   && !float.IsNaN(_ham);
            float ilerleme = ilerlemeGecerli ? _ham : _sonIlerleme;
            if (gecen - _sonBildirim > 60.0)
            {
                _sonBildirim = gecen;
                string tahmin = "?";
                if (ilerleme > 0.01f)
                {
                    double toplam = gecen / ilerleme;
                    tahmin = $"{(toplam - gecen) / 60.0:0} dk";
                }
                Debug.Log($"[Hezarfen] APV pisiyor... {gecen / 60.0:0.0} dk, "
                          + $"%{ilerleme * 100.0:0.0}"
                          + (ilerlemeGecerli ? "" : " (okuma gecersiz)")
                          + $", kalan ~{tahmin}");
            }

            // TAKILMA DENETIMI: ilerleme durursa bekleme durur.
            //
            // Ilerleme olcusu olmadan "takildi" ile "yavas" ayirt
            // edilemiyordu ve tek koruma sure siniriydi — yani bir
            // firinin oldugunu ancak SAATLER sonra ogreniyorduk.
            // Esik %0,1: gercek bir ilerleme bundan buyuk adimlar atar,
            // olcum gurultusu atmaz.
            // ILERLEME **DEGISTI** MI — ARTTI MI DEGIL.
            //
            // Once `> _sonIlerleme + 0,001` yaziyordu, yani en yuksek
            // degeri tutuyordu. Olculdu: `buildProgress` EVRE BASINA
            // sifirlaniyor — D_Galata 95. dakikada %45,1 idi, 100.
            // dakikada %0,1. Ikinci evre boyunca hicbir okuma 0,451'i
            // gecemez, yani zamanlayici son ARTIS aninda donmus kalir
            // ve saglikli bir firin yirmi bes dakika sonra "takildi"
            // diye kesilir.
            //
            // Takilmis bir firin hic kimildamaz; calisan bir firin
            // ister ilerler ister yeni bir evreye doner. Olcut bu
            // yuzden DEGISIM.
            if (ilerlemeGecerli
                && System.Math.Abs(ilerleme - _sonIlerleme) > 0.001f)
            {
                _sonIlerleme = ilerleme;
                _ilerlemeAni = gecen;
            }
            else if (_pisirmeGoruldu
                     && gecen - _ilerlemeAni > (_sonIlerleme > 0f
                         ? TakilmaSiniri : IlkIlerlemeSiniri))
            {
                Debug.LogError("[Hezarfen] APV pisirme TAKILDI: "
                               + $"%{ilerleme * 100.0:0.0} oraninda "
                               + $"{(gecen - _ilerlemeAni) / 60.0:0} dk boyunca "
                               + "hic ilerlemedi. Bekleme sonlandirildi.");
                EditorApplication.update -= PisirmeyiBekle;
                Lightmapping.Cancel();
                EditorApplication.Exit(5);
                return;
            }
            if (Lightmapping.isRunning) _pisirmeGoruldu = true;

            // APV'NIN KENDI CAGRISI BASLAMAZSA KLASIK PISIRME DENENIR.
            //
            // Olculdu: `AdaptiveProbeVolumes.BakeAsync()` toplu kipte
            // `Lightmapping.isRunning`i hic true yapmiyor ve diskteki
            // firin degismiyor. Ikinci yol, ayni isi kuyruga sokan eski
            // cagri; APV hacimleri sahnede oldugu icin o da problari
            // pisirir.
            if (!_pisirmeGoruldu && !_yedekDenendi && gecen > 20.0)
            {
                _yedekDenendi = true;
                Debug.Log("[Hezarfen] APV cagrisi baslamadi — "
                          + "Lightmapping.BakeAsync deneniyor.");
                Lightmapping.BakeAsync();
            }

            // Baslamasi icin makul sure taniyoruz: is kuyruga giriyor.
            if (!_pisirmeGoruldu && gecen < 120.0) return;
            if (_pisirmeGoruldu && Lightmapping.isRunning
                && gecen < EnCokPisirme)
                return;

            EditorApplication.update -= PisirmeyiBekle;
            if (!_pisirmeGoruldu)
            {
                Debug.LogError("[Hezarfen] APV pisirme HIC BASLAMADI "
                               + "(120 sn boyunca Lightmapping.isRunning "
                               + "false). Diskteki fırın eski kaldi.");
                EditorApplication.Exit(3);
                return;
            }
            if (gecen >= EnCokPisirme)
            {
                Debug.LogError("[Hezarfen] APV pisirme "
                               + $"{EnCokPisirme / 60.0:0} dk'da bitmedi.");
                Lightmapping.Cancel();
                EditorApplication.Exit(2);
                return;
            }
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();

            // URUN DENETIMI: "bitti" degil, "ne yazdi".
            //
            // Bu kapinin gerekcesi olculdu. D_Okmeydani 11,7 dakika
            // pisti, kosum basarili dondu ve kumede `m_Values: []`
            // vardi — sifir hucre. Bir isin bittigini gormek, urununu
            // gormek degildir.
            string imzaSonra = SemtProblari.PismisVeriImzasi();
            if (SemtProblari.HucreSayisi() <= 0 || imzaSonra == _imzaOnce)
            {
                Debug.LogError("[Hezarfen] APV pisirme bitti ama DISKTEKI "
                               + "FIRIN DEGISMEDI.\n"
                               + $"  once : {_imzaOnce}\n"
                               + $"  sonra: {imzaSonra}\n"
                               + "Kaydin ustunde 'incompatible cell "
                               + "layout' olabilir.");
                EditorApplication.Exit(7);
                return;
            }
            Debug.Log($"[Hezarfen] APV pisti ve kaydedildi "
                      + $"({gecen / 60.0:0.0} dk). Firin: {imzaSonra}");
            EditorApplication.Exit(0);
        }

    }
}
