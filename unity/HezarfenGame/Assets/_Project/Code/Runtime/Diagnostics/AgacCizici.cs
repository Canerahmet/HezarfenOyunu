using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Hezarfen.Tani
{
    /// <summary>
    /// <b>42 857 ağacı örneklemeyle çizer</b> — hiçbir geometri
    /// üretmeden (ADR 0073).
    ///
    /// ## Neden bu, birleştirilmiş mesh değil
    ///
    /// Önce ağaçlar 64 m'lik hücrelerde tek mesh'te birleştirildi ve
    /// performans hedefi tutturuldu (kule turu 17,83 → 8,86 ms). Ama
    /// üretilen geometrinin bir yeri olması gerekiyordu ve iki denemede
    /// de yer bulunamadı:
    ///
    /// <list type="bullet">
    /// <item>mesh'ler bellekte bırakılınca Unity onları <b>sahneye</b>
    ///   gömdü: 23,7 MB → <b>805 MB</b>.</item>
    /// <item>varlık olarak yazılınca aynı şişkinlik klasöre taşındı:
    ///   <b>~900 MB</b> mesh varlığı.</item>
    /// </list>
    ///
    /// İkisi de aynı hatanın iki yüzü: <b>zaten var olan bir bilgiyi
    /// ikinci kez saklamak</b>. Ağaçların yeri arazi verisinde duruyor
    /// (<c>TerrainData.treeInstances</c>); ondan geometri türetip diske
    /// yazmak, aynı ormanı iki kere depolamaktı.
    ///
    /// Burada hiçbir şey türetilmiyor: konumlar arazi verisinden okunuyor,
    /// dönüşüm matrisleri belleğe kuruluyor (42 857 × 64 bayt ≈ 2,7 MB) ve
    /// GPU örneklemesiyle çiziliyor. <b>Diskte sıfır bayt.</b>
    ///
    /// ## Maliyet
    ///
    /// Örnekleme çağrısı başına en çok 1023 örnek: 42 857 ağaç ≈ 42 çağrı,
    /// üstelik yalnız görünen hücreler. Arazinin kendi ağaç çizimi
    /// (~19 600 çağrı) kapatılıyor.
    /// </summary>
    [DefaultExecutionOrder(-30)]
    public class AgacCizici : MonoBehaviour
    {
        [Tooltip("Ağaçları taşıyan arazi. Boşsa sahnedeki ilk arazi.")]
        public Terrain arazi;

        [Tooltip("Kesme için hücre kenarı (m).")]
        public float hucreKenari = 128f;

        [Tooltip("Bu mesafenin ötesindeki hücreler çizilmez (m).")]
        public float gorusMesafesi = 3000f;

        [Tooltip("Ağaçlar gölge versin mi.")]
        public bool golgeVer = true;

        /// <summary>Bu karede çizilen ağaç sayısı — ölçüm okur.</summary>
        public int CizilenAgac { get; private set; }

        /// <summary>Bu karede yapılan örnekleme çağrısı sayısı.</summary>
        public int CizimCagrisi { get; private set; }

        private sealed class Kutu
        {
            public Bounds sinir;
            // Prototip basina matrisler: [prototip][matris]
            public List<Matrix4x4>[] matrisler;
        }

        private Mesh[] _mesh;
        private Material[] _mat;
        private readonly List<Kutu> _kutular = new();
        private readonly Plane[] _duzlemler = new Plane[6];
        private Matrix4x4[] _tampon;

        private void Awake() => Kur();

        /// <summary>Arazi verisinden matrisleri kurar. Bir kez.</summary>
        public void Kur()
        {
            _kutular.Clear();
            if (arazi == null) arazi = FindAnyObjectByType<Terrain>();
            if (arazi == null) return;

            var data = arazi.terrainData;
            var proto = data.treePrototypes;
            _mesh = new Mesh[proto.Length];
            _mat = new Material[proto.Length];
            for (int i = 0; i < proto.Length; i++)
                KabaLod(proto[i].prefab, out _mesh[i], out _mat[i]);

            Vector3 tPos = arazi.transform.position;
            Vector3 boyut = data.size;
            var harita = new Dictionary<Vector2Int, Kutu>();

            foreach (var ti in data.treeInstances)
            {
                int pi = ti.prototypeIndex;
                if (pi < 0 || pi >= _mesh.Length || _mesh[pi] == null) continue;

                Vector3 d = new Vector3(tPos.x + ti.position.x * boyut.x,
                                        tPos.y + ti.position.y * boyut.y,
                                        tPos.z + ti.position.z * boyut.z);

                var anahtar = new Vector2Int(
                    Mathf.FloorToInt(d.x / hucreKenari),
                    Mathf.FloorToInt(d.z / hucreKenari));

                if (!harita.TryGetValue(anahtar, out var kutu))
                {
                    kutu = new Kutu
                    {
                        sinir = new Bounds(d, Vector3.one),
                        matrisler = new List<Matrix4x4>[_mesh.Length],
                    };
                    harita[anahtar] = kutu;
                    _kutular.Add(kutu);
                }

                kutu.matrisler[pi] ??= new List<Matrix4x4>();
                kutu.matrisler[pi].Add(Matrix4x4.TRS(
                    d, Quaternion.Euler(0f, ti.rotation * Mathf.Rad2Deg, 0f),
                    new Vector3(ti.widthScale, ti.heightScale, ti.widthScale)));

                // Sinir agacin BOYUNU da icermeli; yoksa tepesi ekranda
                // dururken hucre elenip orman goz onunde kayboluyor.
                kutu.sinir.Encapsulate(d + Vector3.up * 25f);
                kutu.sinir.Encapsulate(d - Vector3.up * 2f);
            }

            _tampon = new Matrix4x4[1023];

            // Arazi kendi agaclarini CIZMEZ: ayni orman iki kez cizilirdi.
            arazi.treeDistance = 0f;
        }

        private void LateUpdate()
        {
            CizilenAgac = 0; CizimCagrisi = 0;
            var kam = Camera.main;
            if (kam == null || _mesh == null) return;

            GeometryUtility.CalculateFrustumPlanes(kam, _duzlemler);
            Vector3 goz = kam.transform.position;
            float menzil2 = gorusMesafesi * gorusMesafesi;

            var golge = golgeVer ? ShadowCastingMode.On : ShadowCastingMode.Off;

            foreach (var kutu in _kutular)
            {
                if (kutu.sinir.SqrDistance(goz) > menzil2) continue;
                if (!GeometryUtility.TestPlanesAABB(_duzlemler, kutu.sinir))
                    continue;

                for (int pi = 0; pi < _mesh.Length; pi++)
                {
                    var liste = kutu.matrisler[pi];
                    if (liste == null || liste.Count == 0) continue;
                    if (_mat[pi] == null) continue;

                    var rp = new RenderParams(_mat[pi])
                    {
                        shadowCastingMode = golge,
                        receiveShadows = false,
                        worldBounds = kutu.sinir,
                    };

                    for (int bas = 0; bas < liste.Count; bas += 1023)
                    {
                        int n = Mathf.Min(1023, liste.Count - bas);
                        liste.CopyTo(bas, _tampon, 0, n);
                        Graphics.RenderMeshInstanced(rp, _mesh[pi], 0,
                                                     _tampon, n);
                        CizimCagrisi++;
                        CizilenAgac += n;
                    }
                }
            }
        }

        /// <summary>Prefabın en kaba LOD mesh'i ve malzemesi.</summary>
        private static void KabaLod(GameObject prefab, out Mesh mesh,
                                    out Material mat)
        {
            mesh = null; mat = null;
            if (prefab == null) return;

            Renderer[] adaylar;
            var lg = prefab.GetComponent<LODGroup>();
            if (lg != null && lg.lodCount > 0)
            {
                var lods = lg.GetLODs();
                adaylar = lods[lods.Length - 1].renderers;
            }
            else adaylar = prefab.GetComponentsInChildren<MeshRenderer>(true);

            foreach (var r in adaylar)
            {
                if (r == null) continue;
                var mf = r.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;
                if (mesh == null
                    || mf.sharedMesh.triangles.Length > mesh.triangles.Length)
                { mesh = mf.sharedMesh; mat = r.sharedMaterial; }
            }
        }
    }
}
