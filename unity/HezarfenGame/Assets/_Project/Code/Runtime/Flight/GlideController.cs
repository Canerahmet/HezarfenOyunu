using UnityEngine;

namespace Hezarfen.Flight
{
    /// <summary>
    /// Asılı planör süzülüş fiziği v0 (plan Bölüm 5).
    /// L = ½ρv²S·CL(α), D = ½ρv²S·CD(α); pitch/roll ağırlık aktarımıyla.
    ///
    /// Tasarım notu: pilot doğrudan tork uygulamaz, **hedef hücum açısı komut eder**.
    /// Gerçek ağırlık aktarımının basitleştirilmesidir; v0'da amaç fiziksel doğruluk
    /// değil, tutarlı ve öğrenilebilir davranış (plan Bölüm 2: "gerçekçilik iddiası
    /// fizik sabitlerinde değil, rüzgârın davranışının tutarlılığındadır").
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [AddComponentMenu("Hezarfen/Glide Controller")]
    public class GlideController : MonoBehaviour
    {
        [Tooltip("Tüm katsayılar burada. Boşsa bileşen çalışmaz.")]
        public WindTuning tuning;

        [Tooltip("Rüzgâr alanı. Boşsa sahnede aranır; o da yoksa yalnızca WindTuning.globalWind kullanılır.")]
        public WindField windField;

        [Tooltip("Aerodinamiğin devreye girdiği en düşük hava hızı (m/s). Altında serbest düşüş.")]
        public float minAerodynamicSpeed = 0.5f;

        private Rigidbody rb;
        private IFlightInput input;

        // --- Telemetri: HUD, testler ve ayar için okunur ---
        public float AngleOfAttackDeg { get; private set; }
        public float SideslipDeg { get; private set; }
        public float BankAngleDeg { get; private set; }
        public float AirspeedMps { get; private set; }
        public float GroundSpeedMps { get; private set; }
        public bool IsStalled { get; private set; }
        public float CurrentLift { get; private set; }
        public float CurrentDrag { get; private set; }

        /// <summary>
        /// Takılan kanat parçası sayısı (0–3) — sürüklemeyi düşürür.
        ///
        /// ## Neden bir ilerleme var
        ///
        /// Bir oyuncu iki saat oynadı ve şunu yazdı: *"Yaptığım hiçbir
        /// şeyin sonucu yok. Otuz görev yaptım, değişen tek şey
        /// kesedeki sayı."* Kese doluyordu ve harcanacak bir şey yoktu;
        /// en pahalı mal 3 akçeydi.
        ///
        /// Kanat parçası o boşluğun iki ucunu birden kapatıyor: akçenin
        /// gideceği yer <b>ve</b> uçuşun iyileşeceği yol. Zincir
        /// <b>çalış → parça → daha uzağa uç</b>.
        ///
        /// ## Neden sürükleme, neden taşıma değil
        ///
        /// Taşımayı artırmak kanadı büyütmek olurdu ve `wingArea`
        /// tarihsel bir iddia taşıyor (RESEARCH: kartal kanadı taklidi).
        /// Sürüklemeyi düşürmek ise <b>işçiliktir</b>: daha sıkı
        /// gerilmiş bez, daha temiz yontulmuş kaburga. Aynı kanat, daha
        /// iyi yapılmış.
        ///
        /// Parça başına %6: üç parça sürüklemeyi %17 düşürür, süzülme
        /// oranını 11,56'dan ~13,9'a çıkarır — 51,6 m'den menzil
        /// 597'den ~717 m'ye. Oyunu bitirmeye yetmez (ADR 0084 hâlâ
        /// açık) ama farkı <b>uçarken hissedilir</b>.
        /// </summary>
        [Range(0, 3)] public int kanatParcasi;

        /// <summary>Parça başına sürükleme indirimi.</summary>
        public const float ParcaBasinaIndirim = 0.06f;
        public Vector3 WindAtCraft { get; private set; }

