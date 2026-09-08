using UnityEngine;

namespace NeuroPilotXR.Navigation
{
    [CreateAssetMenu(menuName = "NeuroPilot/Multi-target frequency mapping")]
    public sealed class MultiFrequencyConfig : ScriptableObject
    {
        [Tooltip("Three simultaneous targets use entries 1, 2, 3 respectively. Prototype frequencies must match the algorithm.")]
        public float[] frequencies = new float[0];
        [UnityEngine.Serialization.FormerlySerializedAs("mappingConfirmed")]
        public bool prototypeEnabled;
        [Range(.05f, .95f)] public float dutyCycle = .5f;
        public bool IsValid(int count)
        {
            if (!prototypeEnabled || frequencies == null || frequencies.Length != count) return false;
            for (int i = 0; i < frequencies.Length; i++)
            {
                if (float.IsNaN(frequencies[i]) || float.IsInfinity(frequencies[i]) || frequencies[i] <= 0) return false;
                for (int j = 0; j < i; j++)
                    if (Mathf.Abs(frequencies[i] - frequencies[j]) < .01f || Mathf.Abs(frequencies[i] - frequencies[j] * 2) < .05f ||
                        Mathf.Abs(frequencies[j] - frequencies[i] * 2) < .05f) return false;
            }
            return true;
        }

    }
}
