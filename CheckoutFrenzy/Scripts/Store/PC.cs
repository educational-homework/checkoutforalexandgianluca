using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Cinemachine;
using TMPro;

namespace CryingSnow.CheckoutFrenzy
{
    public class PC : MonoBehaviour, IInteractable
    {
        public static PC Instance { get; private set; }

        [SerializeField, Tooltip("The Cinemachine virtual camera used to display the PC monitor view.")]
        private CinemachineVirtualCamera monitorCamera;

        [SerializeField, Tooltip("The text mesh pro UI element used to display information on the PC monitor.")]
        private TMP_Text monitorText;

        [SerializeField, Tooltip("The duration (in seconds) to simulate the booting of the PC.")]
        private float bootingDuration = 1.5f;

        [SerializeField, Tooltip("The total number of segments used to represent the loading bar on the PC monitor.")]
        private int totalBarSegments = 50;

        public event System.Action<Dictionary<IPurchasable, int>> OnCartChanged;

        private PlayerController player;

        private Dictionary<IPurchasable, int> cart = new Dictionary<IPurchasable, int>();

        private List<IPurchasable> purchaseOrders = new List<IPurchasable>();

        private bool isProcessing;

        private void Awake()
        {
            Instance = this;
            gameObject.layer = GameConfig.Instance.InteractableLayer.ToSingleLayer();

            monitorText.text = "<size=0.5>Standby...";
        }

        private void Start()
        {
            DataManager.Instance.OnSave += () =>
            {
                // Calculate the total price of all pending purchase orders and save it to GameData.
                decimal totalPrice = CalculateOrderPrice(purchaseOrders);
                DataManager.Instance.Data.PendingOrdersValue = totalPrice;
            };
        }

        public void Interact(PlayerController player)
        {
            this.player = player;

            monitorCamera.gameObject.SetActive(true);
            StartCoroutine(BootPC());

            player.StateManager.PushState(PlayerState.Busy);

            UIManager.Instance.InteractMessage.Hide();
        }

        public void OnFocused()
        {
            string message = "Tap to turn on the PC";
            UIManager.Instance.InteractMessage.Display(message);
        }

        public void OnDefocused()
        {
            UIManager.Instance.InteractMessage.Hide();
        }

        private IEnumerator BootPC()
        {
            float elapsedTime = 0f;

            while (elapsedTime < bootingDuration)
            {
                elapsedTime += Time.deltaTime;

                // Calculate the progress (0 to 1).
                float progress = Mathf.Clamp01(elapsedTime / bootingDuration);

                // Update the loading bar.
                int filledSegments = Mathf.RoundToInt(progress * totalBarSegments);
                string loadingBar = new string('|', filledSegments) + new string('.', totalBarSegments - filledSegments);

                // Update the monitor text.
                monitorText.text = $"Booting the PC\nPlease wait...\n<mspace=0.1>[{loadingBar}]</mspace>\n{Mathf.RoundToInt(progress * 100)}%";

                yield return null;
            }

            // Ensure the text shows 100% and a full bar at the end.
            monitorText.text = $"Booting the PC\nPlease wait...\n<mspace=0.1>[{new string('|', totalBarSegments)}]</mspace>\n100%";

            yield return new WaitForSeconds(0.5f);

            UIManager.Instance.PCMonitor.Display(onClose: () =>
            {
                monitorCamera.gameObject.SetActive(false);
                monitorText.text = "<size=0.5>Standby...";

                player.StateManager.PopState();
                player = null;
            });
        }

        /// <summary>
        /// Adds the specified purchasable item to the shopping cart.
        /// </summary>
        /// <param name="purchasable">The item to add to the cart.</param>
        /// <param name="amount">The quantity of the item to add.</param>
        public void AddToCart(IPurchasable purchasable, int amount)
        {
            if (cart.ContainsKey(purchasable)) cart[purchasable] += amount;
            else cart.Add(purchasable, amount);

            OnCartChanged?.Invoke(cart);

            AudioManager.Instance.PlaySFX(AudioID.Click);
        }

        /// <summary>
        /// Removes the specified purchasable item from the shopping cart.
        /// </summary>
        /// <param name="purchasable">The item to remove from the cart.</param>
        public void RemoveFromCart(IPurchasable purchasable)
        {
            cart.Remove(purchasable);
            OnCartChanged?.Invoke(cart);

            AudioManager.Instance.PlaySFX(AudioID.Click);
        }

        /// <summary>
        /// Clears all items from the shopping cart.
        /// </summary>
        public void ClearCart()
        {
            cart.Clear();
            OnCartChanged?.Invoke(cart);

            AudioManager.Instance.PlaySFX(AudioID.Click);
        }

