using Hezarfen.Editor.Diagnostics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Hezarfen.Editor.Lighting
{
    /// <summary>
    /// **GEÇİCİ** aydınlatma takımı — Faz 5'in ışık pasına kadar.
    ///
    /// ## Neden gerekli
    ///
    /// Ölçüldü: gölgedeki sıva duvar ~30/255, kaldırım 3/255. Sonuç yalnız
    /// karanlık değil <b>okunmaz</b>dı — yaya seviyesinden inceleme paketi
    /// üretilemiyordu ve bu, üç turdur açık duran tek boşluktu (ADR 0019 §11,
    /// 0021 §7, 0022 §7).
    ///
    /// ## Sebep "ışık yok" DEĞİL
    ///
    /// Sahnede fizik tabanlı gökyüzü var ve <c>skyAmbientMode = Dynamic</c>:
    /// gök ışığı geliyor. Eksik olan <b>sıçrama</b> terimi. HDRP'de gerçek
    /// zamanlı küresel aydınlatma yoktur; ışık pişirilmediği sürece bir duvar
    /// yalnızca güneşi ve göğü görür — karşı duvardan ve yerden dönen ışığı
    /// görmez. Dar bir Osmanlı sokağında ise gölgeyi asıl dolduran şey odur:
    /// kireç badanalı cepheler ve taş kaldırım çok iyi yansıtır.
    ///
    /// ## Bu takım neyi taklit ediyor
    ///
    /// Eksik <b>sıçrama</b> terimini, elde olan iki terimi büyüterek:
    ///   * <see cref="IndirectLightingController"/> ile gök teriminin çarpanı,
    ///   * iki adet <b>gölgesiz</b> dolgu ışığı — biri güneşin karşısından
    ///     (gök sarması), biri <b>aşağıdan yukarı</b> (yer sıçraması).
    ///
    /// İkisi de fizikî değil; bu yüzden ayrı bir kök nesnede ve ayrı bir
    /// Volume'da yaşıyorlar. Kaldırmak tek menü komutu: kalıcı ışık pası
    /// başladığında bu takım <b>silinir</b>, üstüne kurulmaz. Geçici olduğunu
    /// yorumda söylemek yetmez — yapının kendisi söylemeli.
    /// </summary>
    public static class InterimLighting
    {
        public const string RigName = "GECICI_Aydinlatma";
        private const string ProfileDir = "Assets/_Project/Settings";
        private const string ProfilePath = ProfileDir + "/VP_Gecici_Aydinlatma.asset";

        /// <summary>Gök sarması: güneşin KARŞI azimutundan, soğuk, gölgesiz.</summary>
        private const float FillLux = 11000f;

        /// <summary>Yer sıçraması: AŞAĞIDAN yukarı, sıcak ve tozlu, gölgesiz.</summary>
        private const float BounceLux = 6500f;

        /// <summary>Gök teriminin çarpanı. 1 = dokunma. Ölçülerek seçildi.</summary>
        private const float IndirectMultiplier = 2.4f;

        /// <summary>
        /// Geçici poz (EV100). Kalıcı profil 14,5 diyor — yaz öğle değeri.
        /// Sahnenin güneşi 42°'de ve sonuç ÖLÇÜLDÜ: güneşli zemin bile 90/255
        /// kalıyordu, yani sahne toptan az pozlanmıştı. Değer burada, geçici
        /// Volume'da duruyor; takım kaldırılınca kalıcı profil geri gelir.
        ///
        /// 13,0 <b>süpürülerek</b> seçildi (sokak koridoru / kuşbakışı kare):
        /// <code>
        ///  EV    sokak ort  okunmaz%   kusbakisi p99  patlak%
        ///  14,5     30,8      48,2            —         —
        ///  13,5     54,8      30,9            —         —
        ///  13,2     66  (yak.) 24 (yak.)     179       0,00
        ///  13,0     70,6      20,9            —         —
        ///  12,5     88,7      17,9           206       0,00
        ///  12,0    108,5       6,5            —         —
        /// </code>
        /// Ölçüt gözle "güzel" değil, işe yararlık: yaya seviyesinden inceleme
        /// paketi üretilebilsin (okunmaz &lt; %25) ve hiçbir şey patlamasın
        /// (&gt;250 oranı ~%0). 12,5 ve altı sahneyi kapalı hava gibi düzleştirdi.
        /// </summary>
        public const float DefaultExposureEV = 13.0f;
        public static float ExposureEV = DefaultExposureEV;

        /// <summary>
        /// Pozu değiştirir <b>ve diske yazar</b> — inceleme paketi için.
        ///
        /// Poz geçici Volume'un profilinde yaşıyor ve o profil bir DOSYA. Alanı
        /// değiştirip profili yeniden yazmayan bir çağrı hiçbir şey yapmaz;
        /// bu ikisi ayrı durduğu sürece bir gün ayrılırlar.
        ///
        /// Çağıran, işi bitince <see cref="DefaultExposureEV"/>'ye <b>geri
        /// dönmek zorundadır</b>: değer diskte kalır ve sonraki ölçüm
        /// (LightingTests) sessizce başka bir pozda çalışırdı.
        /// </summary>
        public static void ApplyExposure(float ev)
        {
            ExposureEV = ev;
            EnsureProfile();
        }

        [MenuItem("Hezarfen/Aydinlatma/Gecici aydinlatmayi kur")]
        public static void InstallMenu()
        {
            int n = Install(out string report);
            Debug.Log($"[Hezarfen] Gecici aydinlatma kuruldu ({n} parca). {report}\n"
                      + "GECICIDIR — kalici isik pasinda 'Gecici aydinlatmayi kaldir'.");
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        [MenuItem("Hezarfen/Aydinlatma/Gecici aydinlatmayi kaldir")]
        public static void UninstallMenu()
        {
            bool had = Uninstall();
            Debug.Log(had ? "[Hezarfen] Gecici aydinlatma kaldirildi."
                          : "[Hezarfen] Gecici aydinlatma zaten yoktu.");
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        public static bool Uninstall()
        {
            var old = GameObject.Find(RigName);
            if (old == null) return false;
            Object.DestroyImmediate(old);
            return true;
        }

        /// <summary>
        /// Dolgu ışıklarının kalibre edildiği güneş yüksekliği (ADR 0025 anı).
        /// Ölçek bunun üstüne kurulur.
        /// </summary>
        public const float CalibratedSunAltitudeDeg = 43.2f;

        /// <summary>
        /// Güneş yüksekliğinden dolgu ölçeği.
        ///
        /// İki dolgu ışığı da <b>gökten</b> gelen terimi taklit ediyor ve gök
        /// aydınlığı güneş alçaldıkça düşer — kabaca <c>sin(yükseklik)</c> ile.
        /// Sabit bırakılsaydı gün batımı karesi "yanlış yönden gelen bir öğle
        /// ışığı" olurdu: güneş ufukta, gölgeler uzun, ama gölgelerin içi
        /// öğle kadar dolu. Kadraj doğru, ışık yalan.
        ///
        /// Alt sınır 0,12: sıçrama terimi sıfıra inmez, çünkü gerçekte de
        /// inmiyor (alacakaranlıkta gök hâlâ mavi ışık verir) ve sıfır dolgu
        /// yaya seviyesini yeniden okunmaz yapardı.
        /// </summary>
        public static float FillScaleForAltitude(float altDeg) =>
            Mathf.Clamp(Mathf.Sin(altDeg * Mathf.Deg2Rad)
                        / Mathf.Sin(CalibratedSunAltitudeDeg * Mathf.Deg2Rad),
                        0.12f, 1.6f);

        public static int Install(out string report) => Install(out report, 1f);

        public static int Install(out string report, float fillScale)
        {
            Uninstall();

            var sun = FindSun();
            if (sun == null)
            {
                report = "GUNES YOK — dolgu yonu hesaplanamadi.";
                return 0;
            }

            var root = new GameObject(RigName);
            int n = 0;

            // Dolgu yonu güneşten TÜRETİLİR, elle yazılmaz. Güneş dönerse
            // dolgu da döner; iki yerde tutulan bir açı bir gün ayrışır.
            float sunAzimuth = sun.transform.eulerAngles.y;

            // 1) GÖK SARMASI — güneşin karşısından, alçak açıyla, soğuk.
            //    Gölgede kalan cepheyi dolduran ana terim budur.
            n += MakeFill(root.transform, "FILL_Gok",
                          new Vector3(28f, sunAzimuth + 180f, 0f),
                          new Color(0.62f, 0.72f, 0.92f), FillLux * fillScale);

            // 2) YER SIÇRAMASI — AŞAĞIDAN yukarı, sıcak ve tozlu.
            //    Saçak altını, cumba altını ve kemer içini açan terim.
            //    Yönü negatif eğim: ışık yukarı doğru yol alır.
            n += MakeFill(root.transform, "FILL_Sicrama",
                          new Vector3(-32f, sunAzimuth - 40f, 0f),
                          new Color(0.94f, 0.84f, 0.68f), BounceLux * fillScale);

            // 3) Gök teriminin çarpanı — kendi Volume'unda, YÜKSEK öncelikte.
            //    Ana profile dokunmuyoruz: kalıcı pas o profille çalışacak ve
            //    geçici değerler oraya sızmamalı.
            var vol = new GameObject("GECICI_Volume").AddComponent<Volume>();
            vol.transform.SetParent(root.transform, false);
            vol.isGlobal = true;
            vol.priority = 100f;
            vol.sharedProfile = EnsureProfile();
            n++;

            report = $"gok dolgu {FillLux * fillScale:F0} lx, "
                   + $"sicrama {BounceLux * fillScale:F0} lx (olcek {fillScale:F2}x), "
                   + $"dolayli carpan {IndirectMultiplier:F1}x, poz {ExposureEV:F1} EV";
            return n;
        }

        private static int MakeFill(Transform parent, string name, Vector3 euler,
                                    Color color, float lux)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.rotation = Quaternion.Euler(euler);

            var l = go.AddComponent<Light>();
            l.type = LightType.Directional;
            l.color = color;
            // GÖLGESİZ olmak zorunda: ikinci bir gölge kaynağı, güneşin
            // gölgeleriyle çakışır ve sahne iki güneşli görünür. Dolgu ışığı
            // bir ışık kaynağı değil, eksik terimin yerine konan bir sayıdır.
            l.shadows = LightShadows.None;

            var hd = go.AddComponent<HDAdditionalLightData>();
            // `hd.SetIntensity(lux, LightUnit.Lux)` 2023.3'ten beri kullanımdan
            // kalkmış. Yeni yol birimi ve şiddeti Light'ın kendisine yazmak;
            // SIRA ÖNEMLİ — birim önce, yoksa şiddet eski birimle yorumlanır.
            l.lightUnit = LightUnit.Lux;
            l.intensity = lux;
            // Parlamayı da taklit etmiyoruz: sahte bir kaynağın yansıması
            // metal ve camda hemen yakalanır (kurşun kubbeler — ADR 0021 §1).
            hd.affectSpecular = false;
            hd.affectsVolumetric = false;
            return 1;
        }

        private static VolumeProfile EnsureProfile()
        {
            var prof = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            if (prof == null)
            {
                if (!AssetDatabase.IsValidFolder(ProfileDir))
                    AssetDatabase.CreateFolder("Assets/_Project", "Settings");
                prof = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(prof, ProfilePath);
            }

            var ind = Ensure<IndirectLightingController>(prof);
            ind.indirectDiffuseLightingMultiplier.overrideState = true;
            ind.indirectDiffuseLightingMultiplier.value = IndirectMultiplier;
            ind.reflectionLightingMultiplier.overrideState = true;
            ind.reflectionLightingMultiplier.value = 1f;

            var exp = Ensure<Exposure>(prof);
            exp.mode.overrideState = true;
            exp.mode.value = ExposureMode.Fixed;
            exp.fixedExposure.overrideState = true;
            exp.fixedExposure.value = ExposureEV;

            EditorUtility.SetDirty(prof);
            AssetDatabase.SaveAssets();
            return prof;
        }

        /// <summary>
        /// Volume bileşenini profile ekler <b>ve diske yazar</b>.
        ///
        /// `VolumeProfile.Add&lt;T&gt;()` bileşeni yalnızca bellekte kurar.
        /// Kalıcı olması için <see cref="AssetDatabase.AddObjectToAsset"/> ile
        /// profilin ALT VARLIĞI yapılması şart — yoksa profil diskte
        /// <b>bomboş</b> kaydedilir.
        ///
        /// Bu bir kez sessizce yaşandı ve tam tarif ettiği gibi göründü:
        /// aynı oturumda her şey çalışıyordu (bileşenler bellekte duruyordu),
        /// sahne kapatılıp açılınca poz ve dolaylı çarpan yok oluyor, geriye
        /// yalnız dolgu ışıkları kalıyordu. Sahne "aydınlatılmış" görünüyordu
        /// çünkü ışıklar vardı; eksik olanı yalnızca ölçüm gösterdi
        /// (73,2/255 yerine 18,8/255).
        /// </summary>
        private static T Ensure<T>(VolumeProfile prof) where T : VolumeComponent
        {
            if (prof.TryGet(out T existing) && existing != null) return existing;

            var comp = prof.Add<T>(true);
            comp.hideFlags = HideFlags.HideInHierarchy;
            if (AssetDatabase.Contains(prof))
                AssetDatabase.AddObjectToAsset(comp, prof);
            return comp;
        }

        private static Light FindSun()
        {
            foreach (var l in Object.FindObjectsByType<Light>())
                if (l.type == LightType.Directional && l.shadows != LightShadows.None
                    && l.transform.parent == null)
                    return l;
            foreach (var l in Object.FindObjectsByType<Light>())
                if (l.type == LightType.Directional) return l;
            return null;
        }

        // ------------------------------------------------------------- ölçüm

        /// <summary>
        /// Yaya seviyesinden bir kare render eder ve <b>sayı</b> döndürür.
        ///
        /// Neden menüde bir ölçü var: üç turdur "sokak karanlık" diye
        /// yazıyorum ve her seferinde ekran görüntüsüne bakarak karar
        /// veriyorum. Render bir gözlemdir, kanıt değil (CLAUDE.md). Bu komut
        /// aynı bakış açısından aynı sayıları üretir; öncesi ve sonrası
        /// karşılaştırılabilir.
        ///
        /// Bakış açısı SAHNEDEN türetilir: mahalle çekirdeğinin 14 m önünde,
        /// göz hizasında (1,70 m). Elle yazılmış bir koordinat, sahne yeniden
        /// kurulduğunda sessizce başka bir yere bakardı.
        /// </summary>
        [MenuItem("Hezarfen/Aydinlatma/Sokak parlakligini olc")]
        public static void MeasureMenu()
        {
            string s = Measure(out _);
            Debug.Log("[Hezarfen] " + s);
        }

        public static string Measure(out float darkFraction)
        {
            darkFraction = 1f;

            // ÖLÇÜLEN ŞEY: GÖLGEDEKİ BİR EV CEPHESİ.
            //
            // İki kez yanlış şeyi ölçtüm. Önce çekirdeğin 14 m önünü aldım ve
            // orası avlu duvarının dibine düştü — kare 2 m ötedeki bir duvarla
            // doluydu. Sonra sokak koridoruna baktım ve karenin yarısını
            // yamacın çıplak arazisi kapladı: sayı mimariyi değil araziyi
            // ölçüyordu ve arazinin karanlığı bu turun sorunu değil.
            //
            // Gereklilik neyse ölçü o olmalı: *gölgede kalan bir cephenin
            // dokusu okunuyor mu.* Kadraj o cepheyle dolar.
            var street = GameObject.Find("Sokak_Ana");
            if (street == null)
                foreach (var t in Object.FindObjectsByType<Transform>())
                    if (t.name == "Sokak_Ana") { street = t.gameObject; break; }
            if (street == null) return "Olcum YAPILAMADI: Sokak_Ana yok.";

            var sun = FindSun();
            if (sun == null) return "Olcum YAPILAMADI: gunes yok.";
            Vector3 sunDir = sun.transform.forward;     // isigin YOL ALDIGI yon

            // Kardes sirasi BELIRLEYICIDIR; ada gore siralama degildi. Evlerin
            // cogu ayni prefab adini tasiyor, `List.Sort` esitlikte kararsiz ve
            // ayni sahne iki kosumda iki farkli eve bakiyordu.
            Transform target = null;
            foreach (Transform t in street.transform)
            {
                if (t.GetComponent<LODGroup>() == null) continue;
                // Cephe gunesten YUZ CEVIRMIS olmali: isik yonuyle ayni yone
                // bakan yuzey golgede kalir.
                if (Vector3.Dot(t.forward, sunDir) > 0.25f) { target = t; break; }
            }
            if (target == null) return "Olcum YAPILAMADI: golgede cephe bulunamadi.";

            // Göz BASILAN YÜZEYİN üstünde, evin tabanının değil: ev taş bir
            // kaidenin üstünde durur ve bir kez ölçüm gözü yerden 3,03 m
            // yukarı çıkarmıştı — yaya seviyesi değil birinci kat hizası
            // ölçülüyordu.
            //
            // Arazi de doğru zemin DEĞİL: yaya kaldırıma basar ve kaldırım
            // yamaçta arazinin metrelerce üstündedir. Mahalle paketinde tam
            // bu yüzden kareler taşın ALTINDA çıktı; ölçü de aynı hatayı
            // taşıyordu, aynı aletle düzeltildi.
            Vector3 eye = FrameMetric.OnSurface(target.position + target.forward * 8.0f)
                        + Vector3.up * 1.70f;

            // Kareyi alan ve ölçen kod ORTAKTIR (FrameMetric): aynı ölçüyü
            // arazi örtüsü de kullanıyor. İkinci bir kopya, oradaki iki
            // tuzağın (Volume'ların kaydı, ısınma kareleri) da ikinci
            // kopyası olurdu.
            var st = FrameMetric.Capture(eye, target.position + Vector3.up * 3.0f,
                                         48f, "Captures/olcum_sokak.png");

            // Dışarıya AYRINTI ölçüsü döner; eşik de onun üstünde kurulur.
            // Parlaklık yüzdesi raporda kalır — bilgi olarak yararlı, ölçüt
            // olarak yanıltıcı.
            darkFraction = st.Detail;
            return $"Golgedeki cephe ({target.name}, 8 m, goz hizasi): {st}";
        }
    }
}
