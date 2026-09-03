using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Hezarfen.Editor.Lighting
{
    /// <summary>
    /// <b>Şehri sıçrama ışığına katar.</b>
    ///
    /// ## Kök sebep: hiçbir şey GI'ya katılmıyordu
    ///
    /// Turun üstten çekilen denetim karesinde Sûriçi sokağı simsiyahtı ve
    /// karanlık bölge her yerde <b>aynı</b> rengi okuyordu — üstte
    /// (37,0 / 15,7 / 0,2), ortada (36,5 / 14,9 / 0,2), altta
    /// (36,5 / 14,8 / 0,2). Gölge yüzeye göre değişir; değişmeyen renk
    /// gölge değil, <b>hiç ışık almayan yüzey</b> demektir. Aynı yerin
    /// göz hizası karesinde gölgeli kaldırım 0,63 mavi/kırmızı
    /// okuyordu: aynı yer, aynı saniye, iki kamera, iki sonuç. Farkı
    /// açı yaratıyorsa ışık ekran uzayından geliyor demektir — SSGI.
    ///
    /// Peşine düşerken iki gerçek kusur bulundu ve düzeltildi (fırının
    /// gökyüzü bağlı değildi; prob verisi akmıyordu), ama kare
    /// <b>bayt bayt aynı</b> kaldı. Üçüncü ölçüm sebebi söyledi:
    ///
    /// <code>
    /// D_Surici_Dogu:  498 nesne, m_StaticEditorFlags: 0
    /// D_Galata:       401 nesne, m_StaticEditorFlags: 0
    /// </code>
    ///
    /// Şehrin <b>tamamı</b> "Contribute GI" işaretsizdi. Prob fırını
    /// probu ışığa katılan geometrinin çevresine koyar; katılan hiçbir
    /// şey olmayınca 2,8 milyon prob boş yamaca pişti ve şehirde dolaylı
    /// ışık hiç doğmadı. Fırın her seferinde "başarılı" dedi.
    ///
    /// ## Neden ReceiveGI = LightProbes
    ///
    /// "Contribute GI" açılınca ilerlemeli fırın, aksi söylenmezse o
    /// nesne için <b>ışık haritası</b> üretmek ister. 10.868 evlik bir
    /// şehirde bu ne biter ne sığar. Doğru kurulum APV'nin kendi
    /// kurulumudur: nesne ışığa <i>katılır</i> (sıçratır, gölgeler) ama
    /// ışığı <b>problardan okur</b>.
    ///
    /// ## Neden yalnız GI bayrağı
    ///
    /// Toplu iş (batching) ve örtme (occlusion) bayrakları da kapalı ve
    /// ikisi de muhtemelen kazanç getirir — ama ikisi de <b>kare
    /// süresi</b> ölçüsüyle ayrı bir turun işi. Ölçmediğim bir kazancı
    /// bu turun diffine karıştırmıyorum.
    /// </summary>
    public static class GIKatilimi
    {
        /// <summary>Taban sahne + semt sahneleri.</summary>
        public static List<string> Sahneler()
        {
            var liste = new List<string> { TabanSahne };
            liste.AddRange(SemtProblari.Semtler());
            return liste;
        }

        /// <summary>Taban sahne — sayının sahibi
        /// <see cref="SemtProblari.TabanSahne"/>.</summary>
        public const string TabanSahne = SemtProblari.TabanSahne;

        /// <summary>
        /// Bir çizici GI'ya katılmalı mı.
        ///
        /// Arazi kendi bileşeniyle katılır; hareket eden hiçbir şey
        /// katılmaz. Semt sahnelerinde yalnız şehir var — kalabalık
        /// çalışma zamanında ana sahnede havuzlanıyor
        /// (<c>NPCYonetici</c>), yani buraya hiç uğramaz.
        /// </summary>
        private static bool Katilmali(MeshRenderer r)
        {
            if (r == null || !r.enabled) return false;
            if (r.GetComponentInParent<Terrain>() != null) return false;
            if (r.GetComponent<MeshFilter>() == null) return false;
            return true;
        }

        /// <summary>
        /// <b>GI'ya katılmayan çizici sayısı</b> — kesin sayım.
        /// Sahne sahne döner: (sahne adı, katılmayan, toplam).
        ///
        /// Sahneleri açar, yani pahalıdır; menüden koşan denetim için.
        /// Testin okuduğu ucuz ölçü <see cref="DosyaSayimi"/>.
        /// </summary>
        public static List<(string sahne, int eksik, int toplam)> Sayim()
        {
            var sonuc = new List<(string, int, int)>();
            foreach (string yol in Sahneler())
            {
                var s = EditorSceneManager.GetSceneByPath(yol);
                if (!s.isLoaded)
                    s = EditorSceneManager.OpenScene(yol, OpenSceneMode.Additive);

                int eksik = 0, toplam = 0;
                foreach (var r in Ciziciler(s))
                {
                    toplam++;
                    var bayrak = GameObjectUtility.GetStaticEditorFlags(
                        r.gameObject);
                    if ((bayrak & StaticEditorFlags.ContributeGI) == 0)
                        eksik++;
                }
                sonuc.Add((Path.GetFileNameWithoutExtension(yol), eksik, toplam));
            }
            return sonuc;
        }

        private static IEnumerable<MeshRenderer> Ciziciler(Scene s)
        {
            foreach (var kok in s.GetRootGameObjects())
                foreach (var r in kok.GetComponentsInChildren<MeshRenderer>(true))
                    if (Katilmali(r)) yield return r;
        }

        /// <summary>
        /// <b>Sahne dosyasından GI katılımı sayımı</b> — testin okuduğu
        /// ölçü: (sahne, katılan, toplam nesne).
        ///
        /// Neden dosyadan: doğru sayım sekiz semt sahnesini (150 MB)
        /// açmayı gerektirir ve bunu her test koşumunda yapmak süiti
        /// dakikalarca uzatır. Kusurun kendisi zaten dosyada
        /// görülmüştü — <c>D_Surici_Dogu</c>'nun 498 nesnesinin 498'i
        /// <c>m_StaticEditorFlags: 0</c> taşıyordu. Ölçü, kusuru bulan
        /// ölçünün aynısı.
        ///
        /// Sıfır bayraklı nesne tek başına kusur değil: boş bağlayıcı
        /// düğümlerin ışıkla işi yok. Kusur, bir semtte <b>katılan
        /// hiçbir şeyin olmaması</b>.
        /// </summary>
        public static List<(string sahne, int katilan, int toplam)> DosyaSayimi()
        {
            var sonuc = new List<(string, int, int)>();
            foreach (string yol in Sahneler())
            {
                if (!File.Exists(yol)) continue;
                int katilan = 0, toplam = 0;

                // BAYRAK IKI YERDE YASIYOR.
                //
                // Ilk yazimda yalniz duz `m_StaticEditorFlags:` satiri
                // sayiliyordu ve test uc semti "hic katilan yok" diye
                // kirmizi dondu — oysa araç ayni semtte 280 cizici
                // isaretledigini yazmisti. Sebep: bu nesneler PREFAB
                // ORNEGI ve bir ornekte degisen alan sahnede
                // `m_Modifications` icinde `propertyPath` olarak durur,
                // duz alan olarak degil. D_Bogaz'da 4 duz satir, 280
                // degisiklik girdisi var.
                //
                // Yani olcum degil, olcme bicimi yanlisti — bu depoda
                // tekrar eden dersin bir ornegi daha, ve bu kez benim
                // yeni testimde.
                bool bekleyen = false;
                foreach (string satir in File.ReadLines(yol))
                {
                    if (bekleyen)
                    {
                        int v = satir.IndexOf("value: ",
                            System.StringComparison.Ordinal);
                        if (v >= 0)
                        {
                            bekleyen = false;
                            toplam++;
                            if (Katkili(satir.Substring(v + 7))) katilan++;
                        }
                        continue;
                    }
                    if (satir.IndexOf("propertyPath: m_StaticEditorFlags",
                            System.StringComparison.Ordinal) >= 0)
                    { bekleyen = true; continue; }

                    int i = satir.IndexOf("m_StaticEditorFlags: ",
                                          System.StringComparison.Ordinal);
                    if (i < 0) continue;
                    toplam++;
                    if (Katkili(satir.Substring(i + 21))) katilan++;
                }
                sonuc.Add((Path.GetFileNameWithoutExtension(yol),
                           katilan, toplam));
            }
            return sonuc;
        }

        private static bool Katkili(string deger)
            => int.TryParse(deger.Trim(), out int bayrak)
               && (bayrak & (int)StaticEditorFlags.ContributeGI) != 0;

        [MenuItem("Hezarfen/Aydinlatma/Sehri GI'ya kat")]
        public static void KatMenu()
        {
            int n = Kat(out string rapor);
            Debug.Log($"[Hezarfen] GI katilimi: {n} cizici isaretlendi.\n"
                      + rapor + "\nSONRAKI ADIM: Hezarfen -> Aydinlatma "
                      + "-> Problari pisir");
        }

        /// <summary>
        /// Şehrin çizicilerini GI'ya katar. Sahneler açık değilse
        /// açılır; değişen sahne kaydedilir.
        /// </summary>
        public static int Kat(out string rapor)
        {
            var satirlar = new List<string>();
            int toplamDegisen = 0;

            foreach (string yol in Sahneler())
            {
                var s = EditorSceneManager.GetSceneByPath(yol);
                if (!s.isLoaded)
                    s = EditorSceneManager.OpenScene(yol, OpenSceneMode.Additive);

                int degisen = 0, toplam = 0;
                foreach (var r in Ciziciler(s))
                {
                    toplam++;
                    bool dokunuldu = false;

                    var bayrak = GameObjectUtility.GetStaticEditorFlags(
                        r.gameObject);
                    if ((bayrak & StaticEditorFlags.ContributeGI) == 0)
                    {
                        GameObjectUtility.SetStaticEditorFlags(
                            r.gameObject,
                            bayrak | StaticEditorFlags.ContributeGI);
                        dokunuldu = true;
                    }

                    // ISIK HARITASI DEGIL, PROB: 10.900 evlik bir sehir
                    // icin isik haritasi ne biter ne sigar.
                    //
                    // KAYIT SART. Ilk yazimda duz atama yapiliyordu ve
                    // olculdu: `m_ReceiveGI` sahne dosyasinda HIC
                    // gorunmuyor (D_Bogaz'da 280 `m_StaticEditorFlags`
                    // degisikligi var, `m_ReceiveGI` sifir). Bu nesneler
                    // prefab ORNEGI; bir ornekte degisen alan ancak
                    // degisiklik olarak KAYDEDILIRSE diske gecer.
                    // `SetStaticEditorFlags` bunu kendi yapiyor, duz
                    // atama yapmiyor.
                    //
                    // Bedeli gorunurdu: arac her kosumda ayni 104.748
                    // ciziciyi yeniden "isaretledi" ve 150 MB'lik sekiz
                    // sahneyi yeniden kaydetti — CLAUDE.md'nin yeniden
                    // uretim gurultusu kurali tam olarak bunu yasakliyor.
                    if (r.receiveGI != ReceiveGI.LightProbes)
                    {
                        r.receiveGI = ReceiveGI.LightProbes;
                        if (PrefabUtility.IsPartOfPrefabInstance(r))
                            PrefabUtility
                                .RecordPrefabInstancePropertyModifications(r);
                        dokunuldu = true;
                    }

                    if (!dokunuldu) continue;
                    EditorUtility.SetDirty(r.gameObject);
                    degisen++;
                }

                if (degisen > 0) EditorSceneManager.MarkSceneDirty(s);
                toplamDegisen += degisen;
                satirlar.Add($"{Path.GetFileNameWithoutExtension(yol)}: "
                             + $"{degisen}/{toplam} cizici isaretlendi.");
            }

            EditorSceneManager.SaveOpenScenes();
            rapor = string.Join("\n", satirlar);
            return toplamDegisen;
        }
    }
}
