using UnityEngine;
using UnityEngine.InputSystem;

namespace Hezarfen.Flight
{
    /// <summary>
    /// Klavye + gamepad girdisi (plan Görev 5: "klavye ve gamepad").
    /// Bağlantılar kodda kuruluyor — v0 için .inputactions asset'i fazladan
    /// kırılganlık getirirdi; tuş haritası büyüdüğünde asset'e taşınacak.
    ///
    /// Kural: <see cref="Pitch"/> +1 = burun yukarı. Gamepad'de çubuğu geriye çekmek
    /// (aşağı) burun yukarı demektir — uçuş geleneği — bu yüzden ekseni ters çeviriyoruz.
    /// </summary>
    [AddComponentMenu("Hezarfen/Player Flight Input")]
    public class PlayerFlightInput : MonoBehaviour, IFlightInput
    {
        private InputAction pitchAction;
        private InputAction rollAction;

        public float Pitch { get; private set; }
        public float Roll { get; private set; }

        private void OnEnable()
        {
            pitchAction = new InputAction("Pitch", InputActionType.Value);
            pitchAction.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/w")   // it: burun asagi
                .With("Positive", "<Keyboard>/s");  // cek: burun yukari
            pitchAction.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/upArrow")
                .With("Positive", "<Keyboard>/downArrow");
            pitchAction.AddBinding("<Gamepad>/leftStick/y").WithProcessor("invert");

            rollAction = new InputAction("Roll", InputActionType.Value);
            rollAction.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/a")
                .With("Positive", "<Keyboard>/d");
            rollAction.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/leftArrow")
                .With("Positive", "<Keyboard>/rightArrow");
            rollAction.AddBinding("<Gamepad>/leftStick/x");

            pitchAction.Enable();
            rollAction.Enable();
        }

        private void OnDisable()
        {
            pitchAction?.Disable();
            pitchAction?.Dispose();
            rollAction?.Disable();
            rollAction?.Dispose();
            pitchAction = null;
            rollAction = null;
        }

        /// <summary>
        /// Klavye ekseninin tam değere ulaşma hızı (birim/s).
        ///
        /// <b>Neden rampa var.</b> <c>1DAxis</c> kompoziti klavyeyi
        /// ikili okur: A'ya dokunmak <c>Roll = −1</c>, yani
        /// <c>targetBank = −55°</c> — anında, tam yatış komutu.
        /// Ölçülen model 33° yatışta 2,12 m/s batıyor, yani rotayı 10°
        /// düzeltmek isteyen klavye oyuncusu <b>ara yatış açısı
        /// seçemiyor</b> ve kısacık bir dokunuş ona irtifa ödetiyordu.
        /// Kol analog olduğu için bu kusur yalnız klavyede vardı ve
        /// yalnız klavye dalına rampa uygulanır.
        ///
        /// 3,0/s: tuşu basılı tutmak 0,33 s'de tam yatışa götürür —
        /// bir refleks kadar hızlı, bir kaza kadar hızlı değil.
        /// </summary>
        public const float KlavyeRampasi = 3f;

        private void Update()
        {
            float hamPitch = Mathf.Clamp(
                pitchAction?.ReadValue<float>() ?? 0f, -1f, 1f);
            float hamRoll = Mathf.Clamp(
                rollAction?.ReadValue<float>() ?? 0f, -1f, 1f);

            // Kol takiliysa analog deger zaten ara aciyi tasiyor;
            // rampa onu YAVASLATIR ve his kaybettirir.
            bool kol = Gamepad.current != null
                       && Gamepad.current.leftStick.ReadValue().sqrMagnitude
                          > 0.02f;
            if (kol)
            {
                Pitch = hamPitch; Roll = hamRoll;
                return;
            }

            float h = KlavyeRampasi * Time.deltaTime;
            Pitch = Mathf.MoveTowards(Pitch, hamPitch, h);
            Roll = Mathf.MoveTowards(Roll, hamRoll, h);
        }
    }
}
