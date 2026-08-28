using System.Collections.Generic;
using System.Text;
using Hezarfen.Sehir;
using Hezarfen.Zaman;
using UnityEditor;
using UnityEngine;
using V = Hezarfen.Zaman.VakitHesabi.Vakit;
using T = Hezarfen.Sehir.SokakGrafi.Tur;

namespace Hezarfen.Editor.Gis
{
    /// <summary>
    /// <b>Meslek çizelgelerini üretir.</b> Elle tıklanmaz.
    ///
    /// On ScriptableObject'i Inspector'da doldurmak mümkün ama bu projede
    /// yasak sayılır: elle kurulmuş bir çizelgenin diff'i okunmaz ve
    /// <b>neden öyle olduğu</b> hiçbir yerde yazmaz. Burada her satırın
    /// yanında gerekçesi var.
    ///
    /// ## Çizelgeler nereden geliyor
    ///
    /// Plan Bölüm 11.3 meslek listesini ve rutinin omurgasını veriyor:
    /// *"namaza akış, kepenk açma/kapama, öğle yoğunluğu, gece
    /// sokakların boşalması"*. RESEARCH §6 kolluk ve gece yasağını,
    /// ulaşımın kayık olduğunu, kahvehanelerin 1633'e kadar açık
    /// olduğunu veriyor.
    ///
    /// Ayrıntı — kimin hangi vakitte tam olarak nerede olduğu — <b>belgeli
    /// değil</b> ve olamaz. Çizelgeler bu yüzden T2/taslak: iskeleti
    /// kaynaktan, ayrıntısı makul çıkarımdan.
    /// </summary>
    public static class MeslekKur
    {
        private const string Dizin = "Assets/_Project/Data/Meslek";

        [MenuItem("Hezarfen/GIS/Meslek cizelgelerini uret")]
        public static void Uret()
        {
            System.IO.Directory.CreateDirectory(Dizin);
            var sb = new StringBuilder("MESLEK CIZELGELERI");

            foreach (var (tip, pay, cizelge) in Cizelgeler())
            {
                string yol = $"{Dizin}/NM_{tip}.asset";
                var so = AssetDatabase.LoadAssetAtPath<NPCMeslek>(yol);
                bool yeni = so == null;
                if (yeni) so = ScriptableObject.CreateInstance<NPCMeslek>();
                so.tip = tip;
                so.pay = pay;
                so.cizelge = cizelge;
                if (yeni) AssetDatabase.CreateAsset(so, yol);
                else EditorUtility.SetDirty(so);

                // Her vakitte bir sey yapiyor mu — bosluk, NPC'nin o
                // vakitte yerinde donmesi demek.
                var eksik = new List<V>();
                foreach (V v in System.Enum.GetValues(typeof(V)))
                {
                    bool var_ = false;
                    foreach (var a in cizelge) if (a.vakit == v) var_ = true;
                    if (!var_) eksik.Add(v);
                }
                sb.AppendLine($"  {tip,-12} pay %{pay * 100:0}, "
                              + $"{cizelge.Count} adim"
                              + (eksik.Count > 0
                                 ? $" — EKSIK VAKIT: {string.Join(",", eksik)}"
                                 : ""));
            }

            AssetDatabase.SaveAssets();
            sb.AppendLine($"  -> {Dizin}");
            Debug.Log("[Hezarfen] " + sb);
        }

        private static NPCMeslek.Adim A(V v, T hedef, float olasilik,
                                       bool disarida = false)
            => new() { vakit = v, hedef = hedef, olasilik = olasilik,
                       disarida = disarida };

