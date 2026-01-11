using System.Collections;
using System.Linq;
using UnityEngine;
using Unity.AI.Navigation;
using DG.Tweening;

namespace CryingSnow.CheckoutFrenzy
{
    [RequireComponent(typeof(Rigidbody))]
    public class Box : ProductContainer, IInteractable
    {
        [Header("Box Lids")]
        [SerializeField, Tooltip("Reference to the bone transform of the front lid of the box.")]
        private Transform lidFront;

        [SerializeField, Tooltip("Reference to the bone transform of the back lid of the box.")]
        private Transform lidBack;

        [SerializeField, Tooltip("Reference to the bone transform of the left lid of the box.")]
        private Transform lidLeft;

        [SerializeField, Tooltip("Reference to the bone transform of the right lid of the box.")]
        private Transform lidRight;

        [Header("Sound Settings")]
        [SerializeField, Tooltip("Duration (in seconds) to check for collisions after throwing the box.")]
        private float collisionCheckDuration = 3f;

        /// <summary>
        /// Gets the size of the box, with each dimension floored to the nearest tenth.
        /// The base size is derived from the box collider. This flooring is crucial for accurate inner dimension
        /// calculations, preventing issues that could arise from slight inaccuracies in the collider's reported size.
        /// </summary>
        public override Vector3 Size => base.Size.FloorToTenth();

        public float Height => boxCollider.size.y;

        public bool IsStored { get; set; }
        public bool IsOpen { get; private set; }
        public bool IsDisposable { get; private set; }
        public bool IsCheckingCollision { get; private set; }

        private Message message => UIManager.Instance.Message;

        private Rigidbody body;
        private PlayerController player;

        private Sequence lidSequence;

        private Coroutine disablePhysicsRoutine;

        private void Awake()
        {
            gameObject.layer = GameConfig.Instance.InteractableLayer.ToSingleLayer();

            body = GetComponent<Rigidbody>();
            SetActivePhysics(false);

            // Prevents the box from affecting the navigation mesh
            var navMeshMod = gameObject.AddComponent<NavMeshModifier>();
            navMeshMod.ignoreFromBuild = true;
        }

        private IEnumerator Start()
        {
            DataManager.Instance.OnSave += HandleOnSave;

            yield return new WaitUntil(() => DataManager.Instance.IsLoaded);

            if (!IsStored) SetActivePhysics(true);
        }

        private void OnDestroy()
        {
            if (DataManager.Instance != null)
            {
                DataManager.Instance.OnSave -= HandleOnSave;
            }
        }

        private void HandleOnSave()
        {
            if (IsStored) return;

            var boxData = new BoxData(this);
            DataManager.Instance.Data.SavedBoxes.Add(boxData);
        }

        private void OnCollisionEnter(Collision collision)
        {
            // Ignore collision events if the box is not currently moving.
            if (!IsCheckingCollision) return;

            // Check if the collision impact is significant.
            if (collision.relativeVelocity.magnitude > 2)
            {
                // Play an impact sound effect.
                AudioManager.Instance.PlaySFX(AudioID.Impact);
            }
        }

        /// <summary>
        /// Handles the interaction with the box when the player taps interact button. 
        /// This includes picking up the box, attaching it to the player's hand, 
        /// updating the player's state, and enabling relevant UI elements.
        /// </summary>
        /// <param name="player">The player who is interacting with the box.</param>
        public void Interact(PlayerController player)
        {
            // Store a reference to the interacting player.
            this.player = player;

            // Prevent the box from being disposed of (e.g., thrown to trash can) while it's being held by the player.
            IsDisposable = false;

            if (disablePhysicsRoutine != null)
            {
                StopCoroutine(disablePhysicsRoutine);
            }

            disablePhysicsRoutine = StartCoroutine(DisablePhysicsDelayed());

            // Change the layer of all child objects to the "HeldObject" layer.
            // Making them rendered on top of everything else (except UI).
            foreach (Transform child in transform)
            {
                child.gameObject.layer = GameConfig.Instance.HeldObjectLayer.ToSingleLayer();
            }

            UIManager.Instance.ToggleActionUI(ActionType.Throw, true, Throw);

            // Enable the appropriate button for opening/closing the box.
            if (IsOpen) UIManager.Instance.ToggleActionUI(ActionType.Close, true, Close);
            else UIManager.Instance.ToggleActionUI(ActionType.Open, true, Open);

            UIManager.Instance.HideBoxInfo();

            // Move the box to the player's hand.
            transform.SetParent(player.HoldPoint);
            transform.DOLocalMove(Vector3.zero, 0.5f).SetEase(Ease.OutQuint);
            transform.DOLocalRotate(Vector3.zero, 0.5f).SetEase(Ease.OutQuint);

            AudioManager.Instance.PlaySFX(AudioID.Pick);

            player.StateManager.PushState(PlayerState.Holding);

            UIManager.Instance.InteractMessage.Hide();
        }

