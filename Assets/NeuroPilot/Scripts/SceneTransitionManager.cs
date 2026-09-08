using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NeuroPilotXR.Navigation
{
    public sealed class SceneTransitionManager : MonoBehaviour
    {
        [SerializeField] private string trainingSceneName = "TrainingRoom";
        [SerializeField] private CanvasGroup difficultyContent;
        [SerializeField] private TMP_Text preparingText;
        [SerializeField] private CanvasGroup screenFade;
        [SerializeField] private Canvas fadeCanvas;
        [SerializeField] private Button startButton;
        [SerializeField] private VivePassthroughManager passthroughManager;
        [SerializeField, Min(0.1f)] private float fadeDuration = 0.45f;

        private bool transitioning;
        public bool IsTransitioning => transitioning;

        public void BeginMode(string scene, CanvasGroup content, TMP_Text status, Button trigger)
        {
            if (transitioning) return;
            trainingSceneName = scene;
            difficultyContent = content;
            preparingText = status;
            startButton = trigger;
            BeginTraining();
        }

        public void Configure(
            CanvasGroup content,
            TMP_Text status,
            CanvasGroup fade,
            Canvas transitionCanvas,
            Button trigger,
            VivePassthroughManager passthrough,
            string targetSceneName = "TrainingRoom")
        {
            difficultyContent = content;
            preparingText = status;
            screenFade = fade;
            fadeCanvas = transitionCanvas;
            startButton = trigger;
            passthroughManager = passthrough;
            trainingSceneName = targetSceneName;
        }

        private void Awake()
        {
            if (preparingText != null)
                preparingText.gameObject.SetActive(false);

            if (screenFade != null)
                screenFade.alpha = 0f;
        }

        public void BeginTraining()
        {
            if (!Application.CanStreamedLevelBeLoaded(trainingSceneName))
            {
                Debug.LogError("[NeuroPilot] Training scene is not in Build Settings: " + trainingSceneName);
                return;
            }
            if (!transitioning)
                StartCoroutine(Transition());
        }

        private IEnumerator Transition()
        {
            transitioning = true;
            DontDestroyOnLoad(gameObject);

            if (startButton != null)
                startButton.interactable = false;

            if (difficultyContent != null)
            {
                difficultyContent.interactable = false;
                difficultyContent.blocksRaycasts = false;
                yield return Fade(difficultyContent, difficultyContent.alpha, 0f, fadeDuration);
            }

            if (preparingText != null)
            {
                preparingText.gameObject.SetActive(true);
                for (int count = 3; count >= 1; count--)
                {
                    preparingText.text = "正在准备训练...\n" + count;
                    yield return new WaitForSecondsRealtime(0.55f);
                }
            }

            Coroutine passthroughFade = null;
            if (passthroughManager != null)
                passthroughFade = StartCoroutine(passthroughManager.FadeOutAndDisable(fadeDuration));

            yield return Fade(screenFade, 0f, 1f, fadeDuration);
            if (passthroughFade != null)
                yield return passthroughFade;

            AsyncOperation load = SceneManager.LoadSceneAsync(trainingSceneName);
            while (!load.isDone)
                yield return null;

            yield return null;
            if (fadeCanvas != null)
                fadeCanvas.worldCamera = Camera.main;

            yield return Fade(screenFade, 1f, 0f, fadeDuration);
            Destroy(gameObject);
        }

        private static IEnumerator Fade(CanvasGroup group, float from, float to, float duration)
        {
            if (group == null)
                yield break;

            float elapsed = 0f;
            group.alpha = from;
            while (elapsed < duration)
            {
                if (group == null)
                    yield break;

                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                group.alpha = Mathf.Lerp(from, to, t);
                yield return null;
            }

            if (group != null)
                group.alpha = to;
        }
    }
}
