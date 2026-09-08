using UnityEngine;

namespace NeuroPilotXR.Navigation
{
    /// <summary>Continuous-time requested phase, sampled at rendering rate (not validated optical timing).</summary>
    public sealed class FrequencyStimulus : MonoBehaviour
    {
        public float Frequency { get; private set; }
        private Renderer surface;
        private MaterialPropertyBlock block;
        private double onset;
        private float duty;
        private bool running;
        public float Intensity = 1f;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        public void Begin(float hz, float dutyCycle)
        {
            surface = GetComponent<Renderer>(); block = block ?? new MaterialPropertyBlock();
            Frequency = hz; duty = dutyCycle; onset = Time.realtimeSinceStartupAsDouble; running = true;
            Paint(true);
        }
        public void Stop()
        {
            running = false;
            if (surface != null) surface.SetPropertyBlock(null);
        }
        private void Update()
        {
            if (!running) return;
            double phase = (Time.realtimeSinceStartupAsDouble - onset) * Frequency;
            Paint(phase - System.Math.Floor(phase) < duty);
        }
        private void Paint(bool on)
        {
            block.SetColor(BaseColorId, Color.Lerp(new Color(.35f, .4f, .45f), on ? Color.white : new Color(.07f, .07f, .07f), Intensity));
            surface.SetPropertyBlock(block);
        }
        private void OnDisable() => Stop();
    }
}
