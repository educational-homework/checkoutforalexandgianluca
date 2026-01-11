using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CryingSnow.CheckoutFrenzy
{
    public class MissionTracker : MonoBehaviour
    {
        [SerializeField, Tooltip("TextMeshPro component to display the mission details.")]
        private TMP_Text missionText;

        [SerializeField, Tooltip("Button to claim the reward for completing the mission.")]
        private Button rewardButton;

        [SerializeField, Tooltip("Text component to display the reward amount.")]
        private TMP_Text rewardText;

        [SerializeField, Tooltip("The key used to collect mission reward.")]
        private KeyCode rewardKey = KeyCode.M;

        [SerializeField, Tooltip("Image showing the icon of the collect reward key.")]
        private Image keyIcon;

        [SerializeField, Tooltip("Toggle used to show and hide the tracker on mobile contol mode.")]
        private PanelToggle panelToggle;

        private bool isMobileControl;

        private void Start()
        {
            isMobileControl = GameConfig.Instance.ControlMode == ControlMode.Mobile;

            keyIcon.gameObject.SetActive(!isMobileControl);
            panelToggle.gameObject.SetActive(isMobileControl);

            MissionManager.Instance.OnMissionUpdated += UpdateDisplay; // Subscribe to mission update events.

            // Get the current mission and mission data.
            var mission = MissionManager.Instance.GetCurrentMission();
            var missionData = DataManager.Instance.Data.CurrentMission;
            UpdateDisplay(mission, missionData); // Initial display update.
        }

        private void Update()
        {
            if (Input.GetKeyDown(rewardKey))
            {
                if (!rewardButton.gameObject.activeSelf) return;
                MissionManager.Instance.AdvanceMission();
            }
        }

        private void OnDisable()
        {
            MissionManager.Instance.OnMissionUpdated -= UpdateDisplay; // Unsubscribe to prevent memory leaks.
        }

        /// <summary>
        /// Updates the displayed mission information based on the provided mission and mission data.
        /// </summary>
        /// <param name="mission">The Mission object containing mission details.</param>
        /// <param name="missionData">The MissionData object containing mission progress.</param>
        private void UpdateDisplay(Mission mission, MissionData missionData)
        {
            // Handle cases where mission or mission data is not available.
            if (mission == null || missionData == null)
            {
                missionText.text = "<align=left>Mission not available.\nPlease wait for new update!";
                rewardButton.gameObject.SetActive(false); // Hide the reward button.
                return;
            }

            string displayText = "";

            displayText = $"<u>Mission #{mission.missionId:D3}</u>"; // Format mission ID.
            displayText += $"\n<align=left>{GetFormattedGoal(mission.goalType)}:"; // Add formatted goal type.

            // Add target information based on the goal type.
            if (mission.goalType == Mission.Goal.Sell || mission.goalType == Mission.Goal.Restock)
            {
                var product = DataManager.Instance.GetProductById(mission.targetId);
                displayText += $"\n{product.Name}"; // Add product name.
            }
            else if (mission.goalType == Mission.Goal.Furnish)
            {
                var furniture = DataManager.Instance.GetFurnitureById(mission.targetId);
                if (furniture == null) Debug.Log($"Furniture ID: {mission.targetId}"); // Log if furniture not found.
                displayText += $"\n{furniture.Name}"; // Add furniture name.
            }
            else if (mission.goalType == Mission.Goal.License)
            {
                var license = DataManager.Instance.GetLicenseById(mission.targetId);
                displayText += $"\n{license.Name}";
            }

            // Add progress information based on the goal type.
            if (mission.goalType == Mission.Goal.Revenue)
            {
                displayText += $"\n<align=center>${missionData.Progress / 100m:N2} / ${mission.goalAmount / 100m:N2}"; // Format revenue progress.
            }
            else
            {
                displayText += $"\n<align=center>{missionData.Progress} / {mission.goalAmount}"; // Format other progress.
            }

            // Handle reward display and button interactability based on mission completion.
            if (missionData.IsComplete)
            {
                displayText += "\n<align=left>Collect Reward:";
                rewardText.text = $"${mission.reward:N2}"; // Display reward amount.

                rewardButton.gameObject.SetActive(true); // Show the reward button.

                if (isMobileControl)
                {
                    rewardButton.onClick.RemoveAllListeners(); // Remove previous listeners.
                    rewardButton.onClick.AddListener(MissionManager.Instance.AdvanceMission); // Add listener to advance mission.
                }
            }
            else
            {
                rewardButton.gameObject.SetActive(false); // Hide the reward button if mission is not complete.
            }

            missionText.text = displayText; // Set the mission text.
        }

        /// <summary>
        /// Formats the goal type into a human-readable string.
        /// </summary>
        /// <param name="goalType">The Mission.Goal type.</param>
        /// <returns>The formatted goal string.</returns>
        private string GetFormattedGoal(Mission.Goal goalType)
        {
            string goal = "";

            switch (goalType)
            {
                case Mission.Goal.Checkout:
                    goal = "Perform Checkout";
                    break;

                case Mission.Goal.Revenue:
                    goal = "Collect Revenue";
                    break;

                case Mission.Goal.Sell:
                    goal = "Sell Product";
                    break;

                case Mission.Goal.Restock:
                    goal = "Restock Product";
                    break;

                case Mission.Goal.Furnish:
                    goal = "Furnish The Store";
                    break;

                case Mission.Goal.License:
                    goal = "Own License";
                    break;

                default:
                    break;
            }

            return goal;
        }
    }
}
