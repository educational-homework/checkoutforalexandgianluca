using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using DG.Tweening;

namespace CryingSnow.CheckoutFrenzy
{
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(NavMeshAgent))]
    public class Customer : MonoBehaviour
    {
        [SerializeField] private HandAttachments handAttachments;

        public List<Product> Inventory => inventory;
        public event System.Action OnLeave;

        private Animator animator;
        private NavMeshAgent agent;

        private ShelvingUnit shelvingUnit;
        private List<Product> inventory = new List<Product>();

        private bool isPicking;

        private ChatBubble chatBubble;
        private Dialogue notFoundDialogue => GameConfig.Instance.NotFoundDialogue;
        private Dialogue overpricedDialogue => GameConfig.Instance.OverpricedDialogue;

        private CheckoutCounter checkoutCounter;
        private int queueNumber = int.MaxValue;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            agent = GetComponent<NavMeshAgent>();

            // Initialize NavMeshAgent parameters
            agent.speed = 1.5f;
            agent.angularSpeed = 3600f;
            agent.acceleration = 100f;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
            agent.SetAreaCost(3, 50f);
        }

        private void Start()
        {
            StartCoroutine(CheckEnteringStore());
            StartCoroutine(Shopping());
        }

        private void Update()
        {
            CheckStoreDoors();
        }

        // Detecting store's doors and open them if they are closed.
        private void CheckStoreDoors()
        {
            Ray ray = new Ray(transform.position + Vector3.up, transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, 1f, GameConfig.Instance.InteractableLayer))
            {
                if (hit.transform.TryGetComponent<EntranceDoor>(out EntranceDoor door))
                {
                    door.OpenIfClosed();
                }
            }
        }

        // Check the first time customer entering store, and ring the bell.
        private IEnumerator CheckEnteringStore()
        {
            while (!StoreManager.Instance.IsWithinStore(transform.position))
            {
                yield return new WaitForSeconds(0.1f);
            }

            AudioManager.Instance.PlaySFX(AudioID.Bell);
        }

        private IEnumerator Shopping()
        {
            bool continueShopping = true;

            while (continueShopping)
            {
                yield return FindShelvingUnit();
                yield return PickProduct();

                // 50% chance to continue shopping
                continueShopping = Random.value < 0.5f;
            }

            if (shelvingUnit != null && shelvingUnit.IsOpen)
            {
                // Close the shelving unit if it's open (e.g., Fridges, Freezers)
                shelvingUnit.Close(true, false);
            }

            if (inventory.Count > 0)
            {
                yield return Checkout();
                yield return Leave();
            }
            else
            {
                // Customer leaves without buying anything
                UpdateChatBubble(notFoundDialogue.GetRandomLine());
                yield return Leave();
            }
        }

        private IEnumerator FindShelvingUnit()
        {
            // Get a new shelving unit from the store manager.
            var newShelvingUnit = StoreManager.Instance.GetShelvingUnit();

            // If there's a current shelving unit that's different from the new one and is open, close it.
            if (shelvingUnit != null && shelvingUnit != newShelvingUnit && shelvingUnit.IsOpen)
            {
                shelvingUnit.Close(true, false);
            }

            // Assign the new shelving unit.
            shelvingUnit = newShelvingUnit;

            // If no shelving unit is available, exit the coroutine.
            if (shelvingUnit == null) yield break;

            // Unregister the shelving unit from the store manager so other customers don't target it.
            StoreManager.Instance.UnregisterShelvingUnit(shelvingUnit);

            // Set the agent's destination to the front of the shelving unit.
            agent.SetDestination(shelvingUnit.Front);

            // Wait until the agent has arrived at the shelving unit.
            while (!HasArrived())
            {
                // If the shelving unit is moving, stop the agent and exit the coroutine.
                if (shelvingUnit.IsMoving)
                {
                    agent.SetDestination(transform.position);
                    shelvingUnit = null;
                    yield break;
                }

                yield return null;
            }

            yield return LookAt(shelvingUnit.transform);
        }

        private IEnumerator PickProduct()
        {
            // If no shelving unit is available, exit the coroutine.
            if (shelvingUnit == null) yield break;

            // Get a shelf from the shelving unit.
            var shelf = shelvingUnit.GetShelf();

            // If no shelf is available or the shelving unit is moving, re-register the shelving unit and exit.
            if (shelf == null || shelvingUnit.IsMoving)
            {
                StoreManager.Instance.RegisterShelvingUnit(shelvingUnit);
                yield break;
            }

            var product = shelf.Product;

            if (IsWillingToBuy(product))
            {
                // Add the product to the customer's inventory.
                inventory.Add(product);

                // Take the product model from the shelf.
                var productObj = shelf.TakeProductModel();

                // Open the shelving unit if it's not already open.
                if (!shelf.ShelvingUnit.IsOpen) shelf.ShelvingUnit.Open(true, false);

                // Determine the picking animation trigger based on the shelf height.
                float height = shelf.transform.position.y;
                string pickTrigger = "PickMedium";
                if (height < 0.5f) pickTrigger = "PickLow";
                else if (height > 1.5f) pickTrigger = "PickHigh";

                // Trigger the picking animation.
                animator.SetTrigger(pickTrigger);

                // Wait until the picking animation is complete.
                yield return new WaitUntil(() => isPicking);

                // Get the grip transform for the hand attachment.
                Transform grip = handAttachments.Grip;

                // Set the picked product's parent to the grip.
                productObj.transform.SetParent(grip);

                // Reset the isPicking flag.
                isPicking = false;

                // Animate the product moving to the hand.
                productObj.transform.DOLocalRotate(Vector3.zero, 0.25f);
                productObj.transform.DOLocalMove(Vector3.zero, 0.25f);

                // Wait until the animation is complete (Idle state).
                bool isIdle = false;
                while (!isIdle)
                {
                    isIdle = animator.GetCurrentAnimatorStateInfo(0).IsName("Idle");
                    yield return null;
                }

                // Destroy the temporary product object.
                Destroy(productObj);

                // Wait for a short delay.
                yield return new WaitForSeconds(0.5f);
            }
            else
            {
                // If not willing to buy, display the "overpriced" dialogue.
                string chat = overpricedDialogue.GetRandomLine();
                chat = chat.Replace("{product}", product.Name);
                UpdateChatBubble(chat);
            }

            // Re-register the shelving unit with the store manager.
            StoreManager.Instance.RegisterShelvingUnit(shelvingUnit);
        }

        private bool IsWillingToBuy(Product product)
        {
            // Calculate a price tolerance factor based on random value.
            // Higher values mean more tolerance.
            float priceToleranceFactor = 1f + Mathf.Pow(Random.value, 2f);

            // Calculate the maximum acceptable price based on the product's market price and tolerance.
            decimal maxAcceptablePrice = product.MarketPrice * (decimal)priceToleranceFactor;

            // Get the custom price for the product.
            decimal customPrice = DataManager.Instance.GetCustomProductPrice(product);

            // Return true if the custom price is within the acceptable price range, otherwise false.
            return customPrice <= maxAcceptablePrice;
        }

        private IEnumerator UpdateQueue()
        {
            // While the customer's queue number is greater than 0 (meaning they are still in the queue).
            while (queueNumber > 0)
            {
                int newQueueNumber = checkoutCounter.GetQueueNumber(this);

                // Check if the customer's queue number has improved (become lower).
                if (newQueueNumber < queueNumber)
                {
                    // Update the customer's queue number.
                    queueNumber = newQueueNumber;

                    Vector3 queuePosition = checkoutCounter.GetQueuePosition(this, out Vector3 lookDirection);

                    // Move the customer to their new queue position.
                    yield return MoveTo(queuePosition);

                    // Make the customer look in the correct direction at their new position.
                    yield return LookAt(lookDirection);
                }
                else
                {
                    // If the queue number hasn't improved, wait briefly before checking again.
                    yield return new WaitForSeconds(0.1f);
                }
            }
            // When the queueNumber is 0, this coroutine will stop.
        }

        private IEnumerator Checkout()
        {
            checkoutCounter = StoreManager.Instance.GetShortestQueueCounter();
            checkoutCounter.LiningCustomers.Add(this);
            yield return UpdateQueue();
            yield return checkoutCounter.PlaceProducts(this);
            yield return new WaitUntil(() => checkoutCounter.CurrentState == CheckoutCounter.State.Standby);
        }

        private IEnumerator Leave()
        {
            if (checkoutCounter != null)
            {
                checkoutCounter.LiningCustomers.Remove(this);
            }

            OnLeave?.Invoke();

            var exitPoint = StoreManager.Instance.GetExitPoint();
            yield return MoveTo(exitPoint);

            yield return new WaitForEndOfFrame();
            Destroy(gameObject);
        }

        public IEnumerator HandsPayment(bool isUsingCash, Cashier cashier)
        {
            bool isPaying = true;

            animator.SetBool("IsPaying", isPaying);

            handAttachments.ActivatePaymentObject(isUsingCash);

            Camera mainCamera = Camera.main;

            // Continue the payment process until isPaying is false.
            while (isPaying)
            {
                // If a cashier is available, simulate payment with the cashier (auto-scan).
                if (cashier != null)
                {
                    yield return new WaitForSeconds(0.3f);
                    cashier.TakePayment();
                    yield return new WaitForSeconds(0.7f);
                    isPaying = false;
                }
                // Otherwise, allow the player to manually process the payment (e.g., started by clicking on a payment object).
                else if (Input.GetMouseButtonDown(0))
                {
                    Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

                    // Check if the raycast hits a payment object within the specified layer and range.
                    if (Physics.Raycast(ray, 10f, GameConfig.Instance.PaymentLayer))
                    {
                        isPaying = false;
                    }
                }

                yield return null;
            }

            animator.SetBool("IsPaying", isPaying);

            handAttachments.DeactivatePaymentObjects();
        }

        private IEnumerator MoveTo(Vector3 position)
        {
            agent.SetDestination(position);

            yield return new WaitUntil(() => HasArrived());

            // Wait for the end of the frame.
            // This can be useful for ensuring animations or other visual updates have taken place.
            yield return new WaitForEndOfFrame();
        }

        public void AskToLeave()
        {
            // If the customer has items in their inventory, they shouldn't leave yet.
            if (inventory.Count > 0) return;

            // Stop all coroutines related to the customer's current activity.
            StopAllCoroutines();

            // If the customer was interacting with a shelving unit, re-register it with the store manager.
            if (shelvingUnit != null) StoreManager.Instance.RegisterShelvingUnit(shelvingUnit);

            // Start the "Leave" coroutine to handle the customer leaving the store.
            StartCoroutine(Leave());
        }

        private IEnumerator LookAt(Transform target)
        {
            var lookDirection = (target.position - transform.position).Flatten();
            var lookRotation = Quaternion.LookRotation(lookDirection);
            yield return transform.DORotateQuaternion(lookRotation, 0.5f).WaitForCompletion();
        }

        private IEnumerator LookAt(Vector3 lookDirection)
        {
            var lookRotation = Quaternion.LookRotation(lookDirection.Flatten());
            yield return transform.DORotateQuaternion(lookRotation, 0.5f).WaitForCompletion();
        }

        private bool HasArrived()
        {
            if (!agent.pathPending)
            {
                if (agent.remainingDistance <= agent.stoppingDistance)
                {
                    if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
                    {
                        animator.SetBool("IsMoving", false);
                        return true;
                    }
                }
            }

            animator.SetBool("IsMoving", true);
            return false;
        }

        private void UpdateChatBubble(string chat)
        {
            if (chatBubble != null) return;
            chatBubble = UIManager.Instance.ShowChatBubble(chat, transform);
        }

        public void OnPick(AnimationEvent animationEvent)
        {
            isPicking = true;
        }
    }
}