        public void OnFocused()
        {
            UIManager.Instance.DisplayBoxInfo(this);

            string message = "Tap to pick up this box";
            UIManager.Instance.InteractMessage.Display(message);
        }

        public void OnDefocused()
        {
            UIManager.Instance.HideBoxInfo();
            UIManager.Instance.InteractMessage.Hide();
        }

        private void Throw()
        {
            // Check for collisions within the box's bounds. 
            // If there's an overlap, prevent the throw.
            var center = transform.position;
            var extents = boxCollider.size / 2f;
            var orientation = transform.rotation;
            var layerMask = ~GameConfig.Instance.PlayerLayer; // Create a layer mask that excludes the "Player" layer. 

            if (Physics.OverlapBox(center, extents, orientation, layerMask).Length > 0)
            {
                message.Log("Can't throw object here!", Color.red);
                return;
            }

            if (disablePhysicsRoutine != null)
            {
                StopCoroutine(disablePhysicsRoutine);
                disablePhysicsRoutine = null;
            }

            DOTween.Kill(transform);

            // Detach the box from the player's hand.
            transform.SetParent(null);

            // Enable physics for the box and apply an impulse force.
            SetActivePhysics(true);
            body.AddForce(transform.forward * 3.5f, ForceMode.Impulse);

            StartCoroutine(StartCollisionCheck());

            AudioManager.Instance.PlaySFX(AudioID.Throw);

            // Change the layer of all child objects back to the default layer.
            foreach (Transform child in transform)
            {
                child.gameObject.layer = LayerMask.NameToLayer("Default");
            }

            // Disable UI elements related to holding and interacting with the box.
            UIManager.Instance.ToggleActionUI(ActionType.Throw, false, null);
            UIManager.Instance.ToggleActionUI(ActionType.Open, false, null);
            UIManager.Instance.ToggleActionUI(ActionType.Close, false, null);
            UIManager.Instance.ToggleActionUI(ActionType.Place, false, null);
            UIManager.Instance.ToggleActionUI(ActionType.Take, false, null);

            player.StateManager.PopState();

            player = null;

            IsDisposable = true;
        }

        /// <summary>
        /// Disables physics for the box with a slight delay. 
        /// This prevents issues where the box's position is not 
        /// fully updated by the physics engine after being moved 
        /// using `Transform` (e.g., when picking up stacked boxes).
        /// </summary>
        private IEnumerator DisablePhysicsDelayed()
        {
            yield return new WaitForSeconds(0.2f);

            SetActivePhysics(false);
        }

        public void SetActivePhysics(bool value)
        {
            body.isKinematic = !value;
            boxCollider.enabled = value;
        }

        /// <summary>
        /// Starts a timer to check for collisions after the box is thrown.
        /// Collisions are only checked within the specified `collisionCheckDuration`.
        /// </summary>
        private IEnumerator StartCollisionCheck()
        {
            float timer = collisionCheckDuration;
            IsCheckingCollision = true;

            while (timer > 0f)
            {
                timer -= Time.deltaTime;
                yield return null;
            }

            IsCheckingCollision = false;
        }

        /// <summary>
        /// Opens the box lids with a smooth animation.
        /// Sets the IsOpen flag to true and enables the "Close" button.
        /// </summary>
        private void Open()
        {
            if (lidSequence.IsActive()) return;

            IsOpen = true;
            UIManager.Instance.ToggleActionUI(ActionType.Close, true, Close);
            UIManager.Instance.ToggleActionUI(ActionType.Open, false, null);

            lidSequence = DOTween.Sequence();

            lidSequence.Append(lidFront.DOLocalRotate(Vector3.right * 250f, 0.3f, RotateMode.LocalAxisAdd))
                .Join(lidBack.DOLocalRotate(Vector3.left * 250f, 0.3f, RotateMode.LocalAxisAdd))
                .InsertCallback(0f, () => AudioManager.Instance.PlaySFX(AudioID.Flip))
                .Append(lidLeft.DOLocalRotate(Vector3.back * 250f, 0.3f, RotateMode.LocalAxisAdd))
                .Join(lidRight.DOLocalRotate(Vector3.forward * 250f, 0.3f, RotateMode.LocalAxisAdd))
                .InsertCallback(0.3f, () => AudioManager.Instance.PlaySFX(AudioID.Flip));
        }

