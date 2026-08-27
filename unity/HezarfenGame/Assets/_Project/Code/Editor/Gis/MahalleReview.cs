using System.Collections.Generic;
using Hezarfen.Editor.Diagnostics;
using Hezarfen.Editor.Lighting;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Hezarfen.Editor.Gis
{
    /// <summary>
    /// Mahalle inceleme paketi — <b>Faz 2b'nin kabul ölçütü</b>.
    ///
    /// PLAN.md §7.1: *"Mescidi merkez alan bir mahalle: mescit + şadırvan +
    /// çeşme + birkaç dükkân + mezarlık … HDRP öğle ve gün batımı inceleme
    /// paketi."*
    ///
    /// ## Kadrajlar sahneden TÜRETİLİR
    ///
    /// Hiçbir koordinat elle yazılmadı; her kare sahnedeki bir nesneyi bulur
    /// ve ona göre kurulur. Sokak yerleştiricisi rastgele tohumla çalışıyor ve
    /// yeniden kurulduğunda her şey yer değiştiriyor — sabit koordinatlar
    /// sessizce boş yamaca bakardı. Okmeydanı paketinde aynı ders yazılıydı
    /// (<see cref="OkmeydaniReview"/>); burada baştan uygulandı.
    ///
    /// ## İki AN, aynı kadrajlar
    ///
    /// Kadrajlar bir kez kurulur, iki anda birden çekilir. Öğle ile gün
    /// batımını farklı kadrajlardan çekmek karşılaştırmayı imkânsız kılardı:
    /// bakılan şey ışığın kendisi, kadraj değil.
    ///
    /// "Gün batımı" bir saat değil bir <b>yükseklik</b>tir ve tarihten
    /// hesaplanır (<see cref="SunPlacement.AfternoonHourAtAltitude"/>).
    /// Ufkun tam dibi (0°) kare vermez — sahne kendi gölgesine iner; 6°
    /// hâlâ gün batımıdır ve mimarî okunur.
    ///
    /// ## Poz ÖLÇÜLEREK seçilir, yazılmaz
    ///
    /// Geçici aydınlatmanın 13,0 EV'si 43° yükseklikteki güneşe göre
    /// süpürülmüştü (ADR 0023). Öğle güneşi 64°'de, gün batımı 6°'de — aynı
    /// pozu ikisine de uygulamak birini patlatır ötekini karartır. Her an
    /// için poz, sokak koridoru karesi üstünde bir merdiven süpürülerek
    /// seçilir: <b>hiçbir şey patlamayacak</b> (>250 oranı ≤ %0,5) ve o
    /// kısıt altında <b>ayrıntı en yüksek</b> olacak. Fotoğrafçının pozometre
    /// ile yaptığı işin aynısı, ve seçilen değer log'a yazılır.
    /// </summary>
    public static class MahalleReview
    {
        private const string OutDir = "Captures/mahalle";

        /// <summary>Güneş saati; öğle = 12 (ADR 0025'in günü, 1 Mayıs).</summary>
        public const float NoonSolarHour = 12.0f;

        /// <summary>Gün batımı karesinin güneş yüksekliği — saat değil, açı.</summary>
        public const float LowSunAltitudeDeg = 6.0f;

        /// <summary>Patlak piksel tavanı: bunun üstü poz olarak reddedilir.</summary>
        private const float MaxBlown = 0.005f;

        private static readonly float[] EvLadder =
        {
            15.0f, 14.5f, 14.0f, 13.5f, 13.0f, 12.5f, 12.0f,
            11.5f, 11.0f, 10.5f, 10.0f, 9.5f, 9.0f,
        };

        private struct Shot
        {
            public string Name;      // dosya adi
            public string Claim;     // bu kare NEYI gosteriyor
            public Vector3 Eye, Look;
            public float Fov;
        }

        [MenuItem("Hezarfen/GIS/Mahalle inceleme paketi (ogle + gun batimi)")]
        public static void CaptureGalataMenu() => Capture(OttomanStreetBuilder.Galata);

        public static void Capture(OttomanStreetBuilder.QuarterSpec q)
        {
            EditorSceneManager.OpenScene(q.ScenePath, OpenSceneMode.Single);

            var root = GameObject.Find(q.RootName);
            if (root == null)
            {
                Debug.LogError($"[Hezarfen] {q.RootName} yok — once "
                               + "'Galata sokagi sahnesi kur'.");
                return;
            }

            var shots = Frames(root);
            if (shots.Count == 0)
            {
                Debug.LogError("[Hezarfen] Hicbir kadraj kurulamadi — sahnede "
                               + "cekirdek yapilari bulunamadi.");
                return;
            }

            var lines = new List<string>();
            try
            {
                Moment(q, "ogle", NoonSolarHour, shots, lines);

                float h = SunPlacement.AfternoonHourAtAltitude(
                    SunPlacement.DayOfYear, SunPlacement.LatitudeDeg,
                    LowSunAltitudeDeg);
                Moment(q, "gunbatimi", h, shots, lines);
            }
            finally
            {
                // Poz DISKTE yasiyor: birakilirsa sonraki olcum (LightingTests)
                // sessizce bu turun pozunda calisirdi.
                InterimLighting.ApplyExposure(InterimLighting.DefaultExposureEV);
                // Sahne kirli kaldi (gunes dondu, takim degisti) — KAYDETMEDEN
                // yeniden ac. Inceleme paketi sahneyi degistirmez.
                EditorSceneManager.OpenScene(q.ScenePath, OpenSceneMode.Single);
            }

            Debug.Log($"[Hezarfen] {q.Name} mahalle inceleme paketi "
                      + $"({shots.Count} kadraj x 2 an) -> {OutDir}/\n"
                      + string.Join("\n", lines));
        }

        // ------------------------------------------------------------- an

        private static void Moment(OttomanStreetBuilder.QuarterSpec q, string label,
                                   float solarHour, List<Shot> shots,
                                   List<string> lines)
        {
            var sun = SunPlacement.Find();
            if (sun == null) { lines.Add($"{label}: GUNES YOK — atlandi."); return; }

            SunPlacement.Solar(SunPlacement.DayOfYear, solarHour,
                               SunPlacement.LatitudeDeg,
                               out double alt, out double azi);
            SunPlacement.Apply(sun, alt, azi);

            // Dolgu YONU gunesten turer; gunes dondugune gore takim yeniden
            // kurulmak zorunda. Sadece pozu degistirip dolguyu birakmak,
            // gun batimi karesini "yanlis yerden gelen ogle isigi" yapardi.
            float scale = InterimLighting.FillScaleForAltitude((float)alt);
            InterimLighting.Install(out string rig, scale);

            // Pozometre: SOKAK KORIDORU karesi uzerinde — mahallenin en
            // temsili kadraji (yer, cephe, gokyuzu birlikte). Kurulamadiysa
            // sondan ikinciye duser ve bu log'da gorunur.
            Shot meter = shots[shots.Count - 2];
            foreach (var s in shots) if (s.Name == "07_sokak") { meter = s; break; }
            float ev = PickExposure(meter, out FrameMetric.Stats mst, out string sweep);
            InterimLighting.ApplyExposure(ev);

            lines.Add($"--- {label}: gunes saati {solarHour:F2}, yukseklik "
                      + $"{alt:F1} derece, azimut {azi:F1} derece");
            lines.Add($"    {rig}");
            lines.Add($"    pozometre ({meter.Name}): {sweep}");
            lines.Add($"    secilen {ev:F1} EV -> {mst}");

            foreach (var s in shots)
            {
                var st = FrameMetric.Capture(s.Eye, s.Look, s.Fov,
                                             $"{OutDir}/{label}_{s.Name}.png",
                                             960, 540);
                lines.Add($"    {s.Name,-18} {st}   [{s.Claim}]");
                lines.Add($"       {Ahead(s)}");
            }
        }

        /// <summary>
        /// Kameranın <b>önünde ne var, kaç metrede</b> — kadraj teşhisi.
        ///
        /// Neden gerekli: parlaklık ve ayrıntı sayıları bir duvarla dolu kareyi
        /// makul gösteriyor (duvar da bir dokudur). Üç tur boyunca kadrajları
        /// tahminle düzelttim ve her seferinde başka bir duvara çarptım. Bu
        /// satır tahmini bitiriyor: karede 1,2 m ötede bir ev varsa yazıyor.
        /// </summary>
        private static string Ahead(Shot s)
        {
            Physics.SyncTransforms();
            Vector3 dir = (s.Look - s.Eye).normalized;
            bool inside = Physics.CheckSphere(s.Eye, 0.35f);
            string tag = inside ? "GOZ BIR CIZICININ ICINDE! " : "";
            if (Physics.Raycast(s.Eye, dir, out var hit, 400f))
                return $"{tag}onunde {hit.collider.name} @ {hit.distance:F1} m "
                     + $"(goz {s.Eye.y:F1} m)";
            return $"{tag}onunde carpma yok (goz {s.Eye.y:F1} m)";
        }

        /// <summary>
        /// Poz merdivenini süpürür ve <b>ölçerek</b> birini seçer.
        ///
        /// Ölçüt iki basamaklıdır, ağırlıklı toplam değil: önce patlak piksel
        /// oranı eşiğin altında olmalı (patlamış bir kare geri döndürülemez),
        /// sonra kalanlar arasında ayrıntı enerjisi en yüksek olan kazanır.
        /// Tek bir puanda birleştirmek, patlamayı "biraz ayrıntı" karşılığında
        /// takas edilebilir gösterirdi; edilemez.
        /// </summary>
        private static float PickExposure(Shot meter, out FrameMetric.Stats best,
                                          out string sweep)
        {
            float bestEv = InterimLighting.DefaultExposureEV;
            best = default;
            float bestDetail = -1f;
            var parts = new List<string>();

            foreach (float ev in EvLadder)
            {
                InterimLighting.ApplyExposure(ev);
                var st = FrameMetric.Capture(meter.Eye, meter.Look, meter.Fov,
                                             null, 320, 180);
                parts.Add($"{ev:F1}:{st.Detail:F2}/{st.BlownPct * 100f:F1}%");
                if (st.BlownPct > MaxBlown) continue;
                if (st.Detail <= bestDetail) continue;
                bestDetail = st.Detail; bestEv = ev; best = st;
            }

            // Hicbiri esigi gecemediyse SUSMAK yanlis olur: kare yine de
            // uretilir ama paket bunu soyler.
            if (bestDetail < 0f)
            {
                Debug.LogWarning("[Hezarfen] Poz merdiveninin HICBIR basamagi "
                                 + $"patlamadan gecemedi (tavan %{MaxBlown * 100f:F1}). "
                                 + "En karanlik basamak kullanildi.");
                bestEv = EvLadder[EvLadder.Length - 1];
                InterimLighting.ApplyExposure(bestEv);
                best = FrameMetric.Capture(meter.Eye, meter.Look, meter.Fov,
                                           null, 320, 180);
            }

            sweep = "EV:ayrinti/patlak  " + string.Join("  ", parts);
            return bestEv;
        }

        // -------------------------------------------------------- kadrajlar

        private static List<Shot> Frames(GameObject root)
        {
            var all = root.GetComponentsInChildren<Transform>(true);
            var shots = new List<Shot>();

            Transform mescit = First(all, "PF_Mescit_A", "PF_Sinagog_A");
            Transform kapi = First(all, "PF_AvluKapi");
            Transform sadirvan = First(all, "PF_Sadirvan");
            Transform cesme = First(all, "PF_Cesme");
            Transform kahve = First(all, "PF_Kahvehane");
            var dukkan = AllOf(all, "PF_Dukkan");
            var mezar = AllOf(all, "PF_Mezar");

            // 1) Sokaktan cekirdege: kapi, merdiven, arkasinda mescit.
            //    IDDIA: mahalle mescitten dallanir — sokakta duran kisi once
            //    avlu kapisini gorur.
            //
            // Bakis SOKAKTAN ve CAPRAZ. Sokak 4,6 m: cepheye karsidan bakmak
            // icin karsi evin icine girmek gerekir (denedim, kare oldu). Dar
            // sokakta yapiya capraz bakilir — yaya da oyle gorur.
            if (kapi != null)
                TryEye("01_cekirdek", "avlu kapisi + merdiven + mescit",
                       kapi.position + Vector3.up * 1.8f, 55f, shots,
                       Fan(kapi, OttomanStreetBuilder.StreetWidth * 0.5f,
                           4f, 7f, 10f, 13f));

            // 2) Avlunun icinden: sadirvan ve mescit cephesi.
            //    IDDIA: avlu bir MEYDANDIR, sadirvani ortasindadir.
            //
            // Bu karenin zemini ARAZI DEGIL, TERAS. Ilk denemede goz araziden
            // 1,70 m yukari kondu ve kare **%100 karanlik** cikti (AYRINTI
            // 0,01): avlu yamacta bir teras, arazi onun metrelerce altinda ve
            // kamera tas kaidenin ICINDE kaldi. Sayilar bunu tek bakista
            // soyledi; kareye bakan biri "golge" sanabilirdi.
            //
            // Avlunun kotu sadirvanin kendi kotudur — o terasin ustunde durur.
            //
            // Avlu KUCUKTUR: sadirvanla kapi arasi olculdu, 2,2 m. Karsidan
            // bakacak yer yok; kadraj avlunun KOSESINDEN capraz kurulur ve
            // sadirvani, kapiyi ve mescit cephesini birlikte alir.
            //
            // Goz KAPININ hemen icinde. Avlunun kotu de kapinin kotudur —
            // ikisi de terasin ustunde. Mescide gore hesaplanan bir nokta
            // avlu duvarinin ICINE dustu (cizici sinadi ve soyledi).
            if (sadirvan != null && kapi != null)
                shots.Add(new Shot
                {
                    Name = "02_sadirvan",
                    Claim = "avlu ici: sadirvan + mescit cephesi",
                    // Kapinin tam esiginde: avlu 3,5 m payla kuruluyor ve
                    // sadirvanla kapi arasi OLCULDU, 2,2 m. Daha geri
                    // gidecek yer yok — kadrajin dar olmasi bir kusur degil,
                    // avlunun kendi olcusudur (bkz. inceleme notu).
                    Eye = kapi.position - kapi.forward * 0.4f + Vector3.up * 1.70f,
                    Look = sadirvan.position + Vector3.up * 1.2f,
                    Fov = 78f,
                });

            // 3) Cesme, goz hizasindan.
            //    IDDIA: su alinan yer gecis noktasindadir, sokaga bakar.
            if (cesme != null)
                TryEye("03_cesme", "cesme: ayna tasi + teknelik + kitabe",
                       cesme.position + Vector3.up * 1.6f, 44f, shots,
                       Fan(cesme, 3.5f, 0f, 2.5f, 4.5f));

            // 4) Dukkan sirasi — CAPRAZ, cunku sira olarak okunmali.
            //    IDDIA: ticari cekirdek dini cekirdegin karsi sirasindadir.
            //
            // Bakis SIRANIN BOYUNCA, ortasina degil. Once merkeze bakiliyordu
            // ve olcum her adayi reddetti — haklı olarak: bir sıranın ortasını
            // en yakın dükkânın kendisi kapatır. Sıra, ucundan bakılınca sıra
            // olarak okunur.
            if (dukkan.Count >= 2)
            {
                Transform d0 = dukkan[0], dn = dukkan[dukkan.Count - 1];
                Vector3 row = Horizontal(dn.position - d0.position);
                if (row.sqrMagnitude > 1f)
                {
                    row = row.normalized;
                    // Hedef yapinin MERKEZI degil CEPHESI. Merkeze nisan
                    // alinca on duvar "engel" sayiliyor ve butun adaylar
                    // eleniyordu — dogru, ama sorulan soru o degildi.
                    //
                    // Sira IKI UCTAN da denenir: bir ucta avlu duvari
                    // gorusu kesiyordu (olculdu, 10,5/22,4 m).
                    string claim = $"{dukkan.Count} dukkan, mescidin karsisi";
                    if (!TryEye("04_dukkanlar", claim,
                                InFrontOf(dn, 0.1f) + Vector3.up * 1.6f, 52f, shots,
                                Horizontal(InFrontOf(d0, 4.5f)) - row * 5f,
                                Horizontal(InFrontOf(d0, 3.0f)) - row * 9f,
                                Horizontal(InFrontOf(d0, 6.5f)) - row * 3f))
                        TryEye("04_dukkanlar", claim,
                               InFrontOf(d0, 0.1f) + Vector3.up * 1.6f, 52f, shots,
                               Horizontal(InFrontOf(dn, 4.5f)) + row * 5f,
                               Horizontal(InFrontOf(dn, 3.0f)) + row * 9f,
                               Horizontal(InFrontOf(dn, 6.5f)) + row * 3f);
                }
            }

            // 5) Hazire — mezar ekseni boyunca DEGIL, ona dik bakilir; siralar
            //    ancak o zaman sira olarak okunur.
            //    IDDIA: musluman mezari kibleye diktir (ADR 0016), servi
            //    hazirenin kenarindadir, turbe ucundadir.
            //
            // MEZARLAR IKI YERDE: mescidin haziresi ve kilisenin mezarligi.
            // Hepsinin ortalamasi ikisinin ARASINA, bos bir yere dusuyordu ve
            // kare cimen gosterdi. Cekirdege en yakin mezar bulunur, kume
            // onun cevresinden toplanir.
            if (mezar.Count > 0 && mescit != null)
            {
                Transform seed = mezar[0];
                float bestD = float.MaxValue;
                foreach (var m in mezar)
                {
                    float d = (m.position - mescit.position).sqrMagnitude;
                    if (d < bestD) { bestD = d; seed = m; }
                }
                var cluster = new List<Transform>();
                foreach (var m in mezar)
                    if ((m.position - seed.position).sqrMagnitude < 15f * 15f)
                        cluster.Add(m);

                Vector3 c = Centroid(cluster);
                Vector3 axis = seed.forward;
                Vector3 side = Vector3.Cross(Vector3.up, axis).normalized;
                // Hedef tasin TEPESI (1,2 m), tabani degil: hazire ALCAK bir
                // duvarla cevrilidir ve 0,8 m'ye nisan alan bakis o duvarin
                // ustunden gecemiyordu (olculdu: PF_AvluDuvarKisa @ 3,3 m).
                // Hazireye zaten duvarin ustunden bakilir.
                TryEye("05_hazire", $"{cluster.Count} mezar tasi + servi + turbe",
                       c + Vector3.up * 1.2f, 46f, shots,
                       Horizontal(c + side * 6f), Horizontal(c - side * 6f),
                       Horizontal(c + axis * 6f), Horizontal(c - axis * 6f),
                       Horizontal(c + side * 9f), Horizontal(c - side * 9f),
                       Horizontal(c + axis * 9f), Horizontal(c - axis * 9f));
            }

            // 6) Kahvehane — oyunun ZAMAN ISARETI. 1632'de acik; 2 Eylul 1633
            //    fermanindan sonra bu kare mumkun degil.
            //    Bakis SOKAK BOYUNCA capraz: 12 m geri gitmek karsi evin
            //    duvarina girmisti (kare %68 karanlikti).
            if (kahve != null)
                TryEye("06_kahvehane", "1632 isareti: sundurma + seki + cinar",
                       InFrontOf(kahve, 0.1f) + Vector3.up * 2.2f, 52f, shots,
                       Fan(kahve, OttomanStreetBuilder.StreetWidth * 0.5f,
                           9f, 6f, 12f, 3f));

            // 7) SOKAK KORIDORU — pozometrenin karesi de budur (sondan ikinci).
            //    IDDIA: doku; kaldirim, kaide, cumba, sacak.
            var street = FindChild(root.transform, "Sokak_Ana");
            if (street != null)
            {
                var houses = new List<Transform>();
                foreach (Transform t in street.transform)
                    if (t.GetComponent<LODGroup>() != null) houses.Add(t);
                // Ilk yari eksenin BIR yanidir (yerlestirici once +1, sonra -1
                // tarafini dizer); iki evi ayni yandan almak gerekiyor, yoksa
                // "koridor" sokagin karsisina bakar.
                // KORIDOR YEREL TEGET BOYUNCA — iki evin arasi DEGIL.
                //
                // Once iki evin cephesini birlestirip oraya bakiyordum ve
                // OLCUM her adayi reddetti: gorus hatti 5-8 m'de komsu evin
                // KENDISINE carpiyordu. Sebep kivrim degil, nisan alma
                // yontemiydi — egri bir sirada iki ucu birlestiren kiris
                // aradaki evin ARKASINDAN gecer. Bu, sokak ne kadar duz
                // olursa olsun boyledir.
                //
                // Yaya sokaga bakarken uzaktaki bir eve degil, ONUNDEKI
                // KORIDORA bakar. Yon yerel tegetten alinir; koridorun nerede
                // kapandigi da olculur ve yazilir — sokagin egriligi hakkinda
                // asil sayi odur.
                int half = houses.Count / 2;
                if (half >= 4)
                {
                    int i = half / 6;
                    // GOZ KALDIRIMIN USTUNDE olmali. Evden olculen bir nokta
                    // yetmedi: cephe hattinda sapma var, sacak degisken
                    // tasiyor ve nokta seridin disina dustu — olcum kamerayi
                    // evin TAS KAIDESININ ustunde buldu (Kaideler @ 51,36),
                    // sokakta degil. Sokak, kaldirimin oldugu yerdir; alet
                    // artik dogrudan onu ariyor.
                    if (OnPavement(houses[i], out Vector3 p0)
                        && OnPavement(houses[i + 2], out Vector3 p2)
                        && Horizontal(p2 - p0).sqrMagnitude > 1f)
                    {
                        Vector3 eye = p0 + Vector3.up * 1.70f;
                        Vector3 tan = Horizontal(p2 - p0).normalized;
                        Physics.SyncTransforms();
                        float open = Physics.Raycast(eye, tan, out var h, 120f)
                                   ? h.distance : 120f;
                        shots.Add(new Shot
                        {
                            Name = "07_sokak",
                            Claim = $"sokak koridoru — {open:F0} m sonra kapaniyor",
                            Eye = eye, Look = eye + tan * 40f, Fov = 55f,
                        });
                    }
                    else
                    {
                        Debug.LogWarning("[Hezarfen] Sokak koridoru: evin onunde "
                                         + "KALDIRIM bulunamadi — serit orada yok.");
                    }
                }
            }

            // 8) Kusbakisi — cekirdek gercekten MERKEZ mi.
            if (mescit != null)
            {
                Vector3 c = mescit.position;
                shots.Add(new Shot
                {
                    Name = "08_mahalle",
                    Claim = "doku cekirdekten dalliyor mu",
                    Eye = c + new Vector3(-95f, 82f, -118f),
                    Look = c + Vector3.up * 4f,
                    Fov = 52f,
                });
            }

            // Pozometre sondan ikinciyi kullaniyor: sokak karesi oraya
            // dusmediyse siralamayi duzelt, sessiz kalma.
            if (shots.Count >= 2 && shots[shots.Count - 2].Name != "07_sokak")
                Debug.LogWarning("[Hezarfen] Sokak koridoru karesi kurulamadi — "
                                 + "poz baska bir kare uzerinden olculecek.");
            return shots;
        }

        /// <summary>
        /// Aday gözler arasından, hedefi <b>gerçekten gören</b> ilkini seçer.
        ///
        /// ## Neden bir tarama, tek bir hesap değil
        ///
        /// Kadrajı beş tur elle düzelttim ve her seferinde başka bir duvara
        /// çarptı. Sebep dokunun kendisi: sokak 4,6 m, evler bitişik nizam ve
        /// eksen eğri. "Yapının 9 m yanında dur" gibi tek bir kural bu dokuda
        /// çalışmaz — bazen orada bir ev vardır.
        ///
        /// Aletin ölçtüğü şey artık iddianın kendisi: <b>bu noktadan o yapı
        /// görünüyor mu.</b> Görünmüyorsa aday elenir; hiçbiri görünmüyorsa
        /// kare üretilmez ve bu <b>loglanır</b> — sessizce bir duvar
        /// yayımlamaktansa eksik bir paket yayımlamak yeğdir.
        /// </summary>
        private static bool TryEye(string name, string claim, Vector3 look, float fov,
                                   List<Shot> shots, params Vector3[] candidates)
        {
            Physics.SyncTransforms();
            var tried = new List<string>();
            foreach (var c in candidates)
            {
                Vector3 eye = FrameMetric.OnSurface(c) + Vector3.up * 1.70f;
                if (Physics.CheckSphere(eye, 0.35f))
                { tried.Add("goz cizicinin icinde"); continue; }

                float d = Vector3.Distance(eye, look);
                // Isin hedefin KENDISINE carpar; engel sayilmasi icin carpmanin
                // hedeften belirgin sekilde once olmasi gerekir.
                if (Physics.Linecast(eye, look, out var h) && h.distance < d - 1.2f)
                { tried.Add($"{h.collider.name} @ {h.distance:F1}/{d:F1} m"); continue; }

                shots.Add(new Shot { Name = name, Claim = claim, Eye = eye,
                                     Look = look, Fov = fov });
                return true;
            }
            Debug.LogWarning($"[Hezarfen] {name}: hicbir adaydan hedef gorunmuyor — "
                             + "kare uretilmedi. Denenenler: " + string.Join(", ", tried));
            return false;
        }

        /// <summary>Göz hizası kare: bakan da bakılan da ARAZİNİN üstünde.</summary>
        private static Shot Eye(string name, string claim, Vector3 eye, Vector3 look,
                                float fov) => new Shot
        {
            Name = name,
            Claim = claim,
            // Goz BASILAN YUZEYDEN 1,70 m yukarida — arazidan degil. Mahallede
            // yaya kaldirima ve tas kaideye basar; ikisi de arazinin ustunde.
            // Arazi kotu kullanildiginda kareler kaldirimin ALTINDA cikti.
            Eye = FrameMetric.OnSurface(eye) + Vector3.up * 1.70f,
            Look = look,
            Fov = fov,
        };

        /// <summary>
        /// Yapının <b>cephesinden</b> <paramref name="extra"/> metre öndeki nokta.
        ///
        /// Pivot taban MERKEZİNDEDİR; kadrajı pivottan ölçmek, kamerayı
        /// yapının içinden çıkarmaya çalışmak demek. Ölçüldü ve üç kare
        /// birden bunu gösterdi: biri kapının tahtasına, biri karşı evin
        /// duvarına, biri kendi duvarının içine bakıyordu — üçünde de sayılar
        /// makuldü, çünkü duvar da bir dokudur.
        ///
        /// ## Erim NESNENİN KENDİ EKSENİNDE ölçülür
        ///
        /// İlk hâli <c>Renderer.bounds</c> (dünya hizalı kutu) kullanıyordu ve
        /// ölçüm onu çürüttü: 27° dönmüş bir ev için erim gerçek 2,5 m yerine
        /// <b>7,95 m</b> çıktı — dönmüş bir kutunun eksen hizalı kabuğu çok
        /// daha büyüktür. Kamera 4,6 m'lik sokağı aşıp karşı evin İÇİNE girdi
        /// ve kare bir oda gösterdi.
        ///
        /// Doğrusu mesh köşelerini yapının <b>yerel</b> çerçevesine taşımak ve
        /// en büyük yerel +Z'yi almak: dönme ne olursa olsun aynı sayıyı verir.
        /// </summary>
        private static Vector3 InFrontOf(Transform t, float extra)
        {
            float front = 0f;
            bool any = false;
            Matrix4x4 w2l = t.worldToLocalMatrix;

            foreach (var mf in t.GetComponentsInChildren<MeshFilter>())
            {
                var mesh = mf.sharedMesh;
                if (mesh == null) continue;
                Bounds b = mesh.bounds;
                Matrix4x4 m = w2l * mf.transform.localToWorldMatrix;
                for (int i = 0; i < 8; i++)
                {
                    var c = new Vector3((i & 1) == 0 ? b.min.x : b.max.x,
                                        (i & 2) == 0 ? b.min.y : b.max.y,
                                        (i & 4) == 0 ? b.min.z : b.max.z);
                    float z = m.MultiplyPoint3x4(c).z;
                    if (!any || z > front) { front = z; any = true; }
                }
            }
            return t.position + t.forward * ((any ? front : 0f) + extra);
        }

        private static Transform First(Transform[] all, params string[] prefixes)
        {
            foreach (string p in prefixes)
                foreach (var t in all)
                    if (t.name.StartsWith(p)) return t;
            return null;
        }

        private static List<Transform> AllOf(Transform[] all, string prefix)
        {
            var list = new List<Transform>();
            foreach (var t in all)
                if (t.name.StartsWith(prefix)) list.Add(t);
            return list;
        }

        /// <summary>
        /// Evin önünde <b>kaldırımın üstünde</b> bir nokta arar.
        ///
        /// Ölçüt bir mesafe değil, bir <b>çarpma</b>: aşağı atılan ışın
        /// <c>Kaldirim</c> mesh'ine çarpıyorsa orası sokaktır; çarpmıyorsa
        /// orası sokak değildir, ne kadar sokak gibi görünürse görünsün.
        /// </summary>
        private static bool OnPavement(Transform house, out Vector3 p)
        {
            Physics.SyncTransforms();
            Vector3 f = Horizontal(house.forward).normalized;
            Vector3 r = Horizontal(house.right).normalized;
            Vector3 start = Horizontal(InFrontOf(house, 0.2f));

            for (float d = 0f; d <= 6.0f; d += 0.25f)
                foreach (float lat in new[] { 0f, 1.2f, -1.2f, 2.4f, -2.4f })
                {
                    Vector3 q = start + f * d + r * lat;
                    float g = FrameMetric.OnGround(q).y;
                    if (Physics.Raycast(new Vector3(q.x, g + 4f, q.z), Vector3.down,
                                        out var h, 12f)
                        && h.collider.name == "Kaldirim")
                    { p = h.point; return true; }
                }
            p = Vector3.zero;
            return false;
        }

        /// <summary>Yalnız yatay bileşen — kotu çağıran ayrıca verir.</summary>
        private static Vector3 Horizontal(Vector3 v) => new Vector3(v.x, 0f, v.z);

        /// <summary>
        /// Yapının önünde, sağa ve sola <paramref name="offsets"/> kadar
        /// kaydırılmış aday göz noktaları — dar sokakta karşıdan bakılamaz,
        /// çapraz bakılır.
        /// </summary>
        private static Vector3[] Fan(Transform t, float ahead, params float[] offsets)
        {
            var pts = new List<Vector3>();
            Vector3 front = Horizontal(InFrontOf(t, ahead));
            Vector3 r = Horizontal(t.right).normalized;
            foreach (float o in offsets)
            {
                pts.Add(front + r * o);
                if (o > 0.01f) pts.Add(front - r * o);
            }
            return pts.ToArray();
        }

        private static Vector3 Centroid(List<Transform> ts)
        {
            var s = Vector3.zero;
            foreach (var t in ts) s += t.position;
            return s / Mathf.Max(1, ts.Count);
        }

        private static Transform FindChild(Transform root, string name)
        {
            foreach (Transform t in root)
                if (t.name == name) return t;
            return null;
        }
    }
}
