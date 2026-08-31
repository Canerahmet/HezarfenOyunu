using System.IO;
using System.Text;
using Hezarfen.Sehir;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Hezarfen.Editor.Gis
{
    /// <summary>
    /// Avlu eşyalarını <b>dokunulabilir</b> hâle getirir.
    ///
    /// Şehirde 19.992 avlu eşyası vardı ve hiçbiri cevap vermiyordu.
    /// Su küpü, odunluk ve sebze tahtası zaten oradaydı; eksik olan
    /// onların bir <see cref="IEtkilesim"/> taşıması.
    ///
    /// Ayrı bir geçiş, çünkü bileşen davranıştır: semtleri yeniden
    /// kurmak 10.900 evi yeniden dizmek ve ikili varlıkları boş yere
    /// LFS'e yazmak olurdu (CLAUDE.md).
    ///
    /// ## Neden hepsi değil
    ///
    /// Yalnız üç prefab ailesi işaretleniyor. Çit, çardak ve sepet de
    /// dokunulabilir olabilirdi ama bir dünyayı canlı yapan şey her
    /// nesnenin tıklanabilmesi değil, <b>tıklananın bir işe
    /// yaraması</b>. Su, odun ve sebze envantere girer; çitin
    /// vereceği bir şey yok.
    /// </summary>
    public static class EtkilesimKur
    {
        private const string DistrictDir =
            "Assets/_Project/Scenes/Districts";

        /// <summary>Prefab adı öneki → ne verir.</summary>
        private static readonly (string onek, EsyaTuru tur, int stok)[] Aile =
        {
            ("PF_SuKupu", EsyaTuru.Su, 3),
            ("PF_Odunluk", EsyaTuru.Odun, 2),
            ("PF_Sebze", EsyaTuru.Sebze, 2),
        };

        /// <summary>
        /// İskele önekleri — bunlara <see cref="Perme"/> takılır.
        ///
        /// Ayrı liste, çünkü iskele bir <b>eşya</b> değil bir
        /// <b>geçiş</b>: envantere bir şey koymaz, oyuncuyu taşır.
        /// Aynı arayüzü (<c>IEtkilesim</c>) paylaşmaları tam da o
        /// arayüzün işi — "E'ye basınca bir şey olur" fikri ikisini de
        /// kapsıyor.
        /// </summary>
        private static readonly string[] Iskeleler = { "PF_Iskele" };

        /// <summary>
        /// Dükkân önekleri — bunlara <see cref="Dukkan"/> takılır.
        ///
        /// Ekonominin tek gideri kayık biletiydi ve kese şişip
        /// duruyordu; `Ekonomi`'nin narh defterinden türetilmiş bütün
        /// özeni oyuncuya hiç görünmüyordu. Bir dükkân, kazanmayı ilk
        /// kez bir anlama bağlıyor.
        /// </summary>
        //
        // Adlar sahneden OKUNDU, tahmin edilmedi: semt sahnelerinde
        // 565 `PF_Dukkan` ve 47 `PF_Firin` duruyor — ve hicbiri bir sey
        // satmiyordu. Once `PF_Arasta` ve `PF_Bedesten` yazmistim ve
        // sahnede sifir tanesi var; bir onek listesi, sahnenin
        // kendisine sorulmadan yazilirsa bos calisir.
        private static readonly (string onek, EsyaTuru satar)[] Dukkanlar =
        {
            ("PF_Firin", EsyaTuru.Ekmek),
            ("PF_Dukkan", EsyaTuru.Sebze),
        };

        [MenuItem("Hezarfen/GIS/Etkilesimleri kur (D_Galata)")]
        public static void Galata() => Kur("D_Galata");

        [MenuItem("Hezarfen/GIS/Etkilesimleri kur (tum semtler)")]
        public static void Hepsi()
        {
            foreach (var y in Directory.GetFiles(DistrictDir, "D_*.unity"))
                Kur(Path.GetFileNameWithoutExtension(y));
        }

        public static void Kur(string semt)
        {
            string yol = $"{DistrictDir}/{semt}.unity";
            if (!File.Exists(yol)) { Debug.LogError($"[Hezarfen] {yol} yok."); return; }

            var sahne = EditorSceneManager.OpenScene(yol, OpenSceneMode.Single);
            int eklenen = 0, vardi = 0, colliderEklenen = 0;

            foreach (var go in sahne.GetRootGameObjects())
                foreach (var t in go.GetComponentsInChildren<Transform>())
                {
                    foreach (var (onek, tur, stok) in Aile)
                    {
                        if (!t.name.StartsWith(onek)) continue;
                        if (t.GetComponent<ToplanabilirEsya>() != null)
                        {
                            vardi++;
                            break;
                        }
                        var e = t.gameObject.AddComponent<ToplanabilirEsya>();
                        e.tur = tur;
                        e.stok = stok;
                        eklenen++;

                        // ETKILESIM FIZIKTEN GECER.
                        //
                        // `EtkilesimAlgila` bir kure sorgusu yapiyor;
                        // collider'i olmayan nesne o sorguda hic
                        // gorunmez. Avlu esyalarinin cogu gorsel
                        // oldugu icin collider tasimiyordu.
                        //
                        // Tetikleyici collider secildi: esyalar
                        // dolasimi engellemesin — bahce zaten dar ve
                        // 19.992 kati cisim, yurunebilirligi kirardi.
                        if (t.GetComponentInChildren<Collider>() == null)
                        {
                            var kutu = t.gameObject.AddComponent<BoxCollider>();
                            kutu.isTrigger = true;
                            var b = Sinir(t);
                            kutu.center = t.InverseTransformPoint(b.center);
                            kutu.size = t.InverseTransformVector(b.size);
                            colliderEklenen++;
                        }
                        break;
                    }
                }

            // --- DUKKANLAR ---
            int dukkan = 0;
            foreach (var go in sahne.GetRootGameObjects())
                foreach (var t in go.GetComponentsInChildren<Transform>())
                {
                    foreach (var (onek, satar) in Dukkanlar)
                    {
                        if (!t.name.StartsWith(onek)) continue;
                        if (t.GetComponent<Dukkan>() != null) break;
                        var d = t.gameObject.AddComponent<Dukkan>();
                        d.satilan = satar;
                        dukkan++;
                        if (t.GetComponentInChildren<Collider>() == null)
                        {
                            var k = t.gameObject.AddComponent<BoxCollider>();
                            k.isTrigger = true;
                            var b = Sinir(t);
                            k.center = t.InverseTransformPoint(b.center);
                            k.size = t.InverseTransformVector(b.size);
                        }
                        break;
                    }
                }

            // --- ISKELELER: PERME ---
            int perme = 0;
            foreach (var go in sahne.GetRootGameObjects())
                foreach (var t in go.GetComponentsInChildren<Transform>())
                {
                    bool iskele = false;
                    foreach (var onek in Iskeleler)
                        if (t.name.StartsWith(onek)) { iskele = true; break; }
                    if (!iskele) continue;
                    if (t.GetComponent<Perme>() != null) continue;

                    var pm = t.gameObject.AddComponent<Perme>();
                    perme++;

                    // Iskeleye YAKLASILABILMELI: etkilesim fizikten
                    // geciyor ve iskelenin carpistiricisi yoksa
                    // `OverlapSphere` onu hic gormez.
                    if (t.GetComponentInChildren<Collider>() == null)
                    {
                        var kutu = t.gameObject.AddComponent<BoxCollider>();
                        kutu.isTrigger = true;
                        var b = Sinir(t);
                        kutu.center = t.InverseTransformPoint(b.center);
                        kutu.size = t.InverseTransformVector(b.size);
                    }
                }

            EditorSceneManager.MarkSceneDirty(sahne);
            EditorSceneManager.SaveScene(sahne);

            var sb = new StringBuilder($"ETKILESIM {semt}\n");
            sb.AppendLine($"  {eklenen} esya isaretlendi ({vardi} zaten vardi)");
            sb.AppendLine($"  {colliderEklenen} tetikleyici collider eklendi");
            sb.AppendLine($"  {perme} iskeleye perme takildi");
            sb.AppendLine($"  {dukkan} dukkan acildi");
            Debug.Log("[Hezarfen] " + sb);
        }

        private static Bounds Sinir(Transform t)
        {
            var rs = t.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0)
                return new Bounds(t.position, Vector3.one * 0.6f);
            var b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            return b;
        }
    }
}
