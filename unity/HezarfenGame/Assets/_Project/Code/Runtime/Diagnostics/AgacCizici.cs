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

        /// <summary>
        /// Kesme için hücre kenarı (m). <b>256 — 128 değil.</b>
        ///
        /// Hücre hem kesme birimi hem <b>örnekleme partisi</b>: her hücre,
        /// her prototip ve her alt-mesh için ayrı bir çizim çağrısı çıkar.
        /// Kırsal doku sıklaşınca ölçüldü — 20.282 ağaç <b>1.138 çizim
        /// çağrısı</b> ediyordu, yani parti başına ~18 ağaç. İnstancing'in
        /// anlamı büyük partidir; 18'lik partide sürücü yükü kazancı yer.
        ///
        /// Kenarı ikiye katlamak hücre sayısını dörde böler. Bedeli, kesme
        /// tanesinin kabalaşması: kadrajın kenarındaki bir hücrenin görünmeyen
        /// kısmı da çizilir. Ağaç ucuz, çizim çağrısı pahalı — bu takas
        /// ölçülerek seçildi.
        /// </summary>
        public float hucreKenari = 256f;

        [Tooltip("Bu mesafenin ötesindeki hücreler çizilmez (m).")]
        public float gorusMesafesi = 3000f;

        [Tooltip("Ağaçlar gölge versin mi.")]
        public bool golgeVer = true;

        [Tooltip("Bu mesafeden yakın hücreler İNCE LOD ile çizilir (m).")]
        public float inceMesafe = 220f;

        [Tooltip("Rüzgârda savrulma açısının tepe değeri (derece).")]
        public float savrulmaDerece = 3.5f;

        [Tooltip("Savrulma bu mesafeye kadar hesaplanır (m).")]
        public float savrulmaMesafesi = 260f;

        /// <summary>Bu karede ince LOD ile çizilen ağaç sayısı.</summary>
        public int InceAgac { get; private set; }

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

        /// <summary>
        /// Bir LOD'un tek bir çizim parçası: mesh + malzeme.
        ///
        /// <b>Bir ağaç birden çok parça taşır</b> — kabuk ve yaprak ayrı
        /// çiziciler. İlk yazımda prototip başına TEK mesh alınıyordu ve
        /// en çok üçgenli olan seçiliyordu; uzakta yaprak baskın olduğu
        /// için kimse farketmedi, ama yakın LOD açılınca ağaçlar
        /// <b>çıplak kahverengi çubuklara</b> döndü: yalnız gövde
        /// çiziliyordu. Parçaların hepsi çizilmeli.
        /// </summary>
        private struct Parca
        {
            public Mesh mesh;
            public Material mat;
            /// <summary>Hangi alt-mesh — kabuk ve yaprak AYRI.</summary>
            public int altMesh;
        }

        private Parca[][] _kaba;     // [prototip][parca] uzak
        private Parca[][] _ince;     // [prototip][parca] yakin
        private readonly List<Kutu> _kutular = new();
        private readonly Plane[] _duzlemler = new Plane[6];
        private Matrix4x4[] _tampon;
        private static readonly int RuzgarKimlik =
            Shader.PropertyToID("_HZ_Ruzgar");
        private Vector3 ruzgarYon;
        private float ruzgarHizi;

        /// <summary>
        /// Tampondaki ilk <paramref name="n"/> matrisi rüzgârda eğer.
        ///
        /// Eğme ağacın <b>tabanı etrafında</b> yapılır: merkezden
        /// döndürmek gövdeyi yerden koparırdı. Faz dünya konumundan
        /// geliyor, böylece komşu ağaçlar aynı anda eğilmez — hepsi
        /// birlikte sallanan bir orman mekanik görünürdü.
        /// </summary>
        private void Savur(int n)
        {
            float aci = savrulmaDerece
                        * Mathf.Clamp01(ruzgarHizi / 15f);
            var eksen = Vector3.Cross(Vector3.up, ruzgarYon.normalized);
            if (eksen.sqrMagnitude < 1e-4f) return;

            for (int i = 0; i < n; i++)
            {
                Vector3 taban = _tampon[i].GetColumn(3);
                float faz = taban.x * 0.13f + taban.z * 0.17f;
                float s = Mathf.Sin(Time.time * 1.1f + faz) * 0.85f
                        + Mathf.Sin(Time.time * 2.7f + faz * 1.9f) * 0.15f;

                var don = Matrix4x4.Rotate(
                    Quaternion.AngleAxis(aci * s, eksen));
                _tampon[i] = Matrix4x4.Translate(taban) * don
                             * Matrix4x4.Translate(-taban) * _tampon[i];
            }
        }

        private void Awake() => Kur();

        /// <summary>Arazi verisinden matrisleri kurar. Bir kez.</summary>
        public void Kur()
        {
            _kutular.Clear();
            if (arazi == null) arazi = FindAnyObjectByType<Terrain>();
            if (arazi == null) return;

            var data = arazi.terrainData;
            var proto = data.treePrototypes;
            _kaba = new Parca[proto.Length][];
            _ince = new Parca[proto.Length][];
            for (int i = 0; i < proto.Length; i++)
            {
                _kaba[i] = Parcalar(proto[i].prefab, sonLod: true);
                _ince[i] = Parcalar(proto[i].prefab, sonLod: false);
            }

            Vector3 tPos = arazi.transform.position;
            Vector3 boyut = data.size;
            var harita = new Dictionary<Vector2Int, Kutu>();

            foreach (var ti in data.treeInstances)
            {
                int pi = ti.prototypeIndex;
                if (pi < 0 || pi >= _kaba.Length
                    || _kaba[pi] == null || _kaba[pi].Length == 0) continue;

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
                        matrisler = new List<Matrix4x4>[_kaba.Length],
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
            CizilenAgac = 0; CizimCagrisi = 0; InceAgac = 0;
            var kam = Camera.main;
            if (kam == null || _kaba == null) return;

            GeometryUtility.CalculateFrustumPlanes(kam, _duzlemler);
            Vector3 goz = kam.transform.position;
            float menzil2 = gorusMesafesi * gorusMesafesi;

            var golge = golgeVer ? ShadowCastingMode.On : ShadowCastingMode.Off;

            // Ruzgar: HavaProfili'nin kuresel vektoru (xyz yon, w hiz).
            var rv = Shader.GetGlobalVector(RuzgarKimlik);
            ruzgarYon = new Vector3(rv.x, 0f, rv.z);
            ruzgarHizi = rv.w;

            foreach (var kutu in _kutular)
            {
                if (kutu.sinir.SqrDistance(goz) > menzil2) continue;
                if (!GeometryUtility.TestPlanesAABB(_duzlemler, kutu.sinir))
                    continue;

                // HUCRE YAKINSA INCE LOD.
                //
                // Ilk yazimda her mesafede kaba LOD kullaniliyordu ve
                // Caner bunu oynarken gorurdu: bir servinin dibinde
                // duruyorsun ve agac uzaktan gorunen kaba silueti
                // tasiyor. Kaba LOD uzagin isi; hucre yakinsa incesi
                // cizilir.
                bool yakin = kutu.sinir.SqrDistance(goz)
                             < inceMesafe * inceMesafe;
                bool savrulur = savrulmaDerece > 0.01f && ruzgarHizi > 0.05f
                    && kutu.sinir.SqrDistance(goz)
                       < savrulmaMesafesi * savrulmaMesafesi;

                for (int pi = 0; pi < _kaba.Length; pi++)
                {
                    var liste = kutu.matrisler[pi];
                    if (liste == null || liste.Count == 0) continue;

                    var parcalar = yakin && _ince[pi] != null
                                   && _ince[pi].Length > 0
                        ? _ince[pi] : _kaba[pi];
                    if (parcalar == null) continue;

                    foreach (var parca in parcalar)
                    {
                    if (parca.mesh == null || parca.mat == null) continue;

                    var rp = new RenderParams(parca.mat)
                    {
                        shadowCastingMode = golge,
                        receiveShadows = false,
                        worldBounds = kutu.sinir,
                    };

                    for (int bas = 0; bas < liste.Count; bas += 1023)
                    {
                        int n = Mathf.Min(1023, liste.Count - bas);
                        liste.CopyTo(bas, _tampon, 0, n);

                        // SAVRULMA YALNIZ YAKINDA.
                        //
                        // Rüzgâr 42 857 ağacın hepsinde hesaplanabilirdi
                        // ama uzaktaki bir servinin üç derece eğilmesi
                        // ekranda tek piksel etmez. Görünmediği yerde
                        // hesaplamamak, savrulmayı bedavaya yakın kılıyor.
                        //
                        // Açı, HavaProfili'nin yayınladığı TEK rüzgâr
                        // vektöründen türüyor — uçuş fiziği, dalga ve
                        // bulut da onu okuyor. İkinci bir rüzgâr yok.
                        if (savrulur) Savur(n);
                        Graphics.RenderMeshInstanced(rp, parca.mesh,
                                                     parca.altMesh,
                                                     _tampon, n);
                        CizimCagrisi++;
                        CizilenAgac += n;
                        if (yakin) InceAgac += n;
                    }
                    }
                }
            }
        }

        /// <summary>
        /// Prefabın bir LOD'undaki <b>bütün</b> çizim parçaları.
        ///
        /// <paramref name="sonLod"/> doğruysa en kaba LOD (uzak), yanlışsa
        /// LOD0 (yakın). Parçaların <b>hepsi</b> döner — bir ağaç kabuk ve
        /// yaprak olarak ayrı çizicilerde durur ve yalnız birini çizmek
        /// gövdeyi yapraksız bırakır.
        /// </summary>
        private static Parca[] Parcalar(GameObject prefab, bool sonLod)
        {
            if (prefab == null) return new Parca[0];

            Renderer[] adaylar;
            var lg = prefab.GetComponent<LODGroup>();
            if (lg != null && lg.lodCount > 0)
            {
                var lods = lg.GetLODs();
                adaylar = lods[sonLod ? lods.Length - 1 : 0].renderers;
            }
            else adaylar = prefab.GetComponentsInChildren<MeshRenderer>(true);

            var liste = new List<Parca>();
            foreach (var r in adaylar)
            {
                if (r == null) continue;
                var mf = r.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;

                // HER ALT-MESH AYRI CIZILIR.
                //
                // Agac tek bir birlesik mesh ama IKI alt-mesh tasiyor:
                // kabuk ve yaprak, ayri malzemelerle. Ilk yazimda hep
                // alt-mesh 0 ciziliyordu ve sonuc oyunda gorundu —
                // yakindaki agaclar CIPLAK KAHVERENGI CUBUKLARDI, cunku
                // yalniz govde ciziliyordu. Sayac "3175 agac cizildi"
                // diyordu ve dogruydu; eksik olan agacin yarisiydi.
                var malzemeler = r.sharedMaterials;
                int n = Mathf.Min(mf.sharedMesh.subMeshCount,
                                  malzemeler.Length);
                for (int i = 0; i < n; i++)
                {
                    if (malzemeler[i] == null) continue;
                    liste.Add(new Parca { mesh = mf.sharedMesh,
                                          mat = malzemeler[i],
                                          altMesh = i });
                }
            }
            return liste.ToArray();
        }
    }
}
