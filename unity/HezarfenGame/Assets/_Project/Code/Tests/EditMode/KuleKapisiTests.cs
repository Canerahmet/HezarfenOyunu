using Hezarfen.Sehir;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Hezarfen.Tests
{
    /// <summary>
    /// <b>Kuleye çıkılabiliyor mu.</b>
    ///
    /// ## Neden bu dosya var
    ///
    /// Oyunun adı Hezarfen ve tamamı bir kuleden atlamak için. İki tur
    /// üst üste oyuncu kuleye çıkamadı.
    ///
    /// Birinci turda kapı <b>yoktu</b>: kule tek parça mühürlü bir
    /// kabuk, iç mekân yok, tırmanma yok. Kapı eklendi ve "kapatıldı"
    /// diye rapor edildi.
    ///
    /// İkinci turda kapı <b>vardı ve taşın içindeydi</b>. Kule
    /// çarpıştırıcısı 8,225 m yarıçapında dolu bir silindir; kapı
    /// tetikleyicisi eksenden 6,5 m'ye konmuştu, yani duvarın 1,7 m
    /// içine. <see cref="Hezarfen.Player.EtkilesimAlgila"/> birinci
    /// turda tam bu iş için görüş hattı ışını eklemişti ve kapıyı
    /// doğru biçimde reddediyordu. Ekranda "Kuleye çık" hiç belirmedi;
    /// bir oyuncu otuz dakika kuleyi dolandı.
    ///
    /// Ve aynı sahnede kapının <b>dört üst üste binmiş kopyası</b>
    /// vardı: üreteç her koşuşta bir yenisini ekliyor, eskisini
    /// silmiyordu.
    ///
    /// İkisi de yazıldı, diske geçti ve <b>ölçülmedi</b>. Bu dosya o
    /// boşluğu kapatıyor.
    /// </summary>
    public class KuleKapisiTests
    {
        private const string Sahne =
            "Assets/_Project/Scenes/Faz1_Terrain.unity";

        private static KuleKapisi[] Kapilar()
        {
            var s = EditorSceneManager.GetSceneByPath(Sahne);
            if (!s.isLoaded)
                s = EditorSceneManager.OpenScene(Sahne, OpenSceneMode.Additive);

            var liste = new System.Collections.Generic.List<KuleKapisi>();
            foreach (var kok in s.GetRootGameObjects())
                liste.AddRange(kok.GetComponentsInChildren<KuleKapisi>(true));
            return liste.ToArray();
        }

        [Test]
        public void TheTowerHasExactlyOneDoor()
        {
            var k = Kapilar();
            Assert.AreEqual(1, k.Length,
                $"Sahnede {k.Length} kule kapisi var. Sifir ise oyunun "
                + "doruk noktasi erisilemez; birden fazlaysa ureteç her "
                + "kosumda bir yenisini ekliyor ve eskisini silmiyor "
                + "demektir.");
        }

        /// <summary>
        /// <b>Kapı taşın dışında mı.</b>
        ///
        /// Etkileşim fizikten geçiyor ve görüş hattı istiyor. Kulenin
        /// kendi çarpıştırıcısının içinde duran bir tetikleyiciye
        /// oyuncu asla ulaşamaz — sistem çalışır, kapı görünmez.
        /// </summary>
        [Test]
        public void TheDoorIsOutsideTheStoneNotInsideIt()
        {
            var k = Kapilar();
            Assert.AreEqual(1, k.Length, "Once kapi sayisi duzelmeli.");
            var kapi = k[0];

            var kule = kapi.transform.parent;
            Assert.IsNotNull(kule, "Kapinin ebeveyni yok.");

            Collider tas = null;
            foreach (var c in kule.GetComponentsInChildren<Collider>(true))
                if (!c.isTrigger) { tas = c; break; }
            Assert.IsNotNull(tas, "Kulede kati carpistirici yok.");

            // CETVEL OYUNUN KENDI SORUSUNU SORAR.
            //
            // Once `Collider.ClosestPoint` ile "nokta tasin icinde mi"
            // olculuyordu ve kule carpistiricisi **disbukey olmaktan
            // ciktigi** an o cetvel bozuldu: Unity, disbukey olmayan
            // bir MeshCollider icin `ClosestPoint`'i desteklemez ve
            // verilen noktayi geri dondurur. Test "kapi tasin ICINDE"
            // dedi, oysa kapi disaridaydi.
            //
            // Sorulmasi gereken sey zaten belliydi ve oyunun kendisi
            // onu soruyor: `EtkilesimAlgila` oyuncunun gozunden hedefe
            // bir GORUS HATTI isini atar ve arada kati bir sey varsa
            // hedefi reddeder. Test de tam onu atar — bir vekil degil,
            // fiilin kendisi.
            var p = kapi.transform.position;
            var eksen0 = kule.position;
            var disari = (p - eksen0); disari.y = 0f;
            disari = disari.sqrMagnitude > 1e-4f
                     ? disari.normalized : -kapi.transform.forward;

            // Oyuncunun duracagi yer: kapinin 2 m disi, goz hizasi.
            var goz = p + disari * 2f + Vector3.up * 0.4f;
            var fark = p - goz;

            bool engel = Physics.Raycast(goz, fark.normalized,
                                         out var v0, fark.magnitude, ~0,
                                         QueryTriggerInteraction.Ignore);
            Assert.IsFalse(engel,
                $"Kapiya bakis hatti KAPALI — arada '{(engel ? v0.collider.name : "?")}' "
                + "var. Etkilesim gorus hatti istiyor; oyuncu 'Kuleye "
                + "cik' yazisini hic goremez.");
        }

        /// <summary>
        /// <b>Kapı seni kuleden ÇIKARIYOR mu.</b>
        ///
        /// ## Neden bu test öncekilerin yerine geçti
        ///
        /// Önceki dört test kapıyı ölçüyordu — tek mi, taşın dışında
        /// mı, kotu eşiğin üstünde mi, altında zemin var mı — ve
        /// <b>dördü de yeşilken</b> bir oyuncu kulenin tepesinde,
        /// kapısı olmayan bir odanın içinde kilitli kaldı. Onun cümlesi:
        /// *"Ben 'beş saniye durun' dedim, siz beş saniye durmayı
        /// ölçtünüz. Altıncı saniyeyi kimse sormadı."*
        ///
        /// Kapı artık bir şerefeye değil <b>kalkışa</b> açılıyor
        /// (ADR 0086), çünkü bu kulede gezilecek şerefe yok: korkuluk
        /// ile kasnak arası 0,35 m ve oyuncunun kapsülü 0,70 m. Test de
        /// buna göre soruyor — çıkış noktası kulenin <b>dışında</b> mı.
        /// </summary>
        [Test]
        public void TheDoorTakesYouOutOfTheTowerNotIntoIt()
        {
            var k = Kapilar();
            Assert.AreEqual(1, k.Length, "Once kapi sayisi duzelmeli.");
            var kapi = k[0];
            var kule = kapi.transform.parent;
            Assert.IsNotNull(kule, "Kapinin ebeveyni yok.");

            // Kodun kullandigi yariçapin AYNISI: yerel sinirdan.
            float yaricap = 8.2f;
            foreach (var c in kule.GetComponentsInChildren<Collider>(true))
            {
                if (c.isTrigger) continue;
                if (c is MeshCollider mc && mc.sharedMesh != null)
                {
                    var b = mc.sharedMesh.bounds;
                    var o = kule.lossyScale;
                    yaricap = Mathf.Max(b.extents.x * Mathf.Abs(o.x),
                                        b.extents.z * Mathf.Abs(o.z));
                    break;
                }
            }

            var eksen = kule.position;
            var yon = kapi.transform.position - eksen;
            yon.y = 0f;
            yon = yon.sqrMagnitude > 1e-4f
                  ? yon.normalized : -kapi.transform.forward;

            var cikis = eksen + Vector3.up * kapi.serefeKotu
                        + yon * (yaricap + 1.4f);

            // (a) Cikis noktasindan EKSENE dogru bakinca kule olmali:
            //     yoksa yariçap yanlis okunmus demektir.
            bool kuleVar = Physics.Raycast(cikis, -yon, out var v1, 4f, ~0,
                                           QueryTriggerInteraction.Ignore);
            Assert.IsTrue(kuleVar,
                $"Cikis noktasindan ({cikis.x:F1}, {cikis.y:F1}, {cikis.z:F1}) "
                + "eksene bakinca kule yok — yariçap yanlis okunuyor ve "
                + "oyuncu bosluga birakiliyor.");

            // (b) VE cikis noktasinin KENDISI bos olmali: asagi bakan
            //     bir isin kuleye degil, cok asagida araziye carpmali.
            bool altta = Physics.Raycast(cikis, Vector3.down, out var v2,
                                         6f, ~0,
                                         QueryTriggerInteraction.Ignore);
            Assert.IsFalse(altta,
                $"Cikis noktasinin 6 m altinda '{(altta ? v2.collider.name : "?")}' "
                + "var — oyuncu kulenin ustune degil, icine birakiliyor. "
                + "Bu, dort turdur tekrarlanan tuzagin ta kendisi.");
        }

        /// <summary>
        /// <b>Kanatsız çıkılmıyor mu.</b>
        ///
        /// Kapı açıkken kanatsız çıkmanın karşılığı 46 m'lik
        /// <b>hasarsız</b> bir düşüştü: oyuncu yere çarpıyor, kalkıyor,
        /// yürüyordu. Bir kule, oradan atlayacak şeyi olmayan birine
        /// açılmamalı.
        /// </summary>
        [Test]
        public void TheDoorRefusesSomeoneWithoutAWing()
        {
            var k = Kapilar();
            Assert.AreEqual(1, k.Length, "Once kapi sayisi duzelmeli.");

            // Sahnede ucus dizisi yok (Editor kipi) → kanat yok sayilir.
            var sahte = new GameObject("AKTOR_T");
            bool oldu = k[0].Etkiles(sahte);
            Assert.IsFalse(oldu,
                "Kanatsiz kuleye cikildi — 46 m'lik hasarsiz bir dusus "
                + "bir mekanik degil, bir bosluktur.");
            Object.DestroyImmediate(sahte);
        }
    }
}
