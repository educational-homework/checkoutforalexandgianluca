using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CryingSnow.CheckoutFrenzy
{
    public class Cleanable : MonoBehaviour, IInteractable
    {
        [Tooltip("Unique identifier of this Cleanable.")]
        [SerializeField] private int cleanableId;

        [Tooltip("Prefab used to visually represent each unit of mess (e.g., dirt, stains, leaves).")]
        [SerializeField] private GameObject messVisualPrefab;

        [Tooltip("Total number of mess visuals to spawn for this cleanable.")]
        [SerializeField] private int totalMessUnits = 10;

        [Tooltip("Radius around this object where mess visuals will be scattered.")]
        [SerializeField] private float scatterRadius = 0.5f;

        [Tooltip("Number of interactions (sweeps, mops, etc.) required to fully clean this.")]
        [SerializeField] private int interactionsToClean = 3;

        [Tooltip("The type of the cleaning tool used to clean this cleanable.")]
        [SerializeField] private CleaningToolType toolType;

        public CleaningToolType ToolType => toolType;
        public bool HasCleaner => activeCleaner != null;
        public int CleanableID => cleanableId;

        private readonly List<GameObject> messInstances = new();
        private int currentInteractionCount;
        private ParticleSystem messParticle;
        private AudioSource audioSource;
        private Cleaner activeCleaner;
        private PlayerController player;

        private void Awake()
        {
            gameObject.layer = GameConfig.Instance.InteractableLayer.ToSingleLayer();

            messParticle = GetComponentInChildren<ParticleSystem>();
            audioSource = GetComponentInChildren<AudioSource>();

            var sphereCollider = gameObject.AddComponent<SphereCollider>();
            sphereCollider.isTrigger = true;
            sphereCollider.radius = scatterRadius;

            SpawnMessVisuals();
        }

        private void Start()
        {
            StoreManager.Instance.RegisterCleanable(this);
            DataManager.Instance.OnSave += HandleSave;
        }

        private void SpawnMessVisuals()
        {
            for (int i = 0; i < totalMessUnits; i++)
            {
                Vector2 offset = Random.insideUnitCircle * scatterRadius;
                Vector3 pos = transform.position + new Vector3(offset.x, 0.01f, offset.y);
                Quaternion rot = Quaternion.Euler(90f, Random.Range(0f, 360f), 0f);
                var mess = Instantiate(messVisualPrefab, pos, rot, transform);
                messInstances.Add(mess);
            }
        }

        public void Interact(PlayerController player)
        {
            if (HasCleaner)
            {
                UIManager.Instance.InteractMessage.Hide();
                return;
            }

            activeCleaner = player.GetComponentInChildren<Cleaner>();

            this.player = player;
            player.StateManager.PushState(PlayerState.Working);
            player.SetFOVSmooth(40f);
            UIManager.Instance.InteractMessage.Hide();

            activeCleaner.StartCleaning(this, HandleCleaningStep);
        }

        public void StartCleaningStep(Cleaner cleaner)
        {
            activeCleaner = cleaner;
            activeCleaner.StartCleaning(this, HandleCleaningStep);
        }

        private void HandleCleaningStep()
        {
            if (currentInteractionCount >= interactionsToClean) return;

            currentInteractionCount++;
            messParticle?.Clear(true);
            messParticle?.Play();
            audioSource?.Play();

            int expectedDisabled = Mathf.FloorToInt(((float)currentInteractionCount / interactionsToClean) * totalMessUnits);
            int currentlyDisabled = messInstances.Count(m => !m.activeSelf);
            int toDisableNow = expectedDisabled - currentlyDisabled;

            for (int i = 0; i < messInstances.Count && toDisableNow > 0; i++)
            {
                if (messInstances[i].activeSelf)
                {
                    messInstances[i].SetActive(false);
                    toDisableNow--;
                }
            }

            if (currentInteractionCount >= interactionsToClean)
            {
                activeCleaner.StopCleaning();

                if (player != null)
                {
                    player.StateManager.PopState();
                    player.SetFOVSmooth(60f);
                    player = null;
                }

                StoreManager.Instance.UnregisterCleanable(this);
                DataManager.Instance.OnSave -= HandleSave;

                Destroy(gameObject, 1f);
            }
        }

        private void HandleSave()
        {
            var cleanableData = new CleanableData(this);
            DataManager.Instance.Data.SavedCleanables.Add(cleanableData);
        }

        public void OnFocused()
        {
            UIManager.Instance.InteractMessage.Display(
                "Tap to clean the mess"
            );
        }

        public void OnDefocused()
        {
            UIManager.Instance.InteractMessage.Hide();
        }
    }
}
