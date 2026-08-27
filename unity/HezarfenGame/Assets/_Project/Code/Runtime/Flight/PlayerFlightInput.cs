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

        private void Update()
        {
            Pitch = Mathf.Clamp(pitchAction?.ReadValue<float>() ?? 0f, -1f, 1f);
            Roll = Mathf.Clamp(rollAction?.ReadValue<float>() ?? 0f, -1f, 1f);
        }
    }
}
