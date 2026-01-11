using UnityEngine;
using Cinemachine;
using DG.Tweening;

namespace CryingSnow.CheckoutFrenzy
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(AudioSource))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField, Tooltip("Movement speed of the player.")]
        private float movingSpeed = 7.5f;

        [SerializeField, Tooltip("Gravity applied to the player.")]
        private float gravity = -9.81f;

        [SerializeField, Tooltip("Rotation speed of the player's view.")]
        private float lookSpeed = 2.0f;

        [SerializeField, Tooltip("Maximum angle the player can look up or down.")]
        private float lookXLimit = 45.0f;

        [SerializeField, Tooltip("Maximum distance for interaction.")]
        private float interactDistance = 3.5f;

        [SerializeField, Tooltip("Time in seconds the interact button must be held to trigger an interaction.")]
        private float interactHoldThreshold = 1.0f;

        [SerializeField, Tooltip("Transform representing the player's holding point (hands).")]
        private Transform holdPoint;

        [SerializeField, Tooltip("Transform representing the player's camera.")]
        private Transform playerCamera;

        [Header("Sway Settings")]
        [SerializeField, Tooltip("Amount of sway applied to the camera.")]
        private float swayAmount = 0.05f;

        [SerializeField, Tooltip("Speed of the camera sway.")]
        private float swaySpeed = 5.0f;

        [SerializeField, Tooltip("Maximum amount of sway applied to the camera.")]
        private float maxSwayAmount = 0.2f;

        [Header("Bobbing Settings")]
        [SerializeField, Tooltip("Frequency of the camera bobbing effect.")]
        private float bobFrequency = 10.0f;

        [SerializeField, Tooltip("Horizontal amplitude of the camera bobbing effect.")]
        private float bobHorizontalAmplitude = 0.04f;

        [SerializeField, Tooltip("Vertical amplitude of the camera bobbing effect.")]
        private float bobVerticalAmplitude = 0.04f;

        [SerializeField, Tooltip("Smoothing applied to the camera bobbing effect.")]
        private float bobSmoothing = 8f;

        [Header("Sound Effects")]
        [SerializeField, Tooltip("Array of audio clips used for footstep sounds.")]
        private AudioClip[] footstepClips;

        [SerializeField, Tooltip("Distance traveled before playing a footstep sound.")]
        private float stepDistance = 2.0f;

        public Transform HoldPoint => holdPoint;
        public PlayerStateManager StateManager { get; private set; }

        private Camera mainCamera;
        private CharacterController controller;
        private Vector3 movement;
        private Vector3 playerVelocity = Vector3.zero;
        private float rotationX = 0;

        private AudioSource audioSource;
        private CinemachineVirtualCamera playerVirtualCam;

        private IInteractable lastInteractable;
        private Shelf lastShelf;
        private Rack lastRack;

        private Vector3 holdPointOrigin;
        private float bobTimer = 0.0f;
        private float interactHoldDuration = 0f;
        private float distanceTraveled;

        private void Awake()
        {
            StateManager = new PlayerStateManager(this);
            controller = GetComponent<CharacterController>();
            audioSource = GetComponent<AudioSource>();
            playerVirtualCam = GetComponentInChildren<CinemachineVirtualCamera>();
            holdPointOrigin = holdPoint.localPosition;
        }

        private void Start()
        {
            mainCamera = Camera.main;

            // PC İÇİN FARE AYARLARI
            Cursor.lockState = CursorLockMode.Locked; // Fareyi ekrana kilitler
            Cursor.visible = false;                   // Fareyi gizler
        }

        private void Update()
        {
            HandleMovement();
            HandleSway();
            HandleBobbing();
            HandleFootsteps();

            switch (StateManager.CurrentState)
            {
                case PlayerState.Free:
                    DetectInteractable();
                    DetectShelfToCustomize();
                    DetectRack();
                    break;

                case PlayerState.Holding:
                    DetectShelfToRestock();
                    DetectRack();
                    break;

                case PlayerState.Working:
                    Work();
                    break;
            }

            // ESC tuşuyla fareyi serbest bırakma (Menü geçişleri için kolaylık)
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        private void HandleMovement()
        {
            if (controller.isGrounded && playerVelocity.y < 0)
            {
                playerVelocity.y = 0f;
            }

            Vector3 forward = transform.TransformDirection(Vector3.forward);
            Vector3 right = transform.TransformDirection(Vector3.right);

            // SimpleInput yerine standart Input kullanıldı
            float curSpeedX = !IsMovementBlocked() ? movingSpeed * Input.GetAxis("Vertical") : 0f;
            float curSpeedY = !IsMovementBlocked() ? movingSpeed * Input.GetAxis("Horizontal") : 0f;

            movement = (forward * curSpeedX) + (right * curSpeedY);
            controller.Move(movement * Time.deltaTime);

            playerVelocity.y += gravity * Time.deltaTime;
            controller.Move(playerVelocity * Time.deltaTime);

            if (!IsMovementBlocked())
            {
                // Standart Mouse X/Y kullanıldı
                rotationX += -Input.GetAxis("Mouse Y") * lookSpeed;
                rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
                playerCamera.localRotation = Quaternion.Euler(rotationX, 0, 0);
                transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * lookSpeed, 0);
            }
        }

        private bool IsMovementBlocked()
        {
            return StateManager.CurrentState is PlayerState.Working or PlayerState.Busy or PlayerState.Paused;
        }

        private void HandleSway()
        {
            float lookX = Input.GetAxis("Mouse X");
            float lookY = Input.GetAxis("Mouse Y");

            Vector3 targetPosition = new Vector3(-lookX, -lookY, 0) * swayAmount;
            targetPosition.x = Mathf.Clamp(targetPosition.x, -maxSwayAmount, maxSwayAmount);
            targetPosition.y = Mathf.Clamp(targetPosition.y, -maxSwayAmount, maxSwayAmount);

            holdPoint.localPosition = Vector3.Lerp(holdPoint.localPosition, holdPointOrigin + targetPosition, Time.deltaTime * swaySpeed);
        }

        private void HandleBobbing()
        {
            if (movement.magnitude > 0.1f)
            {
                bobTimer += Time.deltaTime * bobFrequency;
                float horizontalOffset = Mathf.Sin(bobTimer) * bobHorizontalAmplitude;
                float verticalOffset = Mathf.Cos(bobTimer * 2) * bobVerticalAmplitude;
                Vector3 bobPosition = new Vector3(horizontalOffset, verticalOffset, 0);
                holdPoint.localPosition = Vector3.Lerp(holdPoint.localPosition, holdPointOrigin + bobPosition, Time.deltaTime * bobSmoothing);
            }
            else
            {
                holdPoint.localPosition = Vector3.Lerp(holdPoint.localPosition, holdPointOrigin, Time.deltaTime * bobSmoothing);
            }
        }

        private void HandleFootsteps()
        {
            if (movement.magnitude < 0.1f) return;
            distanceTraveled += movement.magnitude * Time.deltaTime;
            if (distanceTraveled >= stepDistance)
            {
                PlayFootstepSound();
                distanceTraveled = 0f;
            }
        }

        private void PlayFootstepSound()
        {
            if (footstepClips.Length == 0) return;
            AudioClip clip = footstepClips[Random.Range(0, footstepClips.Length)];
            audioSource.PlayOneShot(clip);
        }

        public void SetInteractable(IInteractable interactable)
        {
            lastInteractable = interactable;
            InteractWithCurrent();
        }

        private void DetectInteractable()
        {
            Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

            if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, GameConfig.Instance.InteractableLayer))
            {
                IInteractable interactable = hit.transform.GetComponent<IInteractable>();
                if (interactable != lastInteractable)
                {
                    lastInteractable?.OnDefocused();
                    UIManager.Instance.ToggleInteractButton(true);
                    lastInteractable = interactable;
                    lastInteractable.OnFocused();
                    interactHoldDuration = 0f;
                    UIManager.Instance.UpdateHoldProgress(0f);
                }
            }
            else if (lastInteractable != null)
            {
                UIManager.Instance.ToggleInteractButton(false);
                lastInteractable.OnDefocused();
                lastInteractable = null;
                interactHoldDuration = 0f;
                UIManager.Instance.UpdateHoldProgress(0f);
            }

            if (lastInteractable != null)
            {
                if (lastInteractable is Furniture)
                {
                    // MOBİL BUTON YERİNE 'E' TUŞUNA BASILI TUTMA
                    if (Input.GetKey(KeyCode.E))
                    {
                        interactHoldDuration += Time.deltaTime;
                        UIManager.Instance.UpdateHoldProgress(interactHoldDuration / interactHoldThreshold);

                        if (interactHoldDuration >= interactHoldThreshold)
                        {
                            InteractWithCurrent();
                            interactHoldDuration = 0f;
                            UIManager.Instance.UpdateHoldProgress(0f);
                        }
                    }
                    else
                    {
                        interactHoldDuration = 0f;
                        UIManager.Instance.UpdateHoldProgress(0f);
                    }
                }
                else if (Input.GetKeyDown(KeyCode.E)) // DİREKT 'E' TUŞUNA BASMA
                {
                    InteractWithCurrent();
                }
            }
        }

        private void InteractWithCurrent()
        {
            lastInteractable?.Interact(this);
            UIManager.Instance.ToggleInteractButton(false);
        }

        private void DetectShelfToCustomize()
        {
            Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

            if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, GameConfig.Instance.ShelfLayer))
            {
                Shelf detectedShelf = hit.transform.GetComponent<Shelf>();
                if (detectedShelf != lastShelf)
                {
                    UIManager.Instance.ToggleActionUI(ActionType.Price, false, null);
                    UIManager.Instance.ToggleActionUI(ActionType.Label, false, null);

                    if (detectedShelf?.Product != null)
                    {
                        UIManager.Instance.ToggleActionUI(ActionType.Price, true, () =>
                        {
                            UIManager.Instance.ToggleActionUI(ActionType.Price, false, null);
                            StateManager.PushState(PlayerState.Busy);
                            Cursor.lockState = CursorLockMode.None; // Panel açılınca fareyi serbest bırak
                            Cursor.visible = true;

                            var priceCustomizer = UIManager.Instance.PriceCustomizer;
                            priceCustomizer.OnClose.RemoveAllListeners();
                            priceCustomizer.OnClose.AddListener(() =>
                            {
                                StateManager.PopState();
                                Cursor.lockState = CursorLockMode.Locked; // Panel kapanınca fareyi kilitle
                                Cursor.visible = false;
                            });

                            priceCustomizer.Open(detectedShelf.Product);
                            if (!detectedShelf.ShelvingUnit.IsOpen) detectedShelf.ShelvingUnit.Open(true, true);
                            detectedShelf.ShelvingUnit.OnDefocused();
                        });
                    }
                    else
                    {
                        UIManager.Instance.ToggleActionUI(ActionType.Label, true, () =>
                        {
                            UIManager.Instance.ToggleActionUI(ActionType.Label, false, null);
                            StateManager.PushState(PlayerState.Busy);
                            Cursor.lockState = CursorLockMode.None;
                            Cursor.visible = true;

                            var labelCustomizer = UIManager.Instance.LabelCustomizer;
                            labelCustomizer.OnClose.RemoveAllListeners();
                            labelCustomizer.OnClose.AddListener(() =>
                            {
                                StateManager.PopState();
                                Cursor.lockState = CursorLockMode.Locked;
                                Cursor.visible = false;
                            });

                            labelCustomizer.Open(detectedShelf);
                            detectedShelf.ShelvingUnit.OnDefocused();
                        });
                    }
                    lastShelf = detectedShelf;
                }
            }
            else if (lastShelf != null)
            {
                UIManager.Instance.ToggleActionUI(ActionType.Price, false, null);
                UIManager.Instance.ToggleActionUI(ActionType.Label, false, null);
                lastShelf = null;
            }
        }

        private void DetectShelfToRestock()
        {
            Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            Box box = lastInteractable as Box;

            if (box != null && box.IsOpen && Physics.Raycast(ray, out RaycastHit hit, interactDistance, GameConfig.Instance.ShelfLayer))
            {
                Shelf detectedShelf = hit.transform.GetComponent<Shelf>();
                if (detectedShelf != lastShelf)
                {
                    if (box.Quantity > 0)
                        UIManager.Instance.ToggleActionUI(ActionType.Place, true, () =>
                        {
                            bool placed = box.Place(detectedShelf);
                            if (placed && !detectedShelf.ShelvingUnit.IsOpen)
                                detectedShelf.ShelvingUnit.Open(true, true);
                        });

                    if (detectedShelf.Quantity > 0)
                        UIManager.Instance.ToggleActionUI(ActionType.Take, true, () =>
                        {
                            bool taken = box.Take(detectedShelf);
                            if (taken && !detectedShelf.ShelvingUnit.IsOpen)
                                detectedShelf.ShelvingUnit.Open(true, true);
                        });
                    else
                        UIManager.Instance.ToggleActionUI(ActionType.Take, false, null);

                    lastShelf = detectedShelf;
                }
            }
            else if (lastShelf != null)
            {
                UIManager.Instance.ToggleActionUI(ActionType.Place, false, null);
                UIManager.Instance.ToggleActionUI(ActionType.Take, false, null);
                lastShelf = null;
            }
        }

        private void DetectRack()
        {
            Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

            if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, GameConfig.Instance.RackLayer))
            {
                Rack detectedRack = hit.transform.GetComponent<Rack>();
                if (detectedRack != lastRack)
                {
                    Box box = lastInteractable as Box;
                    if (box != null && box.Quantity > 0)
                    {
                        UIManager.Instance.ToggleActionUI(ActionType.Place, true, () =>
                        {
                            box.Store(detectedRack, true);
                        });
                    }
                    else if (box == null && detectedRack.BoxQuantity > 0)
                    {
                        UIManager.Instance.ToggleActionUI(ActionType.Take, true, () =>
                        {
                            detectedRack.RetrieveBox(this);
                            UIManager.Instance.ToggleActionUI(ActionType.Take, false, null);
                        });
                    }
                    else
                    {
                        UIManager.Instance.ToggleActionUI(ActionType.Take, false, null);
                    }
                    lastRack = detectedRack;
                }
            }
            else if (lastRack != null)
            {
                if (StateManager.CurrentState != PlayerState.Moving)
                    UIManager.Instance.ToggleActionUI(ActionType.Place, false, null);

                UIManager.Instance.ToggleActionUI(ActionType.Take, false, null);
                lastRack = null;
            }
        }

        private void Work()
        {
            // Kasadayken tıklama ve etkileşim
            if (Input.GetMouseButtonDown(0) && lastInteractable is CheckoutCounter counter)
            {
                if (counter.CurrentState == CheckoutCounter.State.Placing) return;

                Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, GameConfig.Instance.CheckoutItemLayer))
                {
                    if (hit.transform.TryGetComponent<CheckoutItem>(out CheckoutItem item))
                    {
                        item.Scan();
                    }
                }
            }
        }

        public Vector3 GetFrontPosition()
        {
            float pitch = mainCamera.transform.localEulerAngles.x;
            if (pitch > 180) pitch -= 360;
            float normalizedPitch = Mathf.InverseLerp(lookXLimit, 0f, pitch);
            float minDistance = 1.5f;
            float maxDistance = 3f;
            float offset = Mathf.Lerp(minDistance, maxDistance, normalizedPitch);
            Vector3 front = transform.TransformPoint(Vector3.forward * offset);
            return new Vector3(front.x, 0f, front.z).FloorToTenth();
        }

        public void SetFOVSmooth(float targetFOV, float duration = 0.5f)
        {
            DOTween.To(
                () => playerVirtualCam.m_Lens.FieldOfView,
                fov => playerVirtualCam.m_Lens.FieldOfView = fov,
                targetFOV,
                duration
            ).SetEase(Ease.InOutSine);
        }
    }
}