        /// <summary>Bütün meslekler ve gerekçeleri.</summary>
        private static IEnumerable<(NPCMeslek.Tip, float, List<NPCMeslek.Adim>)>
            Cizelgeler()
        {
            // ESNAF — sehrin coğunluğu. Kepenk gunes dogunca acilir,
            // aksam ezaniyla kapanir; vakitlerde mescide gidilir ama
            // herkes her vakit gitmez (olasilik < 1).
            yield return (NPCMeslek.Tip.Esnaf, 0.30f, new List<NPCMeslek.Adim>
            {
                A(V.Sabah, T.Mescit, 0.55f, true), A(V.Sabah, T.Ev, 0.45f),
                A(V.Gunes, T.Dukkan, 0.90f), A(V.Gunes, T.Firin, 0.10f, true),
                A(V.Ogle, T.Mescit, 0.45f, true), A(V.Ogle, T.Dukkan, 0.55f),
                A(V.Ikindi, T.Dukkan, 0.85f), A(V.Ikindi, T.Cesme, 0.15f, true),
                // Kahvehane 1633 Eylul'une kadar; sonra kapali (Kronoloji).
                A(V.Aksam, T.Kahvehane, 0.35f), A(V.Aksam, T.Mescit, 0.35f, true),
                A(V.Aksam, T.Ev, 0.30f),
                A(V.Yatsi, T.Ev, 1.00f),
            });

            // HAMAL — hanlarda yuk tasir. Sabah erken baslar, aksam biter.
            yield return (NPCMeslek.Tip.Hamal, 0.10f, new List<NPCMeslek.Adim>
            {
                A(V.Sabah, T.Ev, 1.00f),
                A(V.Gunes, T.Han, 0.70f), A(V.Gunes, T.Iskele, 0.30f, true),
                A(V.Ogle, T.Han, 0.80f), A(V.Ogle, T.Mescit, 0.20f, true),
                A(V.Ikindi, T.Han, 0.60f), A(V.Ikindi, T.Iskele, 0.40f, true),
                A(V.Aksam, T.Ev, 0.70f), A(V.Aksam, T.Bozahane, 0.30f),
                A(V.Yatsi, T.Ev, 1.00f),
            });

            // KAYIKCI — Halic'te kopru yok; ulasim bu adamlarin isi
            // (RESEARCH BOLUM 6). Gun boyu iskelededir.
            yield return (NPCMeslek.Tip.Kayikci, 0.08f, new List<NPCMeslek.Adim>
            {
                A(V.Sabah, T.Iskele, 0.80f, true), A(V.Sabah, T.Ev, 0.20f),
                A(V.Gunes, T.Iskele, 1.00f, true),
                A(V.Ogle, T.Iskele, 0.85f, true), A(V.Ogle, T.Mescit, 0.15f, true),
                A(V.Ikindi, T.Iskele, 1.00f, true),
                A(V.Aksam, T.Iskele, 0.50f, true), A(V.Aksam, T.Ev, 0.50f),
                A(V.Yatsi, T.Ev, 1.00f),
            });

            // YENICERI — kolluk ve garnizon. Gunduz gorunur, gece
            // kislada; ases'ten farki gece devriyesinin ases'in isi
            // olmasi.
            yield return (NPCMeslek.Tip.Yeniceri, 0.06f, new List<NPCMeslek.Adim>
            {
                A(V.Sabah, T.Ev, 1.00f),
                A(V.Gunes, T.Han, 0.50f, true), A(V.Gunes, T.Ev, 0.50f),
                A(V.Ogle, T.Mescit, 0.40f, true), A(V.Ogle, T.Han, 0.60f, true),
                A(V.Ikindi, T.Han, 0.70f, true), A(V.Ikindi, T.Kahvehane, 0.30f),
                A(V.Aksam, T.Ev, 0.60f), A(V.Aksam, T.Kahvehane, 0.40f),
                A(V.Yatsi, T.Ev, 1.00f),
            });

            // ASES — GECE bekcisi. Gunduz evde, YATSIDAN SONRA sokakta.
            //
            // Bu meslek cizelgenin tersini yapiyor ve olcum onu goruyor:
            // gece disarida kalan tek gruptur. Fenersiz dolasmayi
            // yakalayan da odur (RESEARCH BOLUM 6).
            yield return (NPCMeslek.Tip.Ases, 0.04f, new List<NPCMeslek.Adim>
            {
                A(V.Sabah, T.Ev, 1.00f),
                A(V.Gunes, T.Ev, 1.00f),
                A(V.Ogle, T.Ev, 1.00f),
                A(V.Ikindi, T.Ev, 0.70f), A(V.Ikindi, T.Cesme, 0.30f, true),
                A(V.Aksam, T.Cesme, 0.60f, true), A(V.Aksam, T.Mescit, 0.40f, true),
                A(V.Yatsi, T.Cesme, 0.50f, true), A(V.Yatsi, T.Dukkan, 0.50f, true),
            });

            // SU SATICISI — cesme ile sokak arasinda gidip gelir.
            yield return (NPCMeslek.Tip.SuSaticisi, 0.05f, new List<NPCMeslek.Adim>
            {
                A(V.Sabah, T.Cesme, 0.60f, true), A(V.Sabah, T.Ev, 0.40f),
                A(V.Gunes, T.Cesme, 1.00f, true),
                A(V.Ogle, T.Cesme, 0.80f, true), A(V.Ogle, T.Mescit, 0.20f, true),
                A(V.Ikindi, T.Cesme, 1.00f, true),
                A(V.Aksam, T.Ev, 1.00f),
                A(V.Yatsi, T.Ev, 1.00f),
            });

            // DILENCI — cami ve turbe kapisi. Vakitlerde kalabalik olur.
            yield return (NPCMeslek.Tip.Dilenci, 0.04f, new List<NPCMeslek.Adim>
            {
                A(V.Sabah, T.Mescit, 1.00f, true),
                A(V.Gunes, T.Mescit, 0.60f, true), A(V.Gunes, T.Turbe, 0.40f, true),
                A(V.Ogle, T.Mescit, 1.00f, true),
                A(V.Ikindi, T.Turbe, 0.50f, true), A(V.Ikindi, T.Mescit, 0.50f, true),
                A(V.Aksam, T.Mescit, 0.70f, true), A(V.Aksam, T.Ev, 0.30f),
                A(V.Yatsi, T.Ev, 1.00f),
            });

            // COCUK — sabah mektebe, ogleden sonra sokakta.
            //
            // Mektep sayisi sehirde 130; bu meslek olmasa o binalarin
            // hepsi bos dururdu.
            yield return (NPCMeslek.Tip.Cocuk, 0.18f, new List<NPCMeslek.Adim>
            {
                A(V.Sabah, T.Ev, 1.00f),
                A(V.Gunes, T.Mektep, 0.85f), A(V.Gunes, T.Ev, 0.15f),
                A(V.Ogle, T.Mektep, 0.55f), A(V.Ogle, T.Ev, 0.45f),
                A(V.Ikindi, T.Cesme, 0.55f, true), A(V.Ikindi, T.Ev, 0.45f),
                A(V.Aksam, T.Ev, 1.00f),
                A(V.Yatsi, T.Ev, 1.00f),
            });

            // IMAM — bes vakit mescitte. Mahallenin kayit ve kefalet
            // sorumlusu da odur (RESEARCH BOLUM 6).
            yield return (NPCMeslek.Tip.Imam, 0.05f, new List<NPCMeslek.Adim>
            {
                A(V.Sabah, T.Mescit, 1.00f),
                A(V.Gunes, T.Mescit, 0.70f), A(V.Gunes, T.Ev, 0.30f),
                A(V.Ogle, T.Mescit, 1.00f),
                A(V.Ikindi, T.Mescit, 1.00f),
                A(V.Aksam, T.Mescit, 1.00f),
                A(V.Yatsi, T.Mescit, 0.60f), A(V.Yatsi, T.Ev, 0.40f),
            });

            // MEDRESELI — medresede okur, vakitlerde mescide.
            yield return (NPCMeslek.Tip.Medreseli, 0.10f, new List<NPCMeslek.Adim>
            {
                A(V.Sabah, T.Mescit, 0.70f, true), A(V.Sabah, T.Medrese, 0.30f),
                A(V.Gunes, T.Medrese, 1.00f),
                A(V.Ogle, T.Mescit, 0.50f, true), A(V.Ogle, T.Medrese, 0.50f),
                A(V.Ikindi, T.Medrese, 0.80f), A(V.Ikindi, T.Kahvehane, 0.20f),
                A(V.Aksam, T.Mescit, 0.60f, true), A(V.Aksam, T.Medrese, 0.40f),
                A(V.Yatsi, T.Medrese, 1.00f),
            });
        }
    }
}