        /// <summary>
        /// Processes the current order in the shopping cart.
        ///
        /// Calculates the total price, checks for sufficient funds, 
        /// creates purchase orders, updates missions, and initiates the order processing.
        /// </summary>
        public void Checkout()
        {
            if (cart.Count == 0)
            {
                UIManager.Instance.Message.Log("Cart is empty. Add products or furnitures first.", Color.red);
                return;
            }

            // Create a list to hold the individual purchase orders.
            List<IPurchasable> newOrders = new List<IPurchasable>();

            // Add each item in the cart to the purchase order list 
            // based on the quantity in the cart.
            foreach (var kvp in cart)
            {
                for (int i = 0; i < kvp.Value; i++)
                {
                    newOrders.Add(kvp.Key);
                }
            }

            // Calculate the total price of all items in the order.
            decimal totalPrice = CalculateOrderPrice(newOrders);

            // Check if the player has enough money to complete the order.
            if (DataManager.Instance.PlayerMoney >= totalPrice)
            {
                // Process each order in the list.
                foreach (var order in newOrders)
                {
                    purchaseOrders.Add(order);

                    if (order is Product product)
                    {
                        // Update the "Restock" mission progress for the product.
                        MissionManager.Instance.UpdateMission(Mission.Goal.Restock, 1, product.ProductID);
                    }
                    else if (order is Furniture furniture)
                    {
                        // Update the "Furnish" mission progress for the furniture.
                        MissionManager.Instance.UpdateMission(Mission.Goal.Furnish, 1, furniture.FurnitureID);
                    }
                }

                ClearCart();

                StartCoroutine(ProcessOrder());

                // Deduct the total price from the player's money.
                DataManager.Instance.PlayerMoney -= totalPrice;

                // Display a success message to the player.
                UIManager.Instance.Message.Log("Checkout successful!");
                AudioManager.Instance.PlaySFX(AudioID.Kaching);
            }
            else
            {
                // Display an error message if the player doesn't have enough money.
                UIManager.Instance.Message.Log("You don't have enough money!", Color.red);
            }
        }

        /// <summary>
        /// Calculates the total price of a list of purchasable items.
        /// </summary>
        /// <param name="orders">The list of purchasable items to calculate the price for.</param>
        /// <returns>The total price of all items in the list.</returns>
        private decimal CalculateOrderPrice(List<IPurchasable> orders)
        {
            decimal totalPrice = 0m;

            foreach (var order in orders)
            {
                if (order is Product product)
                {
                    // Calculate the price of the product based on its box quantity.
                    decimal defaultPrice = product.Price;
                    int boxQuantity = product.GetBoxQuantity();
                    totalPrice += defaultPrice * boxQuantity;
                }
                else
                {
                    // Add the price of the furniture directly.
                    totalPrice += order.Price;
                }
            }

            return totalPrice;
        }

        /// <summary>
        /// Processes the purchase orders in the queue.
        /// 
        /// This method simulates the order delivery process 
        /// by waiting for a specified time for each order.
        /// </summary>
        private IEnumerator ProcessOrder()
        {
            if (isProcessing) yield break;

            isProcessing = true;

            while (purchaseOrders.Count > 0)
            {
                var order = purchaseOrders.FirstOrDefault();
                int time = order.OrderTime;

                // Simulate the order delivery time by waiting for the specified duration.
                while (time > 0)
                {
                    time--;
                    UIManager.Instance.UpdateDeliveryTimer(time);
                    yield return new WaitForSeconds(1f);
                }

                // Deliver the order (instantiate the product or furniture).
                DeliverOrder(order);

                // Remove the processed order from the queue.
                purchaseOrders.Remove(order);
            }

            isProcessing = false;
        }

        /// <summary>
        /// Delivers the specified order to the delivery point.
        /// 
        /// Instantiates the product or furniture at the delivery point.
        /// </summary>
        /// <param name="order">The order to be delivered.</param>
        private void DeliverOrder(IPurchasable order)
        {
            Transform deliveryPoint = StoreManager.Instance.DeliveryPoint;

            if (order is Product product)
            {
                // Instantiate the product's box at the delivery point.
                var box = Instantiate(product.Box, deliveryPoint.position, deliveryPoint.rotation);
                box.name = product.Box.name;
                box.RestoreProducts(product, product.GetBoxQuantity());
            }
            else if (order is Furniture furniture)
            {
                // Instantiate the furniture (in a box) at the delivery point.
                var furnitureBox = Instantiate(
                    StoreManager.Instance.FurnitureBoxPrefab,
                    deliveryPoint.position,
                    deliveryPoint.rotation
                );

                furnitureBox.furnitureId = furniture.FurnitureID;
            }
        }
    }
}
