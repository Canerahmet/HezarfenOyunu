using UnityEngine;

namespace Hezarfen.Player
{
    /// <summary>
    /// <b>Eteğin dalgalanması.</b>
    ///
    /// ## Neden kumaş simülasyonu değil
    ///
    /// Caner'in isteği açıktı: *"kıyafet gerçekçi olsun, yürürken
    /// dalgalansın."* Doğru görünen çözüm Unity'nin <c>Cloth</c>
    /// bileşeni; ölçü onu eliyor. Şehirde aynı anda <b>60 görünür
    /// gövde</b> var ve her biri 16 bin üçgenlik bir deri; altmış
    /// kumaş çözücüsü kare bütçesinin (16,7 ms) tamamını yerdi.
    ///
    /// Bu yüzden salınım <b>kemikle</b> yapılıyor: eteğe dört zincir,
    /// her zincir iki eklem. Altmış gövde × sekiz kemik = 480 transform,
    /// yani ölçüm gürültüsü kadar. Aynı görüntünün yüzde biri fiyatına.
    ///
    /// ## Neden yay, neden sinüs değil
    ///
    /// Sabit bir sinüs etek her zaman aynı dalgayı yapar ve durunca da
    /// yapar; oyuncu bunu bir saniyede yakalar. Yay ise <b>gövdenin
    /// ivmesine</b> tepki verir: yürürken sallanır, dönünce savrulur,
    /// durunca söner. Kumaş fiziğinin oyuncuya ulaşan tarafı bu.
    /// </summary>
    [AddComponentMenu("Hezarfen/Kiyafet Salinimi")]
    public class KiyafetSalinimi : MonoBehaviour
    {
        [Tooltip("Etek kemikleri — boşsa adı 'Etek' ile başlayanlar aranır.")]
        public Transform[] kemikler;

        [Tooltip("Yayın sertliği: büyük değer daha çabuk toparlar.")]
        public float sertlik = 26f;

        [Tooltip("Sönüm: büyük değer daha çabuk durur.")]
        public float sonum = 5.5f;

        [Tooltip("En çok sapma (derece).")]
        public float enCokAci = 34f;

        private Quaternion[] _durus;
        private Vector3[] _hiz;
        private Vector3[] _sapma;
        private Vector3 _oncekiKonum;
        private Vector3 _oncekiHiz;

        private void Awake()
        {
            if (kemikler == null || kemikler.Length == 0) Bul();
            int n = kemikler != null ? kemikler.Length : 0;
            _durus = new Quaternion[n];
            _hiz = new Vector3[n];
            _sapma = new Vector3[n];
            for (int i = 0; i < n; i++)
                if (kemikler[i] != null) _durus[i] = kemikler[i].localRotation;
            _oncekiKonum = transform.position;
        }

        /// <summary>Etek kemiklerini adından bulur.</summary>
        private void Bul()
        {
            var liste = new System.Collections.Generic.List<Transform>();
            foreach (var t in GetComponentsInChildren<Transform>(true))
                if (t.name.StartsWith("Etek")) liste.Add(t);
            kemikler = liste.ToArray();
        }

        private void LateUpdate()
        {
            if (kemikler == null || kemikler.Length == 0) return;

            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            // GOVDENIN IVMESI SURUCU.
            //
            // Kumasi hareket ettiren sey hiz degil ivmedir: sabit hizla
            // giden bir etek arkaya yatar ve orada kalir; hizlanan,
            // duran ya da donen bir etek savrulur.
            var konum = transform.position;
            var hiz = (konum - _oncekiKonum) / dt;
            _oncekiKonum = konum;
            var ivme = (hiz - _oncekiHiz) / dt;
            _oncekiHiz = hiz;

            // Etek geriye yatar: hareketin TERSINE, ve yerel eksende.
            var itme = transform.InverseTransformDirection(
                -hiz * 0.055f - ivme * 0.012f);
            itme.y = 0f;

            for (int i = 0; i < kemikler.Length; i++)
            {
                var k = kemikler[i];
                if (k == null) continue;

                // Yay: hedefe dogru ivmelen, sonumle.
                var kuvvet = (itme - _sapma[i]) * sertlik
                             - _hiz[i] * sonum;
                _hiz[i] += kuvvet * dt;
                _sapma[i] += _hiz[i] * dt;

                var a = Vector3.ClampMagnitude(_sapma[i] * 60f, enCokAci);
                k.localRotation = _durus[i]
                                  * Quaternion.Euler(a.z, 0f, -a.x);
            }
        }
    }
}
