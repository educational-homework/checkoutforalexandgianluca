using System.Collections.Generic;
using UnityEngine;

namespace CryingSnow.CheckoutFrenzy
{
    [CreateAssetMenu()]
    public class Product : ScriptableObject, IPurchasable
    {
        [SerializeField] private int productId;
        [SerializeField] private Category category;

        [SerializeField] private new string name;
        [SerializeField] private Sprite icon;
        [SerializeField] private long priceInCents;
        [SerializeField] private int orderTime = 5;
        [SerializeField] private Section section;

        [SerializeField] private GameObject model;
        [SerializeField] private Box box;

        [SerializeField] private bool overrideBoxQuantity;
        [SerializeField] private Vector3Int boxQuantity;

        [SerializeField] private bool overrideShelfQuantity;
        [SerializeField] private Vector3Int shelfQuantity;

        public enum Category
        {
            FoodAndBeverages,
            PersonalCareAndHygiene,
            HouseholdItems,
            HealthAndWellness,
            ElectronicsAndAccessories,
            Miscellaneous
        }

        private readonly Dictionary<Category, decimal> profitMargins = new Dictionary<Category, decimal>()
        {
            { Category.FoodAndBeverages, 0.25m },
            { Category.PersonalCareAndHygiene, 0.30m },
            { Category.HouseholdItems, 0.35m },
            { Category.HealthAndWellness, 0.40m },
            { Category.ElectronicsAndAccessories, 0.45m },
            { Category.Miscellaneous, 0.50m }
        };

        public int ProductID => productId;
        public Category ProductCategory => category;

        public string Name => name;
        public Sprite Icon => icon;
        public decimal Price => priceInCents / 100m;
        public int OrderTime => orderTime;
        public Section Section => section;

        public decimal MarketPrice => CalculateMarketPrice();
        public bool HasLicense => DataManager.Instance.Data.LicensedProducts.Contains(productId);

        public GameObject Model => model;
        public Box Box => box;

        public bool OverrideBoxQuantity => overrideBoxQuantity;
        public Vector3Int BoxQuantity => boxQuantity;
        public bool OverrideShelfQuantity => overrideShelfQuantity;
        public Vector3Int ShelfQuantity => shelfQuantity;

        public Vector3 Size
        {
            get
            {
                if (model == null)
                {
                    Debug.LogWarning("Model is not assigned for product " + name);
                    return Vector3.zero;
                }

                var meshRenderer = model.GetComponent<MeshRenderer>();
                if (meshRenderer == null)
                {
                    Debug.LogWarning("MeshRenderer is missing on the model for product " + name);
                    return Vector3.zero;
                }

                return meshRenderer.bounds.size;
            }
        }

        public Vector3Int FitOnContainer(Vector3 containerSize)
        {
            int fitX = Mathf.FloorToInt(containerSize.x / Size.x);
            int fitY = Mathf.FloorToInt(containerSize.y / Size.y);
            int fitZ = Mathf.FloorToInt(containerSize.z / Size.z);

            return new Vector3Int(fitX, fitY, fitZ);
        }

        public int GetBoxQuantity()
        {
            if (box == null) return -1;

            if (overrideBoxQuantity) return boxQuantity.x * boxQuantity.y * boxQuantity.z;

            Vector3Int fit = FitOnContainer(box.Size);
            return fit.x * fit.y * fit.z;
        }

        private decimal CalculateMarketPrice()
        {
            decimal profitMargin = profitMargins[category];
            decimal profit = Price * profitMargin;
            return Price + profit;
        }
    }
}
