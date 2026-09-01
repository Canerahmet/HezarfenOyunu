using System.Collections.Generic;
using UnityEngine;

namespace Hezarfen.Sehir
{
    /// <summary>
    /// <b>Replikleri görünür kılar.</b>
    ///
    /// Beş bin replik üretmek, şehri konuşturmaz. *Üretilen ama görünmeyen
    /// bir öğe, olmayan bir öğedir* — korpus bir dosyada dururken oyuncu
    /// için hiç yoktur. Bu bileşen onu duyulur hâle getiriyor: yakındaki
    /// sakinlerin başının üstünde, kısa süre duran bir yazı.
    ///
    /// ## Neden yalnız YAKINDAKİLER
    ///
    /// Otuz kişilik bir meydanda otuz balon aynı anda açılırsa okunacak
    /// hiçbir şey kalmaz; gürültü olur. Duyma mesafesi gerçek bir insan
    /// sesinin mesafesi kadar (<see cref="duyulmaMesafesi"/>) ve aynı anda
    /// en çok <see cref="ayniAndaEnCok"/> tanesi görünür — en yakınları.
    ///
    /// ## Havuz
    ///
    /// Etiketler <see cref="NPCYonetici"/>'nin gövdeleri gibi havuzlanır:
    /// her karede yazı nesnesi yaratmak, konuşan kalabalığı çöp toplayıcı
    /// duraksamalarına çevirirdi.
    /// </summary>
    [DefaultExecutionOrder(50)]
    public class BarkGosterici : MonoBehaviour
    {
        public NPCYonetici yonetici;
        public Transform oyuncu;

        [Tooltip("Bir repliğin duyulduğu mesafe (m).")]
        public float duyulmaMesafesi = 12f;

        [Tooltip("Aynı anda en çok kaç replik görünsün.")]
        //: DORT COKTU. IKI.
        //
        // Karelerde ayni anda ALTI replik sayildi (dordu sinir olsa da:
        // ayrisma suzgeci elenenleri listeden cikariyor ama liste zaten
        // dolmus oluyordu). Sonuc `isik_gunbatimi.png`'de goruldu —
        // ekranin yarisi dev punto dunya yazisi, ust uste binmis,
        // duvarin icinden gecmis, hicbiri okunmuyor.
        //
        // Bir kalabaligin canli duyulmasi icin herkesin ayni anda
        // konusmasi gerekmiyor; tersine, herkes konusursa kimse
        // duyulmaz. Iki replik bir sokak sesidir, alti bir gurultudur.
        [Range(1, 12)] public int ayniAndaEnCok = 2;

        [Tooltip("Yazının başın üstündeki yüksekliği (m).")]
        public float yukseklik = 1.95f;

        public int GorunurReplik { get; private set; }

        private readonly List<TextMesh> _havuz = new();
        /// <summary>
        /// İki replik ekranda bu kadar yakınsa biri gizlenir (piksel).
        ///
        /// Y daha dar, X daha geniş: yazı yatay uzanır, yani yan yana
        /// iki replik birbirine değmeden durabilir ama üst üste gelen
        /// iki satır kesin okunmaz olur. 1080p'ye göre; ekran
        /// yüksekliğiyle ölçeklenmesi HUD'un ölçek işiyle birlikte
        /// gelecek.
        /// </summary>
        /// <summary>
        /// İki etiketin dikeyde ayrık sayıldığı en az piksel — 1080p'de.
        ///
        /// Sayı 1080p'de ölçüldü ve <b>oraya çakılıydı</b>; 1440p ve
        /// 4K'da aynı sahne daha çok piksel ürettiği için üst üste
        /// binme geri dönüyordu. Eşik çözünürlükle ölçeklenir
        /// (<see cref="AyrikY"/>), çünkü ölçülen şey piksel değil
        /// <b>okunabilirlik</b>.
        /// </summary>
        private const float EkranAyrikY = 46f;

        /// <summary>Geçerli çözünürlükte dikey ayrışma eşiği (px).</summary>
        private static float AyrikY => EkranAyrikY * (Screen.height / 1080f);
        private const float EkranAyrikX = 300f;