        /// <summary>Girdi kaynağını değiştirir (testlerde sanal pilot için).</summary>
        public void SetInput(IFlightInput source) => input = source;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();

            if (tuning != null)
            {
                rb.mass = tuning.mass;
            }

            rb.useGravity = true;
            // Aerodinamik sönümlemeyi biz hesaplıyoruz; Unity'ninki üstüne binmemeli.
            rb.linearDamping = 0f;
            rb.angularDamping = 0f;

            if (input == null)
            {
                input = GetComponent<IFlightInput>() as IFlightInput ?? new ConstantFlightInput();
            }
        }

        private void FixedUpdate() => Step();

        /// <summary>
        /// Bir fizik adımının aerodinamik kuvvetlerini uygular.
        /// <c>public</c> olmasının sebebi test: PlayMode testleri
        /// <c>Physics.simulationMode = Script</c> ile bunu elle sürüp
        /// ortaya çıkan süzülme oranını ölçebilsin. Aksi halde FixedUpdate'i
        /// gerçek zamanda beklemek gerekirdi ve test hem yavaş hem kararsız olurdu.
        /// </summary>
        public void Step()
        {
            if (tuning == null) return;

            // Awake her bağlamda çalışmaz (edit-mode araçları, testler).
            if (rb == null) rb = GetComponent<Rigidbody>();
            if (rb == null) return;

            WindAtCraft = SampleWind();

            Vector3 relativeAir = rb.linearVelocity - WindAtCraft;
            GroundSpeedMps = rb.linearVelocity.magnitude;
            AirspeedMps = relativeAir.magnitude;

            if (AirspeedMps < minAerodynamicSpeed)
            {
                // Hava akışı yok — taşıma da yok. Serbest düşüş.
                IsStalled = false;
                CurrentLift = 0f;
                CurrentDrag = 0f;
                return;
            }

            Vector3 flowDir = relativeAir / AirspeedMps;
            Vector3 local = transform.InverseTransformDirection(relativeAir);

            // alpha > 0  => burun, akışa göre YUKARI bakıyor
            AngleOfAttackDeg = Mathf.Atan2(-local.y, local.z) * Mathf.Rad2Deg;
            SideslipDeg = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
            IsStalled = Mathf.Abs(AngleOfAttackDeg) > tuning.stallAngleDeg;

            float cl = Aerodynamics.LiftCoefficient(AngleOfAttackDeg, tuning);
            float cd = Aerodynamics.DragCoefficient(AngleOfAttackDeg, cl, tuning);
            float q = Aerodynamics.DynamicPressure(AirspeedMps, tuning);

            CurrentLift = q * tuning.wingArea * cl;
            // ISCILIK SURUKLEMEYI DUSURUR.
            //
            // Takilan her kanat parcasi %6: daha siki gerilmis bez,
            // daha temiz yontulmus kaburga. Ayni kanat, daha iyi
            // yapilmis.
            float iscilik = 1f - ParcaBasinaIndirim
                            * Mathf.Clamp(kanatParcasi, 0, 3);
            CurrentDrag = q * tuning.wingArea * cd * iscilik;

            // Taşıma akışa DİK, gövde yukarısı tarafında. Çift çapraz çarpım,
            // gövde-yukarı vektörünün akışa dik bileşenini verir.
            Vector3 liftDir = Vector3.Cross(Vector3.Cross(flowDir, transform.up), flowDir);
            liftDir = liftDir.sqrMagnitude > 1e-6f ? liftDir.normalized : transform.up;

            rb.AddForce(liftDir * CurrentLift - flowDir * CurrentDrag);

            ApplyPilotControl(q);
        }

        /// <summary>
        /// Aygıtın bulunduğu noktadaki rüzgâr. WindField varsa yükseltici hacimler
        /// dahil edilir; yoksa yalnızca global lodos.
        /// </summary>
        private Vector3 SampleWind()
        {
            if (windField == null && !windFieldSearched)
            {
                windFieldSearched = true;
                windField = FindAnyObjectByType<WindField>();
            }

            return windField != null ? windField.Sample(transform.position) : tuning.globalWind;
        }

