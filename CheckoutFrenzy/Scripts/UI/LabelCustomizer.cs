using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace CryingSnow.CheckoutFrenzy
{
    public class LabelCustomizer : MonoBehaviour
    {
        [SerializeField, Tooltip("RectTransform of the main label customizer panel.")]
        private RectTransform mainPanel;

        [SerializeField, Tooltip("Button to remove the label on the current shelf.")]
        private Button removeButton;

        [SerializeField, Tooltip("")]
        private RectTransform contentRect;

        [SerializeField, Tooltip("")]
        private GameObject emptyNotif;

        [SerializeField, Tooltip("")]
        private StorageProductUI productUIPrefab;

        private List<StorageProductUI> productUIs = new List<StorageProductUI>();

        /// <summary>
        /// Event invoked when the label customizer is closed.
        /// </summary>
        public UnityEvent OnClose { get; } = new UnityEvent();

        private void Awake()
        {
            mainPanel.anchoredPosition = Vector2.zero;
            gameObject.SetActive(false);
        }

        public void Open(Shelf shelf)
        {
            gameObject.SetActive(true);

            // Toggle the return button and set its action to close the label customizer.
            UIManager.Instance.ToggleActionUI(ActionType.Return, true, () =>
            {
                UIManager.Instance.ToggleActionUI(ActionType.Return, false, null);
                OnClose?.Invoke();
                gameObject.SetActive(false);
            });

            productUIs.ForEach(ui => Destroy(ui.gameObject));
            productUIs.Clear();

            var products = WarehouseManager.Instance.GetProducts(shelf.ShelvingUnit.Section);

            if (products.Count == 0)
            {
                emptyNotif.SetActive(true);
                return;
            }

            emptyNotif.SetActive(false);

            foreach (var kvp in products)
            {
                var productUI = Instantiate(productUIPrefab, contentRect, false);

                productUI.Initialize(shelf, kvp.Key, kvp.Value, onClick: (clickedUI) =>
                {
                    productUIs.ForEach(ui => ui.SetInteractable(ui != clickedUI));
                });

                productUI.SetInteractable(shelf.AssignedProduct != kvp.Key);

                productUIs.Add(productUI);
            }

            removeButton.onClick.RemoveAllListeners();
            removeButton.onClick.AddListener(() =>
            {
                shelf.SetLabel(null);
                productUIs.ForEach(ui => ui.SetInteractable(true));
            });
        }
    }
}
