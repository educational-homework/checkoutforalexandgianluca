namespace CryingSnow.CheckoutFrenzy
{
    [System.Serializable]
    public class ShelfData
    {
        public int ProductID { get; set; }
        public int AssignedProductID { get; set; }
        public int Quantity { get; set; }

        public bool IsEmpty => ProductID == 0 || Quantity == 0;

        public ShelfData(Shelf shelf)
        {
            if (shelf.Product != null)
            {
                ProductID = shelf.Product.ProductID;
            }

            if (shelf.AssignedProduct != null)
            {
                AssignedProductID = shelf.AssignedProduct.ProductID;
            }

            Quantity = shelf.Quantity;
        }
    }
}
