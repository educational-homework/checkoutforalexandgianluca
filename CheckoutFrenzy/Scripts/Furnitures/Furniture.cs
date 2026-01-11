using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace CryingSnow.CheckoutFrenzy
{
    [RequireComponent(typeof(BoxCollider))]
    public class Furniture : MonoBehaviour, IInteractable, IPurchasable
    {
        [SerializeField, Tooltip("Unique identifier for this furniture.")]
        private int furnitureId;

        [SerializeField, Tooltip("Name of the furniture.")]
        private new string name;

        [SerializeField, Tooltip("Icon representing the furniture.")]
        private Sprite icon;

        [SerializeField, Tooltip("Price of the furniture in cents.")]
        private int priceInCents;

        [SerializeField, Tooltip("Time in seconds it takes to order this furniture.")]
        private int orderTime = 5;

        [SerializeField, Tooltip("The type of product section (e.g., General, Shelf, Fridge) that this furniture is configured to display.")]
        private Section section;

        [SerializeField, Tooltip("Mesh Renderer of the furniture.")]
        private MeshRenderer mainRenderer;

        [SerializeField, Tooltip("Material used when the furniture is being moved.")]
        private Material movingMaterial;

        public int FurnitureID => furnitureId;

        // IPurchasable Properties
        public string Name => name;
        public Sprite Icon => icon;
        public decimal Price => priceInCents / 100m;
        public int OrderTime => orderTime;
        public Section Section => section;

        public bool IsMoving { get; private set; }

        private enum Direction { North, East, South, West }
        private Direction currentDirection;

        protected PlayerController player;
        private BoxCollider col;
        private Material defaultMaterial;

        private List<Collider> others = new List<Collider>();

        protected virtual void Awake()
        {
            gameObject.layer = GameConfig.Instance.InteractableLayer.ToSingleLayer();
            col = GetComponent<BoxCollider>();
            defaultMaterial = mainRenderer.material;
            currentDirection = GetFacingDirection();
        }

        protected virtual void Start()
        {
            // Subscribe to the OnSave event to save this furniture's data.
            DataManager.Instance.OnSave += HandleOnSave;
        }

        private void HandleOnSave()
        {
            // Create a new FurnitureData object from this furniture's properties.
            var furnitureData = new FurnitureData(this, player != null ? player.transform.position : Vector3.zero);

            // Add the furniture data to the list of saved furniture.
            DataManager.Instance.Data.SavedFurnitures.Add(furnitureData);
        }

        private void OnDestroy()
        {
            if (DataManager.Instance != null)
            {
                DataManager.Instance.OnSave -= HandleOnSave;
            }
        }

        ///<summary>
        /// Adds a Rigidbody component to enable physics interactions for the furniture.
        /// 
        /// This allows the furniture to respond to gravity and collisions naturally.
        ///</summary>
        public void ActivatePhysics()
        {
            gameObject.AddComponent<Rigidbody>();
        }

        public void Interact(PlayerController player)
        {
            this.player = player;

            StartCoroutine(Move());

            UIManager.Instance.InteractMessage.Hide();
        }

        public virtual void OnFocused()
        {
            string message = "Hold to move this furniture around the store";
            UIManager.Instance.InteractMessage.Display(message);
        }

        public virtual void OnDefocused()
        {
            UIManager.Instance.InteractMessage.Hide();
        }

        private IEnumerator Move()
        {
            SetMovingState(true);
            player.StateManager.PushState(PlayerState.Moving);

            // Ensure furniture is oriented correctly on first move to prevent possible disorientation.
            if (currentDirection == Direction.North) transform.DORotate(Vector3.zero, 0.5f);

            while (IsMoving)
            {
                transform.position = player.GetFrontPosition();

                yield return null;
            }
        }

        protected virtual void SetMovingState(bool isMoving)
        {
            IsMoving = isMoving;

            if (isMoving)
            {
                // If starting to move:
                // Add a Rigidbody component if one doesn't already exist.
                Rigidbody body = GetComponent<Rigidbody>();
                if (body == null) body = gameObject.AddComponent<Rigidbody>();

                // Set the Rigidbody to kinematic so it's controlled by transform.
                body.isKinematic = true;

                // Set the collider to trigger mode to allow overlapping with other objects.
                col.isTrigger = true;

                // Switch to the moving material.
                mainRenderer.material = movingMaterial;

                // Enable the Place, Rotate, and Pack buttons in the UI.
                UIManager.Instance.ToggleActionUI(ActionType.Place, true, Place);
                UIManager.Instance.ToggleActionUI(ActionType.Rotate, true, Rotate);
                UIManager.Instance.ToggleActionUI(ActionType.Pack, true, Pack);

                // If this furniture has doors, disable Open and Close buttons in the UI (e.g., Fridge, Freezer)
                UIManager.Instance.ToggleActionUI(ActionType.Open, false, null);
                UIManager.Instance.ToggleActionUI(ActionType.Close, false, null);
            }
            else
            {
                // If stopping movement:
                // Destroy the Rigidbody component if one exists.
                if (TryGetComponent<Rigidbody>(out Rigidbody body)) Destroy(body);

                // Set the collider back to non-trigger mode.
                col.isTrigger = false;

                // Switch back to the default material.
                mainRenderer.material = defaultMaterial;

                // Disable the Place, Rotate, and Pack buttons in the UI.
                UIManager.Instance.ToggleActionUI(ActionType.Place, false, null);
                UIManager.Instance.ToggleActionUI(ActionType.Rotate, false, null);
                UIManager.Instance.ToggleActionUI(ActionType.Pack, false, null);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (ShouldIgnoreTrigger(other)) return;

            others.Add(other); // Add "other" to the list of overlapping objects.
            ToggleColor();
        }

        private void OnTriggerExit(Collider other)
        {
            if (ShouldIgnoreTrigger(other)) return;

            others.Remove(other); // Remove "other" from the list of overlapping objects.
            ToggleColor();
        }

        // Checks if the current trigger event should be ignored.
        private bool ShouldIgnoreTrigger(Collider other)
        {
            // Ignore triggers if not moving or if the object is on the Ground Layer.
            return !IsMoving || GameConfig.Instance.GroundLayer.Contains(other);
        }

        private void ToggleColor()
        {
            var color = new Color(0f, 1f, 0f, 0.5f); // Green
            if (others.Count > 0) color = new Color(1f, 0f, 0f, 0.5f); // Red
            mainRenderer.material.SetColor("_Color", color);
        }

        protected virtual void Place()
        {
            // If there are any overlapping objects, the furniture cannot be placed.
            if (others.Count > 0)
            {
                UIManager.Instance.Message.Log("Can't place furnitures here!", Color.red);
                return;
            }

            SetMovingState(false);
            player.StateManager.PopState();
            player = null;

            // Update the NavMesh surface so Customer AI can pathfind around the placed furniture correctly.
            StoreManager.Instance.UpdateNavMeshSurface();
        }

        private void Rotate()
        {
            // Calculate the next rotation direction.
            currentDirection = (Direction)(((int)currentDirection + 1) % 4); // Cycle through the Direction enum values.

            // Calculate the target rotation.
            Vector3 targetRotation = Vector3.up * (int)currentDirection * 90f; // 90-degree increments.

            // Rotate the furniture using a smooth animation.
            transform.DORotate(targetRotation, 0.5f);
        }

        private void Pack()
        {
            player.StateManager.PopState();

            Transform holdPoint = player.HoldPoint;

            var furnitureBox = Instantiate(
                StoreManager.Instance.FurnitureBoxPrefab,
                holdPoint.position,
                holdPoint.rotation
            );

            furnitureBox.furnitureId = furnitureId;
            furnitureBox.Interact(player);

            UIManager.Instance.ToggleActionUI(ActionType.Place, false, null);
            UIManager.Instance.ToggleActionUI(ActionType.Rotate, false, null);
            UIManager.Instance.ToggleActionUI(ActionType.Pack, false, null);

            DOTween.Kill(transform);

            Destroy(gameObject);
        }

        private Direction GetFacingDirection()
        {
            Vector3 rotation = transform.eulerAngles;

            if (Mathf.Approximately(rotation.y, 0f) || rotation.y < 45f || rotation.y > 315f)
            {
                return Direction.North;
            }
            else if (Mathf.Approximately(rotation.y, 90f) || (rotation.y > 45f && rotation.y < 135f))
            {
                return Direction.East;
            }
            else if (Mathf.Approximately(rotation.y, 180f) || (rotation.y > 135f && rotation.y < 225f))
            {
                return Direction.South;
            }
            else if (Mathf.Approximately(rotation.y, 270f) || (rotation.y > 225f && rotation.y < 315f))
            {
                return Direction.West;
            }

            return Direction.North;
        }
    }

    public enum Section
    {
        General,    // For furniture that doesn't display any products (e.g., Trash Can, Decorations).
        Shelf,      // For generic shelving units.
        Fridge,     // For refrigerated units with doors.
        Freezer,    // For frozen food units with sliding doors.
        FruitRack   // For fruit and vegetable displays.
    }
}
