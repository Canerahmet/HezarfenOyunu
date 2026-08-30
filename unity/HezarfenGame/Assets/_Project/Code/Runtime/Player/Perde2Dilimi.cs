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
                    // Kuleye YATAY olarak yaklasmak yeter: tepesine cikmak
                    // dikey bir hareket ve onu `dizi` olcuyor.
                    if (Yatay(p, kule) <= kuleYaricapi
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
                // Talim YALNIZ Okmeydani'nda sayilir: baska yerde yapilan
                // deneme talim degil, uçustur.
                if (Yatay(p, okmeydani) <= talimYaricapi
                    && Yatay(p, _kalkis) >= talimMesafesi)
                    TalimSayisi++;
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
