using UnityEngine;

namespace Picker3D.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    public class UIScreenView : MonoBehaviour
    {
        [SerializeField] private UIScreenId screenId;

        private CanvasGroup canvasGroup;

        public UIScreenId ScreenId => screenId;
        public bool IsVisible { get; private set; }

        public virtual void Show()
        {
            EnsureCanvasGroup();
            gameObject.SetActive(true);
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            IsVisible = true;
            OnShown();
        }

        public virtual void Hide()
        {
            EnsureCanvasGroup();
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            IsVisible = false;
            OnHidden();
            gameObject.SetActive(false);
        }

        protected virtual void OnShown()
        {
        }

        protected virtual void OnHidden()
        {
        }

        private void Awake()
        {
            EnsureCanvasGroup();
        }

        private void EnsureCanvasGroup()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }
        }
    }
}
