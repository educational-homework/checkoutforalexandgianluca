using UnityEngine;

namespace CryingSnow.CheckoutFrenzy
{
    public class InteractionBlocker : MonoBehaviour, IInteractable
    {
        private void Awake()
        {
            gameObject.layer = GameConfig.Instance.InteractableLayer.ToSingleLayer();
        }

        /// <summary>
        /// Called when the player interacts with this object.  Does nothing in this case.
        /// </summary>
        /// <param name="player">The PlayerController component of the interacting player.</param>
        public void Interact(PlayerController player) { }

        /// <summary>
        /// Called when the object is no longer focused by the player.  Does nothing in this case.
        /// </summary>
        public void OnDefocused() { }

        /// <summary>
        /// Called when the object is focused by the player. Hides the interact button.
        /// </summary>
        public void OnFocused()
        {
            UIManager.Instance.ToggleInteractButton(false); // Hide the interact button.
        }
    }
}
