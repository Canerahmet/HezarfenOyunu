using System.Collections;
using System.Collections.Generic;
using Hezarfen.Player;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Hezarfen.Tests
{
    /// <summary>
    /// <b>Ayak kayması — oyunda ölçülür, beyanda değil.</b>
    ///
    /// Ayak kayması bu projede yıllardır bir kapı ölçüsü (&lt; 5 cm) ama
    /// bugüne kadar <b>Blender'da</b> ölçüldü ve <c>animasyon.json</c>'a
    /// yazıldı; <c>KarakterTests</c> de o sayıyı JSON'dan okudu. Yani test,
    /// oyunun ölçüsünü değil <b>üreticinin kendi beyanını</b> doğruluyordu.
    ///
    /// Beyanla gerçeğin ayrıldığı gün geldi: taban gövde MPFB2'ye geçince
    /// iskeletin bacak uzunlukları değişti, klipler ise eski iskeletten
    /// çözülmüş hâlinde kaldı. Sonra klipler tümden Mixamo'ya geçti ve
    /// artık üretici bile biz değiliz. JSON'daki sayı iki kez anlamını
    /// yitirdi ve hiçbir test kırmızı yanmadı.
    ///
    /// Burada ölçülen şey basit ve doğrudan: <b>yere basan ayağın dünyada
    /// ne kadar kaydığı.</b> Gövde 2,2 m/s ileri gider, klip ayağı aynı
    /// hızda geriye çeker; ikisi eşitse basılı ayak dünyada durur. Fark
    /// varsa ayak kayar ve gözle görülür.
    ///
    /// Bu cetvel kliplerden <b>önce</b> kuruldu — ölçecek şeyi
    /// beklemeden. Bu oturumda bunun tersi iki kez pahalıya mal oldu.
    /// </summary>
    public class AyakKaymasiTests
    {
        private const string PrefabYolu =
            "Assets/_Project/Art/Prefabs/PF_Hezarfen_Sivil.prefab";

        /// <summary>Kapı: bir basış boyunca izin verilen kayma (m).</summary>
        private const float Esik = 0.05f;

        /// <summary>
        /// Kapı: orta duruşta basan ayağın kayması, <b>yol hızının
        /// oranı</b> olarak.
        ///
        /// ## Neden oran, neden mesafe değil
        ///
        /// Tarihsel kapı "bir basış boyunca 5 cm"di ve bu bir
        /// <b>mesafe</b>dir. Ama basış süresi yürüyüşte ~0,6 s,
        /// koşuda ~0,2 s — aynı 5 cm iki gaitte iki farklı hıza karşılık
        /// gelir. Tek bir hız eşiği yazmak, birinde gereksiz sıkı
        /// ötekinde anlamsız gevşek olurdu.
        ///
        /// %5 her iki gaitte de yaklaşık <b>6 cm</b> eder:
        /// 0,05 × 2,2 × 0,6 s ≈ 6,6 cm · 0,05 × 6,0 × 0,2 s ≈ 6 cm.
        /// Yani tarihsel kapının anlamı korunuyor, yalnız gaitten
        /// bağımsız yazılıyor.
        ///
        /// ## Neden mesafe doğrudan ölçülmüyor
        ///
        /// Ölçmek için basışın başını ve sonunu bilmek gerekir, o da
        /// bir temas eşiği demektir — ve bu oturumda temas eşiğinin
        /// kendisi üç kez yanlış ölçtü (gerekçe <see cref="OrtaDurus"/>).
        /// Orta duruş hızı eşik istemez.
        /// </summary>
        private const float KaymaOrani = 0.05f;

        private GameObject _zemin;
        private GameObject _ornek;

        [SetUp]
        public void Kur()
        {
            _zemin = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _zemin.name = "TestZemini";
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
            _zemin.transform.localScale = new Vector3(80f, 1f, 80f);
        }

        [TearDown]
        public void Sok()
        {
            Time.captureDeltaTime = 0f;
            if (_ornek != null) Object.DestroyImmediate(_ornek);
            if (_zemin != null) Object.DestroyImmediate(_zemin);
        }

        [UnityTest]
        public IEnumerator TheWalkClipCarriesTheGroundSpeedTheControllerUses()
        {
            yield return Olc(WalkController.VarsayilanYurume, "YURUME");
        }

        [UnityTest]
        public IEnumerator TheRunClipCarriesTheGroundSpeedTheControllerUses()
        {
            yield return Olc(WalkController.VarsayilanKosma, "KOSMA");
        }

        /// <summary>
        /// Karakteri `hiz` m/s ileri yürütür ve basılı ayağın dünyadaki
        /// kaymasını ölçer.
        /// </summary>
        private IEnumerator Olc(float hiz, string etiket)
        {
            var pf = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabYolu);
            Assert.IsNotNull(pf, $"{PrefabYolu} yok.");

            _ornek = Object.Instantiate(pf);
            _ornek.transform.position = Vector3.zero;

            var anim = _ornek.GetComponentInChildren<Animator>();
            Assert.IsNotNull(anim, "Prefabda Animator yok.");
            Assert.IsTrue(anim.isHuman, "Avatar Humanoid degil.");
            Assert.IsNotNull(anim.runtimeAnimatorController,
                "Animator kontrolcusu yok — once Boru Hatti -> Animator "
                + "kontrolcusunu uret.");

            // Kok hareketi KAPALI: yer degistirmeyi kontrolcu yazar ve
            // klipteki kok XZ ice aktarmada poza pisirildi.
            anim.applyRootMotion = false;
            // Kamerasiz test sahnesinde varsayilan culling animatoru
            // durdurabilir; olcum sessizce govdenin yolunu okurdu.
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            // AYAK IK KAPALI. IK ayagi zemine ceker ve tam olarak
            // olcmeye calistigimiz kaymayi GIZLER. Burada klip ile
            // kontrolcunun anlasip anlasmadigina bakiyoruz; IK'nin
            // kendi testi ayri (AyakIKTests).
            var ik = anim.GetComponent<AyakIK>();
            if (ik != null) ik.Etkin = false;

            anim.SetFloat("hiz", hiz);
            anim.SetBool("ucuyor", false);

            Transform sol = anim.GetBoneTransform(HumanBodyBones.LeftFoot);
            Transform sag = anim.GetBoneTransform(HumanBodyBones.RightFoot);
            Assert.IsNotNull(sol); Assert.IsNotNull(sag);

            for (int i = 0; i < 480; i++)
            {
                _ornek.transform.position += Vector3.forward * hiz * Time.deltaTime;
                yield return null;
            }

            // Orta durus TEK gecişte, tek tanimla — gerekce OrtaDurus'ta.
            var ornekler = new List<OrtaDurus.Ornek>();
            Vector3 ps = sol.position, pg = sag.position;
            for (int i = 0; i < 900; i++)
            {
                _ornek.transform.position += Vector3.forward * hiz * Time.deltaTime;
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

            float kayma = OrtaDurus.Hiz(ornekler, out int orneksayi);
            Assert.GreaterOrEqual(kayma, 0f,
                $"{etiket}: orta durus yakalanamadi ({orneksayi} ornek, "
                + $"{ornekler.Count} kare). Klip oynamiyor olabilir.");

            Debug.Log($"[Hezarfen] KAYMA {etiket}: {OrtaDurus.Dagilim}");

            float esik = hiz * KaymaOrani;
            Assert.LessOrEqual(kayma, esik,
                $"{etiket}: orta duruşta basan ayak dunyada {kayma:0.000} m/s "
                + $"kayiyor — yol hizinin %{kayma / hiz * 100f:0.0}'i "
                + $"(kapi %{KaymaOrani * 100f:0} = {esik:0.000} m/s, "
                + $"{orneksayi} ornek). "
                + "Klibin yer hizi ile kontrolcunun hizi tutmuyor — "
                + "karisim agacindaki oynatma carpanini duzelt "
                + "(KlipYerHiziOlcumu bu sayiyi olcer).");
        }
    }
}
