using UnityEngine;
using DG.Tweening;

namespace CryingSnow.CheckoutFrenzy
{
    [RequireComponent(typeof(BoxCollider))]
    public class StoreSign : MonoBehaviour, IInteractable
    {
        private bool isOpen;

        private void Start()
        {
            gameObject.layer = GameConfig.Instance.InteractableLayer.ToSingleLayer();
        }

        public void Interact(PlayerController player)
        {
            if (DOTween.IsTweening(transform)) return;

            isOpen = !isOpen;
            StoreManager.Instance.ToggleOpenState(isOpen);

            UpdateSignRotation();
            UIManager.Instance.InteractMessage.Hide();
        }

        public void OnFocused()
        {
            string message = "Tap to close or open the store";
            UIManager.Instance.InteractMessage.Display(message);
        }

        public void OnDefocused()
        {
            UIManager.Instance.InteractMessage.Hide();
        }

        private void UpdateSignRotation()
        {
            float targetAngle = isOpen ? 180f : 0f;
            transform.DOLocalRotate(Vector3.up * targetAngle, 0.5f);
            AudioManager.Instance.PlaySFX(AudioID.Flip);
        }
    }
}
