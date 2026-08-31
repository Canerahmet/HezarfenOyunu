using Hezarfen.Sehir;
using UnityEngine;

namespace Hezarfen.Player
{
    /// <summary>
    /// <b>Oyuncunun önündeki şeyi bulur.</b>
    ///
    /// Etkileşim, hangi nesnenin "kastedildiğini" bilmekten ibarettir
    /// ve bunu yanlış yapan oyun, oyuncuyu nesnenin etrafında dans
    /// ettirir. Burada ölçüt iki parçalı: <b>menzil</b> ve <b>bakış</b>.
    /// En yakın olan değil, <b>en çok bakılan</b> kazanır — kalabalık
    /// bir avluda üç küp yan yanadır ve oyuncu hangisine baktığını
    /// bilir.
    ///
    /// ## Neden fizik sorgusu, neden liste değil
    ///
    /// Şehirde 19.992 avlu eşyası var; hepsini bir listede tutup her
    /// karede taramak, kalabalığın 2,1 ms'sinin yanına ikinci bir
    /// külfet koyardı. <c>OverlapSphere</c> yalnız yakındakileri
    /// döndürür ve fizik dünyası bu sorguyu zaten hızlandırılmış
    /// tutar.
    /// </summary>
    [AddComponentMenu("Hezarfen/Etkilesim Algila")]
    public class EtkilesimAlgila : MonoBehaviour
    {
        [Tooltip("Bu mesafeye kadar uzanılabilir (m).")]
        public float menzil = 2.4f;

        [Tooltip("Bakışla nesne arasındaki en çok açı (derece).")]
        public float aci = 55f;

        [Tooltip("Bakış yönü; boşsa bu nesnenin ileri yönü.")]
        public Transform bakis;

        /// <summary>Şu an nişan alınan şey (yoksa null).</summary>
        public IEtkilesim Hedef { get; private set; }

        /// <summary>Ekranda gösterilecek ipucu (yoksa boş).</summary>
        public string Ipucu => Hedef != null && Hedef.Hazir ? Hedef.Ipucu : "";

        private readonly Collider[] _tampon = new Collider[24];

        private void Update()
        {
            Hedef = EnIyiHedef();
        }

        /// <summary>
        /// Menzildeki en iyi hedef. Ayrı bir metot çünkü test
        /// edilebilir olması gereken şey seçim kuralı — girdi
        /// okumak değil.
        /// </summary>
        public IEtkilesim EnIyiHedef()
        {
            var goz = bakis != null ? bakis : transform;
            int n = Physics.OverlapSphereNonAlloc(
                goz.position, menzil, _tampon, ~0,
                QueryTriggerInteraction.Collide);

            IEtkilesim enIyi = null;
            float enIyiPuan = Mathf.Cos(aci * Mathf.Deg2Rad);

            for (int i = 0; i < n; i++)
            {
                var c = _tampon[i];
                if (c == null) continue;
                var e = c.GetComponentInParent<IEtkilesim>();
                if (e == null || !e.Hazir) continue;

                var d = c.bounds.center - goz.position;
                d.y *= 0.5f;                    // dikey fark daha az onemli
                if (d.sqrMagnitude < 1e-4f) continue;
                float puan = Vector3.Dot(goz.forward, d.normalized);
                if (puan <= enIyiPuan) continue;
                enIyiPuan = puan;
                enIyi = e;
            }
            return enIyi;
        }

        /// <summary>Etkileşimi tetikler. Dönüş: bir şey oldu mu.</summary>
        public bool Tetikle()
        {
            if (Hedef == null || !Hedef.Hazir) return false;
            return Hedef.Etkiles(gameObject);
        }
    }
}
