using System.Collections;
using UnityEngine;
using DG.Tweening;
using Unity.AI.Navigation;

namespace CryingSnow.CheckoutFrenzy
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(BoxCollider))]
    public class FurnitureBox : MonoBehaviour, IInteractable
    {
        public int furnitureId;

        [Header("Box Lids")]
        [SerializeField, Tooltip("Reference to the bone transform of the front lid of the box.")]
        private Transform lidFront;

        [SerializeField, Tooltip("Reference to the bone transform of the back lid of the box.")]
        private Transform lidBack;

        [SerializeField, Tooltip("Reference to the bone transform of the left lid of the box.")]
        private Transform lidLeft;

        [SerializeField, Tooltip("Reference to the bone transform of the right lid of the box.")]
        private Transform lidRight;

        [Header("Sound Settings")]
        [SerializeField, Tooltip("Duration (in seconds) to check for collisions after throwing the box.")]
        private float collisionCheckDuration = 3f;

        public bool IsDisposable { get; private set; }

        private bool isCheckingCollision;

        private Rigidbody body;
        private BoxCollider boxCollider;
        private PlayerController player;
        private Sequence lidSequence;
        private Coroutine disablePhysicsRoutine;

        private void Awake()
        {
            gameObject.layer = GameConfig.Instance.InteractableLayer.ToSingleLayer();

            body = GetComponent<Rigidbody>();
            boxCollider = GetComponent<BoxCollider>();

            SetActivePhysics(false);

            var navMeshMod = gameObject.AddComponent<NavMeshModifier>();
            navMeshMod.ignoreFromBuild = true;
        }

        private IEnumerator Start()
        {
            DataManager.Instance.OnSave += HandleOnSave;

            yield return new WaitUntil(() => DataManager.Instance.IsLoaded);

            SetActivePhysics(true);
        }

        private void OnDestroy()
        {
            if (DataManager.Instance != null)
            {
                DataManager.Instance.OnSave -= HandleOnSave;
            }
        }

        private void HandleOnSave()
        {
            var furnitureBoxData = new FurnitureBoxData(this);
            DataManager.Instance.Data.SavedFurnitureBoxes.Add(furnitureBoxData);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!isCheckingCollision) return;

            if (collision.relativeVelocity.magnitude > 2)
            {
                AudioManager.Instance.PlaySFX(AudioID.Impact);
            }
        }

        public void Interact(PlayerController player)
        {
            this.player = player;

            IsDisposable = false;

            if (disablePhysicsRoutine != null)
            {
                StopCoroutine(disablePhysicsRoutine);
            }

            disablePhysicsRoutine = StartCoroutine(DisablePhysicsDelayed());

            foreach (Transform child in transform)
            {
                child.gameObject.layer = GameConfig.Instance.HeldObjectLayer.ToSingleLayer();
            }

            UIManager.Instance.ToggleActionUI(ActionType.Throw, true, Throw);
            UIManager.Instance.ToggleActionUI(ActionType.Open, true, Open);

            UIManager.Instance.HideBoxInfo();

            transform.SetParent(player.HoldPoint);
            transform.DOLocalMove(Vector3.zero, 0.5f).SetEase(Ease.OutQuint);
            transform.DOLocalRotate(Vector3.zero, 0.5f).SetEase(Ease.OutQuint);

            AudioManager.Instance.PlaySFX(AudioID.Pick);

            player.StateManager.PushState(PlayerState.Moving);

            UIManager.Instance.InteractMessage.Hide();
        }

        public void OnFocused()
        {
            UIManager.Instance.DisplayBoxInfo(this);

            string message = "Tap to pick up this box";
            UIManager.Instance.InteractMessage.Display(message);
        }

        public void OnDefocused()
        {
            UIManager.Instance.HideBoxInfo();
            UIManager.Instance.InteractMessage.Hide();
        }

        public void SetActivePhysics(bool value)
        {
            body.isKinematic = !value;
            boxCollider.enabled = value;
        }

        private IEnumerator DisablePhysicsDelayed()
        {
            yield return new WaitForSeconds(0.2f);

            SetActivePhysics(false);
        }

        private void Throw()
        {
            var center = transform.position;
            var extents = boxCollider.size / 2f;
            var orientation = transform.rotation;
            var layerMask = ~GameConfig.Instance.PlayerLayer;

            if (Physics.OverlapBox(center, extents, orientation, layerMask).Length > 0)
            {
                UIManager.Instance.Message.Log("Can't throw object here!", Color.red);
                return;
            }

            if (disablePhysicsRoutine != null)
            {
                StopCoroutine(disablePhysicsRoutine);
                disablePhysicsRoutine = null;
            }

            DOTween.Kill(transform);

            transform.SetParent(null);

            SetActivePhysics(true);
            body.AddForce(transform.forward * 3.5f, ForceMode.Impulse);

            StartCoroutine(StartCollisionCheck());

            AudioManager.Instance.PlaySFX(AudioID.Throw);

            foreach (Transform child in transform)
            {
                child.gameObject.layer = LayerMask.NameToLayer("Default");
            }

            UIManager.Instance.ToggleActionUI(ActionType.Throw, false, null);
            UIManager.Instance.ToggleActionUI(ActionType.Open, false, null);
            UIManager.Instance.ToggleActionUI(ActionType.Close, false, null);
            UIManager.Instance.ToggleActionUI(ActionType.Place, false, null);

            player.StateManager.PopState();

            player = null;

            IsDisposable = true;
        }

        private IEnumerator StartCollisionCheck()
        {
            float timer = collisionCheckDuration;
            isCheckingCollision = true;

            while (timer > 0f)
            {
                timer -= Time.deltaTime;
                yield return null;
            }

            isCheckingCollision = false;
        }

        private void Open()
        {
            if (lidSequence.IsActive()) return;

            UIManager.Instance.ToggleActionUI(ActionType.Open, false, null);
            UIManager.Instance.ToggleActionUI(ActionType.Throw, false, null);

            lidSequence = DOTween.Sequence();

            lidSequence.Append(lidFront.DOLocalRotate(Vector3.right * 250f, 0.3f, RotateMode.LocalAxisAdd))
                .Join(lidBack.DOLocalRotate(Vector3.left * 250f, 0.3f, RotateMode.LocalAxisAdd))
                .InsertCallback(0f, () => AudioManager.Instance.PlaySFX(AudioID.Flip))
                .Append(lidLeft.DOLocalRotate(Vector3.back * 250f, 0.3f, RotateMode.LocalAxisAdd))
                .Join(lidRight.DOLocalRotate(Vector3.forward * 250f, 0.3f, RotateMode.LocalAxisAdd))
                .InsertCallback(0.3f, () => AudioManager.Instance.PlaySFX(AudioID.Flip))
                .OnComplete(() => SpawnFurniture());
        }

        private void SpawnFurniture()
        {
            var furniturePrefab = DataManager.Instance.GetFurnitureById(furnitureId);

            if (furniturePrefab == null)
            {
                Debug.LogWarning("The Furniture ID is invalid!");
                return;
            }

            player.StateManager.PopState();

            var furniture = Instantiate(furniturePrefab, player.GetFrontPosition(), Quaternion.identity);
            furniture.Interact(player);

            Destroy(gameObject);
        }
    }
}
