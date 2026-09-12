using System.Collections;
using UnityEngine;

namespace NeuroPilotXR.Navigation
{
    public sealed class NavigationController : MonoBehaviour
    {
        [SerializeField] private CanvasGroup welcomePage;
        [SerializeField] private CanvasGroup difficultyPage;
        public CanvasGroup modePage, eyePage, multiPage, settingsPage;
        public SceneTransitionManager sceneTransition;
        public TMPro.TMP_Text eyeStatus, multiStatus, singleStatus;
        public UnityEngine.UI.Button eyeStart, multiStart, singleStart;
        public CanvasGroup singleContent;
        public TMPro.TMP_Text countLabel;
        public TMPro.TMP_Text multiRuleLabel;
        public TMPro.TMP_Text eyeRuleLabel, eyeCountLabel;
        public MultiFrequencyConfig frequencyConfig;
        [SerializeField, Min(0.05f)] private float fadeDuration = 0.28f;

        private Coroutine transition;
        private CanvasGroup current;

        public void Configure(CanvasGroup welcome, CanvasGroup difficulty)
        {
            welcomePage = welcome;
            difficultyPage = difficulty;
        }

        private void Awake()
        {
            SetImmediate(welcomePage, true);
            SetImmediate(difficultyPage, false);
            SetImmediate(modePage, false);
            SetImmediate(eyePage, false);
            SetImmediate(multiPage, false);
            SetImmediate(settingsPage, false);
            current = welcomePage;
            if (TrainingSession.ReturnToModes && modePage != null)
            {
                SetImmediate(welcomePage, false);
                SetImmediate(modePage, true);
                current = modePage;
                TrainingSession.ReturnToModes = false;
            }
            RefreshCount();
        }

        public void ShowDifficulty()
        {
            TrainingSession.SelectedMode = TrainingMode.SingleTarget;
            Show(difficultyPage);
        }

        public void ShowWelcome()
        {
            Show(welcomePage);
        }

        public void ShowModes() => Show(modePage);
        public void ShowSettings() => Show(settingsPage);
        public void ShowEye() { TrainingSession.SelectedMode = TrainingMode.EyeTracking; RefreshCount(); Show(eyePage); }
        public void ShowMulti() { TrainingSession.SelectedMode = TrainingMode.MultiTarget; RefreshCount(); Show(multiPage); }
        public void FourTargets() { TrainingSession.TargetCount = 4; RefreshCount(); }
        public void FiveTargets() { TrainingSession.TargetCount = 5; RefreshCount(); }
        // Existing scene UnityEvents still reference these methods by name.
        public void MultiManual() { TrainingSession.MultiColorGaze = false; RefreshCount(); }
        public void MultiGaze() { TrainingSession.MultiColorGaze = true; RefreshCount(); }
        public void EyeSingle() { TrainingSession.EyeColorGaze = false; RefreshCount(); }
        public void EyeColor() { TrainingSession.EyeColorGaze = true; RefreshCount(); }
        public void EyeFour() { TrainingSession.EyeTargetCount = 4; RefreshCount(); }
        public void EyeFive() { TrainingSession.EyeTargetCount = 5; RefreshCount(); }
        private void RefreshCount()
        {
            if (countLabel != null) countLabel.text = "当前选择：同时 " + TrainingSession.TargetCount + " 个小球";
            if (multiRuleLabel != null) multiRuleLabel.text = TrainingSession.MultiColorGaze
                ? "已选：持续观察蓝色目标约 0.8 秒" : "已选：手柄射线瞄准任意小球，扣扳机确认";
            if (eyeRuleLabel != null) eyeRuleLabel.text = TrainingSession.EyeColorGaze
                ? "已选：持续观察蓝色目标约 0.8 秒" : "已选：连续注视单球 1 秒";
            if (eyeCountLabel != null) eyeCountLabel.text = "当前选择：同时 " + TrainingSession.EyeTargetCount + " 个彩球";
        }
        public void StartSingle() => sceneTransition.BeginMode("TrainingRoom", singleContent, singleStatus, singleStart);
        public void StartEye() => sceneTransition.BeginMode("EyeTrackingRoom", eyePage, eyeStatus, eyeStart);
        public void StartMulti() => sceneTransition.BeginMode("MultiTargetRoom", multiPage, multiStatus, multiStart);

        private void Show(CanvasGroup page)
        {
            if (page == null || page == current || transition != null ||
                (sceneTransition != null && sceneTransition.IsTransitioning)) return;
            SwitchTo(current, page);
            current = page;
        }

        private void SwitchTo(CanvasGroup outgoing, CanvasGroup incoming)
        {
            if (transition != null)
                StopCoroutine(transition);

            transition = StartCoroutine(FadePages(outgoing, incoming));
        }

        private IEnumerator FadePages(CanvasGroup outgoing, CanvasGroup incoming)
        {
            outgoing.interactable = false;
            outgoing.blocksRaycasts = false;
            incoming.gameObject.SetActive(true);
            incoming.alpha = 0f;
            incoming.interactable = false;
            incoming.blocksRaycasts = false;

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / fadeDuration));
                outgoing.alpha = 1f - t;
                incoming.alpha = t;
                yield return null;
            }

            SetImmediate(outgoing, false);
            SetImmediate(incoming, true);
            transition = null;
        }

        private static void SetImmediate(CanvasGroup group, bool visible)
        {
            if (group == null)
                return;

            group.alpha = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;
            group.gameObject.SetActive(visible);
        }
    }
}
