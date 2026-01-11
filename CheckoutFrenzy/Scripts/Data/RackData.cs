using System.Collections.Generic;

namespace CryingSnow.CheckoutFrenzy
{
    [System.Serializable]
    public class RackData
    {
        public int ProductID { get; set; }
        public List<int> Quantities { get; set; }

        public bool IsEmpty => ProductID == 0 || Quantities.Count == 0;

        public RackData(Rack rack)
        {
            if (rack.Product != null)
                ProductID = rack.Product.ProductID;

            Quantities = rack.GetProductQuantities();
        }
    }
}