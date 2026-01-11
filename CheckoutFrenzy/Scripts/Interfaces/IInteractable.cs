namespace CryingSnow.CheckoutFrenzy
{
    public interface IInteractable
    {
        void Interact(PlayerController player);
        void OnFocused();
        void OnDefocused();
    }
}
