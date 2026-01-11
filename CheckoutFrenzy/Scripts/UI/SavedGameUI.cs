using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CryingSnow.CheckoutFrenzy
{
    public class SavedGameUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI slotNumber;
        [SerializeField] private TextMeshProUGUI description;
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button loadGameButton;
        [SerializeField] private Button deleteGameButton;

        private int slot;

        public void Initialize(GameData gameData, System.Action<int> onDelete)
        {
            slot = transform.GetSiblingIndex() + 1;
            slotNumber.text = $"Slot {slot}";

            newGameButton.onClick.RemoveAllListeners();
            loadGameButton.onClick.RemoveAllListeners();
            deleteGameButton.onClick.RemoveAllListeners();

            if (gameData == null)
            {
                description.text = "<i><color=#606060>This slot is empty!";

                newGameButton.gameObject.SetActive(true);
                loadGameButton.gameObject.SetActive(false);
                deleteGameButton.gameObject.SetActive(false);

                newGameButton.onClick.AddListener(StartGameAtSlot);
            }
            else
            {
                description.text = gameData.StoreName
                    + $"\nPlaytime: {gameData.TotalPlaytime.TotalHours:N1} hours"
                    + $"\nBalance: ${gameData.PlayerMoney:N2}"
                    + $"\nSaved: {gameData.LastSaved:MM/dd/yyyy hh:mm tt}";

                newGameButton.gameObject.SetActive(false);
                loadGameButton.gameObject.SetActive(true);
                deleteGameButton.gameObject.SetActive(true);

                loadGameButton.onClick.AddListener(StartGameAtSlot);
                deleteGameButton.onClick.AddListener(() => onDelete?.Invoke(slot));
            }
        }

        private void StartGameAtSlot()
        {
            PlayerPrefs.SetInt("Slot", slot);
            AudioManager.Instance.PlaySFX(AudioID.Click);
            MainMenu.Instance.StartGame();
        }
    }
}
