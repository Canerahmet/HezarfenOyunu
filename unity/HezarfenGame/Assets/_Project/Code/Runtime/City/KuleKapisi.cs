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

        [System.NonSerialized] public Hezarfen.Player.UcusDizisi dizi;

        private Hezarfen.Player.UcusDizisi Dizi =>
            dizi != null ? dizi
            : (dizi = FindAnyObjectByType<Hezarfen.Player.UcusDizisi>());

        /// <summary>Kanat kuşanılmış mı — kanatsız çıkılmaz.</summary>
        private bool KanatVar =>
            Dizi != null && Dizi.Simdiki == Hezarfen.Player.UcusDizisi.Durum.Hazir;

        public string Ipucu =>
            KanatVar ? "Kuleye çık ve atla" : "Kuleye çık · önce kanadı kuşan";

        public bool Hazir => true;

        /// <summary>
        /// Kulenin gerçek yarıçapı (m) — <b>yerel</b> sınırdan.
        ///
        /// Önce <c>Collider.bounds</c> (dünya hizalı kutu) okunuyordu
        /// ve kule 205° dönük: 8,225 m yarıçaplı bir silindirin dünya
        /// kutusu 8,225 × (|cos|+|sin|) = <b>10,92 m</b> ölçülüyordu.
        /// Yani kod 16,45 m çapındaki kuleyi 21,8 m sanıyor, kapıyı
        /// duvarın 3,9 m açığına, çayırın ortasına koyuyordu.
        /// </summary>
        private float Yaricap()
        {
            var t = transform.parent;
            if (t == null) return 8.2f;
            foreach (var c in t.GetComponentsInChildren<Collider>(true))
            {
                if (c.isTrigger) continue;
                if (c is MeshCollider mc && mc.sharedMesh != null)
                {
                    var b = mc.sharedMesh.bounds;
                    var o = t.lossyScale;
                    return Mathf.Max(b.extents.x * Mathf.Abs(o.x),
                                     b.extents.z * Mathf.Abs(o.z));
                }
            }
            return 8.2f;
        }

        public bool Etkiles(GameObject aktor)
        {
            // KANATSIZ CIKILMAZ.
            //
            // Once cikilabiliyordu ve sonucu 46 m'lik **hasarsiz** bir
            // dususten baska bir sey degildi: oyuncu yere carpiyor,
            // kalkiyor, yuruyordu. Bir kule, oradan atlayacak seyi
            // olmayan birine acilmamali.
            if (!KanatVar) return false;

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

            // KAPI BIR SEREFEYE DEGIL, KALKISA ACILIR (ADR 0086).
            //
            // Uc tur boyunca burada bir "durulacak yer" varsayildi ve
            // uc kez tuzak cikti: once tasin icinde, sonra kulahin
            // ustunde, sonunda kapali bir ficinin icinde. Dorduncu
            // turda oyuncu serefede bes saniye durabildi ve
            // **inemedi**.
            //
            // Cunku bu kulede gezilecek bir serefe YOK: korkuluk
            // (8,225 m) ile ahsap kasnak (7,875 m) arasi 0,35 m,
            // oyuncunun kapsulu 0,70 m; ve kulahin sacagi (9,175 m)
            // korkulugun ustunu ortuyor. 1632'de balkon yok — dogru
            // olan kayit buydu, yanlis olan onu var saymamdi.
            //
            // Kuleye cikmak, govdenin icinden yukari cikip tepedeki
            // acikliktan ADIM ATMAKTIR. Oyuncu korkulugun DISINA, acik
            // havaya birakilir ve ayni karede ucusa gecer: ne dusus,
            // ne fici, ne de bir zemin varsayimi.
            var cikis = eksen + Vector3.up * serefeKotu
                        + yon * (Yaricap() + 1.4f);

            bool acikti = cc != null && cc.enabled;
            if (acikti) cc.enabled = false;
            kok.position = cikis;
            if (acikti) cc.enabled = true;

            Dizi.Atla();
            return true;
        }
    }
}
