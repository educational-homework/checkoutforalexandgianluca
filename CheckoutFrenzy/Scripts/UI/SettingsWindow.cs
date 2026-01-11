using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Audio;

namespace CryingSnow.CheckoutFrenzy
{
    public class SettingsWindow : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField, Tooltip("RectTransform of the main settings panel.")]
        private RectTransform mainPanel;

        [SerializeField, Tooltip("Slider to control the background music (BGM) volume.")]
        private Slider bgmSlider;

        [SerializeField, Tooltip("Slider to control the sound effects (SFX) volume.")]
        private Slider sfxSlider;

        [SerializeField, Tooltip("Audio mixer to control the game's audio groups.")]
        private AudioMixer audioMixer;

        [SerializeField, Tooltip("Toggle to enable/disable interaction messages.")]
        private Toggle interactMessageToggle;

        private void Start()
        {
            gameObject.SetActive(false); // Initially hide the settings window.

            mainPanel.anchoredPosition = Vector2.zero; // Set the panel's anchored position to the center.

            // Set a semi-transparent background color if an Image component exists.
            if (TryGetComponent<Image>(out Image image))
            {
                image.color = new Color(0f, 0f, 0f, 0.9f);
            }

            // Initialize BGM volume slider and apply saved settings.
            bgmSlider.value = PlayerPrefs.GetFloat("BGM Volume", 0.8f); // Load saved volume or default to 0.8.
            float bgmDecibelValue = bgmSlider.value > 0f ? Mathf.Log10(bgmSlider.value) * 20 : -80f; // Convert to decibels.
            audioMixer.SetFloat("BGM Volume", bgmDecibelValue); // Set the audio mixer's BGM volume.
            bgmSlider.onValueChanged.AddListener(UpdateBGMVolume); // Add listener for BGM volume changes.

            // Initialize SFX volume slider and apply saved settings.
            sfxSlider.value = PlayerPrefs.GetFloat("SFX Volume", 0.8f); // Load saved volume or default to 0.8.
            float sfxDecibelValue = sfxSlider.value > 0f ? Mathf.Log10(sfxSlider.value) * 20 : -80f; // Convert to decibels.
            audioMixer.SetFloat("SFX Volume", sfxDecibelValue); // Set the audio mixer's SFX volume.
            sfxSlider.onValueChanged.AddListener(UpdateSFXVolume); // Add listener for SFX volume changes.

            // Initialize interact message toggle and apply saved settings.
            interactMessageToggle.isOn = PlayerPrefs.GetInt("Display Interact Message", 1) > 0; // Load saved toggle state.
            interactMessageToggle.onValueChanged.AddListener(isOn =>
            {
                PlayerPrefs.SetInt("Display Interact Message", isOn ? 1 : 0); // Save the toggle state.
                AudioManager.Instance.PlaySFX(AudioID.Click); // Play a click sound.
            });
        }

        /// <summary>
        /// Updates the background music volume based on the slider value.
        /// </summary>
        /// <param name="volume">The new volume value (0-1).</param>
        private void UpdateBGMVolume(float volume)
        {
            PlayerPrefs.SetFloat("BGM Volume", volume); // Save the volume to PlayerPrefs.
            float decibelValue = volume > 0f ? Mathf.Log10(volume) * 20 : -80f; // Convert to decibels.
            audioMixer.SetFloat("BGM Volume", decibelValue); // Set the audio mixer's BGM volume.
        }

        /// <summary>
        /// Updates the sound effects volume based on the slider value.
        /// </summary>
        /// <param name="volume">The new volume value (0-1).</param>
        private void UpdateSFXVolume(float volume)
        {
            PlayerPrefs.SetFloat("SFX Volume", volume); // Save the volume to PlayerPrefs.
            float decibelValue = volume > 0f ? Mathf.Log10(volume) * 20 : -80f; // Convert to decibels.
            audioMixer.SetFloat("SFX Volume", decibelValue); // Set the audio mixer's SFX volume.
        }

        /// <summary>
        /// Handles pointer clicks on the settings window.
        /// Closes the window if clicked outside the main panel.
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

            // Clicked outside the main panel, toggle (close) the window.
            Toggle();
        }

        /// <summary>
        /// Toggles the settings window active state.
        /// </summary>
        public void Toggle()
        {
            gameObject.SetActive(!gameObject.activeSelf);

            AudioManager.Instance.PlaySFX(AudioID.Click);
        }
    }
}
