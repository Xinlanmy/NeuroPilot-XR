using UnityEngine;

namespace NeuroPilotXR.Navigation
{
    public sealed class PracticeTarget : MonoBehaviour
    {
        public string Id { get; internal set; }
        public int Slot { get; internal set; }
        public int ColorIndex { get; internal set; }
        public FrequencyStimulus Stimulus { get; internal set; }
        public bool Stimulating { get; internal set; }
        public float Frequency { get; internal set; }
        public float FeedbackUntil { get; internal set; }
        public Renderer Surface { get; internal set; }
        public bool Available => FeedbackUntil <= 0f;
    }
}
