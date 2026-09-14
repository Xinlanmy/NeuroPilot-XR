using UnityEngine;

namespace NeuroPilotXR.Training
{
    /// <summary>
    /// Accumulates attention_update / fatigue_update / cognitive_profile envelopes for the end-of-round
    /// summary. It draws nothing on purpose: the head-locked live readout sat inside the target area and
    /// fought the HUD, so the numbers only surface in the centred result card via <see cref="ResultSummary"/>.
    /// It stays a component because FusionEegBridge attaches it at runtime and both round drivers read it.
    /// </summary>
    public sealed class VrTelemetryPanel : MonoBehaviour
    {
        private float attentionScore = 0.5f;
        private string state = "NORMAL";
        private int validSamples;
        private double attentionSum;
        private double fatigueDuration;

        public float AttentionScore => attentionScore;

        public void SetAttention(FusionJson payload)
        {
            if (payload == null) return;
            attentionScore = Read01(payload, "score", attentionScore);
            if (payload.Num("valid", 0.0) > 0.5)
            {
                attentionSum += attentionScore;
                validSamples++;
            }
        }

        public void SetFatigue(FusionJson payload)
        {
            if (payload == null) return;
            state = payload.Str("state", state);
            fatigueDuration = payload.Num("low_duration_s", fatigueDuration);
        }

        /// <summary>The profile repeats the round counters Unity already owns; only its fatigue total adds
        /// anything, and it is authoritative when it arrives.</summary>
        public void SetCognitiveProfile(FusionJson payload)
        {
            if (payload == null) return;
            double fatigueSeconds = payload.Num("fatigue_total_s", double.NaN);
            if (!double.IsNaN(fatigueSeconds)) fatigueDuration = fatigueSeconds;
        }

        public void ResetRound()
        {
            validSamples = 0;
            attentionSum = 0.0;
            fatigueDuration = 0.0;
        }

        /// <summary>Empty when no attention sample ever arrived, so the result card never prints a made-up score.</summary>
        public string ResultSummary()
        {
            if (validSamples <= 0 && fatigueDuration <= 0.0) return string.Empty;
            string text = "\n\n<size=30><color=#8DA9BA>认知画像</color></size>\n";
            if (validSamples > 0) text += string.Format("平均注意力  {0:0.00}\n", attentionSum / validSamples);
            if (fatigueDuration > 0.0) text += string.Format("低注意持续  {0:0.0} s\n", fatigueDuration);
            return text + "结束状态  " + state;
        }

        private static float Read01(FusionJson payload, string key, float fallback)
        {
            double value = payload.Num(key, fallback);
            return double.IsNaN(value) || double.IsInfinity(value)
                ? fallback
                : Mathf.Clamp01((float)value);
        }
    }
}
