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
        /// <summary>
        /// Durulacak yerin kule tabanından yüksekliği (m).
        ///
        /// <b>46 idi ve orası KÜLAHIN TEPESİYDİ.</b> Sayıyı bir
        /// ölçümden almıştım — <c>UcusDenemesi.KuleTepesi()</c>
        /// yukarıdan aşağı ışın atıp 98,2 m okuyor — ama o ışının
        /// bulduğu şey konik ahşap külahın uç noktası. Oyuncu oraya
        /// ışınlanınca kulenin <b>1,2 m üstünde ve 3,4 m yanında</b>,
        /// yani havada bırakılıyor ve hemen düşüyordu.
        ///
        /// Durulacak yer zaten modelde var: kâgir gövde
        /// <c>SHAFT_H_1632 = 34,50</c> m'de biter ve üstünde 1,70 m'lik
        /// <b>mazgallı korkuluk</b> vardır — yani bir seyir sahanlığı.
        /// 1632'de balkon yoktur (katalog notu doğru: 1831 sofası ve
        /// demir korkuluk yok), ama mazgalın arkasındaki yürüyüş yolu
        /// vardır ve Hezarfen'in kalktığı yer orasıdır.
        ///
        /// 35,2 = gövde (34,50) + bir basamak.
        /// </summary>
        public float serefeKotu = 35.2f;

        /// <summary>
        /// Sahanlıkta oyuncunun duracağı yarıçap (m).
        ///
        /// Korkuluğun <b>içinde</b> olmalı: dışına konursa oyuncu
        /// boşlukta durur. Kule yarıçapı ~8,2 m; 5,5 m mazgalın
        /// arkasında, kenardan bir buçuk metre içeride.
        /// </summary>
        public float serefeYaricapi = 5.5f;

        public string Ipucu => "Kuleye çık";
        public bool Hazir => true;

        public bool Etkiles(GameObject aktor)
        {
            var cc = aktor.GetComponentInParent<CharacterController>();
            var kok = cc != null ? cc.transform : aktor.transform;

            // NOKTA KULENIN EKSENINDEN TURER, KAPIDAN DEGIL.
            //
            // Once inis noktasi kapinin konumundan hesaplaniyordu ve
            // bunun bedeli olculdu: kapiyi tastan cikarmak icin
            // eksenden 6,5 → 9,39 m'ye tasidim, inis noktasi da
            // 9,7 → 12,59 m'ye kaydi. Kapiyi tastan cikardim, oyuncuyu
            // da kuleden cikardim. Bir konumu baska bir konumdan
            // turetmek, birini duzeltince otekini sessizce bozar.
            var eksen = transform.parent != null
                        ? transform.parent.position : transform.position;
            var yon = (transform.position - eksen);
            yon.y = 0f;
            yon = yon.sqrMagnitude > 1e-4f ? yon.normalized : -transform.forward;

            var nokta = eksen + Vector3.up * serefeKotu
                        + yon * serefeYaricapi;

            // VE ZEMIN SORULUR — VARSAYILMAZ.
            //
            // Bir oyuncu su cumleyi yazdi: *"Kapiyi yaptiniz, kolu
            // cevirdim, kapi acildi — arkasinda oda yok, bosluk var."*
            // Yukaridan asagi bir isin, altta gercekten bir sey olup
            // olmadigini soyler; bulamazsa oyuncu ISINLANMAZ, cunku
            // havaya birakmak yerinde birakmaktan kotudur.
            if (!Physics.Raycast(nokta + Vector3.up * 4f, Vector3.down,
                                 out var v, 9f, ~0,
                                 QueryTriggerInteraction.Ignore))
                return false;
            nokta = v.point + Vector3.up * 0.15f;

            bool acikti = cc != null && cc.enabled;
            if (acikti) cc.enabled = false;
            kok.position = nokta;
            if (acikti) cc.enabled = true;
            return true;
        }
    }
}
