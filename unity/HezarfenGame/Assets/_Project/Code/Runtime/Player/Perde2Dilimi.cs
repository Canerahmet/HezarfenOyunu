using System;
using UnityEngine;

namespace Hezarfen.Player
{
    /// <summary>
    /// <b>Perde 2 dikey dilimi:</b> talim → kule → uçuş → iniş → tepki.
    /// PLAN Bölüm 11, Faz 6 kabul ölçütünün son maddesi.
    ///
    /// ## Neden bir durum makinesi, neden bir sinematik değil
    ///
    /// Kabul ölçütü *"baştan sona <b>oynanabilir</b>"* diyor. Bir kesme
    /// sahne zinciri bunu karşılamaz: oyuncunun yaptığı bir şey olmalı ve
    /// her aşamanın <b>ölçülebilir</b> bir bitiş koşulu olmalı. Aşağıdaki
    /// her eşik bir sayıdır, bir izlenim değil.
    ///
    /// ## Yerler uydurulmadı
    ///
    /// Dördü de katalogdan, dördü de belgeli:
    /// <list type="bullet">
    /// <item><b>Okmeydanı</b> — II. Bayezid vakfı talim/atış alanı;
    ///   Okçular (Kemankeş) Tekkesi ve minberli namazgâh 1632'de mevcut.
    ///   <b>Hezarfen'in talim yaptığı yer</b> (RESEARCH §4.6).</item>
    /// <item><b>Galata Kulesi</b> — kalkış. Dünya orijini burasıdır.</item>
    /// <item><b>Doğancılar Meydanı</b> — iniş noktası, 1632'de faal meydan
    ///   (RESEARCH §5.5).</item>
    /// <item><b>İncili Köşk</b> — IV. Murad'ın uçuşu seyrettiği yer;
    ///   tepki sahnesi orada geçer.</item>
    /// </list>
    ///
    /// ## Uçuşun kendisi TARTIŞMALIDIR ve oyun bunu saklamaz
    ///
    /// Anlatı yalnız Evliya Çelebi'de geçer, başka kaynakla doğrulanmaz;
    /// kese altın ihsanının mali kayıtlarda izi yoktur; gereken süzülme
    /// oranı (~55:1) modern delta kanadın (~15:1) çok üstündedir; uçuş
    /// tarihi bile çelişkilidir (çoğu kaynak 1632, bazıları 1638).
    /// <see cref="TepkiKodeksi"/> bunu <b>oyuncuya söyler</b> — oyunun
    /// zirvesi bir belge değil bir rivayettir ve bunu gizlemek, üç yıl
    /// boyunca kaynak dipnotu tutmanın anlamını ortadan kaldırırdı.
    /// </summary>
    public class Perde2Dilimi : MonoBehaviour
    {
        public enum Asama
        {
            /// <summary>Okmeydanı'nda talim — süzülüş denemeleri.</summary>
            Talim = 0,
            /// <summary>Galata Kulesi'ne çıkış.</summary>
            Kule = 1,
            /// <summary>Uçuş — Boğaz geçilir.</summary>
            Ucus = 2,
            /// <summary>Doğancılar'a iniş.</summary>
            Inis = 3,
            /// <summary>Tepki sahnesi — İncili Köşk.</summary>
            Tepki = 4,
            Bitti = 5,
        }

        [Header("Bağlantılar")]
        public UcusDizisi dizi;
        public Transform oyuncu;

        [Header("Yerler (katalogdan — ADR 0007 dünya orijini)")]
        [Tooltip("Okmeydanı — Hezarfen'in talim alanı.")]
        public Vector3 okmeydani = new Vector3(-1143f, 94.6f, 3331f);

        [Tooltip("Galata Kulesi — dünya orijini, kalkış.")]
        public Vector3 kule = new Vector3(0f, 52f, 0f);

        [Tooltip("Doğancılar Meydanı — iniş noktası.")]
        public Vector3 dogancilar = new Vector3(3267.6f, 46.6f, -672.9f);

