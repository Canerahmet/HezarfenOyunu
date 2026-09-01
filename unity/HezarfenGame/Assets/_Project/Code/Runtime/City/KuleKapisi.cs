using UnityEngine;

namespace Hezarfen.Sehir
{
    /// <summary>
    /// <b>Kuleye çıkış.</b>
    ///
    /// ## Neden bu bileşen var
    ///
    /// Oyunun adını taşıyan fiil — kuleden atlamak — <b>oyuncuya
    /// kapalıydı</b>. Kulenin iç mekânı yok, tırmanma mekaniği yok
    /// (<c>HezarfenAnimator.Tirmaniyor</c> "dışarıdan bir tetikleyici
    /// alan set eder" diyor ve depoda onu set eden sıfır satır var).
    /// Uçuşu ölçen tek araç oyuncuyu <c>govde.position = tepe</c> ile
    /// <b>ışınlıyordu</b>: yani bu uçuş hiçbir zaman bir oyuncunun
    /// erişebileceği bir şey olmadı.
    ///
    /// Perdenin kapısı da bunu görmüyordu — kule <b>dibinde</b> G ve
    /// Space'e basmak "kalkış" sayılıyordu ve oyuncu doruk noktasını
    /// yaşamadan geçiyordu.
    ///
    /// ## Neden iç mekân değil
    ///
    /// 1632'de Galata Kulesi bir tersane ambarı ve zindandır
    /// (RESEARCH); merdiveni oynanabilir bir mekân olarak kurmak ayrı
    /// bir tur. Dürüst ve ucuz olan şey, kapıyı bir <b>kapı</b> yapmak:
    /// açılır, çıkılır, şerefede bitilir. Bir mekânı taklit etmiyoruz,
    /// bir geçişi anlatıyoruz.
    /// </summary>
    [AddComponentMenu("Hezarfen/Kule Kapisi")]
    public class KuleKapisi : MonoBehaviour, IEtkilesim
    {
        [Tooltip("Şerefenin kotu (m) — dünya orijini kule tabanıdır.")]
        public float serefeKotu = 46f;

        [Tooltip("Şerefede oyuncunun duracağı yarıçap (m).")]
        public float serefeYaricapi = 3.2f;

        public string Ipucu => "Kuleye çık";
        public bool Hazir => true;

        public bool Etkiles(GameObject aktor)
        {
            var cc = aktor.GetComponentInParent<CharacterController>();
            var kok = cc != null ? cc.transform : aktor.transform;

            // Serefede nereye cikilacagi HESAPLANMAZ, SORULUR.
            //
            // Kule tabaninin kotunu arazi belirler ve bu depoda
            // "hesaplanan yukseklik" iki kez yanlis cikti. Isin
            // yukaridan asagi bakar; carptigi yer serefedir.
            var tepe = transform.position + Vector3.up * serefeKotu;
            var yon = -transform.forward;   // kapinin baktigi yonun tersi
            var nokta = tepe + yon * serefeYaricapi;

            if (Physics.Raycast(nokta + Vector3.up * 6f, Vector3.down,
                                out var v, 14f, ~0,
                                QueryTriggerInteraction.Ignore))
                nokta = v.point + Vector3.up * 0.15f;

            bool acikti = cc != null && cc.enabled;
            if (acikti) cc.enabled = false;
            kok.position = nokta;
            if (acikti) cc.enabled = true;
            return true;
        }
    }
}
