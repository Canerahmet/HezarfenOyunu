using System.Collections;
using Hezarfen.Player;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Hezarfen.Tests
{
    /// <summary>
    /// <b>Ayak yokuşta zemine oturuyor mu.</b>
    ///
    /// Bu testin ölçtüğü şey doğrudan görünen kusurdur: eğimli bir
    /// yüzeyde duran karakterin bir ayağı havada, öteki gömülü kalır.
    /// Klipler düz zeminde çekilmiştir ve o düzlem yokuşta yanlış
    /// yerdedir.
    ///
    /// ## Neden bir <i>ölçüm</i> testi, "bileşen var mı" testi değil
    ///
    /// "AyakIK bileşeni takılı mı" diye sormak kolay olurdu ve hiçbir
    /// şey söylemezdi: bu projede aynı sınıf hata yaşandı — bileşen
    /// yerindeydi, animatörün IK pass'i kapalıydı ve
    /// <c>OnAnimatorIK</c> hiç çağrılmadı. Hata yok, log yok, sadece
    /// ayaklar yerde değil. O yüzden test <b>tabanın zemine olan
    /// mesafesini</b> ölçer; hangi ayarın eksik olduğu umurunda değildir.
    /// </summary>
    public class AyakIKTests
    {
        private const string PrefabYolu =
            "Assets/_Project/Art/Prefabs/PF_Hezarfen_Sivil.prefab";

        /// <summary>Kabul eşiği: taban zeminden bu kadar sapabilir (m).</summary>
        private const float Esik = 0.05f;

        private GameObject _yokus;
        private GameObject _ornek;

        [SetUp]
        public void Kur()
        {
            // 18 derecelik bir rampa. Galata'nin sokak egimleri bu
            // aralikta; duz bir zemin bu testi anlamsiz kilardi.
            //
            // EGIM Z EKSENINDE, X'te DEGIL. Once X ekseninde (ileri-geri)
            // egiliyordu ve duruş klibi degisince kontrol sondu: yeni
            // duruşta ayaklar YAN YANA duruyor, yani ileri-geri egim iki
            // ayagi neredeyse ayni kota koyuyor — olculen fark 1,5 cm,
            // kapinin ucte biri. Ayaklarin gercekten ayrildigi eksen
            // X'tir (±0,19 m); egimi oraya cevirince fark 0,38 × tan18°
            // ≈ 12 cm olur ve IK'nin isi gorunur hale gelir.
            _yokus = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _yokus.name = "TestYokusu";
            _yokus.transform.position = new Vector3(0f, -0.5f, 0f);
            _yokus.transform.localScale = new Vector3(20f, 1f, 20f);
            _yokus.transform.rotation = Quaternion.Euler(0f, 0f, 18f);
        }

        [TearDown]
        public void Sok()
        {
            if (_ornek != null) Object.DestroyImmediate(_ornek);
            if (_yokus != null) Object.DestroyImmediate(_yokus);
        }

        [UnityTest]
        public IEnumerator TheFootRestsOnTheSlopeNotOnTheClipsFlatPlane()
        {
            var pf = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabYolu);
            Assert.IsNotNull(pf, $"{PrefabYolu} yok.");

            _ornek = Object.Instantiate(pf);
            var anim = _ornek.GetComponentInChildren<Animator>();
            Assert.IsNotNull(anim, "Prefabda Animator yok.");
            Assert.IsTrue(anim.isHuman,
                "Avatar Humanoid degil — ayak IK Humanoid gerektirir.");
            Assert.IsNotNull(anim.runtimeAnimatorController,
                "Animator kontrolcusu yok — once Boru Hatti -> Animator "
                + "kontrolcusunu uret.");

            // Karakteri rampanin ortasina, yuzeyin hemen ustune koy.
            Vector3 tepe = new Vector3(0f, 3f, 0f);
            Assert.IsTrue(
                Physics.Raycast(tepe, Vector3.down, out RaycastHit v, 10f),
                "Test rampasi isinla bulunamadi.");
            _ornek.transform.position = v.point;
            // KAMERASIZ SAHNEDE ANIMATOR CALISMAZ.
            //
            // Varsayilan culling "ekranda degilse guncelleme"dir ve test
            // sahnesinde kamera yoktur — yani animasyon HIC ilerlemez.
            // Bu sessizce yanlis olcum uretir: ayaklar govdeyle birlikte
            // gider ve "kayma" diye olculen sey aslinda govdenin yoludur.
            // Ilk kayma olcumum tam bunu yapti (yurume 16,4 cm, kosma
            // 34,2 cm — ikisi de hiza oranti, yani klip degil govde).
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            var ik = anim.GetComponent<AyakIK>()
                     ?? anim.gameObject.AddComponent<AyakIK>();
            ik.yumusatma = 0f;   // testte yumusatmayi bekleyecek zaman yok

            // Animatorun ve IK'nin oturmasi icin birkac kare.
            for (int i = 0; i < 12; i++) yield return null;

            float sol = TabanFarki(anim, HumanBodyBones.LeftFoot, ik);
            float sag = TabanFarki(anim, HumanBodyBones.RightFoot, ik);

            Assert.LessOrEqual(Mathf.Abs(sol), Esik,
                $"SOL ayak zeminden {sol:+0.000;-0.000} m sapiyor "
                + $"(esik {Esik:0.00} m). Eksi = havada, arti = gomulu. "
                + "Animator katmaninin IK pass'i kapali olabilir.");
            Assert.LessOrEqual(Mathf.Abs(sag), Esik,
                $"SAG ayak zeminden {sag:+0.000;-0.000} m sapiyor "
                + $"(esik {Esik:0.00} m).");
        }

        /// <summary>
        /// <b>Testin kendisi bir şey ölçüyor mu.</b>
        ///
        /// Yukarıdaki test IK <i>kapalıyken de</i> yeşil kalıyorsa hiçbir
        /// şey söylemiyordur. Bu projede tam olarak o oldu: yön testi
        /// yazıldı, yeşildi ve model 180 derece tersti — ölçüt yönü
        /// okumuyordu. O yüzden burada IK kapatılıp <b>kusurun geri
        /// geldiği</b> gösteriliyor. Bu test kırmızıya dönerse anlamı
        /// "IK bozuldu" değil, <b>"ölçüm bozuk"</b>tur.
        /// </summary>
        [UnityTest]
        public IEnumerator WithoutIKTheFootLeavesTheSlopeMeasurably()
        {
            var pf = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabYolu);
            Assert.IsNotNull(pf, $"{PrefabYolu} yok.");

            _ornek = Object.Instantiate(pf);
            var anim = _ornek.GetComponentInChildren<Animator>();
            Assert.IsNotNull(anim, "Prefabda Animator yok.");

            Assert.IsTrue(
                Physics.Raycast(new Vector3(0f, 3f, 0f), Vector3.down,
                                out RaycastHit v, 10f),
                "Test rampasi isinla bulunamadi.");
            _ornek.transform.position = v.point;

            var ik = anim.GetComponent<AyakIK>();
            if (ik != null) ik.Etkin = false;

            for (int i = 0; i < 12; i++) yield return null;

            float sol = TabanFarki(anim, HumanBodyBones.LeftFoot, null);
            float sag = TabanFarki(anim, HumanBodyBones.RightFoot, null);
            float enBuyuk = Mathf.Max(Mathf.Abs(sol), Mathf.Abs(sag));

            Assert.Greater(enBuyuk, Esik,
                $"IK KAPALIYKEN de ayaklar zeminde ({sol:+0.000;-0.000} / "
                + $"{sag:+0.000;-0.000} m). Yani ustteki test IK'yi degil "
                + "baska bir seyi olcuyor — rampa duz kalmis ya da poz "
                + "hic uygulanmamis olabilir.");
        }

        /// <summary>
        /// Ayak tabanının zemine olan farkı (m). Eksi = havada.
        ///
        /// Taban kotu, ayak kemiğinin kotundan modelin kendi ofseti
        /// çıkarılarak bulunur; ofset karakterin kökünden ölçülür, elle
        /// yazılmaz.
        /// </summary>
        private static float TabanFarki(Animator anim, HumanBodyBones kemik,
                                        AyakIK _ = null)
        {
            Transform t = anim.GetBoneTransform(kemik);
            Assert.IsNotNull(t, $"{kemik} kemigi yok.");

            float ofset = OlculenOfset(anim);
            float taban = t.position.y - ofset;

            Vector3 bas = new Vector3(t.position.x, t.position.y + 1f,
                                      t.position.z);
            Assert.IsTrue(
                Physics.Raycast(bas, Vector3.down, out RaycastHit v, 4f),
                $"{kemik} altinda zemin bulunamadi.");
            return taban - v.point.y;
        }

        /// <summary>
        /// Ayak kemiğinin tabandan yüksekliği — <see cref="AyakIK"/> ile
        /// <b>aynı</b> tanım. İki yerde iki farklı tanım olsaydı test,
        /// ölçtüğü şeyi değil kendi varsayımını doğrulardı.
        /// </summary>
        private static float OlculenOfset(Animator anim)
        {
            var pf = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabYolu);
            var temiz = Object.Instantiate(pf);
            try
            {
                var a = temiz.GetComponentInChildren<Animator>();
                float kok = temiz.transform.position.y;
                return Mathf.Min(
                    a.GetBoneTransform(HumanBodyBones.LeftFoot).position.y - kok,
                    a.GetBoneTransform(HumanBodyBones.RightFoot).position.y - kok);
            }
            finally
            {
                Object.DestroyImmediate(temiz);
            }
        }
    }
}
