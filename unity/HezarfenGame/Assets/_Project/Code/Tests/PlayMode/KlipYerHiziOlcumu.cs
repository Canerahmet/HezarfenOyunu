using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Hezarfen.Player;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Hezarfen.Tests
{
    /// <summary>
    /// <b>Kliplerin yer hızını ölçer ve kaydı YAZAR.</b>
    ///
    /// Bu bir kapı testi değil, bir <b>alet</b>: karışım ağacındaki
    /// oynatma çarpanının dayandığı sayıyı üretir. Kapıyı
    /// <see cref="AyakKaymasiTests"/> tutar.
    ///
    /// ## Neden burada, Editör'de değil
    ///
    /// Sayı üç kez yanlış yerden alınmaya çalışıldı ve üçü de ölçüldü:
    ///
    /// | deneme | sonuç | neden yanlış |
    /// |---|---|---|
    /// | Blender'da kök yolu | yürüme 1,786 m/s | Mixamo'nun iskeleti; yeniden hedefleme adımı HEDEFİN oranlarıyla ölçekler |
    /// | Editör'de `SampleAnimation` | yürüme 0,133 m/s | kök hareketini nesnenin kendisine uyguluyor, ayak köke göre duruyor |
    /// | Editör'de `Animator.Update` | 0,000 m/s | Edit kipinde Animator değerlendirmiyor |
    ///
    /// Doğrusu: gövde <b>sabit</b> tutulur, klip oynatılır, basan ayağın
    /// <b>dünyadaki</b> hızı okunur. Oyun karakteri o hızda yürütülürse
    /// ayak dünyada durur — kaymanın tanımı budur.
    ///
    /// Ölçüm <c>art/mixamo/meta.json</c>'a <c>unity_hiz_ms</c> olarak
    /// yazılır; <c>AnimatorKur</c> çarpanı oradan türetir. Tek sahip.
    /// </summary>
    public class KlipYerHiziOlcumu
    {
        private const string PrefabYolu =
            "Assets/_Project/Art/Prefabs/PF_Hezarfen_Sivil.prefab";

        private GameObject _zemin, _ornek;

        [SetUp]
        public void Kur()
        {
            _zemin = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _zemin.name = "OlcumZemini";
            _zemin.transform.position = new Vector3(0f, -0.5f, 0f);

            // ZAMAN ADIMI SABITLENIR.
            //
            // Test kosucusunda Time.deltaTime duzensizdir ve animasyon
            // her turda baska bir noktada orneklenir. Olcum bunu
            // gosterdi: ayni klip icin arka arkaya 2,34 ve 0,67 m/s.
            // Bir cetvel her okumada baska sey soyluyorsa esik
            // tartisilamaz. captureDeltaTime her kareyi tam 1/240 s
            // yapar; olcum tekrarlanabilir olur.
            //
            // 240 Hz, 60 degil: 6 m/s'lik kosuda govde kare basina 10 cm
            // gider ve temasin orta ani 60 Hz'de bir-iki kareye siger.
            // Olcum o yuzden basisin ortasini degil dokunus/kalkis
            // anlarini yakaliyordu ve turdan tura 0,67 ile 2,05 m/s
            // arasinda zipliyordu. Dort kat sik ornekleme gercek bir
            // plato birakir.
            Time.captureDeltaTime = 1f / 240f;
            _zemin.transform.localScale = new Vector3(40f, 1f, 40f);
        }

        [TearDown]
        public void Sok()
        {
            Time.captureDeltaTime = 0f;
            if (_ornek != null) Object.DestroyImmediate(_ornek);
            if (_zemin != null) Object.DestroyImmediate(_zemin);
        }

        [UnityTest]
        public IEnumerator MeasureTheGroundSpeedEachClipActuallyDelivers()
        {
            var pf = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabYolu);
            Assert.IsNotNull(pf, $"{PrefabYolu} yok.");
            _ornek = Object.Instantiate(pf);
            _ornek.transform.position = Vector3.zero;

            var anim = _ornek.GetComponentInChildren<Animator>();
            Assert.IsNotNull(anim, "Animator yok.");
            Assert.IsNotNull(anim.runtimeAnimatorController, "Kontrolcu yok.");
            anim.applyRootMotion = false;
            // Kamerasiz test sahnesinde her sey ekran disidir; varsayilan
            // culling animatoru hic degerlendirmeyebilir.
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var ik = anim.GetComponent<AyakIK>();
            if (ik != null) ik.Etkin = false;

            var sonuc = new Dictionary<string, float>();
            var rapor = new StringBuilder("KLIP YER HIZI (govde SABIT)\n");

            foreach (var (ad, hiz) in new[]
            {
                ("Yurume", WalkController.VarsayilanYurume),
                ("Kosma", WalkController.VarsayilanKosma),
            })
            {
                anim.SetBool("ucuyor", false);
                anim.SetFloat("hiz", hiz);
                for (int i = 0; i < 480; i++) yield return null;   // karisim otursun

                var sol = anim.GetBoneTransform(HumanBodyBones.LeftFoot);
                var sag = anim.GetBoneTransform(HumanBodyBones.RightFoot);

                // Orta durus TEK gecişte, tek tanimla — gerekce OrtaDurus'ta.
                var ornekler = new List<OrtaDurus.Ornek>();
                Vector3 ps = sol.position, pg = sag.position;
                for (int i = 0; i < 900; i++)
                {
                    yield return null;
                    float dt = Time.deltaTime;
                    if (dt <= 0.0001f) { ps = sol.position; pg = sag.position; continue; }
                    float kok = _ornek.transform.position.y;
                    float hS = sol.position.y - kok, hG = sag.position.y - kok;
                    bool solAlcak = hS <= hG;
                    Vector3 a = solAlcak ? ps : pg;
                    Vector3 b = solAlcak ? sol.position : sag.position;
                    ps = sol.position; pg = sag.position;
                    a.y = 0f; b.y = 0f;
                    // EKSENEL bilesen: yanal salinim kayma degildir.
                    Vector3 ileri = _ornek.transform.forward;
                    ileri.y = 0f;
                    ileri.Normalize();
                    ornekler.Add(new OrtaDurus.Ornek
                    {
                        yukseklik = solAlcak ? hS : hG,
                        hiz = Mathf.Abs(Vector3.Dot(b - a, ileri)) / dt,
                    });
                }

                float teslim = OrtaDurus.Hiz(ornekler, out int orneksayi);
                Assert.GreaterOrEqual(teslim, 0f,
                    $"{ad}: orta durus yakalanamadi ({orneksayi} ornek).");

                // CARPANIN ETKISI GERI BOLUNUR.
                //
                // Olcum su anki karisim agacindaki oynatma carpaniyla
                // yapiliyor; boldugumuzde klibin HAM yeniden hedeflenmis
                // hizi kalir. Bolmeseydik kayit kendi carpanini olcer ve
                // her turda baska bir sayi verirdi.
                float carpan = MevcutCarpan(ad);
                float ham = carpan > 0.01f ? teslim / carpan : teslim;
                sonuc[ad] = ham;

                rapor.AppendLine(
                    $"  {ad,-8} esik {hiz:0.0}  teslim {teslim:0.000} m/s  "
                    + $"carpan {carpan:0.000}  ->  HAM {ham:0.000} m/s  "
                    + $"(gereken carpan {(ham > 0.01f ? hiz / ham : 0f):0.000})");
            }

            Debug.Log("[Hezarfen] " + rapor);
            Yaz(sonuc);

            foreach (var kv in sonuc)
            {
                Assert.Greater(kv.Value, 0.05f,
                    $"{kv.Key}: yer hizi olculemedi ({kv.Value:0.000} m/s) — "
                    + "klip oynamiyor olabilir.");
            }
        }

        /// <summary>
        /// Karışım ağacında bu klibe şu an uygulanan oynatma çarpanı.
        /// </summary>
        private static float MevcutCarpan(string klipAdi)
        {
            var ac = AssetDatabase.LoadAssetAtPath<
                UnityEditor.Animations.AnimatorController>(
                "Assets/_Project/Art/Animation/AC_Hezarfen.controller");
            if (ac == null) return 1f;
            foreach (var st in ac.layers[0].stateMachine.states)
            {
                if (!(st.state.motion is UnityEditor.Animations.BlendTree bt))
                    continue;
                foreach (var c in bt.children)
                    if (c.motion != null && c.motion.name == klipAdi)
                        return c.timeScale;
            }
            return 1f;
        }

        /// <summary>
        /// Ölçümü <c>art/mixamo/meta.json</c>'a <c>unity_hiz_ms</c> olarak
        /// yazar. Alan yoksa eklenir, varsa güncellenir.
        ///
        /// JSON'u elle düzenlemek yerine buradan yazmak, sayının tek
        /// sahibini korur: ölçen taraf yazar.
        /// </summary>
        private static void Yaz(Dictionary<string, float> olcum)
        {
            string kok = Path.GetFullPath(
                Path.Combine(Application.dataPath, "../../.."));
            string yol = Path.Combine(kok, "art/mixamo/meta.json");
            if (!File.Exists(yol))
            {
                Debug.LogWarning($"[Hezarfen] {yol} yok — olcum yazilamadi.");
                return;
            }

            string metin = File.ReadAllText(yol);
            foreach (var kv in olcum)
            {
                string deger = kv.Value.ToString(
                    "0.0000", CultureInfo.InvariantCulture);
                string desen = "\"ad\": \"" + kv.Key + "\"";
                int i = metin.IndexOf(desen);
                if (i < 0) continue;

                const string alan = "\"unity_hiz_ms\":";
                int sonrakiKlip = metin.IndexOf("\"dosya\":", i);
                if (sonrakiKlip < 0) sonrakiKlip = metin.Length;
                int mevcut = metin.IndexOf(alan, i);
                if (mevcut >= 0 && mevcut < sonrakiKlip)
                {
                    int son = metin.IndexOfAny(new[] { ',', '\n', '}' },
                                               mevcut + alan.Length);
                    metin = metin.Substring(0, mevcut) + alan + " " + deger
                            + metin.Substring(son);
                }
                else
                {
                    metin = metin.Insert(i + desen.Length,
                        ",\n  " + alan + " " + deger);
                }
            }
            File.WriteAllText(yol, metin);
            Debug.Log($"[Hezarfen] unity_hiz_ms yazildi -> {yol}");
        }
    }
}
