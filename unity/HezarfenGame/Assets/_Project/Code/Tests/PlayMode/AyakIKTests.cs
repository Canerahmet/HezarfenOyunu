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

        /// <summary>
        /// Rampanın kurulduğu yer — <b>başka hiçbir testin dokunmadığı</b>
        /// bir köşe.
        ///
        /// Rampa önce başlangıç noktasına kuruluyordu ve orası kalabalık:
        /// başka test sınıflarının düz zeminleri de oraya kuruluyor,
        /// sahne aralarında hepsi silinmiyor ve ışın önce onlardan birine
        /// çarpıyor. Sonuç ölçüldü: negatif kontrol IK <i>kapalıyken</i>
        /// ±0,000 m dedi — oysa 18°'lik rampada iki ayak arası
        /// 0,382 × tan18° ≈ 12 cm olmalıydı. Yani test rampayı değil
        /// başkasının düz zeminini ölçüyordu.
        ///
        /// Adla temizlemeyi denemek (o da burada yazılıydı) yanlış cinsten
        /// bir çözümdü: her yeni test sınıfının zeminine ad uydurmak
        /// gerekirdi ve unutulan ilk adda ölçüm sessizce bozulurdu.
        /// Uzaklık böyle bir anlaşma istemez.
        /// </summary>
        private const float Uzak = 2000f;

        private GameObject _yokus;
        private GameObject _ornek;

        [SetUp]
        public void Kur()
        {
            // Rampanin kendisi `Rampa()`'da kuruluyor; buradaki is
            // yalniz devralinan zeminleri temizlemek.
            //
            // EGIM Z EKSENINDE, X'te DEGIL. Once X ekseninde (ileri-geri)
            // egiliyordu ve duruş klibi degisince kontrol sondu: yeni
            // duruşta ayaklar YAN YANA duruyor, yani ileri-geri egim iki
            // ayagi neredeyse ayni kota koyuyor — olculen fark 1,5 cm,
            // kapinin ucte biri. Ayaklarin gercekten ayrildigi eksen
            // X'tir (±0,19 m); egimi oraya cevirince fark 0,38 × tan18°
            // ≈ 12 cm olur ve IK'nin isi gorunur hale gelir.
            // BASKA TESTIN ZEMINI ORTADA KALMASIN.
            //
            // Izole kosunca iki test de geciyordu, tam takimda negatif
            // kontrol soneuyordu: olculen sapma 12 cm yerine 4,7 cm
            // cikiyordu. Sebep artik geometriydi — AyakKaymasiTests'in
            // 80x80 DUZ zemini sahnede kaliyor ve karakter rampaya degil
            // ona basiyor. Duz zeminde IK'siz ayak zaten yerde olur, yani
            // kontrol "kusur geri gelmedi" der ve YANLIS yere bakar.
            //
            // Testler birbirinin sahnesini miras almamali; devralinan
            // her sey once temizlenir.
            foreach (var eski in Object.FindObjectsByType<GameObject>(
                         FindObjectsSortMode.None))
            {
                if (eski.name.EndsWith("Zemini") || eski.name == "TestYokusu")
                    Object.DestroyImmediate(eski);
            }

            Rampa();
        }

        /// <summary>
        /// Işını <b>rampanın kendi çarpıştırıcısına</b> atar.
        ///
        /// <c>Physics.Raycast</c> sahnedeki her şeye çarpar ve test
        /// sahnesinde ne olduğu testin denetiminde değil: bir kez
        /// başka bir sınıfın düz zeminine çarptı ve ölçüm sessizce
        /// yanlış oldu, bir kez de hiçbir şeye çarpmadı ve test
        /// "rampa bulunamadı" dedi. İkisi de aynı sebepten — testin
        /// ölçtüğü yüzey, testin kurduğu yüzey olmalı.
        /// </summary>
        /// <summary>
        /// Rampanın (x, z)'deki yüzey kotu — <b>düzlemden hesaplanır,
        /// ışınla aranmaz</b>.
        ///
        /// Bu satır üç kez ışın attı ve üçünde de başka bir şey ölçtü:
        /// bir kez başka bir testin düz zeminine çarptı (negatif kontrol
        /// ±0,000 m dedi), iki kez de hiçbir şeye çarpmadı ("rampa
        /// bulunamadı") — çünkü çarpıştırıcı, yeni kurulmuş bir nesnede
        /// bir fizik adımı geçmeden yerine oturmuş olmayabiliyor.
        ///
        /// Rampanın kendisi bir düzlem ve düzlemin kotu hesaplanabilir
        /// bir sayı. Hesaplanabilen bir şeyi aramak, aramanın
        /// başarısızlığını ölçüme karıştırmak demek.
        /// </summary>
        private float ZeminKotu(float x, float z)
        {
            var t = Rampa().transform;
            Vector3 n = t.up;                       // ust yuzun normali
            Vector3 p = t.position + n * (t.localScale.y * 0.5f);
            // Duzlem: n . (X - p) = 0  ->  y = p.y - (n.x dx + n.z dz)/n.y
            return p.y - (n.x * (x - p.x) + n.z * (z - p.z)) / n.y;
        }

        /// <summary>
        /// Rampayı <b>gerektiğinde</b> kurar.
        ///
        /// Rampa yalnız <c>[SetUp]</c>'ta kuruluyordu ve ikinci test
        /// "rampa ışınla bulunamadı" diyerek düştü: bu koşumda alan
        /// (<c>_yokus</c>) ikinci teste taşınmıyor. Neden taşınmadığını
        /// aramak yerine testi o varsayımdan kurtarmak daha sağlam —
        /// bir test, kendi ölçtüğü yüzeyin var olduğundan kendi emin
        /// olmalı. Kurulum yine <c>[SetUp]</c>'ta duruyor (temizlik
        /// oradan yapılıyor), burası yalnızca eksikse tamamlıyor.
        /// </summary>
        private GameObject Rampa()
        {
            if (_yokus != null) return _yokus;
            _yokus = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _yokus.name = "TestYokusu";
            _yokus.transform.position = new Vector3(Uzak, -0.5f, Uzak);
            _yokus.transform.localScale = new Vector3(20f, 1f, 20f);
            // EGIM 26 DERECE — OLCU ALETI, SOKAK DEGIL.
            //
            // Once 18 dereceydi ("Galata'nin sokak egimleri bu
            // aralikta") ve olcum yapilinca yetmedigi gorundu: duruş
            // pozunda ayaklar x = +-0,148 m'de, yani IK'siz sapma
            // 0,148 x tan18 = 4,8 cm. Kabul esigi 5 cm. Yani negatif
            // kontrolun gostermesi gereken kusur, kabul esiginin
            // ALTINDA kaliyor ve iki kosul ayni anda saglanamiyor.
            //
            // Bir rampa Galata'yi temsil etmek zorunda degil; olcunun
            // gorunur olmasi icin yeterince dik olmak zorunda.
            // 26 derecede sapma 7,2 cm: esigin bir buçuk kati.
            _yokus.transform.rotation = Quaternion.Euler(0f, 0f, 26f);
            // AyakIK'nin kendi isini carpistiriciyi bulabilsin: yeni
            // kurulmus bir nesnenin PhysX bicimi, bir fizik adimi
            // gecmeden yerine oturmus olmayabilir.
            Physics.SyncTransforms();
            return _yokus;
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
            Vector3 tepe = new Vector3(Uzak, 3f, Uzak);
            _ornek.transform.position =
                new Vector3(tepe.x, ZeminKotu(tepe.x, tepe.z), tepe.z);
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

            // KURULUM YUKSEK SESLE DOGRULANIR.
            //
            // Bir tur, karakter rampanin 43 cm ustunde duruyordu ve test
            // "IK calismiyor" diye kirmizi yandi — oysa IK calisiyordu,
            // ayak o mesafeden zemini bulamiyordu. Kurulumun kendisi
            // yanlissa testin sonucu hicbir sey soylemez.
            yield return null;
            float ilk = TabanFarki(anim, HumanBodyBones.RightFoot);
            Assert.LessOrEqual(Mathf.Abs(ilk), 0.25f,
                $"KURULUM BOZUK: karakter zeminden {ilk:+0.000;-0.000} m "
                + "uzakta basliyor. Test IK'yi degil kendi yerlestirmesini "
                + "olcerdi.");

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

            _ornek.transform.position =
                new Vector3(Uzak, ZeminKotu(Uzak, Uzak), Uzak);

            // POZ ILERLEMELI — YOKSA IKI TEST AYNI SEYI KIYASLAMIYOR.
            //
            // Ustteki test `AlwaysAnimate` kuruyor, bu kurmuyordu:
            // kamerasiz sahnede animator hic guncellenmiyor ve ayaklar
            // BIND pozunda kaliyor. Yani "IK kapaliyken" olculen sey
            // duruş klibinin pozu degil, hic oynamamis bir iskeletti.
            // Bir negatif kontrol, kontrol ettigi seyle ayni kosullarda
            // kosmali.
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;

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
        private float TabanFarki(Animator anim, HumanBodyBones kemik,
                                 AyakIK _ = null)
        {
            Transform t = anim.GetBoneTransform(kemik);
            Assert.IsNotNull(t, $"{kemik} kemigi yok.");

            float ofset = OlculenOfset(anim);
            float taban = t.position.y - ofset;

            return taban - ZeminKotu(t.position.x, t.position.z);
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
