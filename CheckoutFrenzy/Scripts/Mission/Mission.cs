using UnityEngine;

namespace CryingSnow.CheckoutFrenzy
{
    public class Mission : ScriptableObject
    {
        public int missionId;
        public Goal goalType;
        public int targetId;
        public int goalAmount;
        public int reward;

        public enum Goal
        {
            Checkout,   // Complete a certain number of checkouts.
            Revenue,    // Earn a specific amount of revenue.
            Sell,       // Sell a specific number of a particular product.
            Restock,    // Purchase specific number of a particular product.
            Furnish,    // Purchase and place a specific number of furniture items.
            License     // Purchase a specific product license.
        }
    }
}
