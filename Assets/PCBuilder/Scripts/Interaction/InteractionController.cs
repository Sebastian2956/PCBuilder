using UnityEngine;
using UnityEngine.InputSystem;
using PCBuilder.Core;
using PCBuilder.Interaction;

public class InteractionController : MonoBehaviour
{
    [Header("Raycast Configurations")]
    [Tooltip("Which layers should receive hover/select raycasts.")]
    [SerializeField] private LayerMask interactableLayer = ~0;
    [Tooltip("Maximum length of interaction raycast from camera.")]
    [SerializeField] private float maxRaycastDistance = 100f;

    private Camera mainCamera;
    private SelectableObject currentHovered;
    private DraggablePart heldPart;
    private SelectableObject heldSelectable;

    private InputAction pointAction;
    private InputAction clickAction;
    private InputAction cancelAction;

    public static InteractionController Instance { get; private set; }
    public bool IsHoldingObject => heldPart != null;

    private void Awake()
    {
        Instance = this;
        mainCamera = Camera.main;

        // Automatically fetch preloaded actions from Project-Wide Actions if configured
        if (InputSystem.actions != null)
        {
            pointAction = InputSystem.actions.FindAction("Point");
            clickAction = InputSystem.actions.FindAction("Click");
            cancelAction = InputSystem.actions.FindAction("Cancel");
        }
    }

    private void Update()
    {
        // 1. Gather input from Actions or Fallback API safely
        Vector2 mousePos = GetMousePosition();
        bool clickPressed = IsClickPressedThisFrame();
        bool clickReleased = IsClickReleasedThisFrame();
        bool cancelPressed = IsCancelPressedThisFrame();

        // 2. State-driven interaction loop
        if (heldPart != null)
        {
            // Object is held: listen for cancellation keys (Escape) or mouse release
            if (cancelPressed)
            {
                heldPart.Cancel();
                heldSelectable.Deselect();
                heldPart = null;
                heldSelectable = null;
            }
            else if (clickReleased)
            {
                HandleRelease();
            }
        }
        else
        {
            // No object held: raycast to find and hover/select parts under mouse pointer
            PerformRaycast(mousePos, clickPressed);
        }
    }

    private void HandleRelease()
    {
        if (heldPart == null) return;

        InstallablePart installable = heldPart.GetComponent<InstallablePart>();
        bool didInstall = false;

        if (installable != null)
        {
            // Find all slots to see if any are currently overlapping with this part
            var slots = UnityEngine.Object.FindObjectsByType<InstallationSlot>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var slot in slots)
            {
                if (slot.OverlappingPart == installable)
                {
                    // Attempt installation!
                    string feedback;
                    // First validate with Procedure system
                    bool isValidStep = ProcedureRunner.Instance.ValidateAction(
                        ProcedureActionType.InstallPart,
                        installable.ComponentId,
                        slot.SlotId,
                        out feedback
                    );

                    if (isValidStep)
                    {
                        // Proceed to install
                        didInstall = slot.TryInstall(installable, out feedback);
                        ProcedureRunner.Instance.TriggerFeedback(feedback, false);
                    }
                    else
                    {
                        // Invalid procedure step (out of order, or wrong slot)
                        ProcedureRunner.Instance.TriggerFeedback(feedback, true);
                        // Also trigger visual rejection
                        slot.TryInstall(installable, out string dumpFeedback); // Plays fail sound
                    }
                    break;
                }
            }
        }

        if (didInstall)
        {
            heldSelectable.Deselect();
            heldPart = null;
            heldSelectable = null;
        }
        else
        {
            // Default return to start behavior if release was not a successful installation
            heldPart.Release();
            heldSelectable.Deselect();
            heldPart = null;
            heldSelectable = null;
        }

        // Clear penalized actions hashset for the next drag event
        ScoringService.Instance.ClearPenaltiesForNewDrag();
    }

    private void PerformRaycast(Vector2 mousePosition, bool clickPressed)
    {
        Ray ray = mainCamera.ScreenPointToRay(mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxRaycastDistance, interactableLayer))
        {
            // Traverse up to find the root selectable component if any
            SelectableObject selectable = hit.collider.GetComponentInParent<SelectableObject>();

            if (selectable != null)
            {
                // Hover management
                if (currentHovered != selectable)
                {
                    if (currentHovered != null)
                    {
                        currentHovered.HoverExit();
                    }
                    currentHovered = selectable;
                    currentHovered.HoverEnter();
                }

                // Click pickup or click trigger management
                if (clickPressed)
                {
                    DraggablePart draggable = selectable.GetComponent<DraggablePart>();
                    if (draggable != null)
                    {
                        // Pick up draggable object
                        heldPart = draggable;
                        heldSelectable = selectable;

                        heldSelectable.Select();
                        heldPart.PickUp();

                        // Exit hover visual state since we are now holding it
                        currentHovered.HoverExit();
                        currentHovered = null;
                    }
                    else
                    {
                        // Non-draggable clicked (like a slot retaining clip!)
                        var clip = selectable.GetComponent<RamSlotClip>();
                        if (clip != null)
                        {
                            // Find parent slot to know which Slot ID this clip belongs to
                            var slot = selectable.GetComponentInParent<InstallationSlot>();
                            if (slot != null)
                            {
                                // Validate action before permitting clip interaction
                                string feedback;
                                bool isValid = ProcedureRunner.Instance.ValidateAction(
                                    ProcedureActionType.OpenClip,
                                    slot.SlotId,
                                    slot.SlotId,
                                    out feedback
                                );

                                if (isValid)
                                {
                                    selectable.Select(); // Toggles clip automatically
                                    ProcedureRunner.Instance.TriggerFeedback(feedback, false);
                                }
                                else
                                {
                                    ProcedureRunner.Instance.TriggerFeedback(feedback, true);
                                    MaintSimAudio.PlaySound("Reject");
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                ClearHover();
            }
        }
        else
        {
            ClearHover();
        }
    }

    private void ClearHover()
    {
        if (currentHovered != null)
        {
            currentHovered.HoverExit();
            currentHovered = null;
        }
    }

    private Vector2 GetMousePosition()
    {
        if (pointAction != null)
        {
            return pointAction.ReadValue<Vector2>();
        }
        return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
    }

    private bool IsClickPressedThisFrame()
    {
        if (clickAction != null)
        {
            return clickAction.WasPressedThisFrame();
        }
        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
    }

    private bool IsClickReleasedThisFrame()
    {
        if (clickAction != null)
        {
            return clickAction.WasReleasedThisFrame();
        }
        return Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame;
    }

    private bool IsCancelPressedThisFrame()
    {
        if (cancelAction != null)
        {
            return cancelAction.WasPressedThisFrame();
        }
        return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
    }
}