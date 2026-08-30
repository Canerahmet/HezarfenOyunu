using UnityEngine;

namespace Hezarfen.Gis
{
    /// <summary>
    /// <b>Zeminin ne olduğunu bilen katman.</b>
    ///
    /// Bugüne kadar bir şeyi nereye koyacağımıza karar veren her araç
    /// kendi filtresini yazdı: ağaç yerleştirici eğime ve çim dokusuna
    /// baktı, kırsal doku bina yakınlığına baktı ama <i>yalnız kendi
    /// eklediklerine</i>, bahçe eşyası kapalılık saydı. Her biri kendi
    /// içinde doğruydu ve hiçbiri ötekinin bildiğini bilmiyordu.
    ///
    /// Bedeli ölçüldü: <b>40.765 ağaç binaların içinden bitti.</b>
    /// Kusur temizlendi ama sebebi duruyordu ve
    /// <see cref="AgacTemizligi"/> kendi belgesinde yazıyordu:
    /// <i>"GreeneryBuilder ağaçları semtlerden ÖNCE diker ve o sırada
    /// bina yoktur."</i> Yani sorun bir filtre eksikliği değil, bir
    /// <b>sıra</b> sorunuydu — ve sıra sorunları filtre eklenerek
    /// çözülmez, ortak bir gerçek kaydı kurularak çözülür.
    ///
    /// Bu katman o kayıt. Semt kurulduktan sonra bir kez hesaplanır ve
    /// ondan sonra <b>her</b> yerleştirici aynı sayılara bakar.
    ///
    /// ## Ne tutuluyor, hangi çözünürlükte
    ///
    /// Yükseklik ve eğim zaten <c>TerrainData</c>'da var; onları
    /// kopyalamak ikinci bir sahip yaratırdı. Burada yalnız <b>araziden
    /// okunamayan</b> üç şey var:
    ///
    /// | katman | ne | neden gerekli |
    /// |---|---|---|
    /// | <c>binaUzakligi</c> | en yakın ev/çekirdek duvarına metre | ağaç binanın içinde bitmesin |
    /// | <c>yolUzakligi</c> | en yakın sokak/kaldırım yüzeyine metre | sokak açık kalsın |
    /// | <c>suUzakligi</c> | en yakın su yüzeyine metre | söğüt/sazlık kıyıda artsın |
    ///
    /// Hücre <b>4 m</b>: bir ev cephesinden dar, bir sokaktan (7,2 m)
    /// dar, yani ikisini de ayırt eder. Daha ince olsaydı Galata için
    /// 1,6 milyon hücre ederdi ve kazancı yoktu.
    ///
    /// Mesafe <b>0,5 m adımlarla bir bayta</b> sığdırılır ve 63,5 m'de
    /// doyar. Ötesi "uzak"tır ve hiçbir kural 63 m'nin ötesini
    /// ayırt etmiyor.
    /// </summary>
    public class AraziOznitelik : ScriptableObject
    {
        /// <summary>Hücre kenarı (m).</summary>
        public const float Hucre = 4f;

        /// <summary>Mesafe baytının adımı (m). 127 × 0,5 = 63,5 m doyum.</summary>
        public const float Adim = 0.5f;

        /// <summary>Doyum mesafesi (m) — ötesi "uzak".</summary>
        public const float Uzak = 63.5f;

        [Tooltip("Hangi semt için hesaplandı.")]
        public string semt;

        [Tooltip("Izgaranın dünya köşesi (güneybatı), XZ.")]
        public Vector2 kok;

        public int en;
        public int boy;

        /// <summary>En yakın bina duvarına uzaklık (0,5 m adım).</summary>
        public byte[] binaUzakligi;

        /// <summary>En yakın sokak/kaldırım yüzeyine uzaklık.</summary>
        public byte[] yolUzakligi;

        /// <summary>En yakın su yüzeyine uzaklık.</summary>
        public byte[] suUzakligi;

        /// <summary>Kaç bina, kaç yol, kaç su hücresi tohumlandı.</summary>
        public int binaHucre, yolHucre, suHucre;

        /// <summary>Bu dünya noktası ızgaranın içinde mi.</summary>
        public bool Icinde(float x, float z)
        {
            int i = Mathf.FloorToInt((x - kok.x) / Hucre);
            int j = Mathf.FloorToInt((z - kok.y) / Hucre);
            return i >= 0 && i < en && j >= 0 && j < boy;
        }

        /// <summary>
        /// Bu noktadaki bina uzaklığı (m). Izgara dışıysa
        /// <see cref="Uzak"/> — dışarısı "bina yok" sayılır, çünkü
        /// katman semt sınırına göre kuruldu.
        /// </summary>
        public float BinaUzakligi(float x, float z) => Oku(binaUzakligi, x, z);

        /// <summary>Bu noktadaki yol uzaklığı (m).</summary>
        public float YolUzakligi(float x, float z) => Oku(yolUzakligi, x, z);

        /// <summary>Bu noktadaki su uzaklığı (m).</summary>
        public float SuUzakligi(float x, float z) => Oku(suUzakligi, x, z);

        private float Oku(byte[] katman, float x, float z)
        {
            if (katman == null || katman.Length != en * boy) return Uzak;
            int i = Mathf.FloorToInt((x - kok.x) / Hucre);
            int j = Mathf.FloorToInt((z - kok.y) / Hucre);
            if (i < 0 || i >= en || j < 0 || j >= boy) return Uzak;
            return katman[j * en + i] * Adim;
        }
    }
}
