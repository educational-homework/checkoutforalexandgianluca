using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;

namespace CryingSnow.CheckoutFrenzy
{
    [RequireComponent(typeof(CanvasGroup))]
    public class Message : MonoBehaviour
    {
        private CanvasGroup canvasGroup;
        private TMP_Text messageText;
        private Queue<(string message, Color color, float time)> messageQueue = new();
        private bool isDisplaying = false;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            messageText = GetComponentInChildren<TMP_Text>();

            canvasGroup.alpha = 0f;
            messageText.text = "";
        }

        /// <summary>
        /// Displays a message with an optional color and display time.
        /// Messages are queued if one is already being displayed.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="color">The color of the message text (optional, defaults to white).</param>
        /// <param name="time">The duration (in seconds) to display the message (defaults to 2 seconds).</param>
        public void Log(string message, Color? color = null, float time = 1f)
        {
            messageQueue.Enqueue((message, color ?? Color.white, time));

            if (!isDisplaying)
            {
                StartCoroutine(ProcessQueue());
            }
        }

        private IEnumerator ProcessQueue()
        {
            isDisplaying = true;

            while (messageQueue.Count > 0)
            {
                var (message, color, time) = messageQueue.Dequeue();

                messageText.text = $"<color=#{ColorUtility.ToHtmlStringRGBA(color)}>{message}</color>";

                DOTween.Kill(canvasGroup);
                yield return canvasGroup.DOFade(1f, 0.25f).WaitForCompletion(); // Fade in
                yield return new WaitForSeconds(time); // Display duration
                yield return canvasGroup.DOFade(0f, 0.25f).WaitForCompletion(); // Fade out
            }

            isDisplaying = false;
        }
    }
}
