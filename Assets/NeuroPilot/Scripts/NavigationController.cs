using System.Collections;
using UnityEngine;

namespace NeuroPilotXR.Navigation
{
    public sealed class NavigationController : MonoBehaviour
    {
        [SerializeField] private CanvasGroup welcomePage;
        [SerializeField] private CanvasGroup difficultyPage;
        [SerializeField, Min(0.05f)] private float fadeDuration = 0.28f;

        private Coroutine transition;

        public void Configure(CanvasGroup welcome, CanvasGroup difficulty)
        {
            welcomePage = welcome;
            difficultyPage = difficulty;
        }

        private void Awake()
        {
            SetImmediate(welcomePage, true);
            SetImmediate(difficultyPage, false);
        }

        public void ShowDifficulty()
        {
            SwitchTo(welcomePage, difficultyPage);
        }

        public void ShowWelcome()
        {
            SwitchTo(difficultyPage, welcomePage);
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