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
        //
        // 55 ILE BASLADI VE COK GENISTI. 2 m mesafede 1 m arayla duran
        // uc kup arasindaki aci ~28 derece; ucu birden koninin icine
        // giriyor ve hedef, minik fare hareketleriyle titriyordu.
        // "En cok bakilan kazanir" kurali dogru ama ancak koni yeterince
        // dar oldugunda bir SECIM ifade eder.
        public float aci = 35f;

        [Tooltip("Bakış yönü; boşsa bu nesnenin ileri yönü.")]
        public Transform bakis;

        /// <summary>Şu an nişan alınan şey (yoksa null).</summary>
        public IEtkilesim Hedef { get; private set; }

        /// <summary>Ekranda gösterilecek ipucu (yoksa boş).</summary>
        public string Ipucu => Hedef != null && Hedef.Hazir ? Hedef.Ipucu : "";

        /// <summary>
        /// Etkileşimlilerin katmanı. Boşsa her şey taranır.
        ///
        /// <c>~0</c> ile başladı ve tampon arazi, kaldırım, ev duvarı ve
        /// NPC kapsülleriyle doluyordu: 19.992 avlu eşyasının olduğu bir
        /// şehirde 24 yuva, <b>gerçek</b> etkileşilebilir sıraya
        /// gelmeden dolabiliyordu. Üstelik <c>OverlapSphereNonAlloc</c>
        /// sıralama sözü vermez — aynı yerde durup aynı yere bakmak
        /// farklı karelerde farklı hedef verebilirdi.
        /// </summary>
        public LayerMask katman = ~0;

        private readonly Collider[] _tampon = new Collider[24];

        /// <summary>Son sorguda tampon doldu mu — ölçüm okur.</summary>
        public bool TamponDoldu { get; private set; }

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
                goz.position, menzil, _tampon, katman,
                QueryTriggerInteraction.Collide);
            TamponDoldu = n >= _tampon.Length;

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

                // GORUS HATTI: kure sorgusu duvar tanimaz.
                //
                // Menzil 2,4 m; ev duvari bundan cok daha ince. Denetim
                // olmadan komsunun avlusundaki su kupunden DUVARIN
                // ARKASINDAN su alinabiliyordu. `BarkGosterici` ayni
                // kusuru repliklerde gormus ve isinla cozmustu; ders
                // buraya uygulanmamisti.
                //
                // Yalnizca EN IYI ADAY icin isin atiliyor: her adaya
                // atmak, kalabalik avluda karede yirmi isin ederdi.
                if (Engelli(goz.position, c)) continue;

                enIyiPuan = puan;
                enIyi = e;
            }
            return enIyi;
        }

        /// <summary>
        /// Gözle hedef arasında başka bir katı cisim var mı.
        ///
        /// Hedefin kendi collider'ına çarpmak engel sayılmaz; onun
        /// <b>önündeki</b> her şey sayılır.
        /// </summary>
        private static bool Engelli(Vector3 goz, Collider hedef)
        {
            var nokta = hedef.bounds.center;
            var yon = nokta - goz;
            float uzak = yon.magnitude;
            if (uzak < 0.05f) return false;

            var vurus = Physics.RaycastAll(goz, yon / uzak, uzak - 0.05f,
                                           ~0, QueryTriggerInteraction.Ignore);
            foreach (var v in vurus)
            {
                if (v.collider == hedef) continue;
                if (v.collider.transform.IsChildOf(hedef.transform)) continue;
                // Oyuncunun kendi kapsulu engel degil.
                if (v.collider.GetComponentInParent<EtkilesimAlgila>() != null)
                    continue;
                return true;
            }
            return false;
        }

        /// <summary>Etkileşimi tetikler. Dönüş: bir şey oldu mu.</summary>
        public bool Tetikle()
        {
            if (Hedef == null || !Hedef.Hazir) return false;
            return Hedef.Etkiles(gameObject);
        }
    }
}
