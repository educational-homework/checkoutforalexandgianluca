using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DG.Tweening;

namespace CryingSnow.CheckoutFrenzy
{
    [RequireComponent(typeof(Animator))]
    public class Cleaner : MonoBehaviour
    {
        private List<CleaningTool> cleaningTools;

        private CleaningTool cleaningTool;

        private Animator animator;
        private System.Action onCleanStep;
        private bool isCleaning;
        private bool isPlayer;

        private void Awake()
        {
            animator = GetComponent<Animator>();

            cleaningTools = GetComponentsInChildren<CleaningTool>(true).ToList();
            cleaningTools.ForEach(ct => ct.gameObject.SetActive(false));

            isPlayer = GetComponentInParent<PlayerController>() != null;
        }

        public void StartCleaning(Cleanable target, System.Action onCleanStep)
        {
            cleaningTool = cleaningTools.FirstOrDefault(ct => ct.ToolType == target.ToolType);

            if (!isPlayer)
            {
                if (isCleaning) return;
                this.onCleanStep = onCleanStep;
                isCleaning = true;
                animator.SetBool(cleaningTool.AnimationTrigger, true);
                cleaningTool.gameObject.SetActive(true);
                cleaningTool.transform.DOScale(Vector3.one, 0.1f);

                return;
            }

            Vector3 direction = (target.transform.position - transform.position).Flatten().normalized;
            Vector3 targetPos = target.transform.position - direction * cleaningTool.StoppingDistance;

            Quaternion lookRotation = Quaternion.LookRotation(target.transform.position - targetPos);

            Sequence seq = DOTween.Sequence();
            seq.Append(transform.DOMove(targetPos, 0.3f).SetEase(Ease.InOutSine));
            seq.Join(transform.DORotateQuaternion(lookRotation, 0.3f));
            seq.OnComplete(() =>
            {
                if (isCleaning) return;
                this.onCleanStep = onCleanStep;
                isCleaning = true;
                animator.SetBool(cleaningTool.AnimationTrigger, true);
                cleaningTool.gameObject.SetActive(true);
                cleaningTool.transform.DOScale(Vector3.one, 0.1f);
            });
        }

        public void StopCleaning()
        {
            isCleaning = false;
            onCleanStep = null;
            animator.SetBool(cleaningTool.AnimationTrigger, false);

            if (isPlayer)
            {
                transform.DOLocalMove(Vector3.zero, 0.3f);
                transform.DOLocalRotate(Vector3.zero, 0.3f);
            }

            cleaningTool.transform.DOScale(Vector3.zero, 0.1f)
                .OnComplete(() =>
                {
                    cleaningTool.gameObject.SetActive(false);
                    cleaningTool = null;
                });
        }

        public void OnClean(AnimationEvent evt)
        {
            if (evt.animatorClipInfo.weight < 0.9f)
                return;

            if (isCleaning)
                onCleanStep?.Invoke();
        }

        public float GetStoppingDistanceForTool(CleaningToolType toolType)
        {
            return cleaningTools.FirstOrDefault(ct => ct.ToolType == toolType)
                .StoppingDistance;
        }
    }
}
