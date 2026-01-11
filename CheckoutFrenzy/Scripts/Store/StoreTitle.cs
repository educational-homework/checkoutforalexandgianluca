using System.Collections;
using UnityEngine;
using Cinemachine;
using TMPro;

namespace CryingSnow.CheckoutFrenzy
{
    [RequireComponent(typeof(BoxCollider))]
    [RequireComponent(typeof(TextMeshPro))]
    public class StoreTitle : MonoBehaviour, IInteractable
    {
        [SerializeField, Tooltip("Virtual camera to focus on the store name (on the roof) during renaming.")]
        private CinemachineVirtualCamera roofCamera;

        private TextMeshPro textMeshPro;

        private void Awake()
        {
            textMeshPro = GetComponent<TextMeshPro>();
        }

        private void Start()
        {
            gameObject.layer = GameConfig.Instance.InteractableLayer.ToSingleLayer();

            textMeshPro.text = DataManager.Instance.Data.StoreName;

            if (ColorUtility.TryParseHtmlString(DataManager.Instance.Data.NameColor, out var nameColor))
            {
                textMeshPro.color = nameColor;
            }

            DataManager.Instance.OnSave += () =>
            {
                DataManager.Instance.Data.StoreName = textMeshPro.text;

                var nameColor = "#" + ColorUtility.ToHtmlStringRGB(textMeshPro.color);
                DataManager.Instance.Data.NameColor = nameColor;
            };
        }

        public void Interact(PlayerController player)
        {
            var keyboard = UIManager.Instance.VirtualKeyboard;
            keyboard.Open(textMeshPro);

            if (roofCamera != null)
                StartCoroutine(FocusOnTitle(keyboard.gameObject, player));

            UIManager.Instance.InteractMessage.Hide();
        }

        public void OnFocused()
        {
            string message = "Tap to rename your store.";
            UIManager.Instance.InteractMessage.Display(message);
        }

        public void OnDefocused()
        {
            UIManager.Instance.InteractMessage.Hide();
        }

        private IEnumerator FocusOnTitle(GameObject keyboard, PlayerController player)
        {
            roofCamera.gameObject.SetActive(true);

            player.StateManager.PushState(PlayerState.Busy);

            while (keyboard.activeSelf) yield return null;

            player.StateManager.PopState();

            roofCamera.gameObject.SetActive(false);
        }
    }
}
