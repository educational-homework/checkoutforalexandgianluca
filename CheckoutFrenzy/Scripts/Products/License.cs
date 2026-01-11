using System.Collections.Generic;
using UnityEngine;

namespace CryingSnow.CheckoutFrenzy
{
    [CreateAssetMenu()]
    public class License : ScriptableObject
    {
        [SerializeField] private int licenseId;
        [SerializeField] private new string name;
        [SerializeField] private int price;
        [SerializeField] private int level;
        [SerializeField] private bool isOwnedByDefault;
        [SerializeField] private List<Product> products;
        [SerializeField] private License requiredLicense;

        public bool IsOwnedByDefault => isOwnedByDefault;
        public bool IsPurchased => CheckPurchased();

        public int LicenseID => licenseId;
        public string Name => name;
        public int Price => price;
        public int Level => level;
        public List<Product> Products => products;
        public License RequiredLicense => requiredLicense;

        private bool CheckPurchased()
        {
            foreach (var product in products)
            {
                if (!DataManager.Instance.Data.LicensedProducts.Contains(product.ProductID))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