        [Tooltip("İncili Köşk — IV. Murad uçuşu buradan seyreder.")]
        public Vector3 incilikosk = new Vector3(1210f, 0.1f, -1225f);

        [Header("Eşikler")]
        [Tooltip("Talimin sayıldığı yarıçap (m).")]
        public float talimYaricapi = 250f;

        [Tooltip("Bir talim süzülüşünün en az kaç metre olması gerekir.")]
        public float talimMesafesi = 60f;

        [Tooltip("Kaç başarılı talim süzülüşü geçilir.")]
        public int talimHedefi = 3;

        [Tooltip("Kuleye çıkmış sayılma yarıçapı (m).")]
        public float kuleYaricapi = 40f;

        [Tooltip("Doğancılar'a inmiş sayılma yarıçapı (m).")]
        public float inisYaricapi = 220f;

        /// <summary>
        /// Kalkışın sayılması için kule tabanının kaç metre üstü (m).
        ///
        /// Şerefe 98,2 m, kule tabanı 52 m. 40 m, şerefeyi ister ve
        /// yakındaki bir damdan atlamayı istemez.
        /// </summary>
        public const float KalkisKotu = 40f;

        /// <summary>
        /// Bir uçuşun uçuş sayılması için en az yatay yol (m).
        ///
        /// <b>800 idi ve bir kapan kuruyordu.</b> Hedefe doğru kıyı
        /// <b>652 m</b>'de bitiyor (DEM ölçümü): 800 m şartı, kuralına
        /// uyan her oyuncuyu kesin olarak denize düşürüyordu. Bir
        /// oyuncu raporu bunu buldu ve eşiği ben koymuştum.
        ///
        /// 500 m: kıyının belirgin biçimde altında, ama kule dibinden
        /// atlayıp bir saniyede inmenin de üstünde — perdenin
        /// atlanmasını engelleyen sayı buydu, oyuncuyu denize atan
        /// değil.
        /// </summary>
        public const float EnAzUcus = 500f;

        [Tooltip("Tepki sahnesinin geçtiği yarıçap (m).")]
        public float tepkiYaricapi = 120f;

        /// <summary>Şu anki aşama.</summary>
        public Asama Simdiki { get; private set; } = Asama.Talim;

        /// <summary>Tamamlanmış talim süzülüşü sayısı.</summary>
        public int TalimSayisi { get; private set; }

        /// <summary>Uçuşta katedilen yatay mesafe (m).</summary>
        public float UcusMesafesi { get; private set; }

        /// <summary>İniş sert miydi — çakılmak da bir sonuçtur.</summary>
        public bool Cakildi { get; private set; }

        public event Action<Asama> AsamaDegisti;

        /// <summary>Talim denemesinin sonucu — HUD okur.</summary>
        public event Action<string> TalimBildirimi;

        /// <summary>
        /// Kaçıncı talimin kaç metre istediği.
        /// 
        /// Üç denemenin üçü de 60 m istiyordu ve ilk deneme düz
        /// zeminden <b>hiç</b> geçemiyordu. Artan bir merdiven
        /// oyuncuya ilk denemede bir <b>evet</b> verir; bugünkü hâlde
        /// ilk üç cevabın üçü de hayır.
        ///
        /// 30 m: kalkış hızıyla hafif bir eğimden erişilir.
        /// 60 m: ~5 m'lik bir düşüş ister. 120 m: gerçek bir yamaç.
        /// </summary>
        public float TalimEsigi(int kacinci) => kacinci switch
        {
            0 => talimMesafesi * 0.5f,
            1 => talimMesafesi,
            _ => talimMesafesi * 2f,
        };

