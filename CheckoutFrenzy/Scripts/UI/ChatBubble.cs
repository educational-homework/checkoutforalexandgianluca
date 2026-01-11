using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CryingSnow.CheckoutFrenzy
{
    public class ChatBubble : MonoBehaviour
    {
        [SerializeField, Tooltip("The duration (in seconds) for the chat bubble to be displayed.")]
        private float showTime = 5f;

        private Image bubble;
        private TMP_Text chatText;
        private Transform speaker;

        private Camera m_cam;
        private Camera cam
        {
            get
            {
                if (m_cam == null) m_cam = Camera.main;
                return m_cam;
            }
        }

        private float timer;

        private void Awake()
        {
            bubble = GetComponent<Image>();
            chatText = GetComponentInChildren<TMP_Text>();

            bubble.enabled = false;
            chatText.text = "";
        }

        /// <summary>
        /// Shows the chat bubble with the specified text above the given speaker.
        /// </summary>
        /// <param name="chat">The text to display in the chat bubble.</param>
        /// <param name="speaker">The Transform of the object to display the chat bubble above.</param>
        public void Show(string chat, Transform speaker)
        {
            bubble.enabled = true;
            chatText.text = chat;
            this.speaker = speaker;
        }

        /// <summary>
        /// Updates the position of the chat bubble and manages its visibility and lifetime.
        /// </summary>
        private void LateUpdate()
        {
            if (speaker == null) return; // Do nothing if there's no speaker.

            // Position the bubble above the speaker in screen space.
            transform.position = cam.WorldToScreenPoint(speaker.transform.position + Vector3.up * 2);

            // Only show the bubble if it's in front of the camera.
            bubble.enabled = chatText.enabled = transform.position.z > 0f;

            timer += Time.deltaTime;
            if (timer >= showTime) Destroy(gameObject);
        }
    }
}
