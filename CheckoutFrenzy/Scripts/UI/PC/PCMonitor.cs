using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

namespace CryingSnow.CheckoutFrenzy
{
    public class PCMonitor : MonoBehaviour
    {
        [Header("Market Screen")]
        [SerializeField, Tooltip("TextMeshPro text displaying the cart label and item count.")]
        private TMP_Text cartTabLabel;

        [Header("Product Screen")]
        [SerializeField, Tooltip("Transform of the parent object where product listings are instantiated.")]
        private Transform productListingParent;

        [SerializeField, Tooltip("Prefab of the ProductListing component to instantiate for each product.")]
        private ProductListing productListingPrefab;

        [SerializeField, Tooltip("Dropdown used to filter the displayed products by category.")]
        private TMP_Dropdown categoryDropdown;

        [Header("Furniture Screen")]
        [SerializeField, Tooltip("Transform of the parent object where furniture listings are instantiated.")]
        private Transform furnitureListingParent;

        [SerializeField, Tooltip("Prefab of the FurnitureListing component to instantiate for each furniture item.")]
        private FurnitureListing furnitureListingPrefab;

        [SerializeField, Tooltip("Dropdown used to filter the displayed furniture by store section.")]
        private TMP_Dropdown sectionDropdown;

        [Header("Cart Screen")]
        [SerializeField, Tooltip("Transform of the parent object where cart items are instantiated.")]
        private Transform cartItemsParent;

        [SerializeField, Tooltip("Prefab of the CartItem component to instantiate for each item in the cart.")]
        private CartItem cartItemPrefab;

        [SerializeField, Tooltip("TextMeshPro text displaying the total price of items in the cart.")]
        private TMP_Text totalPriceText;

        [SerializeField, Tooltip("Button to clear the shopping cart.")]
        private Button clearCartButton;

        [SerializeField, Tooltip("Button to proceed to checkout.")]
        private Button checkoutButton;

        [Header("License Screen")]
        [SerializeField, Tooltip("Transform of the parent object where license listings are instantiated.")]
        private Transform licenseListingParent;

        [SerializeField, Tooltip("Prefab of the LicenseListing component to instantiate for each license.")]
        private LicenseListing licenseListingPrefab;

        [Header("Expansion Screen")]
        [SerializeField, Tooltip("Transform of the parent object where expansion listings are instantiated.")]
        private Transform expansionListingParent;

        [SerializeField, Tooltip("Prefab of the ExpansionListing component to instantiate for each expansion.")]
        private ExpansionListing expansionListingPrefab;

        [Header("Employee Screen")]
        [SerializeField, Tooltip("Transform of the parent object where employee listings are instantiated.")]
        private Transform employeeListingParent;

        [SerializeField, Tooltip("Prefab of the EmployeeListing component to instantiate for each employee.")]
        private EmployeeListing employeeListingPrefab;

        [Header("Bill Screen")]
        [SerializeField, Tooltip("Transform of the parent object where Bill UIs are instantiated.")]
        private Transform billUIParent;

        [SerializeField, Tooltip("Prefab of the BillUI component to instantiate for each bill.")]
        private BillUI billUIPrefab;

        [SerializeField, Tooltip("TextMeshPro text displaying the outstanding bills.")]
        private TMP_Text outstandingBillsText;

        [SerializeField, Tooltip("Button to pay all outstanding bills.")]
        private Button payAllBillsButton;

        [Header("Loan Screen")]
        [SerializeField, Tooltip("Transform of the parent object where LoanListings are instantiated.")]
        private Transform loanListingParent;

        [SerializeField, Tooltip("Prefab of the LoanListing component to instantiate for each LoanTemplate.")]
        private LoanListing loanListingPrefab;

        [SerializeField, Tooltip("TextMeshPro component to display currently active loans (if any).")]
        private TextMeshProUGUI loanInfoText;

        private List<ProductListing> productListings = new List<ProductListing>();
        private List<FurnitureListing> furnitureListings = new List<FurnitureListing>();
        private List<BillUI> billUIs = new List<BillUI>();