        /// <summary>
        /// Closes the box lids with a smooth animation.
        /// Sets the IsOpen flag to false and enables the "Open" button.
        /// </summary>
        private void Close()
        {
            if (lidSequence.IsActive()) return;

            IsOpen = false;
            UIManager.Instance.ToggleActionUI(ActionType.Open, true, Open);
            UIManager.Instance.ToggleActionUI(ActionType.Close, false, null);

            lidSequence = DOTween.Sequence();

            lidSequence.Append(lidLeft.DOLocalRotate(Vector3.forward * 250f, 0.3f, RotateMode.LocalAxisAdd))
                .Join(lidRight.DOLocalRotate(Vector3.back * 250f, 0.3f, RotateMode.LocalAxisAdd))
                .InsertCallback(0f, () => AudioManager.Instance.PlaySFX(AudioID.Flip))
                .Append(lidFront.DOLocalRotate(Vector3.left * 250f, 0.3f, RotateMode.LocalAxisAdd))
                .Join(lidBack.DOLocalRotate(Vector3.right * 250f, 0.3f, RotateMode.LocalAxisAdd))
                .InsertCallback(0.3f, () => AudioManager.Instance.PlaySFX(AudioID.Flip));
        }

        /// <summary>
        /// Opens the box lids immediately without animation. 
        /// Primarily used for initialization purposes.
        /// </summary>
        public void SetLidsOpen()
        {
            lidFront.localRotation = Quaternion.Euler(Vector3.right * 160f);
            lidBack.localRotation = Quaternion.Euler(Vector3.left * 160f);
            lidLeft.localRotation = Quaternion.Euler(Vector3.back * 160f);
            lidRight.localRotation = Quaternion.Euler(Vector3.forward * 160f);

            IsOpen = true;
        }

        public IEnumerator OpenLidsSmooth()
        {
            float duration = 0.3f;

            lidFront.DOLocalRotate(Vector3.right * 250f, duration, RotateMode.LocalAxisAdd);
            lidBack.DOLocalRotate(Vector3.left * 250f, duration, RotateMode.LocalAxisAdd);

            yield return new WaitForSeconds(duration);

            lidLeft.DOLocalRotate(Vector3.back * 250f, duration, RotateMode.LocalAxisAdd);
            lidRight.DOLocalRotate(Vector3.forward * 250f, duration, RotateMode.LocalAxisAdd);

            yield return new WaitForSeconds(duration);

            IsOpen = true;
        }

        public void CloseIfOpened()
        {
            if (!IsOpen) return;

            var lidSequence = DOTween.Sequence();

            lidSequence.Append(lidLeft.DOLocalRotate(Vector3.forward * 250f, 0.3f, RotateMode.LocalAxisAdd))
                .Join(lidRight.DOLocalRotate(Vector3.back * 250f, 0.3f, RotateMode.LocalAxisAdd))
                .Append(lidFront.DOLocalRotate(Vector3.left * 250f, 0.3f, RotateMode.LocalAxisAdd))
                .Join(lidBack.DOLocalRotate(Vector3.right * 250f, 0.3f, RotateMode.LocalAxisAdd));

            IsOpen = false;
        }

        /// <summary>
        /// Places the last product from the box onto the specified shelf.
        /// Performs necessary checks for compatibility (product type, shelf space) 
        /// and updates the UI accordingly.
        /// </summary>
        /// <param name="shelf">The target shelf to place the product on.</param>
        /// <returns>True if the product was placed successfully, false otherwise.</returns>        
        public bool Place(Shelf shelf)
        {
            if (shelf.AssignedProduct != null && Product != shelf.AssignedProduct)
            {
                message.Log("This shelf is assigned to a different product.");
                return false;
            }
            else if (shelf.ShelvingUnit.Section != Product.Section)
            {
                message.Log("Product doesn't belong in this section.");
                return false;
            }
            else if (shelf.Product == null)
            {
                shelf.Initialize(Product);
            }
            else if (shelf.Product != Product)
            {
                message.Log("Shelf contains a different product.");
                return false;
            }

            var productModel = productModels.LastOrDefault();
            int prevShelfQty = shelf.Quantity;

            if (shelf.PlaceProductModel(productModel, out Vector3 position))
            {
                productModel.transform.SetParent(shelf.transform);
                DOTween.Kill(productModel.transform);
                productModel.transform.DOLocalJump(position, 0.5f, 1, 0.5f);
                productModel.transform.DOLocalRotate(Vector3.zero, 0.5f);

                AudioManager.Instance.PlaySFX(AudioID.Draw);

                productModel.layer = LayerMask.NameToLayer("Default");

                productModels.Remove(productModel);

                if (Quantity == 0)
                {
                    Product = null;
                    UIManager.Instance.ToggleActionUI(ActionType.Place, false, null);
                }

                if (prevShelfQty == 0)
                    UIManager.Instance.ToggleActionUI(ActionType.Take, true, () => Take(shelf));

                return true;
            }

            return false;
        }

