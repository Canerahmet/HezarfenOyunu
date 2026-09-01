using UnityEngine;

namespace Hezarfen.Sehir
{
    /// <summary>
    /// <b>Konuşulabilen insan.</b>
    ///
    /// ## Neden bu bileşen var
    ///
    /// <see cref="IEtkilesim"/>'i üç sınıf uyguluyordu —
    /// <see cref="Dukkan"/>, <see cref="Perme"/>,
    /// <see cref="ToplanabilirEsya"/> — ve <b>hiçbiri insan değildi</b>.
    /// Şehirde kırk bin sakin var, altmışı her an ekranda yürüyor,
    /// <see cref="BarkKorpusu"/> beş binden fazla replik taşıyor; ve
    /// oyuncu hiçbirine dokunamıyordu. Kalabalık konuşuyordu ama
    /// kalabalıkla konuşulamıyordu.
    ///
    /// ## Görevi neden insan veriyor
    ///
    /// <see cref="GorevYonetici"/> oyunun sıfırıncı saniyesinde iş
    /// atıyor, biten görevin yerine anında yenisini koyuyordu. Kimin
    /// eşyası, kim sordu, neden — hiçbirinin cevabı yoktu; sonsuz bir
    /// konveyör. Şimdi iş <b>konuştuğun kişiden</b> geliyor ve görev
    /// tam da onun durduğu yerin yakınında üretiliyor.
    ///
    /// Bu sıralamanın ikinci bir faydası var ve tasarımın esas
    /// sebebi o: görevi önceden üretip bir düğüme bağlamak, o düğümün
    /// yakınına hiç kimse uğramazsa oyunu <b>kilitler</b>. Görevi
    /// konuşulan kişinin yanında üretmek bu kilidi imkânsız kılar —
    /// karşında biri varsa iş vardır.
    /// </summary>
    [AddComponentMenu("Hezarfen/Sakin")]
    public class Sakin : MonoBehaviour, IEtkilesim
    {
        /// <summary>Bu gövdenin şu anki sahibi; havuzda −1.</summary>
        [System.NonSerialized] public NPCAjan ajan;

        [System.NonSerialized] public GorevYonetici gorev;
        [System.NonSerialized] public Hezarfen.Zaman.ZamanSistemi zaman;
        [System.NonSerialized] public AranmaSistemi aranma;

        /// <summary>Son söylenen söz — HUD okur.</summary>
        public string SonSoz { get; private set; } = "";

        public bool Hazir => ajan != null;

        public string Ipucu
        {
            get
            {
                if (ajan == null) return "";
                return IsVerebilir ? "İş sor" : "Konuş";
            }
        }

        /// <summary>
        /// Bu kişi iş verebilir mi.
        ///
        /// Oyuncunun elinde görev varken herkes yeniden iş teklif
        /// ederse görev bir seçim olmaktan çıkar; o yüzden yalnız
        /// <b>eli boşken</b>.
        /// </summary>
        private bool IsVerebilir =>
            gorev != null && gorev.Simdiki == null;

        public bool Etkiles(GameObject aktor)
        {
            if (ajan == null) return false;

            if (IsVerebilir)
            {
                bool oldu = gorev.IsIste(ajan.konum);
                SonSoz = oldu
                    ? $"«{IsSozu()}»"
                    : "«Bugün bir işim yok.»";
                return oldu;
            }

            SonSoz = $"«{Lakirdi()}»";
            return true;
        }

        /// <summary>
        /// İşi verirken söylenen söz — <b>arketipe göre</b>.
        ///
        /// Korpustan gelmiyor, çünkü korpus ambiyans için yazıldı
        /// (<see cref="BarkTuru"/>) ve orada "sana bir iş vereyim"
        /// diyen bir replik yok. Uydurulmuş bir korpus satırı, korpusun
        /// kaynak alanını (<c>Replik.kaynak</c>) yalan söyletirdi.
        /// </summary>
        private string IsSozu()
        {
            var g = gorev != null ? gorev.Simdiki : null;
            if (g == null) return "Hele bir dur.";
            return g.arketip switch
            {
                GorevArketip.Teslimat => "İskeleye bir yük geldi, çarşıya götürüver.",
                GorevArketip.KayikYolcu => "Şu yolcuyu karşıya geçirecek biri lazım.",
                GorevArketip.Kayip => "Mahallede bir kayıp var, imam efendi soruyor.",
                GorevArketip.Kacakcilik => "Sesini alçalt. Bir yük var, görünmesin.",
                GorevArketip.Tedarik => "Fırının işi var, esnaf bekliyor.",
                _ => "Bir işim var sana.",
            };
        }

        /// <summary>
        /// İşi olmayan bir insanın söyleyeceği söz — korpustan.
        ///
        /// Korpus zaten mesleğe, vakte ve aranma durumuna göre
        /// süzülüyor (<see cref="BarkKorpusu.Sec"/>); burada
        /// uydurulacak bir şey yok, yalnız <b>sorulacak</b> bir şey var.
        /// </summary>
        private string Lakirdi()
        {
            if (ajan.meslek == null) return "Selâmün aleyküm.";
            var vakit = zaman != null
                ? Hezarfen.Zaman.VakitHesabi.SuAnki(zaman.Bugun, zaman.saat)
                : Hezarfen.Zaman.VakitHesabi.Vakit.Ogle;
            bool arananMi = aranma != null
                            && aranma.SuAn != AranmaSistemi.Durum.Temiz;
            int yil = zaman != null ? zaman.yil : 1632;
            int gun = zaman != null ? zaman.yilinGunu : 121;

            var r = BarkKorpusu.Sec(ajan.meslek.tip, vakit,
                                    yil, gun, arananMi, ajan.tohum);
            return r != null && !string.IsNullOrEmpty(r.metin)
                   ? r.metin : "Selâmün aleyküm.";
        }
    }
}
