namespace CryingSnow.CheckoutFrenzy
{
    [System.Serializable]
    public class SummaryData
    {
        public int TotalCustomers { get; set; }
        public decimal PreviousBalance { get; set; }
        public decimal TotalRevenues { get; set; }
        public decimal TotalSpending { get; set; }

        public SummaryData(decimal currentBalance)
        {
            PreviousBalance = currentBalance;
        }
    }
}
