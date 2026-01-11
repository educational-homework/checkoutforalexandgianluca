using System.Collections.Generic;
using UnityEngine;

namespace CryingSnow.CheckoutFrenzy
{
    [CreateAssetMenu(fileName = "New Dialogue")]
    public class Dialogue : ScriptableObject
    {
        [System.Serializable]
        private struct Line
        {
            [TextArea(3, 5)]
            public string Text;
        }

        [SerializeField] private List<Line> lines;

        /// <summary>
        /// Returns a random dialogue line from the list.
        /// </summary>
        /// <returns>A random dialogue line as a string.</returns>
        public string GetRandomLine()
        {
            if (lines == null || lines.Count == 0)
            {
                Debug.LogWarning("Dialogue list is empty.");
                return "";
            }

            int index = Random.Range(0, lines.Count);
            return lines[index].Text;
        }
    }
}
