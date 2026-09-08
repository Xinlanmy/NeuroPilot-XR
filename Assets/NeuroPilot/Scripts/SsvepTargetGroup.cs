using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeuroPilotXR.Navigation
{
    /// <summary>Three independent, simultaneous SSVEP targets. No eye-tracking dependency.</summary>
    public sealed class SsvepTargetGroup : MonoBehaviour
    {
        public MultiFrequencyConfig config;
        public event Action<PracticeTarget, bool> EpisodeChanged;
        private readonly List<PracticeTarget> targets = new List<PracticeTarget>();
        public int ActiveCount => targets.FindAll(t => t != null && t.Stimulating).Count;
        public void SetTargets(IEnumerable<PracticeTarget> items)
        {
            StopAll(); targets.Clear(); targets.AddRange(items);
            foreach (var target in targets) Begin(target);
        }
        public void Begin(PracticeTarget target)
        {
            if (target == null || target.Stimulating || !target.Available || config == null || !config.IsValid(3)) return;
            target.Frequency = config.frequencies[target.Slot - 1];
            target.Id = Guid.NewGuid().ToString("N");
            target.Stimulating = true;
            target.Stimulus.Intensity = 1f;
            target.Stimulus.Begin(target.Frequency, config.dutyCycle);
            EpisodeChanged?.Invoke(target, true);
        }
        public void Stop(PracticeTarget target)
        {
            if (target == null || !target.Stimulating) return;
            target.Stimulus.Stop();
            // Latch before callbacks to prevent duplicate offsets during re-entrant cleanup.
            target.Stimulating = false;
            EpisodeChanged?.Invoke(target, false);
            target.Id = null; target.Frequency = 0;
        }
        public void StopAll() { foreach (var target in targets) Stop(target); }
        private void OnDisable() => StopAll();
    }
}
