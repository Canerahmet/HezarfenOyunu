using Hezarfen.Zaman;
using UnityEngine;

namespace Hezarfen.Sehir
{
    /// <summary>
    /// <b>Şehrin havası: baca dumanı ve martı.</b>
    ///
    /// Şehir bugüne kadar durgun bir maketti. Ev vardı, sokak vardı,
    /// kalabalık vardı — ama <b>hava boştu</b>. Bir yerin canlı
    /// okunmasını sağlayan şeyin büyük kısmı zeminde değil, zeminin
    /// ÜSTÜNDE olur: bacadan çıkan duman o evde birinin yemek
    /// pişirdiğini söyler, gökyüzündeki martı buranın bir liman
    /// olduğunu söyler. İkisi de bir NPC'den ucuzdur.
    ///
    /// ## Neden tek bileşen, neden her eve parçacık değil
    ///
    /// 10.900 evin her birine bir <c>ParticleSystem</c> koymak kare
    /// bütçesini tek başına yerdi — bütçe ölçüldü, 16,2 ms, tavan
    /// 16,7. Onun yerine <b>havuz</b>: sabit sayıda duman, oyuncuya
    /// en yakın bacalara <b>taşınır</b>. Bu, <see cref="NPCYonetici"/>
    /// gövde havuzuyla aynı fikir; orada ölçüldü, burada tekrarlanıyor.
    ///
    /// ## Duman ne zaman çıkar
    ///
    /// Her saat değil. Ocak sabah ve akşam yanar — ekmek ve akşam
    /// yemeği. Öğlen bacalar çoğunlukla soğuktur. Bu bir oyun ayarı
    /// değil, günün ritmi; <see cref="ZamanSistemi"/> zaten o ritmi
    /// tutuyor ve ona ikinci bir saat eklemiyoruz.
    /// </summary>
    [AddComponentMenu("Hezarfen/Sehir VFX")]
    public class SehirVFX : MonoBehaviour
    {
        [Header("Bağlantılar (boşsa sahnede aranır)")]
        public Transform oyuncu;
        public ZamanSistemi zaman;

        [Header("Baca dumanı")]
        [Tooltip("Aynı anda kaç baca tütebilir.")]
        public int dumanHavuzu = 10;

        [Tooltip("Bu yarıçaptaki damlar aday (m).")]
        public float dumanMenzili = 70f;

        [Header("Martı")]
        [Tooltip("Gökyüzündeki martı sayısı.")]
        public int martiSayisi = 24;

        [Tooltip("Sürünün oyuncuya göre süzüldüğü yükseklik (m).")]
        public float martiYuksekligi = 45f;

        /// <summary>Şu an tüten baca sayısı — ölçüm okur.</summary>
        public int TutenBaca { get; private set; }

        private ParticleSystem[] _duman;
        private Transform[] _marti;
        private float[] _martiFaz;
        private float _sonArama = -99f;

        private void Awake()
        {
            if (oyuncu == null)
            {
                var go = GameObject.Find("OYUNCU");
                if (go != null) oyuncu = go.transform;
            }
            if (zaman == null) zaman = FindAnyObjectByType<ZamanSistemi>();
            Kur();
        }

        private void Kur()
        {
            _duman = new ParticleSystem[Mathf.Max(0, dumanHavuzu)];
            for (int i = 0; i < _duman.Length; i++)
                _duman[i] = DumanYap("DUMAN_" + i);

            _marti = new Transform[Mathf.Max(0, martiSayisi)];
            _martiFaz = new float[_marti.Length];
            for (int i = 0; i < _marti.Length; i++)
            {
                _marti[i] = MartiYap("MARTI_" + i);
                _martiFaz[i] = i * 0.618f * Mathf.PI * 2f;   // altın açı
            }
        }