        /// <summary>
        /// <b>Kayıttan gelen ilerlemeyi geri koyar.</b>
        ///
        /// Aşama ve talim sayısı yalnız okunurdu; kayıt dosyasındaki
        /// <c>perde2Asama</c> ve <c>talimSayisi</c> alanları hiç
        /// doldurulmuyordu. Sonuç: uçuşu tamamlayıp kaydeden oyuncu,
        /// yükleyince Okmeydanı'nda talimin başında uyanıyordu.
        /// </summary>
        public void DurumuGeriYukle(int asama, int talimSayisi)
        {
            TalimSayisi = Mathf.Max(0, talimSayisi);
            var yeni = (Asama)Mathf.Clamp(asama, 0,
                                          Enum.GetValues(typeof(Asama)).Length - 1);
            if (Simdiki == yeni) return;
            Simdiki = yeni;
            AsamaDegisti?.Invoke(Simdiki);
        }

        private Vector3 _kalkis;
        private bool _ucusta;

        /// <summary>
        /// Tepki sahnesinin kodeks metni.
        ///
        /// Ödül ve sürgün <b>aynı sahnededir</b>, çünkü kaynakta da
        /// öyledir: padişah bir kese altın verir ve *"bu adam korkulacak
        /// bir adamdır, her istediğini yapabilir"* diyerek onu Cezayir'e
        /// sürer. Zirve bir zafer değil, zaferin cezalandırılmasıdır.
        /// </summary>
        public const string TepkiKodeksi =
            "Sinan Paşa (İncili) Köşkü'nden seyredildi. Bir kese altın "
            + "ihsan edildi; ardından sürgün. — Anlatının kaynağı tektir: "
            + "Evliya Çelebi. Başka kayıtla doğrulanmaz, kese altının mali "
            + "kayıtlarda izi yoktur ve gereken süzülme oranı (~55:1) modern "
            + "delta kanadın (~15:1) çok üstündedir. Kaynakların çoğu 1632 "
            + "der, bazıları 1638. Oyun bu rivayeti oynatır, belge diye "
            + "sunmaz.";

        private void Update() => Ilerle(Time.deltaTime);

        /// <summary>
        /// Dilimi bir adım ilerletir. Testler bunu doğrudan çağırır —
        /// zamana bağlı bir dilim, zamanı verilebilir olmalı.
        /// </summary>
        public void Ilerle(float dt)
        {
            if (oyuncu == null || Simdiki == Asama.Bitti) return;
            Vector3 p = oyuncu.position;

            switch (Simdiki)
            {
                case Asama.Talim:
                    TalimiIzle(p);
                    if (TalimSayisi >= talimHedefi) Gec(Asama.Kule);
                    break;

                case Asama.Kule:
                    // IRTIFA SORULUR — YOKSA PERDE KENDINI ATLAR.
                    //
                    // Once yalniz YATAY yakinlik ve "ucuyor mu"
                    // soruluyordu; gerekcesi "tepeye cikmak dikey bir
                    // hareket ve onu `dizi` olcuyor" idi. Olcmuyordu:
                    // kule DIBINDE (kot 52 m) G + Space'e basmak iki
                    // sarti da karsiliyor, bir saniye sonra yere
                    // deginca `Ucus -> Inis` oluyordu. Yani oyuncu
                    // 3.336 m'lik suzulusu **iki vapur biletiyle**
                    // geciyor ve durum makinesi bunu ucus sayiyordu.
                    //
                    // Serefe kotu 98,2 m, kule tabani 52 m: 40 m'lik
                    // sart serefeyi ister, damdan atlamayi istemez.
                    if (Yatay(p, kule) <= kuleYaricapi
                        && p.y >= kule.y + KalkisKotu
                        && dizi != null
                        && dizi.Simdiki == UcusDizisi.Durum.Ucuyor)
                    {
                        _kalkis = p;
                        UcusMesafesi = 0f;
                        Gec(Asama.Ucus);
                    }
                    break;

                case Asama.Ucus:
                    UcusMesafesi = Yatay(p, _kalkis);
                    if (dizi != null
                        && dizi.Simdiki != UcusDizisi.Durum.Ucuyor)
                    {
                        Cakildi = dizi.Simdiki == UcusDizisi.Durum.Cakildi;
                        Gec(Asama.Inis);
                    }
                    break;

                case Asama.Inis:
                    // INIS BASARILI MI: Dogancilar'a varildi ve cakilmadi.
                    // Cakilmak dilimi bitirmez, BASA dondurur — "kacis VE
                    // yakalanma sonuclari" ilkesiyle ayni: her iki sonuc
                    // da oynanabilir olmali.
                    if (Cakildi) { Basa(); break; }

                    // UCULMEDIYSE INILMIS SAYILMAZ.
                    //
                    // `Inis` yalnizca "Dogancilar'a 220 m" soruyordu,
                    // yani oyuncu kayikla karsiya gecip yuruyerek de
                    // perdeyi bitirebiliyordu. Oyunun doruk noktasi,
                    // orada OLMAKLA gecilemez.
                    if (UcusMesafesi < EnAzUcus) { Basa(); break; }
                    if (Yatay(p, dogancilar) <= inisYaricapi)
                        Gec(Asama.Tepki);
                    break;

                case Asama.Tepki:
                    if (Yatay(p, incilikosk) <= tepkiYaricapi)
                        Gec(Asama.Bitti);
                    break;
            }
        }

