using Hezarfen.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Hezarfen.Tests
{
    /// <summary>
    /// Üretim ve su yapılarının <b>tarihe bağlı</b> iddialarını kilitler
    /// (Faz 2b'nin kalan yedisi).
    ///
    /// Buradaki iki şey gözle denetlenemez ve ikisi de bir <b>tarihtir</b>:
    ///
    ///   * <b>Bozahane 1632'de AÇIKTIR</b> ve IV. Murad döneminde kapatılmıştır
    ///     — kahvehaneden sonra oyunun ikinci zaman işareti. 1633 sonrası bir
    ///     sahne kurulursa bu yapı kaldırılmalıdır; kaldırılacağını bilen tek
    ///     kayıt etiketidir.
    ///   * <b>Muvakkithane bir yerleştirme kuralı taşır:</b> 1632'de vardır ama
    ///     yalnız <b>selâtin</b> camisinde. Mahalle mescidine muvakkithane
    ///     koymak, tekkeye minare koymakla aynı hatadır — ikisi de "vardı ama
    ///     burada değil" cinsinden.
    ///
    /// Not düşerse iddia da düşer: prefab her boru hattı koşuşunda yeniden
    /// yazılır ve elle konan etiket kaybolur, o yüzden kaynak katalogdadır ve
    /// test etiketin oraya <b>ulaştığını</b> ölçer.
    /// </summary>
    public class WorksKitTests
    {
        private const string Dir = "Assets/_Project/Art/Prefabs/";

        private static GameObject Load(string name)
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>($"{Dir}PF_{name}.prefab");
            if (go == null)
                Assert.Ignore($"PF_{name} yok — once gen_works_kit.py + boru hatti.");
            return go;
        }

        private static string Note(string name)
        {
            var tag = Load(name).GetComponent<HistoricalTag>();
            Assert.IsNotNull(tag, $"PF_{name}: HistoricalTag yok.");
            Assert.IsTrue(tag.IsValid, $"PF_{name}: etiket eksik.");
            return tag.sourceNote.ToUpperInvariant();
        }

        [Test]
        public void EveryWorksAssetCarriesItsSource()
        {
            foreach (string n in new[]
            {
                "Imaret_A", "Imaret_Kucuk", "Arasta_A", "Arasta_Acik", "Bozahane_A",
                "Degirmen_Su", "Degirmen_At", "SuTerazisi_A", "SuTerazisi_Kisa",
                "Muvakkithane_A", "Cami_Orta",
            })
                Assert.IsNotEmpty(Note(n), $"PF_{n}: kaynak notu bos.");
        }

        [Test]
        public void BozahaneIsOpenIn1632AndSaysWhenItCloses()
        {
            // Kahvehane 2 Eylul 1633'te yasaklandi; bozahaneler de IV. Murad
            // doneminde kapatildi. Ikisi de 1632'de AYAKTA. Bu not, 1633
            // sahnesini kuracak olan kisiye "beni kaldir" diyen tek sey.
            string note = Note("Bozahane_A");
            StringAssert.Contains("1632'DE ACIK", note);
            StringAssert.Contains("KAPATILMISTIR", note);
            StringAssert.Contains("IV. MURAD", note);
        }

        [Test]
        public void MuvakkithaneCarriesItsPlacementRule()
        {
            // Muvakkithane 1632'de VARDIR (ilki Fatih Camii, 1470) ama
            // yayginlasmasi 18. yy sonudur. Yani varligi degil YERI kisitli.
            string note = Note("Muvakkithane_A");
            StringAssert.Contains("SELATIN", note);
            StringAssert.Contains("MAHALLE MESCIDINE DEGIL", note);
        }

        [Test]
        public void ArastaBayWidthComesFromTheOnlyMeasurementWeHave()
        {
            // Selimiye Arastasi 256 m'de 73 kemer tasir: 256/73 = 3,507 m.
            // Elimizdeki TEK metrik deger budur ve arastanin goz genisligi
            // odur. Kutle bunu tasimazsa sayi belgeden kopmus demektir.
            var go = Load("Arasta_A");
            var b = Bounds(go);
            const int bays = 8;
            float bay = b.size.x / bays;
            Assert.AreEqual(256.0f / 73.0f, bay, 0.25f,
                $"Goz genisligi {bay:F2} m — Selimiye capasi 3,51 m.");
        }

        [Test]
        public void WaterTowerIsATowerNotAChimney()
        {
            // Terazi TASIYICI bir kagir kuledir; ince uzun bir tas boru
            // bacadir. Olcut: yukseklik / taban <= 8.
            var b = Bounds(Load("SuTerazisi_A"));
            float slender = b.size.y / Mathf.Max(b.size.x, 0.01f);
            Assert.Less(slender, 8.0f,
                $"Kule {b.size.y:F1} m yuksek, tabani {b.size.x:F1} m — "
                + "bu oran bacaya benziyor.");
            Assert.Greater(b.size.y, 4.0f, "Terazi su YUKARI cikarir; alcak olmaz.");
        }

        [Test]
        public void MidSizeMosqueIsBiggerThanTheNeighbourhoodMescit()
        {
            // Orta olcek cami ile mahalle mescidi AYNI kitten cikar ve tek
            // fark parametredir; ikisi ayrisamazsa katalogda iki ayri yapi
            // tipi olmasinin anlami kalmaz.
            var cami = Bounds(Load("Cami_Orta"));
            var mescit = Bounds(Load("Mescit_A"));
            Assert.Greater(cami.size.x, mescit.size.x + 2.0f,
                "Orta olcek cami mahalle mescidinden genis olmali.");
            Assert.Greater(cami.size.y, mescit.size.y + 3.0f,
                "Orta olcek caminin minaresi de daha yuksek olmali.");
        }

        private static Bounds Bounds(GameObject prefab)
        {
            var mn = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var mx = -mn;
            foreach (var r in prefab.GetComponentsInChildren<Renderer>(true))
            {
                if (!r.gameObject.name.EndsWith("LOD0")) continue;
                mn = Vector3.Min(mn, r.bounds.min);
                mx = Vector3.Max(mx, r.bounds.max);
            }
            Assert.Less(mn.x, mx.x, $"{prefab.name}: LOD0 render'i bulunamadi.");
            return new Bounds((mn + mx) * 0.5f, mx - mn);
        }
    }
}
