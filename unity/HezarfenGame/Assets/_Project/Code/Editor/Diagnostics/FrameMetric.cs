using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Hezarfen.Editor.Diagnostics
{
    /// <summary>
    /// Sahneden bir kare alır ve <b>sayı</b> döndürür — "render bir gözlemdir,
    /// kanıt değil" kuralının aleti (CLAUDE.md).
    ///
    /// Buraya taşındı çünkü aynı ölçüm iki yerde gerekiyor: gölgedeki bir ev
    /// cephesi okunuyor mu (aydınlatma) ve ayağın bastığı zemin okunuyor mu
    /// (arazi örtüsü). İkinci kopyayı yazmak, aşağıdaki iki tuzağın da
    /// ikinci bir kopyasını yazmak demekti — ve biri bir turu tek başına
    /// yanlış yöne çevirmişti.
    ///
    /// ## Ölçü PARLAKLIK değil AYRINTI
    ///
    /// "Karanlık piksel oranı" yanlış aletti: Balat'ın paleti <b>bilerek</b>
    /// koyudur (zimmî renk kısıtı) ve gayet okunabilir bir Balat cephesi %56
    /// "okunmaz" çıkıyordu — karanlık ışıkla karanlık malzeme aynı sayıyı
    /// veriyordu. Sorulacak soru şu: <b>doku deseni görünüyor mu.</b> Ölçü,
    /// her pikselin 3×3 komşu ortalamasından sapmasının ortalaması: ezilmiş
    /// siyahta sıfıra iner, doku okunduğunda yükselir, palete kördür.
    /// </summary>
    public static class FrameMetric
    {
        public struct Stats
        {
            public float Detail;      // ayrıntı enerjisi — ASIL ölçü
            public float Mean, P50, P95;
            public float DarkPct;     // < 30/255
            public float BlownPct;    // > 250/255

            public override string ToString() =>
                $"AYRINTI {Detail:F2} | ort {Mean:F1}/255, p50 {P50:F0}, p95 {P95:F0}"
                + $" | koyu(<30) %{DarkPct * 100f:F1} | patlak(>250) %{BlownPct * 100f:F1}";
        }

        /// <summary>
        /// <paramref name="eye"/> noktasından <paramref name="lookAt"/> yönüne
        /// bakan bir kare alır, isteğe bağlı olarak diske yazar ve ölçer.
        ///
        /// <paramref name="savePath"/> boş bırakılmamalı: sayı neye baktığını
        /// söylemez. Bir kez ölçüm gözü yerden 3 m yukarı çıkmıştı ve sayılar
        /// makul görünüyordu; yanlışı ancak kare gösterdi.
        /// </summary>
        public static Stats Capture(Vector3 eye, Vector3 lookAt, float fov,
                                    string savePath, int w = 480, int h = 270)
        {
            var camGo = new GameObject("__hz_olcum_kamera");
            var cam = camGo.AddComponent<Camera>();
            camGo.AddComponent<HDAdditionalCameraData>();
            camGo.transform.position = eye;
            camGo.transform.LookAt(lookAt);
            cam.fieldOfView = fov;
            cam.nearClipPlane = 0.2f;
            cam.farClipPlane = 4000f;

            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32,
                                       RenderTextureReadWrite.sRGB);
            cam.targetTexture = rt;

            // VOLUME'LARI YENİDEN KAYDET — ölçümün en sinsi hatası buydu.
            //
            // Sahne diskten açıldığında `Volume` bileşenleri henüz Volume
            // yöneticisine kayıtlı olmayabiliyor; editörde bir güncelleme tıkı
            // geçmeden yapılan `Camera.Render()` çağrısı o sahnenin
            // Volume'larını **hiç görmüyor**. Sonuç: aynı sahne, aynı kod, aynı
            // bakış — biri 18,8/255, öteki 73,2/255. Ölçü, ölçtüğü şeye değil
            // ölçüm ANINA bakıyordu.
            //
            // Isınma kareleri de gerekli: fizik tabanlı gökyüzünün ortam
            // sondası tek karede hazır olmuyor.
            foreach (var v in Object.FindObjectsByType<Volume>())
            { v.enabled = false; v.enabled = true; }
            for (int i = 0; i < 8; i++) cam.Render();

            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false, false);
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;

            var px = tex.GetPixels32();
            var grid = new float[w * h];
            for (int i = 0; i < px.Length; i++)
                grid[i] = 0.2126f * px[i].r + 0.7152f * px[i].g + 0.0722f * px[i].b;

            if (!string.IsNullOrEmpty(savePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(savePath));
                File.WriteAllBytes(savePath, tex.EncodeToPNG());
            }

            cam.targetTexture = null;
            Object.DestroyImmediate(camGo);
            Object.DestroyImmediate(tex);
            rt.Release();
            Object.DestroyImmediate(rt);

            return Analyse(grid, w, h);
        }

        public static Stats Analyse(float[] grid, int w, int h)
        {
            float detail = 0f;
            int dn = 0;
            for (int y = 1; y < h - 1; y++)
                for (int x = 1; x < w - 1; x++)
                {
                    float s = 0f;
                    for (int dy = -1; dy <= 1; dy++)
                        for (int dx = -1; dx <= 1; dx++)
                            s += grid[(y + dy) * w + (x + dx)];
                    detail += Mathf.Abs(grid[y * w + x] - s / 9f);
                    dn++;
                }

            var lum = new List<float>(grid);
            lum.Sort();
            float P(float q) => lum[Mathf.Clamp(Mathf.RoundToInt(q * (lum.Count - 1)),
                                                0, lum.Count - 1)];
            float mean = 0f;
            foreach (float v in lum) mean += v;
            mean /= Mathf.Max(1, lum.Count);

            int dark = 0, blown = 0;
            foreach (float v in lum)
            {
                if (v < 30f) dark++;
                if (v > 250f) blown++;
            }

            return new Stats
            {
                Detail = detail / Mathf.Max(1, dn),
                Mean = mean,
                P50 = P(0.50f),
                P95 = P(0.95f),
                DarkPct = dark / (float)lum.Count,
                BlownPct = blown / (float)lum.Count,
            };
        }

        /// <summary>Verilen noktanın arazi üstündeki dünya kotu.</summary>
        public static Vector3 OnGround(Vector3 p)
        {
            foreach (var t in Object.FindObjectsByType<Terrain>())
                p.y = t.SampleHeight(p) + t.transform.position.y;
            return p;
        }

        /// <summary>
        /// Verilen noktada <b>basılan</b> yüzeyin kotu — arazi değil.
        ///
        /// ## Neden ayrı bir alet
        ///
        /// Bir mahallede yaya araziye basmaz: kaldırım şeridi ve evlerin taş
        /// kaidesi arazinin ÜSTÜNDEDİR (kaldırım kesitin en yüksek noktasından
        /// alınır, kaide en yüksek köşeye oturur). Yamaçta bu fark metrelerle
        /// ölçülür.
        ///
        /// Ölçüldü ve kare gösterdi: <see cref="OnGround"/> ile kurulan bir göz
        /// hizası karesi **kaldırımın altında** kaldı — çerçeveyi taşın alt
        /// yüzü doldurdu. Sayılar (ort 133/255, AYRINTI 4,9) gayet makuldü,
        /// çünkü taşın altı da bir dokudur. "Render bir gözlemdir, kanıt
        /// değil"in tersi de doğru: sayı da tek başına kanıt değil.
        ///
        /// Işın araziden 3 m yukarıdan aşağı atılır. Üst sınır bilinçli:
        /// saçak ve dam 3,5 m'nin üstündedir, kaldırım ve kaide altındadır —
        /// yani ışın çatıya takılmaz, basılan yüzeyi bulur. Hiçbir çarpma
        /// yoksa arazi kotuna düşülür.
        /// </summary>
        public static Vector3 OnSurface(Vector3 p)
        {
            const float StartAbove = 3.0f;      // sacak alti, kaldirim ustu
            const float Reach = 9.0f;

            Vector3 g = OnGround(p);
            // Editorde ciziciler kendiliginden esitlenmez; ısın eski
            // konumlara carpardi.
            Physics.SyncTransforms();
            var origin = new Vector3(p.x, g.y + StartAbove, p.z);
            if (Physics.Raycast(origin, Vector3.down, out var hit, Reach))
                return new Vector3(p.x, hit.point.y, p.z);
            return g;
        }
    }
}