        private void Start()
        {
            StoreManager.Instance.OnLicensePurchased += UpdateProductListing;

            FinanceManager.Instance.OnBillCreated += CreateBillUI;
            FinanceManager.Instance.OnBillsUpdated += UpdateBillUIs;
            FinanceManager.Instance.OnBillPaid += UpdateBillSummaryUI;
            FinanceManager.Instance.OnLoanTaken += RefreshLoanInfoUI;

            // Initialize Product Screen
            foreach (var product in DataManager.Instance.ProductDB)
            {
                if (product.HasLicense)
                {
                    CreateProductListing(product);
                }
            }

            InitializeDropdown<Product.Category>(categoryDropdown, "All Categories", OnCategoryChanged);

            // Initialize Furniture Screen
            foreach (var furniture in DataManager.Instance.FurnitureDB)
            {
                var furnitureListing = Instantiate(furnitureListingPrefab, furnitureListingParent);
                furnitureListing.Initialize(furniture);
                furnitureListings.Add(furnitureListing);
            }

            InitializeDropdown<Section>(sectionDropdown, "All Sections", OnSectionChanged);

            // Initialize Cart Screen
            clearCartButton.onClick.AddListener(() => PC.Instance.ClearCart());
            checkoutButton.onClick.AddListener(() => PC.Instance.Checkout());
            PC.Instance.OnCartChanged += HandleCartChanged;
            totalPriceText.text = "Total: $0.00";

            // Initialize License Screen
            foreach (var license in DataManager.Instance.LicenseDB)
            {
                var licenseListing = Instantiate(licenseListingPrefab, licenseListingParent);
                licenseListing.Initialize(license);
            }

            // Initialize Expansion Screen
            foreach (var expansion in StoreManager.Instance.Expansions)
            {
                var expansionListing = Instantiate(expansionListingPrefab, expansionListingParent);
                expansionListing.Initialize(expansion);
            }

            // Initialize Employee Screen
            foreach (var kvp in EmployeeManager.Instance.EmployeeLookup)
            {
                var type = kvp.Key;
                var employees = kvp.Value;

                for (int i = 0; i < employees.Count; i++)
                {
                    var employeeListing = Instantiate(employeeListingPrefab, employeeListingParent);

                    var employeeData = new EmployeeData
                    {
                        Type = type,
                        PointIndex = i
                    };

                    employeeListing.Initialize(employeeData);
                }
            }

            // Initialize Bill Screen
            foreach (var bill in DataManager.Instance.Data.Bills)
            {
                CreateBillUI(bill);
            }

            payAllBillsButton.onClick.AddListener(() =>
            {
                if (FinanceManager.Instance.PayAllBills())
                {
                    AudioManager.Instance.PlaySFX(AudioID.Kaching);
                    UpdateBillSummaryUI();
                }
            });

            UpdateBillSummaryUI();

            // Initialize Loan Screen
            foreach (var loanTemplate in DataManager.Instance.LoanTemplateDB)
            {
                var loanListing = Instantiate(loanListingPrefab, loanListingParent);
                loanListing.Initialize(loanTemplate);
            }

            RefreshLoanInfoUI();

            // Initialize All Screens
            foreach (Transform screen in transform)
            {
                var screenRect = screen.GetComponent<RectTransform>();
                screenRect.anchoredPosition = Vector2.zero;
                screen.gameObject.SetActive(screen.GetSiblingIndex() <= 0);
            }

            gameObject.SetActive(false);
        }

        /// <summary>
        /// Displays the PC monitor UI and sets up the return button functionality.
        /// </summary>
        /// <param name="onClose">An action to be performed when the PC monitor is closed.</param>
        public void Display(System.Action onClose)
        {
            gameObject.SetActive(true);

            UIManager.Instance.ToggleActionUI(ActionType.Return, true, () =>
            {
                onClose?.Invoke();
                gameObject.SetActive(false);
                UIManager.Instance.ToggleActionUI(ActionType.Return, false, null);
            });
        }

        public void PlayClickSound(bool isOn)
        {
            if (!isOn) return;

            AudioManager.Instance.PlaySFX(AudioID.Click);
        }