        /// <summary>
        /// Bir duman kaynağı. Ölçüler odun ateşinden: yavaş yükselir,
        /// genişleyerek soğur, seyrelir. Parçacık sayısı bilerek
        /// düşük — on baca × yüz parçacık bin parçacık eder ve
        /// görünür bir duman için bu yeter.
        /// </summary>
        private ParticleSystem DumanYap(string ad)
        {
            var go = new GameObject(ad);
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop();

            var ana = ps.main;
            ana.startLifetime = 4.5f;
            ana.startSpeed = 0.85f;
            ana.startSize = 0.55f;
            ana.startColor = new Color(0.72f, 0.70f, 0.66f, 0.30f);
            ana.maxParticles = 100;
            ana.simulationSpace = ParticleSystemSimulationSpace.World;
            ana.playOnAwake = false;

            var yay = ps.emission;
            yay.rateOverTime = 11f;

            var sekil = ps.shape;
            sekil.shapeType = ParticleSystemShapeType.Cone;
            sekil.angle = 9f;
            sekil.radius = 0.16f;
            sekil.rotation = new Vector3(-90f, 0f, 0f);   // yukarı

            var boy = ps.sizeOverLifetime;
            boy.enabled = true;
            boy.size = new ParticleSystem.MinMaxCurve(
                1f, AnimationCurve.Linear(0f, 0.6f, 1f, 3.4f));

            var renk = ps.colorOverLifetime;
            renk.enabled = true;
            var gecis = new Gradient();
            gecis.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f),
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.5f, 0.18f),
                    new GradientAlphaKey(0f, 1f),
                });
            renk.color = new ParticleSystem.MinMaxGradient(gecis);

            var ciz = go.GetComponent<ParticleSystemRenderer>();
            ciz.renderMode = ParticleSystemRenderMode.Billboard;
            ciz.shadowCastingMode =
                UnityEngine.Rendering.ShadowCastingMode.Off;
            ciz.receiveShadows = false;
            return ps;
        }

        /// <summary>
        /// Bir martı: iki üçgenli bir kanat çifti. Model değil, çünkü
        /// 45 m yukarıdaki bir kuş ekranda birkaç pikseldir; o
        /// piksellere üçgen harcamak, yerdeki eve harcamamak demektir.
        /// </summary>
        private Transform MartiYap(string ad)
        {
            var go = new GameObject(ad);
            go.transform.SetParent(transform, false);
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = MartiAgi();
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = MartiMalzeme();
            mr.shadowCastingMode =
                UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return go.transform;
        }

        private static Mesh _martiAgi;

        private static Mesh MartiAgi()
        {
            if (_martiAgi != null) return _martiAgi;
            // Kanat açıklığı ~1,2 m — gümüş martı için gerçek ölçü.
            var m = new Mesh { name = "SM_Marti" };
            m.vertices = new[]
            {
                new Vector3(0f, 0f, 0.06f),
                new Vector3(-0.60f, 0.10f, -0.06f),
                new Vector3(0f, 0f, -0.10f),
                new Vector3(0f, 0f, 0.06f),
                new Vector3(0f, 0f, -0.10f),
                new Vector3(0.60f, 0.10f, -0.06f),
            };
            m.triangles = new[] { 0, 1, 2, 3, 4, 5 };
            m.RecalculateNormals();
            m.RecalculateBounds();
            _martiAgi = m;
            return m;
        }

        private static Material _martiMalzeme;

        private static Material MartiMalzeme()
        {
            if (_martiMalzeme != null) return _martiMalzeme;
            var s = Shader.Find("HDRP/Unlit") ?? Shader.Find("Unlit/Color");
            _martiMalzeme = new Material(s) { name = "M_Marti" };
            var beyaz = new Color(0.93f, 0.93f, 0.95f);
            if (_martiMalzeme.HasProperty("_UnlitColor"))
                _martiMalzeme.SetColor("_UnlitColor", beyaz);
            else if (_martiMalzeme.HasProperty("_Color"))
                _martiMalzeme.SetColor("_Color", beyaz);
            return _martiMalzeme;
        }

        /// <summary>
        /// Ocağın yandığı saatler. Sabah ekmek, akşam yemek; öğlen
        /// baca soğuktur. Dönüş 0..1 — bacaların kaçta kaçı tüter.
        ///
        /// Saf ve statik: test bunu sahne kurmadan sorabilsin diye.
        /// </summary>
        public static float OcakYogunlugu(float saat)
        {
            float sabah = Mathf.Exp(-Mathf.Pow((saat - 6.5f) / 1.6f, 2f));
            float aksam = Mathf.Exp(-Mathf.Pow((saat - 18.0f) / 1.9f, 2f));
            // Taban 0,12: gece bile bir iki ocak külünü tüttürür.
            return Mathf.Clamp01(0.12f + 0.88f * Mathf.Max(sabah, aksam));
        }

        /// <summary>Martılar bu saatlerde uçmaz.</summary>
        public static bool MartiGecesi(float saat) =>
            saat < 5.0f || saat > 20.5f;

        private void Update()
        {
            if (oyuncu == null) return;
            float saat = zaman != null ? zaman.saat : 12f;
            Bacalar(saat);
            Martilar(saat);
        }

        private void Bacalar(float saat)
        {
            // Dam aramak pahalı; saniyede bir yeter — baca kaçmaz.
            if (Time.time - _sonArama < 1.0f) return;
            _sonArama = Time.time;
            Yerlestir(saat);
        }

        private void Yerlestir(float saat)
        {
            if (_duman == null || _duman.Length == 0) return;
            int istenen = Mathf.RoundToInt(
                _duman.Length * OcakYogunlugu(saat));

            // DAM ARAMAK ICIN EV LISTESI GEREKMIYOR.
            //
            // Oyuncunun cevresinde yukaridan asagi isin atiyoruz;
            // carptigi yer damdir. Ev listesi tutmak, semt akisiyla
            // birlikte ikinci bir sahiplik yaratirdi — bu projede
            // tekrar tekrar bedelini odedigimiz kusur. Ustelik bu
            // yontem ic mekanda kendiliginden susar: isin tavana
            // carpar ve tavan oyuncunun 2,5 m ustunde degildir.
            int kondu = 0;
            int tohum = Mathf.FloorToInt(oyuncu.position.x) * 73856093
                        ^ Mathf.FloorToInt(oyuncu.position.z) * 19349663;
            var rng = new System.Random(tohum);

            for (int deneme = 0;
                 deneme < _duman.Length * 6 && kondu < istenen;
                 deneme++)
            {
                float a = (float)rng.NextDouble() * Mathf.PI * 2f;
                float r = 12f + (float)rng.NextDouble()
                                * Mathf.Max(1f, dumanMenzili - 12f);
                float x = oyuncu.position.x + Mathf.Cos(a) * r;
                float z = oyuncu.position.z + Mathf.Sin(a) * r;
                var tepe = new Vector3(x, oyuncu.position.y + 40f, z);

                if (!Physics.Raycast(tepe, Vector3.down, out var vurus, 80f,
                                     ~0, QueryTriggerInteraction.Ignore))
                    continue;
                if (vurus.collider is TerrainCollider) continue;
                if (vurus.point.y < oyuncu.position.y + 2.5f) continue;

                var ps = _duman[kondu];
                ps.transform.position = vurus.point + Vector3.up * 0.35f;
                if (!ps.isPlaying) ps.Play();
                kondu++;
            }

            for (int i = kondu; i < _duman.Length; i++)
                if (_duman[i].isPlaying) _duman[i].Stop();
            TutenBaca = kondu;
        }

        private void Martilar(float saat)
        {
            if (_marti == null || _marti.Length == 0) return;

            // Martılar gece yatar. Karanlıkta dönen bir kuş "kuş var"
            // demez, "bir şey yanlış" der.
            bool gece = MartiGecesi(saat);
            float t = Time.time;

            for (int i = 0; i < _marti.Length; i++)
            {
                var tr = _marti[i];
                if (gece)
                {
                    if (tr.gameObject.activeSelf)
                        tr.gameObject.SetActive(false);
                    continue;
                }
                if (!tr.gameObject.activeSelf) tr.gameObject.SetActive(true);

                // Geniş, yavaş bir termik dairesi — martılar kanat
                // çırpmadan süzülür ve bu onların imzasıdır.
                float faz = _martiFaz[i];
                float yaricap = 28f + (i % 5) * 11f;
                float hiz = 0.11f + (i % 7) * 0.012f;
                float a = faz + t * hiz;
                var merkez = oyuncu.position
                             + new Vector3(Mathf.Cos(faz) * 40f, 0f,
                                           Mathf.Sin(faz) * 40f);
                var p = merkez + new Vector3(
                    Mathf.Cos(a) * yaricap,
                    martiYuksekligi + Mathf.Sin(a * 0.7f + faz) * 6f,
                    Mathf.Sin(a) * yaricap);
                var ileri = new Vector3(-Mathf.Sin(a), 0f, Mathf.Cos(a));
                tr.SetPositionAndRotation(
                    p,
                    Quaternion.LookRotation(ileri, Vector3.up)
                    * Quaternion.Euler(0f, 0f, Mathf.Sin(a * 3f) * 12f));
            }
        }
    }
}
