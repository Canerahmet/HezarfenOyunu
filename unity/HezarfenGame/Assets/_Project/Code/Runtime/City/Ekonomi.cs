using UnityEngine;

namespace Hezarfen.Sehir
{
    /// <summary>
    /// <b>Akçe: tek para birimi.</b>
    ///
    /// RESEARCH §6: *"akçe temel birim, kuruş/altın üst birimler; narh
    /// defterleri ekmek/et/pirinç fiyatlarını verir."*
    ///
    /// ## İki çapa, gerisi türetme
    ///
    /// Elimizde <b>iki</b> belgeli sayı var, ikisi de Evliya'dan ve
    /// ikisi de <b>sipahi yevmiyesi</b>:
    ///
    /// <list type="bullet">
    /// <item>*"40 akçe yevmiye ile sipahi"*</item>
    /// <item>Lagari Hasan Çelebi için *"70 akça ile sipahi yazıldı"* —
    ///       yani ödül olarak verilen, olağanın üstü bir yevmiye.</item>
    /// </list>
    ///
    /// Kayıkçı ve ırgat yevmiyesi <b>kayıtlı değil</b>; RESEARCH bunu
    /// açıkça söylüyor: *"narh ve sicil kayıtlarından çıkarılmalı"* —
    /// yani henüz çıkarılmadı. Bu yüzden geri kalan her sayı o iki
    /// çapadan <b>türetildi</b> ve <b>T2</b>'dir.
    ///
    /// Türetmenin kuralı şu: bir askerî yevmiye, gündelikçi bir işçinin
    /// birkaç katıdır. Sipahi 40 ise ırgat 8-12 bandındadır; bu, çağdaş
    /// Osmanlı ücret çalışmalarının verdiği orandır ve burada <b>oran</b>
    /// olarak kullanılıyor, mutlak değer olarak değil.
    ///
    /// ## Sayılar neden burada
    ///
    /// Ceza, ücret ve kazanç ayrı ayrı yazılsaydı ilk dengeleme turunda
    /// birbirinden kopardı: bir gün çalışan oyuncu bir geçişi
    /// karşılayamaz olurdu ve kimse sebebini bulamazdı. Hepsi tek yerde
    /// ve <b>hepsi yevmiyeden türüyor</b>.
    /// </summary>
    public static class Ekonomi
    {
        /// <summary>
        /// <b>Sipahi yevmiyesi (akçe/gün) — T1, Evliya.</b>
        /// Ekonominin çapası; başka her sayı buna göredir.
        /// </summary>
        public const int SipahiYevmiyesi = 40;

        /// <summary>
        /// Lagari'ye ödül olarak verilen yevmiye — T1, Evliya.
        /// Olağanın üstü olduğunu göstermek için burada.
        /// </summary>
        public const int LagariYevmiyesi = 70;

        /// <summary>
        /// <b>Irgat/hamal yevmiyesi</b> — T2, sipahinin dörtte biri.
        /// Oyuncunun taban kazancı bu bandda.
        /// </summary>
        public const int IrgatYevmiyesi = SipahiYevmiyesi / 4;   // 10

        /// <summary>
        /// <b>Kayık geçişi</b> (akçe) — T2.
        ///
        /// Bir ırgat yevmiyesinin onda biri. Sebebi mekanik: Haliç'te
        /// köprü yok ve insanlar <b>her gün</b> karşıya geçiyor. Geçiş
        /// yevmiyenin belirgin bir kısmını yeseydi kimse geçemezdi ve
        /// ulaşım mekaniği ölü doğardı.
        /// </summary>
        public const int KayikUcreti = 1;

        /// <summary>
        /// <b>Pereme (uzun geçiş)</b> — Boğaz'ı geçmek Haliç'i geçmekten
        /// pahalıdır: daha uzun, daha açık su, daha büyük tekne.
        /// </summary>
        public const int PeremeUcreti = 3;

        /// <summary>Bir günlük ekmek (akçe) — T2, yevmiyenin onda biri.</summary>
        public const int GunlukEkmek = 1;

        /// <summary>
        /// Mesafeye göre kayık ücreti.
        ///
        /// Kısa geçiş (Haliç, ~700 m) taban ücret; uzun geçiş (Boğaz,
        /// 2 km+) pereme ücreti. Aradaki her mesafe ikisinin arasında.
        /// </summary>
        public static int Ucret(float mesafeMetre)
        {
            const float kisa = 900f;
            const float uzun = 2400f;
            if (mesafeMetre <= kisa) return KayikUcreti;
            if (mesafeMetre >= uzun) return PeremeUcreti;
            float t = (mesafeMetre - kisa) / (uzun - kisa);
            return Mathf.RoundToInt(Mathf.Lerp(KayikUcreti, PeremeUcreti, t));
        }

        /// <summary>
        /// Bir cezanın kaç günlük ırgat yevmiyesine denk geldiği.
        ///
        /// Dengeyi okunur kılan sayı bu: "280 akçe" bir şey söylemez,
        /// "yirmi sekiz günlük yevmiye" söyler.
        /// </summary>
        public static float GunCinsinden(int akce)
            => akce / (float)IrgatYevmiyesi;
    }

    /// <summary>
    /// <b>Kese.</b> Akçe tutar ve <b>ne olduğunu kaydeder</b>.
    ///
    /// Defter tutmak bir lüks değil: ekonominin dengeli olup olmadığı
    /// ancak paranın nereden gelip nereye gittiği bilinerek ölçülebilir.
    /// Yalnız bir sayı tutan kese, "oyuncu neden hep parasız" sorusuna
    /// hiçbir cevap veremez.
    /// </summary>
    [System.Serializable]
    public class Kese
    {
        public int akce;

        /// <summary>Toplam kazanç ve harcama — defter.</summary>
        public int Kazanilan { get; private set; }
        public int Harcanan { get; private set; }

        public Kese(int baslangic = 0) => akce = baslangic;

        /// <summary>Yeter mi.</summary>
        public bool Yeter(int tutar) => akce >= tutar;

        public void Kazan(int tutar)
        {
            if (tutar <= 0) return;
            akce += tutar;
            Kazanilan += tutar;
        }

        /// <summary>
        /// Öder. Yetmezse <b>eldeki kadarını</b> alır ve `false` döner.
        ///
        /// Borç yok: 1632'de kesende ne varsa odur. Eksiye düşen bir
        /// bakiye, olmayan bir kredi kurumu uydurmak olurdu.
        /// </summary>
        public bool Ode(int tutar)
        {
            if (tutar <= 0) return true;
            int alinan = Mathf.Min(akce, tutar);
            akce -= alinan;
            Harcanan += alinan;
            return alinan == tutar;
        }
    }
}
