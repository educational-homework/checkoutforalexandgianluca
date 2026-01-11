using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace CryingSnow.CheckoutFrenzy
{
    public class VirtualKeyboard : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private bool disableVirtualKeyboardOnPC;

        [SerializeField] private Slider redSlider;
        [SerializeField] private Slider greenSlider;
        [SerializeField] private Slider blueSlider;

        private TextMeshPro tmp; // The TextMeshPro component to write to.
        private RectTransform mainPanel; // The RectTransform of the main keyboard panel.
        private bool isMobileControl;

        private List<KeyCode> alphabetKeys = new List<KeyCode>();

        private void Awake()
        {
            // Add A-Z
            for (KeyCode k = KeyCode.A; k <= KeyCode.Z; k++)
            {
                alphabetKeys.Add(k);
            }

            alphabetKeys.AddRange(new[] { KeyCode.Backspace, KeyCode.Space, KeyCode.Return, KeyCode.KeypadEnter });

            // Add a listener to each key button.
            foreach (var key in GetComponentsInChildren<Button>())
            {
                key.onClick.AddListener(() => Append(key.name)); // Pass the button's name as input.
            }

            mainPanel = transform.GetChild(0).GetComponent<RectTransform>(); // Get the main panel.
            mainPanel.anchoredPosition = Vector2.zero; // Center the panel.

            gameObject.SetActive(false); // Initially hide the keyboard.
        }

        /// <summary>
        /// Opens the virtual keyboard and sets the target TextMeshPro component.
        /// </summary>
        /// <param name="tmp">The TextMeshPro component to write to.</param>
        public void Open(TextMeshPro tmp)
        {
            gameObject.SetActive(true); // Activate the keyboard.
            this.tmp = tmp; // Set the target TMP component.
        }

        /// <summary>
        /// Handles pointer clicks on the virtual keyboard. Closes the keyboard if clicked outside the main panel.
        /// </summary>
        /// <param name="eventData">The pointer event data.</param>
        public void OnPointerClick(PointerEventData eventData)
        {
            // Check if the click originated from the main panel.
            if (RectTransformUtility.RectangleContainsScreenPoint(mainPanel, eventData.position))
            {
                // Clicked on the main panel, do nothing.
                return;
            }

            // Clicked outside the main panel, deactivate the game object.
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Appends the input to the target TextMeshPro component.
        /// </summary>
        /// <param name="input">The input string (key name).</param>
        private void Append(string input)
        {
            if (input == "Back") // Handle backspace.
            {
                if (tmp.text.Length > 0)
                {
                    tmp.text = tmp.text.Substring(0, tmp.text.Length - 1); // Remove the last character.
                }
            }
            else if (input == "Enter") // Handle enter key.
            {
                gameObject.SetActive(false); // Close the keyboard.
            }
            else if (tmp.text.Length < GameConfig.Instance.StoreNameMaxCharacters) // Limit text length.
            {
                if (input == "Space") tmp.text += " "; // Handle space key.
                else tmp.text += input; // Append other characters.
            }

            AudioManager.Instance.PlaySFX(AudioID.Click);
        }

        private void Start()
        {
            if (disableVirtualKeyboardOnPC && GameConfig.Instance.ControlMode == ControlMode.PC)
            {
                // GetComponentsInChildren<Image>().ToList().ForEach(img => img.enabled = false);
                // GetComponentsInChildren<TMP_Text>().ToList().ForEach(txt => txt.enabled = false);

                for (int i = 1; i < mainPanel.childCount; i++)
                {
                    mainPanel.GetChild(i).gameObject.SetActive(false);
                }

                isMobileControl = false;
            }
            else
            {
                isMobileControl = true;
            }

            InitializeColorSliders();
        }

        private void Update()
        {
            if (isMobileControl || tmp == null) return;

            foreach (KeyCode key in alphabetKeys)
            {
                if (Input.GetKeyDown(key))
                {
                    HandleKeyInput(key);
                }
            }
        }

        private void HandleKeyInput(KeyCode key)
        {
            if (key == KeyCode.Backspace)
            {
                if (tmp.text.Length > 0)
                {
                    tmp.text = tmp.text.Substring(0, tmp.text.Length - 1);
                }
            }
            else if (key == KeyCode.Return || key == KeyCode.KeypadEnter)
            {
                gameObject.SetActive(false);
            }
            else if (key == KeyCode.Space)
            {
                if (tmp.text.Length < GameConfig.Instance.StoreNameMaxCharacters)
                    tmp.text += " ";
            }
            else
            {
                string keyString = key.ToString();
                if (keyString.Length == 1 && char.IsLetterOrDigit(keyString[0]))
                {
                    if (tmp.text.Length < GameConfig.Instance.StoreNameMaxCharacters)
                        tmp.text += keyString;
                }
            }

            AudioManager.Instance.PlaySFX(AudioID.Click);
        }

        private void InitializeColorSliders()
        {
            redSlider.onValueChanged.AddListener(UpdateColor);
            greenSlider.onValueChanged.AddListener(UpdateColor);
            blueSlider.onValueChanged.AddListener(UpdateColor);

            if (ColorUtility.TryParseHtmlString(DataManager.Instance.Data.NameColor, out var nameColor))
            {
                redSlider.value = nameColor.r;
                greenSlider.value = nameColor.g;
                blueSlider.value = nameColor.b;
            }

            UpdateColor(0); // Initial update
        }

        private void UpdateColor(float _)
        {
            Color newColor = new Color(
                redSlider.value,
                greenSlider.value,
                blueSlider.value
            );

            tmp.color = newColor;
        }
    }
}