        private readonly List<Vector3> _ekran = new List<Vector3>();

        /// <summary>
        /// Repliğin ekrandaki kabaca genişliği (piksel).
        ///
        /// Gerçek ölçü <c>Renderer.bounds</c>'un ekrana izdüşümü olurdu
        /// ama etiket bu karede henüz yerleştirilmedi; metinden tahmin
        /// etmek, sabit bir sayı kullanmaktan ölçülebilir biçimde iyi.
        /// </summary>
        private float MetinEni(NPCAjan a, Vector3 ekranNoktasi)
        {
            int harf = a.replik != null && a.replik.metin != null
                ? a.replik.metin.Length : 0;
            // Etiket dunya uzayinda `uzak/12` olcekli ve ekrandaki boyu
            // bu yuzden mesafeden bagimsiz; 1080p'de harf basina ~11 px
            // olcuduk.
            return Mathf.Clamp(harf * 11f * (Screen.height / 1080f),
                               40f, Screen.width * 0.8f);
        }

        private readonly List<(NPCAjan ajan, float d2)> _adaylar = new();
        private Camera _kamera;

        private void LateUpdate()
        {
            if (yonetici == null || oyuncu == null) return;
            if (_kamera == null) _kamera = Camera.main;

            float menzil2 = duyulmaMesafesi * duyulmaMesafesi;
            _adaylar.Clear();

            // KIRK BIN DEGIL, ALTMIS.
            //
            // Burada `yonetici.Sakinler` taraniyordu — sehrin TAMAMI,
            // kirk bin kisi, her karede, mesafe hesabiyla. Oysa hemen
            // altindaki satir govdesi olmayani zaten eliyor ve govdesi
            // olan altmis kisi var. Tarama yuzde 99,85'i bulup atmak
            // icin kosuyordu.
            //
            // Olculen bedel 1,0 ms, butcenin %6'si, ekranda EN COK IKI
            // yazi icin. Once gorus hatti isinini sucladim ve onu
            // tembel yaptim — dogru bir duzeltmeydi ama sayi
            // oynamadi. Tahmin edilen darbogaz ile olculen darbogaz
            // ayni sey degil; ikincisi buydu.
            foreach (var a in yonetici.GorunurSakinler)
            {
                // Govdesi olmayan konusmaz: gorunmeyen bir agizdan cikan
                // yazi havada asili kalirdi.
                if (a.govde == null || a.replik == null) continue;
                float d2 = (a.konum - oyuncu.position).sqrMagnitude;
                if (d2 > menzil2) continue;

                _adaylar.Add((a, d2));
            }

            // En yakinlar konusur — kalabalikta duyulan da odur.
            _adaylar.Sort((x, y) => x.d2.CompareTo(y.d2));

            // GORUS HATTI SART — AMA SIRALAMADAN SONRA.
            //
            // Yazi dunyada duruyor ve duvarin ARKASINDAN da okunuyordu:
            // cami avlusunda cekilen karede dort replik direklerin
            // onunde asili duruyordu. Duyulmasi degil GORULMESI sorun.
            //
            // Kusur ISININ KENDISINDE degil, ne zaman atildigindaydi:
            // suzgec siralamadan **once** kosuyor ve menzildeki her
            // adaya (60'a kadar) 36.302 carpistiricili bir sahnede ayri
            // bir isin atiyordu — sonra 58'i zaten eleniyordu. Olculen
            // bedel +1,0 ms, yani 16,7 ms'lik butcenin %6'si, ekranda
            // IKI etiket icin.
            //
            // Dogru sira: once en yakini sec, sonra yalnizca
            // gosterecegin kadarina sor. Ayni cevap, otuz kat az isin.
            if (_kamera != null)
            {
                var goz = _kamera.transform.position;
                int gecen = 0;
                for (int i = 0; i < _adaylar.Count; i++)
                {
                    if (gecen >= ayniAndaEnCok)
                    { _adaylar.RemoveRange(i, _adaylar.Count - i); break; }

                    var agiz = _adaylar[i].ajan.konum + Vector3.up * yukseklik;
                    var fark = agiz - goz;
                    if (Physics.Raycast(goz, fark.normalized,
                                        fark.magnitude - 0.4f, ~0,
                                        QueryTriggerInteraction.Ignore))
                    { _adaylar.RemoveAt(i); i--; continue; }
                    gecen++;
                }
            }

            // EKRANDA AYRI DURSUNLAR — VE OLCU EKRANDA ALINIR.
            //
            // Once kural dunya uzayindaydi: "birbirine 3 m'den yakin
            // konusanlardan yalniz biri konusur". Yanlis cetveldi ve
            // uc ayri karede ayni kusuru birakti — bakis ekseni
            // boyunca dizilmis iki konusmaci dunyada 20 m ayriktir ama
            // EKRANDA ust uste biner. Yazi dunyada degil ekranda
            // okunuyor; ayrik olmasi gereken yer de orasi.
            //
            // Elenen aday susmaz, yalnizca YAZISI gorunmez: konusma
            // sesi ve rutini yerinde kalir.
            if (_kamera != null)
            {
                _ekran.Clear();
                for (int i = _adaylar.Count - 1; i >= 0; i--)
                {
                    var d = _kamera.WorldToScreenPoint(
                        _adaylar[i].ajan.konum + Vector3.up * yukseklik);
                    if (d.z <= 0f) { _adaylar.RemoveAt(i); continue; }

                    // AYRISMA YAZININ GENISLIGINE BAKAR, CAPAYA DEGIL.
                    //
                    // Sabit 300 px kullaniliyordu ve replikler farkli
                    // uzunlukta: 800 px genisligindeki bir yazi,
                    // 301 px otedeki komsusunu gecirip ustune biniyordu.
                    // Capalar ayrik, yazilar ic ice.
                    //
                    // Genislik metinden turetiliyor: karakter basina
                    // kabaca 0,55 punto ve etiket ortalanmis, yani
                    // yarisi her yana tasar. Kaba ama olcunun kendisi
                    // metne bagli ve bu, sabit bir sayidan iyi.
                    float benimEn = MetinEni(_adaylar[i].ajan, d);
                    bool cakisti = false;
                    foreach (var e in _ekran)
                    {
                        float pay = (benimEn + e.z) * 0.5f + 16f;
                        if (Mathf.Abs(e.y - d.y) < AyrikY
                            && Mathf.Abs(e.x - d.x) < pay)
                        { cakisti = true; break; }
                    }
                    d.z = benimEn;   // z artik derinlik degil GENISLIK

                    // EKRAN KENARINDAN TASAN YAZI DA ELENIR.
                    //
                    // Karelerde iki replik sol kenardan kesikti: capa
                    // ekranin icindeydi ama yazi disari tasiyordu.
                    // Yarisi gorunmeyen bir replik, gorunmeyen bir
                    // repliktir.
                    float yari = benimEn * 0.5f;
                    bool tasti = d.x - yari < 8f
                                 || d.x + yari > Screen.width - 8f;

                    if (cakisti || tasti) _adaylar.RemoveAt(i);
                    else _ekran.Add(d);
                }
            }

            int n = Mathf.Min(_adaylar.Count, ayniAndaEnCok);
            GorunurReplik = n;

            for (int i = 0; i < n; i++)
            {
                var t = Etiket(i);
                var a = _adaylar[i].ajan;
                t.text = a.replik.metin;
                // Golge yaziyi izler: metin degisince ikisi birlikte
                // degismeli, yoksa kontur bir onceki repligi gosterir.
                var g = t.transform.childCount > 0
                    ? t.transform.GetChild(0).GetComponent<TextMesh>() : null;
                if (g != null) g.text = t.text;
                t.transform.position = a.konum + Vector3.up * yukseklik;
                if (_kamera != null)
                {
                    var bakis = t.transform.position - _kamera.transform.position;
                    t.transform.rotation = Quaternion.LookRotation(bakis);

                    // EKRANDAKI BOYU SABIT TUT.
                    //
                    // TextMesh dunya uzayindadir: 3 m'deki bir replik
                    // ekranin ucte birini kapliyordu ("Selamunaleykum"
                    // karenin yarisi kadardi). Olcek mesafeyle buyur,
                    // boylece yazi uzakta okunur, yakinda ekrani yemez.
                    // KELEPCE YORUMUN TAM TERSINI YAPIYORDU.
                    //
                    // "Alt sinir 6 m: daha yakinda buyumesin" yaziyordu
                    // ve `Mathf.Max(6f, uzak)` tam olarak buyumesine
                    // sebep oluyordu: 6 m'nin altinda olcek 0,5'te
                    // sabitleniyor ama mesafe kuculmeye devam ediyor,
                    // yani ekrandaki boy BUYUYOR. Karelerde bir replik
                    // karenin yarisini kapliyordu.
                    //
                    // Olcek mesafeyle DOGRU ORANTILI oldugunda ekrandaki
                    // boy her mesafede sabit kalir — istenen buydu ve
                    // kelepce onu bozan seydi. Kaldirmak yeterli.
                    float uzak = Mathf.Max(0.5f, bakis.magnitude);
                    t.transform.localScale = Vector3.one * (uzak / 12f);
                }
                t.gameObject.SetActive(true);
            }
            for (int i = n; i < _havuz.Count; i++)
                _havuz[i].gameObject.SetActive(false);
        }

