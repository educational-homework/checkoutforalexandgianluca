using UnityEngine;

namespace CryingSnow.CheckoutFrenzy
{
    public interface IPurchasable
    {
        string Name { get; }
        Sprite Icon { get; }
        decimal Price { get; }
        int OrderTime { get; }
        Section Section { get; }
    }
}