        /// <summary>
        /// Takes a product from the specified shelf and adds it to the box.
        /// Performs necessary checks for compatibility (box capacity, product type, box size).
        /// Handles UI updates to reflect changes in the box and shelf states.
        /// </summary>
        /// <param name="shelf">The shelf to take a product from.</param>
        /// <returns>True if a product was successfully taken from the shelf, false otherwise.</returns>
        public bool Take(Shelf shelf)
        {
            // Check if the box is full
            if (Product != null && Quantity >= Capacity)
            {
                message.Log("Box is full.");
                return false;
            }

            // If the box is not empty, check if the product types match
            if (Quantity > 0 && shelf.Product != Product)
            {
                message.Log("Box contains a different product.");
                return false;
            }

            // If the box is empty, check if the product's box size is compatible
            if (Quantity == 0 && Size != shelf.Product.Box.Size)
            {
                message.Log("Incompatible box size.");
                return false;
            }

            // Initialize the box if empty and compatible
            if (Quantity == 0)
            {
                Initialize(shelf.Product);
            }

            // Take the product from the shelf and add it to the box
            int prevQuantity = Quantity;
            var position = productPositions[prevQuantity];

            var productModel = shelf.TakeProductModel();
            productModels.Add(productModel);

            productModel.layer = GameConfig.Instance.HeldObjectLayer.ToSingleLayer();

            productModel.transform.SetParent(transform);
            DOTween.Kill(productModel.transform);
            productModel.transform.DOLocalJump(position, 0.5f, 1, 0.5f);
            productModel.transform.DOLocalRotate(Vector3.zero, 0.5f);

            AudioManager.Instance.PlaySFX(AudioID.Draw);

            // If this was the first product added, enable the Place button
            if (prevQuantity == 0)
            {
                UIManager.Instance.ToggleActionUI(ActionType.Place, true, () => Place(shelf));
            }

            if (shelf.Quantity == 0)
            {
                UIManager.Instance.ToggleActionUI(ActionType.Take, false, null);
            }

            return true;
        }

        public bool Store(Rack rack, bool isPlayer)
        {
            if (rack.Product == null)
            {
                rack.Initialize(Product);
            }
            else if (rack.Product != Product)
            {
                if (isPlayer) message.Log("Rack contains a different product.");
                return false;
            }

            if (rack.CanStoreBox(this, out Vector3 position, isPlayer))
            {
                IsStored = true;
                IsDisposable = false;

                transform.SetParent(rack.transform);
                DOTween.Kill(transform);
                transform.DOLocalJump(position, 0.5f, 1, 0.5f);
                transform.DOLocalRotate(Vector3.zero, 0.5f);

                // Change the layer of all products in the box back to the default layer.
                foreach (Transform child in transform)
                {
                    child.gameObject.layer = LayerMask.NameToLayer("Default");
                }

                if (isPlayer)
                {
                    if (IsOpen) Close();

                    // Disable UI elements related to holding and interacting with the box.
                    UIManager.Instance.ToggleActionUI(ActionType.Throw, false, null);
                    UIManager.Instance.ToggleActionUI(ActionType.Open, false, null);
                    UIManager.Instance.ToggleActionUI(ActionType.Close, false, null);
                    UIManager.Instance.ToggleActionUI(ActionType.Place, false, null);

                    AudioManager.Instance.PlaySFX(AudioID.Throw);

                    player.StateManager.PopState();

                    player = null;
                }

                return true;
            }

            return false;
        }

        public void Stock(Shelf shelfToStock)
        {
            if (shelfToStock.Product == null)
            {
                shelfToStock.Initialize(shelfToStock.AssignedProduct);
            }

            var productModel = productModels.LastOrDefault();

            if (shelfToStock.PlaceProductModel(productModel, out Vector3 position))
            {
                productModel.layer = LayerMask.NameToLayer("Default");

                productModel.transform.SetParent(shelfToStock.transform);
                DOTween.Kill(productModel.transform);
                productModel.transform.DOLocalJump(position, 0.5f, 1, 0.5f);
                productModel.transform.DOLocalRotate(Vector3.zero, 0.5f);

                productModels.Remove(productModel);

                if (Quantity == 0)
                {
                    Product = null;
                }
            }
        }
    }
}