        /// <summary>
        /// Handles changes to the shopping cart, updating the cart UI.
        /// </summary>
        /// <param name="cart">
        /// A dictionary representing the current state of the shopping cart,
        /// where the key is the IPurchasable item and the value is the quantity.
        /// </param>
        private void HandleCartChanged(Dictionary<IPurchasable, int> cart)
        {
            // Clear existing cart items in the UI.
            foreach (Transform child in cartItemsParent)
            {
                Destroy(child.gameObject);
            }

            decimal totalPrice = 0m;
            int totalItems = 0;

            // Iterate through the cart items and update the UI.
            foreach (var item in cart)
            {
                // Instantiate a new cart item prefab for each item in the cart.
                CartItem newCartItem = Instantiate(cartItemPrefab, cartItemsParent);

                // Initialize the cart item with the item details and quantity.
                newCartItem.Initialize(item.Key, item.Value);

                // Calculate the total price and item count.
                int quantity = item.Key is Product product ? product.GetBoxQuantity() : 1; // Handle different purchasable types.
                totalPrice += item.Key.Price * quantity * item.Value;
                totalItems += item.Value;
            }

            // Update the total price and cart label text in the UI.
            totalPriceText.text = $"Total: ${totalPrice:N2}";
            cartTabLabel.text = "Cart";
            if (totalItems > 0) cartTabLabel.text += $"<color=#FFB414> ({totalItems})"; // Add item count to the cart label.
        }

        private void CreateProductListing(Product product)
        {
            var productListing = Instantiate(productListingPrefab, productListingParent);
            productListing.Initialize(product);
            productListings.Add(productListing);
        }

        private void UpdateProductListing(License license)
        {
            foreach (var product in license.Products)
            {
                CreateProductListing(product);
            }

            OnCategoryChanged(categoryDropdown.value);
        }

        private void CreateBillUI(Bill bill)
        {
            var billUI = Instantiate(billUIPrefab, billUIParent);
            billUI.Initialize(bill);
            billUIs.Add(billUI);

            UpdateBillSummaryUI();

            if (bill.Type == BillType.Repayment) RefreshLoanInfoUI();
        }

        private void UpdateBillUIs()
        {
            for (int i = billUIs.Count - 1; i >= 0; i--)
            {
                var billUI = billUIs[i];

                if (!DataManager.Instance.Data.Bills.Contains(billUI.Bill))
                {
                    billUIs.RemoveAt(i);
                    Destroy(billUI.gameObject);
                    continue;
                }

                billUI.UpdateUI();
            }

            UpdateBillSummaryUI();
        }

        private void UpdateBillSummaryUI()
        {
            int count = FinanceManager.Instance.GetActiveBills().Count;
            decimal sum = FinanceManager.Instance.GetTotalOutstandingAmount();
            outstandingBillsText.text = $"Outstanding Bills: {count} | Total Amount: ${sum:N2}";
            outstandingBillsText.color = count > 0 ? Color.white : Color.gray;

            payAllBillsButton.interactable = count > 0;
        }

        private void RefreshLoanInfoUI()
        {
            var loans = DataManager.Instance.Data.Loans;
            if (loans.Count == 0)
            {
                loanInfoText.text = "Congratulations!\nYou have no active loans.";
                return;
            }

            System.Text.StringBuilder sb = new();

            foreach (var loan in loans)
            {
                sb.AppendLine($"<b>{loan.DisplayName}</b>");
                sb.AppendLine($"Progress: {loan.PaymentsMade} of {loan.TotalPayments} Bills");
                sb.AppendLine(); // Extra line for spacing
            }

            loanInfoText.text = sb.ToString();
        }

        private void InitializeDropdown<TEnum>(TMP_Dropdown dropdown, string allLabel, UnityAction<int> onValueChanged) where TEnum : System.Enum
        {
            dropdown.ClearOptions();

            var options = new List<string> { allLabel };
            options.AddRange(System.Enum.GetNames(typeof(TEnum)).Select(name => name.ToTitleCase()));

            dropdown.AddOptions(options);
            dropdown.onValueChanged.AddListener(onValueChanged);
        }

        private void OnCategoryChanged(int index)
        {
            if (index == 0)
            {
                productListings.ForEach(listing => listing.gameObject.SetActive(true));
                return;
            }

            var selectedCategory = (Product.Category)(index - 1);

            foreach (var listing in productListings)
            {
                bool shouldShow = listing.Category == selectedCategory;
                listing.gameObject.SetActive(shouldShow);
            }
        }

        private void OnSectionChanged(int index)
        {
            if (index == 0)
            {
                furnitureListings.ForEach(listing => listing.gameObject.SetActive(true));
                return;
            }

            var selectedSection = (Section)(index - 1);

            foreach (var listing in furnitureListings)
            {
                bool shouldShow = listing.Section == selectedSection;
                listing.gameObject.SetActive(shouldShow);
            }
        }
    }
}
