using UnityEngine;

namespace Hezarfen.Player
{
    /// <summary>
    /// <b>Kanadın görünmesi.</b>
    ///
    /// ## Neden bu bileşen var
    ///
    /// Üç kanat prefabı — <c>PF_Kanat_Katli</c>, <c>PF_Kanat_Acik</c>,
    /// <c>PF_Kanat_Kirik</c> — modellendi, dışa aktarıldı, depoya
    /// girdi ve <b>hiçbir sahnede, hiçbir prefabta, hiçbir kod
    /// satırında referansı yoktu</b>. GUID taraması sıfır döndü.
    ///
    /// Yani oyuncu kuleden atlıyor ve ekranda kollarını iki yana açmış
    /// entarili bir adam düşüyor. Bir oyuncunun raporundaki cümle:
    /// *"Oyunun adı Hezarfen. Oyunun tamamı bir kanat için. Kuleden
    /// atladığımda sırtımda, elimde, hiçbir yerimde kanat yok."*
    ///
    /// Bu, bu oturumda defalarca çıkan desenin en pahalı örneği:
    /// üretildi, ölçüldü, kataloglandı — ve oyuna bağlanmadı.
    ///
    /// ## Neden durum makinesine bağlı
    ///
    /// Kanat üç hâlde görünür ve üçü ayrı model: sırtta katlı (yerde),
    /// açık (havada), kırık (çakılınca). <see cref="UcusDizisi"/> zaten
    /// bu üç durumu biliyor ve <c>DurumDegisti</c> olayını yayıyor;
    /// burada yeni bir durum tutulmaz, var olan duruma <b>bakılır</b>.
    /// </summary>
    [AddComponentMenu("Hezarfen/Kanat Gorseli")]
    public class KanatGorseli : MonoBehaviour
    {
        [Header("Modeller (boru hattı bağlar)")]
        public GameObject katli;
        public GameObject acik;
        public GameObject kirik;

        [Tooltip("Uçuş dizisi — boşsa aynı nesnede aranır.")]
        public UcusDizisi dizi;

        private void Awake()
        {
            if (dizi == null) dizi = GetComponentInParent<UcusDizisi>();
            Uygula(dizi != null ? dizi.Simdiki : UcusDizisi.Durum.Yerde);
        }

        private void OnEnable()
        {
            if (dizi != null) dizi.DurumDegisti += Uygula;
        }

        private void OnDisable()
        {
            if (dizi != null) dizi.DurumDegisti -= Uygula;
        }

        /// <summary>
        /// Duruma göre hangi model görünür.
        ///
        /// <c>Yerde</c> ve <c>Kusaniyor</c>: sırtta katlı — kanadı olan
        /// bir adam, kanadı olmayan bir adam gibi görünmemeli.
        /// <c>Hazir</c>'dan itibaren açık, <c>Cakildi</c>'da kırık.
        /// </summary>
        public void Uygula(UcusDizisi.Durum d)
        {
            bool cakildi = d == UcusDizisi.Durum.Cakildi;
            bool havada = d == UcusDizisi.Durum.Hazir
                          || d == UcusDizisi.Durum.Ucuyor
                          || d == UcusDizisi.Durum.Iniyor;

            if (katli != null) katli.SetActive(!havada && !cakildi);
            if (acik != null) acik.SetActive(havada);
            if (kirik != null) kirik.SetActive(cakildi);
        }
    }
}
