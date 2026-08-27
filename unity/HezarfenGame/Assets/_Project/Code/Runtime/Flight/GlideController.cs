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
            CurrentDrag = q * tuning.wingArea * cd;

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

            // Pilot hedef hücum açısı komut eder.
            float targetAlpha = Mathf.Lerp(
                tuning.minCommandAlphaDeg,
                tuning.maxCommandAlphaDeg,
                (pitchIn + 1f) * 0.5f);

            float alphaErrorRad = (targetAlpha - AngleOfAttackDeg) * Mathf.Deg2Rad;
            float sideslipRad = SideslipDeg * Mathf.Deg2Rad;

            // Pilot bank AÇISI komut eder, bank HIZI değil.
            // Bank hızı komut edilseydi sabit girdi aygıtı durmadan yuvarlardı —
            // ilk sürümde tam olarak bu oldu ve RollCommand_TurnsTheCraft testi yakaladı.
            BankAngleDeg = ComputeBankAngleDeg();
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
