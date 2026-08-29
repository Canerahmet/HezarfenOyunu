using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Hezarfen.Editor.Player
{
    /// <summary>
    /// <b>Karakter hangi yöne bakıyor — ölçer ve gerekirse çevirir.</b>
    ///
    /// Caner (2026-08-29): *"omuz ustu kamerasinda karakterin arkasindan
    /// olmasi gerekirken ters olmus gibi onden bakiyor kamera."*
    ///
    /// ## Kamera suçsuz çıktı
    ///
    /// Önce kamerayı ölçtüm: oyun çalışırken kamera oyuncunun
    /// <b>3,20 m arkasında</b> (ileri eksenine izdüşümü pozitif). Yani
    /// kadraj doğruydu. Sonra iki kare alındı: oyuncunun <b>önünden</b>
    /// bakınca <b>ense</b> görünüyor, <b>arkasından</b> bakınca <b>yüz</b>.
    /// Ters olan kamera değil, <b>model</b>.
    ///
    /// ## Neden testler yakalamadı
    ///
    /// <c>KameraKipi</c> testleri kameranın gövdenin arkasında olduğunu
    /// sınıyordu ve o doğruydu. Modelin hangi yöne baktığını sınayan
    /// hiçbir şey yoktu. <b>Ölçülmeyen yön, olmayan yöndür.</b>
    ///
    /// ## Ölçülebilir belirteç: burun
    ///
    /// Gövde önden-arkadan neredeyse simetrik (ayak +0,275 / −0,271; baş
    /// +0,111 / −0,107), yani kaba ölçü yön söylemiyor. Ama <b>yüz
    /// hizasında, merkez şeridinde</b> burun belirgin bir çıkıntıdır:
    /// ölçüldüğünde ileri +0,107, geri <b>−0,128</b> çıktı — burun geride.
    /// Bu, bir teste bağlanabilecek kadar keskin bir sayı.
    ///
    /// ## Neden prefabta düzeltiliyor
    ///
    /// Aynı prefab oyuncuya, NPC'lere ve uçuş dizisine gidiyor. Oyuncu
    /// tarafında 180° eklemek NPC'leri ters bırakırdı — yani şehirdeki
    /// herkes geri geri yürümeye devam ederdi.
    /// </summary>
    public static class KarakterYonu
    {
        private static readonly string[] Prefablar =
        {
            "Assets/_Project/Art/Prefabs/PF_Hezarfen_Sivil.prefab",
            "Assets/_Project/Art/Prefabs/PF_Hezarfen_Ucus.prefab",
        };

        /// <summary>Yüz bandı — boyun oranı olarak.</summary>
        public const float BandAlt = 0.86f, BandUst = 0.93f;

        /// <summary>Merkez şeridinin yarı genişliği (m).</summary>
        public const float MerkezSerit = 0.035f;

        [MenuItem("Hezarfen/Boru Hatti/Karakter yonunu duzelt")]
        public static void Duzelt()
        {
            foreach (string yol in Prefablar)
            {
                var pf = AssetDatabase.LoadAssetAtPath<GameObject>(yol);
                if (pf == null)
                {
                    Debug.LogWarning($"[Hezarfen] {yol} yok — atlandi.");
                    continue;
                }

                float once = YuzYonu(pf);
                if (float.IsNaN(once))
                {
                    Debug.LogWarning($"[Hezarfen] {pf.name}: yon olculemedi "
                                     + "(SkinnedMeshRenderer yok?).");
                    continue;
                }

                if (once > 0f)
                {
                    Debug.Log($"[Hezarfen] {pf.name}: yuz ZATEN ileri "
                              + $"({once:+0.000;-0.000} m) — dokunulmadi.");
                    continue;
                }

                Cevir(yol);
                var yeni = AssetDatabase.LoadAssetAtPath<GameObject>(yol);
                float sonra = YuzYonu(yeni);
                Debug.Log($"[Hezarfen] {pf.name}: yuz {once:+0.000;-0.000} -> "
                          + $"{sonra:+0.000;-0.000} m (180 derece cevrildi).");
            }
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Yüzün baktığı yön — <b>pozitif = +Z (ileri), doğru</b>.
        ///
        /// Değer, yüz bandındaki merkez şeridinin ileri uzanımı eksi geri
        /// uzanımıdır: burun hangi taraftaysa o taraf büyük çıkar.
        /// </summary>
        public static float YuzYonu(GameObject prefab)
        {
            var ornek = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            try { return YuzYonuOrnekten(ornek); }
            finally { Object.DestroyImmediate(ornek); }
        }

        /// <summary>
        /// Aynı ölçüm, <b>sahnedeki bir örnek</b> üzerinde.
        ///
        /// Ayrı durması testin ölçütü sınayabilmesi için: örneği 180°
        /// çevirip işaretin döndüğünü görmek, ölçütün gerçekten yönü
        /// okuduğunu kanıtlar. Yoksa yeşil bir test hiçbir şey söylemez.
        /// </summary>
        public static float YuzYonuOrnekten(GameObject ornek)
        {
            {
                var smr = ornek.GetComponentInChildren<SkinnedMeshRenderer>();
                if (smr == null || smr.sharedMesh == null) return float.NaN;

                var mesh = new Mesh();
                smr.BakeMesh(mesh, true);
                var v = mesh.vertices;
                var m = smr.transform.localToWorldMatrix;

                float yMin = float.MaxValue, yMax = float.MinValue;
                var d = new Vector3[v.Length];
                for (int i = 0; i < v.Length; i++)
                {
                    d[i] = m.MultiplyPoint3x4(v[i]);
                    yMin = Mathf.Min(yMin, d[i].y);
                    yMax = Mathf.Max(yMax, d[i].y);
                }
                float boy = yMax - yMin;
                if (boy < 0.1f) { Object.DestroyImmediate(mesh); return float.NaN; }

                var merkez = Vector3.zero;
                foreach (var p in d) merkez += p;
                merkez /= d.Length;

                float ileriEnCok = float.MinValue, geriEnCok = float.MaxValue;
                foreach (var p in d)
                {
                    float t = (p.y - yMin) / boy;
                    if (t < BandAlt || t > BandUst) continue;
                    var yerel = p - merkez;
                    if (Mathf.Abs(yerel.x) > MerkezSerit) continue;
                    ileriEnCok = Mathf.Max(ileriEnCok, yerel.z);
                    geriEnCok = Mathf.Min(geriEnCok, yerel.z);
                }
                Object.DestroyImmediate(mesh);
                if (ileriEnCok == float.MinValue) return float.NaN;

                // Burun hangi tarafta daha cok cikiyor.
                return ileriEnCok - (-geriEnCok);
            }
        }

        /// <summary>
        /// Prefabın model çocuklarını kökün Y ekseni etrafında 180° çevirir.
        ///
        /// Kökün kendisi çevrilmez: kök, oyuncunun/NPC'nin dönüşünü taşır ve
        /// onu çevirmek hareket yönünü de çevirirdi. Çevrilen şey modelin
        /// <b>kökün içindeki</b> duruşudur.
        /// </summary>
        private static void Cevir(string yol)
        {
            var kok = PrefabUtility.LoadPrefabContents(yol);
            try
            {
                var yariDonus = Quaternion.Euler(0f, 180f, 0f);
                var cocuklar = new List<Transform>();
                foreach (Transform t in kok.transform) cocuklar.Add(t);

                foreach (var t in cocuklar)
                {
                    t.localPosition = yariDonus * t.localPosition;
                    t.localRotation = yariDonus * t.localRotation;
                }
                PrefabUtility.SaveAsPrefabAsset(kok, yol);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(kok);
            }
        }
    }
}
