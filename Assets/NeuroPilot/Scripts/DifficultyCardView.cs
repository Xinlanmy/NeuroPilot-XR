using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NeuroPilotXR.Navigation
{
    [RequireComponent(typeof(Button))]
    public sealed class DifficultyCardView : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private DifficultyLevel level;
        [SerializeField] private DifficultySelector selector;
        [SerializeField] private Button button;
        [SerializeField] private Image surface;
        [SerializeField] private Image border;
        [SerializeField] private TMP_Text selectedLabel;

        private readonly Color normalSurface = new Color(0.012f, 0.055f, 0.14f, 0.96f);
        private readonly Color selectedSurface = new Color(0.025f, 0.24f, 0.58f, 0.98f);
        private readonly Color normalBorder = new Color(0.03f, 0.40f, 1f, 0.72f);
        private readonly Color selectedBorder = new Color(0.52f, 0.92f, 1f, 1f);

        private Vector3 baseScale;
        private bool selected;
        private bool hovered;
        private bool pressed;

        public DifficultyLevel Level => level;

        public void Configure(
            DifficultyLevel value,
            DifficultySelector owner,
            Button cardButton,
            Image cardSurface,
            Image cardBorder,
            TMP_Text selectedText)
        {
            level = value;
            selector = owner;
            button = cardButton;
            surface = cardSurface;
            border = cardBorder;
            selectedLabel = selectedText;
        }

        private void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();

            baseScale = transform.localScale;
            button.onClick.AddListener(Choose);
            ApplyVisuals();
        }

        private void OnDestroy()
        {
            if (button != null)
                button.onClick.RemoveListener(Choose);
        }

        private void Update()
        {
            float multiplier = pressed ? 0.985f : hovered ? 1.025f : 1f;
            Vector3 target = baseScale * multiplier;
            transform.localScale = Vector3.Lerp(
                transform.localScale,
                target,
                1f - Mathf.Exp(-12f * Time.unscaledDeltaTime));
        }

        public void SetSelected(bool value)
        {
            selected = value;
            ApplyVisuals();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            hovered = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hovered = false;
            pressed = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            pressed = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            pressed = false;
        }

        private void Choose()
        {
            if (selector != null)
                selector.Select(level);
        }

        private void ApplyVisuals()
        {
            if (surface != null)
                surface.color = selected ? selectedSurface : normalSurface;

            if (border != null)
                border.color = selected ? selectedBorder : normalBorder;

            if (selectedLabel != null)
                selectedLabel.gameObject.SetActive(selected);
        }
    }
}
