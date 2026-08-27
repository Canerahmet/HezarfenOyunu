namespace Hezarfen.Flight
{
    /// <summary>
    /// Uçuş girdisi kaynağı. Arayüz olmasının sebebi test: PlayMode testleri
    /// gerçek klavye/gamepad olmadan "sanal pilot" sürebilsin diye.
    /// </summary>
    public interface IFlightInput
    {
        /// <summary>-1 burun aşağı (hız), +1 burun yukarı (yavaşlama/stall riski).</summary>
        float Pitch { get; }

        /// <summary>-1 sola yatış, +1 sağa yatış.</summary>
        float Roll { get; }
    }

    /// <summary>Sabit girdi — testler ve "eller serbest" süzülüş ölçümü için.</summary>
    public sealed class ConstantFlightInput : IFlightInput
    {
        public float Pitch { get; set; }
        public float Roll { get; set; }

        public ConstantFlightInput(float pitch = 0f, float roll = 0f)
        {
            Pitch = pitch;
            Roll = roll;
        }
    }
}
