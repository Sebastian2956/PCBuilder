using UnityEngine;

public class DraggablePart : MonoBehaviour
{
    public enum DragState
    {
        Idle,
        Dragging,
        Returning
    }

    [Header("Drag Movement")]
    [Tooltip("How fast the part slides towards the screen-space mouse cursor location.")]
    [SerializeField] private float dragSpeed = 15f;
    [Tooltip("How fast the part lerps back to its original position/rotation upon release.")]
    [SerializeField] private float returnSpeed = 10f;
    [Tooltip("Distance from camera to maintain while dragging. If set to 0, it auto-calculates current distance at the point of click.")]
    [SerializeField] private float dragDistance = 0f;

    [Header("Rotation Settings")]
    [Tooltip("Speed of rotation around local Y-axis using Q and E keys.")]
    [SerializeField] private float rotationSpeed = 120f;

    private DragState currentState = DragState.Idle;
    private Vector3 startPosition;
    private Quaternion startRotation;
    private float currentDragDistance;
    private Camera mainCamera;

    public DragState CurrentState => currentState;
    public bool IsDragging => currentState == DragState.Dragging;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (currentState == DragState.Dragging)
        {
            HandleDragging();
        }
        else if (currentState == DragState.Returning)
        {
            HandleReturning();
        }
    }

    public void PickUp()
    {
        if (currentState == DragState.Dragging) return;

        // Save origin coordinates for cancellation or return fallback
        if (currentState == DragState.Idle)
        {
            startPosition = transform.position;
            startRotation = transform.rotation;
        }

        // Auto-calculate appropriate camera depth if set to auto-detect
        if (dragDistance <= 0f)
        {
            currentDragDistance = Vector3.Distance(mainCamera.transform.position, transform.position);
        }
        else
        {
            currentDragDistance = dragDistance;
        }

        currentState = DragState.Dragging;
    }

    public void Release()
    {
        if (currentState != DragState.Dragging) return;
        currentState = DragState.Returning;
    }

    public void Cancel()
    {
        if (currentState != DragState.Dragging) return;
        currentState = DragState.Returning;
        Debug.Log($"[{gameObject.name}] Drag cancelled. Smoothly returning to workbench starting position.");
    }

    private void HandleDragging()
    {
        // 1. Map mouse coordinate depth using New Input System Mouse current position
        Vector2 mousePos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
        Vector3 screenPosWithDepth = new Vector3(mousePos.x, mousePos.y, currentDragDistance);
        Vector3 targetWorldPos = mainCamera.ScreenToWorldPoint(screenPosWithDepth);

        // Smoothly slide object towards cursor depth position
        transform.position = Vector3.Lerp(transform.position, targetWorldPos, Time.deltaTime * dragSpeed);

        // 2. Q/E Rotation keys
        float rotInput = 0f;
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.qKey.isPressed) rotInput += 1f;
            if (keyboard.eKey.isPressed) rotInput -= 1f;
        }

        if (rotInput != 0f)
        {
            // Spin the part smoothly around its local Y-axis
            transform.Rotate(Vector3.up, rotInput * rotationSpeed * Time.deltaTime, Space.Self);
        }
    }

    private void HandleReturning()
    {
        // Smoothly lerp back to starting coordinates
        transform.position = Vector3.Lerp(transform.position, startPosition, Time.deltaTime * returnSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, startRotation, Time.deltaTime * returnSpeed);

        // Snap and return to Idle when extremely close
        if (Vector3.Distance(transform.position, startPosition) < 0.001f && 
            Quaternion.Angle(transform.rotation, startRotation) < 0.1f)
        {
            transform.position = startPosition;
            transform.rotation = startRotation;
            currentState = DragState.Idle;
            Debug.Log($"[{gameObject.name}] Returned to start position successfully.");
        }
    }
}