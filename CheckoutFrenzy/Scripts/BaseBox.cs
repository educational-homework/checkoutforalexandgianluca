using System.Collections;
using UnityEngine;
using DG.Tweening;
using Unity.AI.Navigation;

namespace CryingSnow.CheckoutFrenzy
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(BoxCollider))]
    public abstract class BaseBox : MonoBehaviour, IInteractable
    {
        [Header("Box Lids")]
        [SerializeField] protected Transform lidFront;
        [SerializeField] protected Transform lidBack;
        [SerializeField] protected Transform lidLeft;
        [SerializeField] protected Transform lidRight;

        [Header("Collision Settings")]
        [SerializeField] protected float collisionCheckDuration = 3f;

        protected Rigidbody body;
        protected BoxCollider boxCollider;
        protected PlayerController player;
        protected Coroutine disablePhysicsRoutine;
        protected Sequence lidSequence;

        public bool IsDisposable { get; protected set; }
        public bool IsCheckingCollision { get; protected set; }

        protected virtual void Awake()
        {
            body = GetComponent<Rigidbody>();
            boxCollider = GetComponent<BoxCollider>();

            SetActivePhysics(false);
            gameObject.layer = GameConfig.Instance.InteractableLayer.ToSingleLayer();

            var navModifier = gameObject.AddComponent<NavMeshModifier>();
            navModifier.ignoreFromBuild = true;
        }

        protected virtual IEnumerator Start()
        {
            yield return new WaitUntil(() => DataManager.Instance.IsLoaded);
            SetActivePhysics(true);
        }

        protected virtual void OnCollisionEnter(Collision collision)
        {
            if (!IsCheckingCollision) return;
            if (collision.relativeVelocity.magnitude > 2f)
            {
                AudioManager.Instance.PlaySFX(AudioID.Impact);
            }
        }

        public virtual void Interact(PlayerController player)
        {
            this.player = player;
            IsDisposable = false;

            if (disablePhysicsRoutine != null)
                StopCoroutine(disablePhysicsRoutine);

            disablePhysicsRoutine = StartCoroutine(DisablePhysicsDelayed());

            foreach (Transform child in transform)
                child.gameObject.layer = GameConfig.Instance.HeldObjectLayer.ToSingleLayer();

            UIManager.Instance.ToggleActionUI(ActionType.Throw, true, Throw);
            UIManager.Instance.HideBoxInfo();

            transform.SetParent(player.HoldPoint);
            transform.DOLocalMove(Vector3.zero, 0.5f).SetEase(Ease.OutQuint);
            transform.DOLocalRotate(Vector3.zero, 0.5f).SetEase(Ease.OutQuint);

            AudioManager.Instance.PlaySFX(AudioID.Pick);
            player.StateManager.PushState(PlayerState.Holding);
            UIManager.Instance.InteractMessage.Hide();
        }

        public virtual void OnFocused()
        {
            UIManager.Instance.InteractMessage.Display("Tap to pick up this box");
        }

        public virtual void OnDefocused()
        {
            UIManager.Instance.InteractMessage.Hide();
        }

        public virtual void Throw()
        {
            var center = transform.position;
            var extents = boxCollider.size / 2f;
            var orientation = transform.rotation;
            var mask = ~GameConfig.Instance.PlayerLayer;

            if (Physics.OverlapBox(center, extents, orientation, mask).Length > 0)
            {
                UIManager.Instance.Message.Log("Can't throw object here!", Color.red);
                return;
            }

            if (disablePhysicsRoutine != null)
                StopCoroutine(disablePhysicsRoutine);

            DOTween.Kill(transform);
            transform.SetParent(null);

            SetActivePhysics(true);
            body.AddForce(transform.forward * 3.5f, ForceMode.Impulse);
            StartCoroutine(StartCollisionCheck());

            AudioManager.Instance.PlaySFX(AudioID.Throw);

            foreach (Transform child in transform)
                child.gameObject.layer = LayerMask.NameToLayer("Default");

            UIManager.Instance.ToggleActionUI(ActionType.Throw, false, null);
            UIManager.Instance.ToggleActionUI(ActionType.Open, false, null);
            UIManager.Instance.ToggleActionUI(ActionType.Close, false, null);
            UIManager.Instance.ToggleActionUI(ActionType.Place, false, null);

            player.StateManager.PopState();
            player = null;
            IsDisposable = true;
        }

        public void SetActivePhysics(bool value)
        {
            body.isKinematic = !value;
            boxCollider.enabled = value;
        }

        protected virtual IEnumerator DisablePhysicsDelayed()
        {
            yield return new WaitForSeconds(0.2f);
            SetActivePhysics(false);
        }

        protected virtual IEnumerator StartCollisionCheck()
        {
            float timer = collisionCheckDuration;
            IsCheckingCollision = true;

            while (timer > 0f)
            {
                timer -= Time.deltaTime;
                yield return null;
            }

            IsCheckingCollision = false;
        }

        protected void AnimateLidsOpen(System.Action onComplete = null)
        {
            if (lidSequence.IsActive()) return;

            lidSequence = DOTween.Sequence()
                .Append(lidFront.DOLocalRotate(Vector3.right * 250f, 0.3f, RotateMode.LocalAxisAdd))
                .Join(lidBack.DOLocalRotate(Vector3.left * 250f, 0.3f, RotateMode.LocalAxisAdd))
                .InsertCallback(0f, () => AudioManager.Instance.PlaySFX(AudioID.Flip))
                .Append(lidLeft.DOLocalRotate(Vector3.back * 250f, 0.3f, RotateMode.LocalAxisAdd))
                .Join(lidRight.DOLocalRotate(Vector3.forward * 250f, 0.3f, RotateMode.LocalAxisAdd))
                .InsertCallback(0.3f, () => AudioManager.Instance.PlaySFX(AudioID.Flip))
                .OnComplete(() => onComplete?.Invoke());
        }

        protected void AnimateLidsClose()
        {
            if (lidSequence.IsActive()) return;

            lidSequence = DOTween.Sequence()
                .Append(lidLeft.DOLocalRotate(Vector3.forward * 250f, 0.3f, RotateMode.LocalAxisAdd))
                .Join(lidRight.DOLocalRotate(Vector3.back * 250f, 0.3f, RotateMode.LocalAxisAdd))
                .InsertCallback(0f, () => AudioManager.Instance.PlaySFX(AudioID.Flip))
                .Append(lidFront.DOLocalRotate(Vector3.left * 250f, 0.3f, RotateMode.LocalAxisAdd))
                .Join(lidBack.DOLocalRotate(Vector3.right * 250f, 0.3f, RotateMode.LocalAxisAdd))
                .InsertCallback(0.3f, () => AudioManager.Instance.PlaySFX(AudioID.Flip));
        }
    }
}
