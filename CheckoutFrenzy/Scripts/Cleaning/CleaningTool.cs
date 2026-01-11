using UnityEngine;

namespace CryingSnow.CheckoutFrenzy
{
    public enum CleaningToolType { BroomIndoor, BroomOutdoor, Mop }

    public class CleaningTool : MonoBehaviour
    {
        [SerializeField] private CleaningToolType toolType;
        [SerializeField] private float stoppingDistance;
        [SerializeField] private string animationTrigger;

        public CleaningToolType ToolType => toolType;
        public float StoppingDistance => stoppingDistance;
        public string AnimationTrigger => animationTrigger;
    }
}