        private TextMesh Etiket(int i)
        {
            while (_havuz.Count <= i)
            {
                var go = new GameObject($"BARK_{_havuz.Count}");
                go.transform.SetParent(transform, false);
                var tm = go.AddComponent<TextMesh>();
                tm.characterSize = 0.055f;
                tm.fontSize = 64;
                tm.anchor = TextAnchor.LowerCenter;
                tm.alignment = TextAlignment.Center;
                tm.color = new Color(1f, 0.96f, 0.86f, 1f);

                // KONTUR: BEYAZ YAZI BEYAZ SIVANIN USTUNDE OKUNMUYOR.
                //
                // Tek renk bir yazi, arka plani ne olursa olsun ayni
                // parlaklikta ciziliyordu. `isik_ogle.png`'de "Bre
                // ignelikci geldi!" acik siva uzerinde eriyip gidiyor,
                // `isik_gunbatimi.png`'de turuncu gokyuzunde kayboluyor.
                //
                // Cozum ayri bir sader: aynı yazidan koyu bir kopya,
                // birkaç santim geride. Kamera yaziya bakiyor, yani
                // "geride" kameradan uzakta demek — koyu kopya her
                // acidan yazinin arkasinda kalir ve bir kontur gibi
                // okunur. TextMesh'in kendi kontur destegi yok ve
                // TMP'ye gecmek ayri bir is (HUD'un tamami IMGUI).
                var golgeGo = new GameObject("golge");
                golgeGo.transform.SetParent(go.transform, false);
                // OTELEME EKRAN DUZLEMINDE OLMALI, BAKIS EKSENINDE DEGIL.
                //
                // Once `(0, 0, 0.03)` yaziliyordu ve gerekcesi "koyu
                // kopya geride kalsin" idi. Ama etiket her karede
                // `LookRotation(konum - kamera)` ile donduruluyor, yani
                // **yerel +Z tam olarak bakis eksenidir**. Perspektif
                // izdusumunde bakis ekseni boyunca oteleme ekranda
                // hicbir yere kaymaz; yalnizca %0,25 kucultur. 30
                // piksellik bir harfte aciga cikan kontur 0,075
                // piksel — yani hic.
                //
                // Yerel X ve Y ekran duzlemine paraleldir; oteleme
                // oraya konur. Olcek mesafeyle orantili oldugu icin
                // (`uzak/12`) ekrandaki kayma her mesafede sabit kalir.
                golgeGo.transform.localPosition = new Vector3(0.035f, -0.035f, 0.02f);
                var golge = golgeGo.AddComponent<TextMesh>();
                golge.characterSize = tm.characterSize;
                golge.fontSize = tm.fontSize;
                golge.anchor = tm.anchor;
                golge.alignment = tm.alignment;
                golge.color = new Color(0f, 0f, 0f, 0.85f);

                _havuz.Add(tm);
            }
            return _havuz[i];
        }
    }
}
