using UnityEngine;
using UnityEngine.EventSystems;

namespace NeuroPilotXR.Navigation
{
    public sealed class UIInteractionFeedback : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private float hoverScale = 1.018f;
        [SerializeField] private float pressedScale = 0.975f;
        [SerializeField] private float response = 14f;

        private Vector3 baseScale;
        private bool hovered;
        private bool pressed;

        private void Awake()
        {
            baseScale = transform.localScale;
        }

        private void Update()
        {
            float multiplier = pressed ? pressedScale : hovered ? hoverScale : 1f;
            transform.localScale = Vector3.Lerp(
                transform.localScale,
                baseScale * multiplier,
                1f - Mathf.Exp(-response * Time.unscaledDeltaTime));
        }

        public void OnPointerEnter(PointerEventData eventData) => hovered = true;

        public void OnPointerExit(PointerEventData eventData)
        {
            hovered = false;
            pressed = false;
        }

        public void OnPointerDown(PointerEventData eventData) => pressed = true;
        public void OnPointerUp(PointerEventData eventData) => pressed = false;
    }
}