        private bool windFieldSearched;

        /// <summary>
        /// Yatış açısı: gövde "sağ" vektörünün, ufuktaki sağ vektörüne göre sapması.
        /// Pozitif = sağa yatış (sağ kanat aşağıda).
        /// </summary>
        private float ComputeBankAngleDeg()
        {
            Vector3 levelRight = Vector3.Cross(Vector3.up, transform.forward);

            // Burun tam dikeyse "ufuktaki sağ" tanımsız — son bilinen değeri koru.
            if (levelRight.sqrMagnitude < 1e-6f) return BankAngleDeg;

            // NEGATİF: sağa yatışta (sağ kanat aşağıda) SignedAngle negatif döner.
            // İşaret çevrilmezse yatış denetimi negatif geri besleme yerine POZİTİF
            // geri besleme olur ve en ufak sapma aygıtı ters çevirir — ilk sürümde
            // tam olarak bu oldu, GlideSimulationTests yakaladı.
            return -Vector3.SignedAngle(levelRight.normalized, transform.right, transform.forward);
        }

        private void ApplyPilotControl(float dynamicPressure)
        {
            float pitchIn = Mathf.Clamp(input?.Pitch ?? 0f, -1f, 1f);
            float rollIn = Mathf.Clamp(input?.Roll ?? 0f, -1f, 1f);

            // Yatış açısı ÖNCE hesaplanır: hedef hücum açısı ona bağlı.
            BankAngleDeg = ComputeBankAngleDeg();

            // ELLER SERBEST = EN IYI SUZULUS.
            //
            // Once dogrusal esleme vardi: pitch −1 → 1°, +1 → 24°,
            // yani **notr cubuk 12,5°**. O aci bir tesadüf degil, EN AZ
            // BATIS acisi (0,94 m/s) — termikte donmek icin dogru,
            // mesafe icin yanlis. En iyi suzulus 6,2°'de ve L/D orada
            // 11,56; 12,5°'de 9,84.
            //
            // Olculen bedel: 51,6 m'den menzil 596 m yerine **508 m**,
            // yani her ucusun **%15'i**. Ve klavye oyuncusu bu 88 m'yi
            // geri alamiyordu bile: `1DAxis` kompoziti yalniz −1, 0, +1
            // uretir, arada bir yer TUTTURULAMAZ. Yani kanadin gercek
            // menzili oyuncunun ulasabildigi uc trimin de disindaydi.
            //
            // Yeni esleme uc noktalari korur (−1 hala 1°, +1 hala 24°)
            // ama merkezi kanadin kendi en iyi noktasina tasir: itmek
            // hizlandirir, cekmek yavaslatir, birakmak **en uzaga**
            // goturur. Bir suzulme oyununda notr girdinin karsiligi
            // budur.
            float enIyiAlfa = Aerodynamics.BestGlideRatio(tuning).alphaDeg;
            enIyiAlfa = Mathf.Clamp(enIyiAlfa, tuning.minCommandAlphaDeg,
                                    tuning.maxCommandAlphaDeg);
            float targetAlpha = pitchIn >= 0f
                ? Mathf.Lerp(enIyiAlfa, tuning.maxCommandAlphaDeg, pitchIn)
                : Mathf.Lerp(enIyiAlfa, tuning.minCommandAlphaDeg, -pitchIn);

            // YATIŞTA DAHA ÇOK TAŞIMA GEREKİR — VE MODEL BUNU İSTEMİYORDU.
            //
            // Yatmış uçuşta taşımanın dikey bileşeni cos φ kadar
            // azalır; düz uçuşu sürdürmek için gereken taşıma
            // W / cos φ olur (55°'de 1,74 katı). Pilot hedef hücum
            // açısı komut ediyordu ve o hedef <b>yatıştan habersizdi</b>.
            //
            // Sonuç ölçüldü ve ders kitabı: uçuş yolu, gövdenin
            // dönebileceğinden hızlı dikleşiyor, hücum açısı çöküyor,
            // taşıma çöküyor, dalış dikleşiyor — <b>sarmal ıraksaması</b>.
            //
            // | yatış | ölçülen batış | teorik |
            // |------:|--------------:|-------:|
            // |    0° |          0,93 |   1,08 |
            // |   33° |          2,49 |   1,40 |
            // |   55° |         10,39 |   2,48 |
            //
            // 55°'de teorinin 4,2 katı ve hücum açısı −0,1°. Yani
            // termikte dönmek hiçbir yatış açısında mümkün değildi.
            // Hiçbir test bunu görmedi çünkü süzülme oranını ölçen
            // test yalnız <b>yatışsız</b> uçuyor.
            //
            // Telafi, gerçek bir pilotun dönüşte yaptığı şeyin ta
            // kendisi: <b>çekmek</b>. Yük katsayısı 2,5'te kırpılır —
            // kırpma olmadan 80°+ yatışta hedef açı komut sınırını
            // aşıp stall'a sokardı.
            //
            // Yatışsız uçuşta cos 0 = 1, yani bu terim <b>etkisizdir</b>:
            // mevcut süzülme testleri aynen geçer.
            float cosBank = Mathf.Cos(BankAngleDeg * Mathf.Deg2Rad);

            // DONUSTE TABAN ACI EN AZ BATISA KAYAR.
            //
            // Notr aciyi en iyi suzulusa (6,2°) tasimak duz ucusu
            // %17 uzattı ve **donusu bozdu**: 33° yatista batis
            // 1,27'den 4,36 m/s'ye cikti. Sebep aerodinamik ve
            // dogru — yatmis kanat daha cok CL ister, en iyi
            // suzulusun dusuk CL'i orada yetmez.
            //
            // Cozum carpani buyutmek degil, TABANI degistirmek:
            // gercek bir pilot donuste yavaslar. Yatis arttikca
            // hedef aci en iyi suzulusten en az batisa (12,5°)
            // kayar. Duz ucusta terim etkisiz (cos 0 = 1), yani
            // menzil kazanci aynen durur.
            if (pitchIn >= -0.01f && pitchIn <= 0.01f)
            {
                float minBatisAlfa = Aerodynamics.MinSinkRate(tuning).alphaDeg;
                // KAYMA 25 DERECEDE TAMAMLANIR, 55'TE DEGIL.
                //
                // Once 55°'ye (izin verilen en cok yatis) gore
                // olceklendi ve olcum yetmedigini soyledi: 33°
                // yatista batis 4,36'dan 3,13 m/s'ye indi ama kapi
                // 2,92. Sebep aritmetikte — 33°'de kayma yalnizca
                // 0,375 oluyor, yani taban aci 8,6°'de kaliyor.
                //
                // Dogru esik dönüşün ne zaman DAYANIKLILIK istedigi:
                // 25 derecelik bir yatis zaten bir manevradir, tam
                // yatisi beklemenin sebebi yok. Boylece 33°'de kayma
                // tamamlanir ve olculen eski davranisa (2,30 m/s)
                // donulur — menzil kazanci ise duz ucusta oldugu
                // icin aynen kalir.
                const float TamKaymaYatisi = 25f;
                float esik = 1f - Mathf.Cos(TamKaymaYatisi * Mathf.Deg2Rad);
                float kayma = Mathf.Clamp01((1f - cosBank) / esik);
                targetAlpha = Mathf.Lerp(targetAlpha, minBatisAlfa, kayma);
            }
            float yukKatsayisi = Mathf.Min(2.5f, 1f / Mathf.Max(0.25f,
                                                                 cosBank));
            // ...AMA TELAFI STALL'A SOKMAMALI.
            //
            // Kirpma siniri `maxCommandAlphaDeg` = 24 idi, oysa
            // `stallAngleDeg` = 15. Notr pitch'te taban aci 12,5
            // oldugu icin hesap sudur: φ = 33,6° → komut 15,0° = tam
            // stall esigi; φ = 55° (izin verilen en cok yatis) →
            // 21,8°, yani stall'in **6,8 derece icinde**. Donus
            // araliginin ust 21 derecesi, tanimi geregi bir stall
            // komutuydu.
            //
            // Ve testler bunu goremedi: suzulme testleri yatissiz
            // uciyor, sarmal testi 33°'de olcuyor — cetvel kusurun bir
            // milimetre berisinde duruyordu.
            //
            // Tavan stall acisinin 1,5 derece altinda. Fazla yuk artik
            // stall'a degil **hiz kaybina** doner; asili planörün de
            // yaptigi sey budur.
            // ...AMA TAVAN PILOTUN KENDI KOMUTUNU KIRPMAZ.
            //
            // Ilk halde tavan kosulsuz uygulandi ve olcum aninda
            // soyledi: "tam burun yukarida stall'a girilemiyor,
            // alpha 10,3 derece". Yani stall'i kaza olmaktan
            // cikarayim derken onu SECILEMEZ yapmistim — oysa
            // burnu tam yukari cekmek bir hatanin degil bir
            // KARARIN karsiligi olmali (kisa alana inmek, hizi
            // hizlica kirmak).
            //
            // Tavan yalnizca telafinin FAZLASINI tutar: kendi
            // komutun her zaman gecer, yatis yuzunden eklenen pay
            // seni stall'a itemez.
            // TAVAN, UCULACAK ACIYA UYGULANIR — KOMUT EDILENE DEGIL.
            //
            // Bu satirlar once burada duruyor, on-telafi ise yirmi
            // satir asagida uygulaniyordu ve sira yanlisti: donuste
            // taban aci 12,04'e kayiyor, tavan 13,5'te tutuyor, sonra
            // telafi 1,364 ile carpip **16,4** yapiyordu. Stall acisi
            // 15. Yani kanat her donuste stall'a giriyor, kontrol
            // otoritesini kaybediyor ve 20 derece komut edilen yatista
            // ancak 15,6 derece yatabiliyordu — iki donus testi bunu
            // yakaladi.
            //
            // Tavan `uculacak` acinin tavanidir; telafi ondan SONRA
            // gelir ve kendi siniri `maxCommandAlphaDeg`dir.
            float alfaTavani = Mathf.Max(targetAlpha,
                Mathf.Min(tuning.maxCommandAlphaDeg,
                          tuning.stallAngleDeg - 1.5f));
            targetAlpha = Mathf.Min(targetAlpha * yukKatsayisi, alfaTavani);

            // ON-TELAFI: KOMUT EDILEN ACI GERCEKTEN UCULSUN.
            //
            // Iki tork terimi ayni anda calisiyor: `pitchAuthority`
            // (2,2) hedefe cevirir, `pitchStability` (0,8) aciyi sifira
            // geri ceker. Denge `α = hedef × 2,2/3,0`, yani kanat
            // komut edilenin **%73'unu** uçuyordu ve
            // `BestGlideRatio`'nun 6,23 derecesine hic ulasmamisti.
            float telafi = (tuning.pitchAuthority + tuning.pitchStability)
                           / Mathf.Max(0.01f, tuning.pitchAuthority);
            targetAlpha = Mathf.Min(targetAlpha * telafi,
                                    tuning.maxCommandAlphaDeg);


            float alphaErrorRad = (targetAlpha - AngleOfAttackDeg) * Mathf.Deg2Rad;
            float sideslipRad = SideslipDeg * Mathf.Deg2Rad;

            // Pilot bank AÇISI komut eder, bank HIZI değil.
            // Bank hızı komut edilseydi sabit girdi aygıtı durmadan yuvarlardı —
            // ilk sürümde tam olarak bu oldu ve RollCommand_TurnsTheCraft testi yakaladı.
            float targetBank = rollIn * tuning.maxBankAngleDeg;
            float bankErrorRad = (targetBank - BankAngleDeg) * Mathf.Deg2Rad;

            // Kontrol otoritesi hava hızıyla artar — yavaşken kanat "boşa düşer".
            // Bu, stall'ı gerçekten cezalandıran şeydir.
            float authority = Mathf.Clamp01(dynamicPressure / 100f);

            Vector3 torque = Vector3.zero;

            // Burun yukarı = -right ekseni etrafında (sağ el kuralı: +right burnu AŞAĞI alır)
            torque += -transform.right * (alphaErrorRad * tuning.pitchAuthority * authority);

            // Aerodinamik pitch kararlılığı: hücum açısını sıfıra geri iter.
            // Stall aşıldığında ek "kırılma" momenti burnu kesin biçimde aşağı atar.
            // Bu terim olmadan aygıt takla atıyordu (bkz. GlideSimulationTests).
            // KARARLILIK SIFIRA ITER — VE OYLE KALIYOR.
            //
            // Teshis, bu terimin hucum acisini komut edilen aciya
            // itmesini onerdi: yatista pilot 1/cos φ kadar buyuk bir
            // aci istiyor ve sabit geri cekme telafinin yarisini geri
            // aliyor. Mantik dogru gorunuyordu.
            //
            // DENENDI VE OLCUM REDDETTI. `(AngleOfAttackDeg -
            // targetAlpha) * pitchStability` yazildiginda duz ucus
            // coktu: suzulme orani 11,2:1 -> **2,57:1**, notr girdide
            // hucum acisi 15,5 derece (stall), donus 20 dereceden 9,7
            // dereceye dustu. Bes test birden kirmizi yandi.
            //
            // Sebep: alfa-orantili geri yukleme egimi, bu modelde pitch
            // dongusunun TEK kararlilik kaynagi. Onu hedefe kaydirmak
            // egimi korumus gibi gorunuyor ama komut edilen aci hava
            // hizina bagli olarak kaydigi icin dongu marjinal kararli
            // hale geliyor.
            //
            // Yatis telafisi (yukarida) tek basina 2,49 -> 2,12 m/s
            // kazandirdi ve hicbir seyi kirmadi. Kalan acik icin dogru
            // yol muhtemelen sabit ALFA yerine sabit HAVA HIZI trimi —
            // ayri bir tur ve ayri bir olcum isi.
            float stabilizingDeg = AngleOfAttackDeg * tuning.pitchStability;
            float overStall = Mathf.Abs(AngleOfAttackDeg) - tuning.stallAngleDeg;
            if (overStall > 0f)
            {
                stabilizingDeg += Mathf.Sign(AngleOfAttackDeg) * overStall * tuning.stallBreakMoment;
            }

            // Kararlılık, pilot kontrolünün aksine hava hızıyla sıfıra gitmez — bkz.
            // minStabilityAuthority. Aksi halde stall kendi kendini besleyen bir tuzak olurdu.
            float stabilityAuthority = Mathf.Max(authority, tuning.minStabilityAuthority);
            torque += transform.right * (stabilizingDeg * Mathf.Deg2Rad * stabilityAuthority);

            // Sağa yatış = -forward ekseni etrafında (sağ el kuralı: +forward sol kanadı indirir)
            torque += -transform.forward * (bankErrorRad * tuning.rollAuthority * authority);

            // Rüzgâr gülü: yan kaymayı sıfırlamak için burnu akışa çevir
            torque += transform.up * (sideslipRad * tuning.yawStability * authority);

            // Salınım sönümlemesi
            torque -= rb.angularVelocity * tuning.angularDamping;

            rb.AddTorque(torque, ForceMode.Acceleration);
        }
    }
}
