using UnityEngine;

namespace CryingSnow.CheckoutFrenzy
{
    public struct ChangeMoney
    {
        public int amount;
        public GameObject money;

        public ChangeMoney(int amount, GameObject money)
        {
            this.amount = amount;
            this.money = money;
        }
    }
}
