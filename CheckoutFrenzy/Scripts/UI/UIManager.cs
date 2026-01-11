using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SimpleInputNamespace;

namespace CryingSnow.CheckoutFrenzy
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("Controls")]
        [SerializeField, Tooltip("Image used for the player's crosshair.")]
        private Image crosshair;

        [SerializeField, Tooltip("Joystick component for player movement input.")]
        private Joystick joystick;

        [SerializeField, Tooltip("Button for interacting with interactables.")]
        private Button interactButton;

        [Header("Gameplay UI")]
        [SerializeField, Tooltip("Displays information about a box.")]
        private BoxInfo boxInfo;

        [SerializeField, Tooltip("Displays in-game messages.")]
        private Message message;

        [SerializeField, Tooltip("Allows the player to customize product prices.")]
        private PriceCustomizer priceCustomizer;

        [SerializeField, Tooltip("Displays the PC monitor interface.")]
        private PCMonitor pcMonitor;

        [SerializeField, Tooltip("Handles interactions with the cash register.")]
        private CashRegister cashRegister;

        [SerializeField, Tooltip("Handles interactions with the payment terminal.")]
        private PaymentTerminal paymentTerminal;

        [SerializeField, Tooltip("Displays the summary screen at the end of a session or day.")]
        private SummaryScreen summaryScreen;

        [SerializeField, Tooltip("Controls the skip dialog functionality.")]
        private SkipDialog skipDialog;

        [SerializeField, Tooltip("Displays and manages the virtual keyboard.")]
        private VirtualKeyboard virtualKeyboard;

        [SerializeField, Tooltip("Allows customization of shelf product labels.")]
        private LabelCustomizer labelCustomizer;

        [SerializeField, Tooltip("Displays the remaining time for deliveries.")]
        private TMP_Text deliveryTimerText;

        [SerializeField, Tooltip("Radial fill image indicating hold progress (e.g., when moving furniture).")]
        private Image holdProgressImage;

        [SerializeField, Tooltip("Prefab for the customer's chat bubble.")]
        private ChatBubble chatBubblePrefab;

        [Header("Action UIs")]
        [SerializeField, Tooltip("Parent for action buttons (Mobile).")]
        private RectTransform actionButtonsParent;

        [SerializeField, Tooltip("Parent for action prompts (PC).")]
        private RectTransform actionPromptsParent;

        [Header("Game Pause")]
        [SerializeField, Tooltip("Reference to the pause menu.")]
        private PauseMenu pauseMenu;

        [SerializeField, Tooltip("Key to pause the game and display the pause menu.")]
        private KeyCode pauseKey = KeyCode.Escape;

        public Message Message => message;
        public PriceCustomizer PriceCustomizer => priceCustomizer;
        public PCMonitor PCMonitor => pcMonitor;
        public CashRegister CashRegister => cashRegister;
        public PaymentTerminal PaymentTerminal => paymentTerminal;
        public SummaryScreen SummaryScreen => summaryScreen;
        public SkipDialog SkipDialog => skipDialog;
        public InteractMessage InteractMessage { get; private set; }
        public VirtualKeyboard VirtualKeyboard => virtualKeyboard;
        public LabelCustomizer LabelCustomizer => labelCustomizer;

        private Canvas canvas;
        private bool isMobileControl;
        private bool isGamePaused => Time.timeScale < 1f;
        private List<IActionUI> actionUIs;

        private void Awake()
        {
            Instance = this;

            canvas = GetComponentInChildren<Canvas>();
        }

        private void Start()
        {
            isMobileControl = GameConfig.Instance.ControlMode == ControlMode.Mobile;

            if (isMobileControl)
            {
                actionUIs = actionButtonsParent.GetComponentsInChildren<IActionUI>().ToList();

                actionPromptsParent.gameObject.SetActive(false);
            }
            else
            {
                actionUIs = actionPromptsParent.GetComponentsInChildren<IActionUI>().ToList();

                actionButtonsParent.gameObject.SetActive(false);

                Vector2 originalPos = actionPromptsParent.anchoredPosition;
                Vector2 targetPos = new Vector2(0f, originalPos.y);
                actionPromptsParent.anchoredPosition = targetPos;
            }

            this.InteractMessage = GetComponentsInChildren<InteractMessage>(true)
                .FirstOrDefault(m => m.ControlMode == GameConfig.Instance.ControlMode);

            HideBoxInfo();

            ToggleInteractButton(false);
            ToggleCrosshair(true);

            actionUIs.ForEach(actionUI => actionUI.SetActive(false));

            ToggleDeliveryTimer(false);
            UpdateHoldProgress(0f);
        }

        private void Update()
        {
            if (Input.GetKeyDown(pauseKey))
            {
                if (pauseMenu.gameObject.activeSelf) pauseMenu.Close();
                else if (!isGamePaused) pauseMenu.Open();
            }
        }

        public void ToggleInteractButton(bool active)
        {
            if (active && !isMobileControl) return;

            interactButton.gameObject.SetActive(active);
        }

        public void ToggleCrosshair(bool active)
        {
            crosshair.gameObject.SetActive(active);
            joystick.gameObject.SetActive(active);

            if (!isMobileControl)
            {
                Cursor.visible = !active;
                Cursor.lockState = active ? CursorLockMode.Locked : CursorLockMode.None;
            }
        }

        public void ToggleActionUI(ActionType actionType, bool active, System.Action action)
        {
            var actionUI = actionUIs.FirstOrDefault(a => a.ActionType == actionType);

            actionUI.SetActive(active);

            actionUI.OnClick.RemoveAllListeners();
            actionUI.OnClick.AddListener(() => action?.Invoke());
        }

        public void DisplayBoxInfo(Box box)
        {
            boxInfo.gameObject.SetActive(true);
            boxInfo.UpdateInfo(box);
        }

        public void DisplayBoxInfo(FurnitureBox furnitureBox)
        {
            boxInfo.gameObject.SetActive(true);
            boxInfo.UpdateInfo(furnitureBox);
        }

        public void HideBoxInfo()
        {
            boxInfo.gameObject.SetActive(false);
        }

        public void UpdateHoldProgress(float progress)
        {
            if (progress > 0.2f)
            {
                if (!holdProgressImage.gameObject.activeSelf)
                {
                    holdProgressImage.gameObject.SetActive(true);
                }
                holdProgressImage.fillAmount = Mathf.Clamp01(progress);
            }
            else
            {
                holdProgressImage.gameObject.SetActive(false);
            }
        }

        public void UpdateDeliveryTimer(int time)
        {
            ToggleDeliveryTimer(true);

            System.TimeSpan timeSpan = System.TimeSpan.FromSeconds(time);
            deliveryTimerText.text = $"Next Delivery:\n{timeSpan:mm\\:ss}";

            if (time <= 0) ToggleDeliveryTimer(false);
        }

        private void ToggleDeliveryTimer(bool active)
        {
            GameObject deliveryTimer = deliveryTimerText.transform.parent.gameObject;
            deliveryTimer.SetActive(active);
        }

        public ChatBubble ShowChatBubble(string chat, Transform speaker)
        {
            var chatBubble = Instantiate(chatBubblePrefab, canvas.transform, false);
            chatBubble.transform.SetAsFirstSibling();
            chatBubble.Show(chat, speaker);
            return chatBubble;
        }
    }
}
