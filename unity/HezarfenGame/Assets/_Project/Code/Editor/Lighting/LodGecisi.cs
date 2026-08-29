using UnityEditor;
using UnityEngine;

namespace Hezarfen.Editor.Lighting
{
    /// <summary>
    /// <b>LOD geçişlerini yumuşatır — "modellerin üzerinde titreme"nin
    /// en güçlü adayı.</b>
    ///
    /// Caner (2026-08-29, üçüncü kez): *"titreme sorunu ozellikle
    /// modellerin uzerinde oluyor, modelleri kontrol et veya oyuna nasil
    /// ekliyorsun orada bir problem olabilir."*
    ///
    /// ## Neden AA bunu çözmedi
    ///
    /// Titreme için önce kenar yumuşatma açıldı (TAA), sonra özgül
    /// parlaklık takozlanması bastırıldı (Geometric Specular AA). İkisi de
    /// gerekliydi; ikisi de bu kusura <b>dokunmuyor</b>.
    ///
    /// Ölçüldü: <b>135 prefabın hepsinde</b> <c>m_FadeMode: 0</c>
    /// (LODFadeMode.None) ve <c>fadeTransitionWidth: 0</c>. Yani bir bina
    /// LOD0'dan LOD1'e geçerken <b>tek karede</b> başka bir mesh'e
    /// dönüşüyor. Yürürken şehrin dört bir yanında bu sıçrama sürekli
    /// tekrarlanır ve gözde <i>titreme</i> olarak okunur.
    ///
    /// Hiçbir kenar yumuşatma bunu düzeltemez, çünkü kaynak örnekleme
    /// değil <b>geometrinin kendisinin değişmesi</b>. Caner "modelleri
    /// kontrol et, oyuna ekleme biçiminde bir problem olabilir" derken
    /// doğru yere bakıyordu: kusur malzemede ya da kamerada değil, modelin
    /// <b>prefab'a konma biçiminde</b>.
    ///
    /// ## Çözüm: taramalı geçiş
    ///
    /// <c>LODFadeMode.CrossFade</c> + sıfırdan büyük bir geçiş bandı, iki
    /// LOD'u geçiş boyunca <b>taramalı (dither) karıştırır</b>. HDRP/Lit
    /// bunu destekler ve TAA taramayı çözerek yumuşak bir geçişe çevirir —
    /// yani üç düzeltme burada birbirini tamamlıyor.
    ///
    /// ## Bedeli ölçülmeli
    ///
    /// Geçiş bandında <b>iki</b> LOD birden çizilir. Bant ne kadar genişse
    /// o kadar uzun süre çift çizim. 0,25 seçildi: görünür sıçramayı
    /// kapatacak kadar geniş, kare bütçesini yiyecek kadar değil. Faz 7'nin
    /// ölçüm koşusu bu değişiklikten sonra tekrarlanmalı.
    /// </summary>
    public static class LodGecisi
    {
        /// <summary>
        /// Geçiş bandı — LOD'un ekran oranının yüzde kaçı boyunca iki mesh
        /// birden çizilir. 0 = sert sıçrama.
        /// </summary>
        public const float GecisBandi = 0.25f;

        private const string PrefabKlasoru = "Assets/_Project/Art/Prefabs";

        [MenuItem("Hezarfen/Aydinlatma/LOD gecislerini yumusat")]
        public static void Uygula()
        {
            int taranan = 0, degisen = 0, lodsuz = 0;

            foreach (string guid in AssetDatabase.FindAssets(
                         "t:Prefab", new[] { PrefabKlasoru }))
            {
                string yol = AssetDatabase.GUIDToAssetPath(guid);
                var kok = AssetDatabase.LoadAssetAtPath<GameObject>(yol);
                if (kok == null) continue;
                taranan++;

                var gruplar = kok.GetComponentsInChildren<LODGroup>(true);
                if (gruplar.Length == 0) { lodsuz++; continue; }

                bool dokunuldu = false;
                foreach (var g in gruplar)
                {
                    if (g.fadeMode != LODFadeMode.CrossFade)
                    {
                        g.fadeMode = LODFadeMode.CrossFade;
                        dokunuldu = true;
                    }

                    var lodlar = g.GetLODs();
                    for (int i = 0; i < lodlar.Length; i++)
                    {
                        if (lodlar[i].fadeTransitionWidth >= GecisBandi - 1e-4f)
                            continue;
                        lodlar[i].fadeTransitionWidth = GecisBandi;
                        dokunuldu = true;
                    }
                    if (dokunuldu) g.SetLODs(lodlar);
                }

                if (!dokunuldu) continue;
                EditorUtility.SetDirty(kok);
                PrefabUtility.SavePrefabAsset(kok);
                degisen++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[Hezarfen] LOD gecisi: {taranan} prefab tarandi, "
                      + $"{degisen} tanesinde sert sicrama YUMUSATILDI, "
                      + $"{lodsuz} tanesinde LODGroup yok. "
                      + $"Bant {GecisBandi:0.00}.");
        }

        /// <summary>
        /// Kaç prefabta hâlâ sert sıçrama var — test okur.
        ///
        /// Sayı bir ölçümdür: "komutu çalıştırdım" demek yetmez, kaçının
        /// düzeldiği yazmalı.
        /// </summary>
        public static int SertSicramaSayisi()
        {
            int n = 0;
            foreach (string guid in AssetDatabase.FindAssets(
                         "t:Prefab", new[] { PrefabKlasoru }))
            {
                var kok = AssetDatabase.LoadAssetAtPath<GameObject>(
                    AssetDatabase.GUIDToAssetPath(guid));
                if (kok == null) continue;
                foreach (var g in kok.GetComponentsInChildren<LODGroup>(true))
                {
                    if (g.fadeMode != LODFadeMode.CrossFade) { n++; break; }
                    bool sert = false;
                    foreach (var l in g.GetLODs())
                        if (l.fadeTransitionWidth < 1e-4f) sert = true;
                    if (sert) { n++; break; }
                }
            }
            return n;
        }
    }
}
