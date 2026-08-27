using System.Text;
using Hezarfen.Flight;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Hezarfen.Editor.Flight
{
    /// <summary>
    /// <b>Uçuş gerçekten yapılabiliyor mu?</b> — ADR 0037'nin sınavı.
    ///
    /// `FlightBudget` sakin havada menzili ölçer ve 597 m der; gereken 3 278 m.
    /// Aradaki farkı kapatacak şey kaldıraçtır ve Caner **gerçek termik
    /// simülasyonunu** seçti. Bu araç o seçimin işe yarayıp yaramadığını
    /// ölçer — kabaca değil, adım adım.
    ///
    /// ## İki evreli uçuş
    ///
    /// Termik <b>kara üstünde</b> doğar, su üstünde doğmaz. Uçuş Boğaz'ı
    /// geçtiği için süzülüş sırasında kaldıraç yok; tersine, deniz üstünde
    /// hava <b>alçalır</b>. Yani uçuş tek bir süzülüş değil, iki evre:
    ///
    /// <list type="number">
    /// <item><b>Tırmanış.</b> Galata yamacında en iyi kaldıracı bul ve
    ///       gereken irtifayı topla. Bu bir bekleme değil, bir zanaat.</item>
    /// <item><b>Geçiş.</b> İrtifa yeterliyse bağlan. Yeterli değilse
    ///       oyuncu denize düşer — ve bu, oyunun öğrettiği şeyin
    ///       kendisidir.</item>
    /// </list>
    ///
    /// ## Ne simüle edilir
    ///
    /// Her adımda gerçek arazi örneklenir (termik, çökelme, yamaç
    /// kaldıracı), gerçek süzülme oranı ve alçalma hızı kullanılır. Kabaca
    /// ortalama alınmaz: koridorun %70'i su üstündedir ve orada kaldıraç
    /// eksidir; ortalamayla hesaplamak o eksiyi karaya yayarak gizlerdi.
    /// </summary>
    public static class ThermalFlightSim
    {
        /// <summary>Adım (s). Küçük olması sonucu bir yere yakınsatır.</summary>
        const float Dt = 0.5f;

        /// <summary>Tırmanışta bu kadar süre kazanç yoksa vazgeçilir (s).</summary>
        const float ClimbGiveUp = 900f;

        [MenuItem("Hezarfen/Ucus/Termik ile ucusu sina")]
        public static void Simulate()
        {
            var tgo = GameObject.Find("TR_Istanbul");
            var terrain = tgo != null ? tgo.GetComponent<Terrain>() : null;
            if (terrain == null) { Debug.LogError("[Hezarfen] TR_Istanbul yok."); return; }

            var th = tgo.GetComponent<TerrainThermal>();
            if (th == null) th = tgo.AddComponent<TerrainThermal>();

            var tuning = FindTuning();
            if (tuning == null) { Debug.LogError("[Hezarfen] WindTuning yok."); return; }

            Vector3 hedef = LandingPoint();
            Vector3 kule = LaunchPoint(terrain);
            var wind = tuning.globalWind;

            // Kanadin en iyi suzulme noktasi: FlightBudget ile ayni ayardan.
            float trim = 12.4f, sink = 1.08f, glide = trim / sink;

            var sb = new StringBuilder();
            sb.AppendLine("TERMIK ILE UCUS SINAVI");
            sb.AppendLine($"kule tepesi {kule.y:0.0} m -> Dogancilar {hedef.y:0.0} m");
            float mesafe = Vector2.Distance(new Vector2(kule.x, kule.z),
                                            new Vector2(hedef.x, hedef.z));
            sb.AppendLine($"mesafe {mesafe:0} m, suzulme {glide:0.00} : 1, "
                          + $"alcalma {sink:0.00} m/s, ruzgar {wind.magnitude:0.0} m/s");

            // --- 1) EN IYI KALDIRAC nerede -----------------------------
            var (termikP, termikLift) = BestLift(terrain, th, kule, wind, 1400f);
            sb.AppendLine();
            sb.AppendLine($"en iyi kaldirac: {termikLift:0.00} m/s, kuleden "
                          + $"{Vector2.Distance(new Vector2(kule.x, kule.z), new Vector2(termikP.x, termikP.z)):0} m");

            if (termikLift <= sink + 0.05f)
            {
                sb.AppendLine("SONUC: TIRMANIS IMKANSIZ — kaldirac alcalmayi "
                              + "yenmiyor. Ucus yapilamaz.");
                Debug.LogWarning("[Hezarfen] " + sb);
                return;
            }

            // --- 2) TIRMANIS -------------------------------------------
            float y = kule.y, t = 0f;
            float netEnIyi = termikLift - sink;
            float tavan = th.ceilingMeters * 0.92f;
            float gereken = GerekenIrtifa(terrain, th, termikP, hedef, wind,
                                          trim, sink);
            while (y < gereken && t < ClimbGiveUp)
            {
                var p = new Vector3(termikP.x, y, termikP.z);
                float lift = th.SampleVertical(p, wind);
                float net = lift - sink;
                if (net <= 0.01f) break;
                y += net * Dt;
                t += Dt;
                if (y > tavan) break;
            }
            sb.AppendLine($"gereken irtifa: {gereken:0} m");
            sb.AppendLine($"tirmanis: {t:0} s ({t / 60f:0.0} dk) -> {y:0} m "
                          + $"(en iyi net {netEnIyi:0.00} m/s, tavan {tavan:0} m)");

            if (y < gereken - 5f)
            {
                sb.AppendLine($"SONUC: YETMEDI — {gereken - y:0} m eksik. "
                              + "Tavan ya da kaldirac artmali.");
                Debug.LogWarning("[Hezarfen] " + sb);
                return;
            }

            // --- 3) GECIS ----------------------------------------------
            var poz = new Vector3(termikP.x, y, termikP.z);
            float gecis = 0f;
            bool vardi = false;
            for (int i = 0; i < 4000; i++)
            {
                var duz = new Vector2(hedef.x - poz.x, hedef.z - poz.z);
                float kalan = duz.magnitude;
                if (kalan < 30f) { vardi = true; break; }
                var yon = duz.normalized;
                // Yer hizi = hava hizi + ruzgarin yol yonundeki bileseni.
                float ruz = Vector2.Dot(new Vector2(wind.x, wind.z), yon);
                float yerHiz = Mathf.Max(2f, trim + ruz);
                poz.x += yon.x * yerHiz * Dt;
                poz.z += yon.y * yerHiz * Dt;
                float hava = th.SampleVertical(poz, wind);
                poz.y += (hava - sink) * Dt;
                gecis += Dt;
                float zemin = terrain.SampleHeight(poz)
                              + terrain.transform.position.y;
                if (poz.y <= Mathf.Max(zemin, 0f) + 2f) break;
            }
            sb.AppendLine($"gecis: {gecis:0} s ({gecis / 60f:0.0} dk), "
                          + $"varis kotu {poz.y:0.0} m (hedef {hedef.y:0.0} m)");
            sb.AppendLine();
            sb.AppendLine(vardi
                ? $"SONUC: UCUS YAPILABILIR. Toplam {(t + gecis) / 60f:0.0} dk "
                  + $"({t / 60f:0.0} dk tirmanis + {gecis / 60f:0.0} dk gecis)."
                : $"SONUC: DUSTU — hedefe "
                  + $"{Vector2.Distance(new Vector2(poz.x, poz.z), new Vector2(hedef.x, hedef.z)):0} m kala.");
            Debug.Log("[Hezarfen] " + sb);
        }

        /// <summary>
        /// Geçiş için gereken irtifa — <b>simüle edilerek</b> bulunur.
        ///
        /// Kapalı formül yazmak cazip ama yanlış olurdu: koridorun bir kısmı
        /// su (çökelme), bir kısmı kara (kaldıraç) ve oran yola göre değişir.
        /// Bu yüzden geriye doğru denenir: bir irtifadan başla, geçişi
        /// simüle et, varamazsan yükselt.
        /// </summary>
        static float GerekenIrtifa(Terrain terrain, TerrainThermal th,
                                   Vector3 baslangic, Vector3 hedef,
                                   Vector3 wind, float trim, float sink)
        {
            for (float y0 = hedef.y + 40f; y0 < th.ceilingMeters; y0 += 10f)
            {
                var poz = new Vector3(baslangic.x, y0, baslangic.z);
                for (int i = 0; i < 4000; i++)
                {
                    var duz = new Vector2(hedef.x - poz.x, hedef.z - poz.z);
                    if (duz.magnitude < 30f) return y0;
                    var yon = duz.normalized;
                    float ruz = Vector2.Dot(new Vector2(wind.x, wind.z), yon);
                    float yerHiz = Mathf.Max(2f, trim + ruz);
                    poz.x += yon.x * yerHiz * Dt;
                    poz.z += yon.y * yerHiz * Dt;
                    poz.y += (th.SampleVertical(poz, wind) - sink) * Dt;
                    float zemin = terrain.SampleHeight(poz)
                                  + terrain.transform.position.y;
                    if (poz.y <= Mathf.Max(zemin, hedef.y) + 2f) break;
                }
            }
            return th.ceilingMeters;
        }

        /// <summary>Kulenin çevresinde en iyi kaldıracın olduğu nokta.</summary>
        static (Vector3, float) BestLift(Terrain terrain, TerrainThermal th,
                                         Vector3 kule, Vector3 wind, float menzil)
        {
            float enIyi = -99f;
            Vector3 enIyiP = kule;
            for (float r = 80f; r <= menzil; r += 60f)
            for (int i = 0; i < 32; i++)
            {
                float a = Mathf.PI * 2f * i / 32f;
                var p = new Vector3(kule.x + Mathf.Cos(a) * r, 0f,
                                    kule.z + Mathf.Sin(a) * r);
                float g = terrain.SampleHeight(p) + terrain.transform.position.y;
                if (g < 3f) continue;                      // su
                p.y = Mathf.Max(kule.y, g + 80f);
                float lift = th.SampleVertical(p, wind);
                if (lift > enIyi) { enIyi = lift; enIyiP = p; }
            }
            return (enIyiP, enIyi);
        }

        static Vector3 LandingPoint()
        {
            var kok = GameObject.Find("LANDMARK_1632");
            if (kok != null)
                foreach (Transform t in kok.transform)
                    if (t.name.Contains("Dogancilar")) return t.position;
            return new Vector3(3215f, 46f, -643f);
        }

        static Vector3 LaunchPoint(Terrain terrain)
        {
            var kok = GameObject.Find("LANDMARK_1632");
            if (kok != null)
                foreach (Transform t in kok.transform)
                    if (t.name.Contains("GalataKulesi"))
                        return t.position + Vector3.up * 46f;
            return new Vector3(0f, 98f, 0f);
        }

        static WindTuning FindTuning()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:WindTuning"))
            {
                var w = AssetDatabase.LoadAssetAtPath<WindTuning>(
                    AssetDatabase.GUIDToAssetPath(guid));
                if (w != null) return w;
            }
            return null;
        }
    }
}