        private void TalimiIzle(Vector3 p)
        {
            if (dizi == null) return;
            bool ucuyor = dizi.Simdiki == UcusDizisi.Durum.Ucuyor;

            if (ucuyor && !_ucusta)
            {
                _ucusta = true;
                _kalkis = p;
            }
            else if (!ucuyor && _ucusta)
            {
                _ucusta = false;
                float d = Yatay(p, _kalkis);

                // SAYILMAYAN DENEME SESSIZ GECMEZ.
                //
                // Once bu blok yalnizca sayiyordu ve sayamadiginda
                // hicbir sey soylemiyordu. Aritmetik acikti: kalkis
                // yatay 12,5 m/s, dikey bilesen yok, kanat 11,56:1 —
                // yani **duz zeminden bir suzulus ~22 m**. Esik 60 m.
                // Duz zeminde atilan her sicrama sayilmiyordu ve sayac
                // 0/3'te kaliyordu.
                //
                // Oyuncu bos bir cayirda dort-bes kez atliyor, her
                // seferinde ayni "0/3"u okuyor ve kanadin bozuk
                // oldugunu dusunup **oyunu burada kapatiyor**. Bir
                // ogretmenin en kotu hali, yanlisi soylemeden
                // tekrarlatandir.
                // CEMBER KALKISTA OLCULUR, INISTE DEGIL.
                //
                // Once inis noktasi soruluyordu ve bu bir tuzak
                // kuruyordu: ucuncu esik 120 m, cember 250 m — yani
                // COK IYI suzulen oyuncu cemberin disina dusuyor ve
                // "Talim Okmeydani'nda sayilir" cevabini aliyordu.
                // Bir oyuncu bunu yasadi ve hakli olarak kizdi:
                // "uzun atarsan cezalandiriliyorsun".
                //
                // Talim NEREDEN atladiginla ilgilidir.
                if (Yatay(_kalkis, okmeydani) > talimYaricapi)
                    TalimBildirimi?.Invoke("Talim Okmeydanı'nda sayılır.");
                else if (d < TalimEsigi(TalimSayisi))
                    TalimBildirimi?.Invoke(
                        $"{d:F0} m — {TalimEsigi(TalimSayisi):F0} m gerek. "
                        + "Yüksek bir yerden atla.");
                else
                {
                    TalimSayisi++;
                    TalimBildirimi?.Invoke(
                        $"{d:F0} m süzüldün · talim {TalimSayisi}/{talimHedefi}");
                }
            }
        }

        /// <summary>Çakılınca dilim kuleye döner — uçuş tekrar denenir.</summary>
        private void Basa()
        {
            Cakildi = false;
            UcusMesafesi = 0f;
            Gec(Asama.Kule);
        }

        private static float Yatay(Vector3 a, Vector3 b)
            => new Vector2(a.x - b.x, a.z - b.z).magnitude;

        private void Gec(Asama yeni)
        {
            if (Simdiki == yeni) return;
            Simdiki = yeni;
            AsamaDegisti?.Invoke(yeni);
        }
    }
}
