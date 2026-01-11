using UnityEngine;
using Unity.AI.Navigation;
using DG.Tweening;

namespace CryingSnow.CheckoutFrenzy
{
    [RequireComponent(typeof(BoxCollider))]
    public class EntranceDoor : MonoBehaviour, IInteractable
    {
        [SerializeField, Tooltip("The rotation applied to the door when it's opened")]
        private Vector3 openAngle;

        [SerializeField, Tooltip("A reference to the other door if this is a double door")]
        private EntranceDoor pairDoor;

        private BoxCollider boxCollider;
        private bool isOpen;

        private void Awake()
        {
            // Set the layer of the door object to the "Interactable" layer.
            gameObject.layer = GameConfig.Instance.InteractableLayer.ToSingleLayer();

            // Add a NavMeshModifier component and disable it to prevent the door from affecting navigation.
            var navMeshMod = gameObject.AddComponent<NavMeshModifier>();
            navMeshMod.ignoreFromBuild = true;

            // Cache a reference to the door's box collider component.
            boxCollider = GetComponent<BoxCollider>();
        }

        public void Interact(PlayerController player)
        {
            // Toggle the door state (open or close) based on its current state.
            if (isOpen)
            {
                Close();
                pairDoor.Close();

                AudioManager.Instance.PlaySFX(AudioID.EntranceClose);
            }
            else
            {
                Open();
                pairDoor.Open();

                AudioManager.Instance.PlaySFX(AudioID.EntranceOpen);
            }

            // Hide the interaction message UI element.
            UIManager.Instance.InteractMessage.Hide();
        }

        public void OnFocused()
        {
            // Construct a message indicating whether to "open" or "close" the door.
            string action = isOpen ? "close" : "open";
            string message = $"Tap to {action} the entrance doors.";

            UIManager.Instance.InteractMessage.Display(message);
        }

        public void OnDefocused()
        {
            UIManager.Instance.InteractMessage.Hide();
        }

        /// <summary>
        /// Opens the door with a smooth animation and updates the door's state.
        /// Temporarily disables the door's collider during the opening animation.
        /// </summary>
        public void Open()
        {
            // Disable the box collider to allow player interaction through the open door.
            boxCollider.enabled = false;

            // Rotate the door to the open angle with a smooth animation.
            transform.DOLocalRotate(openAngle, 0.5f).OnComplete(() =>
            {
                // Update the isOpen flag and re-enable the collider after the animation completes.
                isOpen = true;
                boxCollider.enabled = true;
            });
        }

        /// <summary>
        /// Closes the door with a smooth animation and updates the door's state.
        /// Temporarily disables the door's collider during the closing animation.
        /// </summary>
        public void Close()
        {
            // Disable the box collider to allow player interaction through the closing door.
            boxCollider.enabled = false;

            // Rotate the door back to its closed position with a smooth animation.
            transform.DOLocalRotate(Vector3.zero, 0.5f).OnComplete(() =>
            {
                // Update the isOpen flag and re-enable the collider after the animation completes.
                isOpen = false;
                boxCollider.enabled = true;
            });
        }

        /// <summary>
        /// Opens the door and its paired door if they are currently closed.
        /// </summary>
        public void OpenIfClosed()
        {
            // If the door is currently closed, open it and its paired door (if applicable).
            if (!isOpen)
            {
                Open();
                pairDoor.Open();
            }
        }
    }
}
