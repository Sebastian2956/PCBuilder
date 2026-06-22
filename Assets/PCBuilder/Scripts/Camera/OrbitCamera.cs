using UnityEngine;
using UnityEngine.InputSystem;

namespace PCBuilder.Cam
{
    public class OrbitCamera : MonoBehaviour
    {
        [Header("Focus Target")]
        [SerializeField] private Vector3 focusPoint = new Vector3(0.5f, 0.35f, 0.5f); // Motherboard center
        [SerializeField] private float defaultDistance = 5f;
        [SerializeField] private float minDistance = 2f;
        [SerializeField] private float maxDistance = 10f;

        [Header("Speeds")]
        [SerializeField] private float orbitSpeedX = 250f;
        [SerializeField] private float orbitSpeedY = 120f;
        [SerializeField] private float zoomSpeed = 8f;
        [SerializeField] private float smoothSpeed = 10f;

        [Header("Limits")]
        [SerializeField] private float minAngleY = 5f;
        [SerializeField] private float maxAngleY = 85f;
        [SerializeField] private float minCameraY = 0.4f; // Prevent clipping through workbench (Y = 0)

        private float targetX = 0f;
        private float targetY = 45f;
        private float targetDistance;

        private float currentX = 0f;
        private float currentY = 45f;
        private float currentDistance;

        private void Start()
        {
            Vector3 angles = transform.eulerAngles;
            targetX = angles.y;
            targetY = angles.x;
            targetDistance = defaultDistance;

            currentX = targetX;
            currentY = targetY;
            currentDistance = targetDistance;
        }

        private void LateUpdate()
        {
            // Do not permit camera orbit rotation while dragging a RAM module to avoid input conflicts
            bool isDragging = InteractionController.Instance != null && InteractionController.Instance.IsHoldingObject;

            // 1. Orbit Rotation (Right Mouse Hold and Drag)
            if (!isDragging && Mouse.current != null && Mouse.current.rightButton.isPressed)
            {
                Vector2 mouseDelta = Mouse.current.delta.ReadValue();
                targetX += mouseDelta.x * orbitSpeedX * 0.02f;
                targetY -= mouseDelta.y * orbitSpeedY * 0.02f;
                targetY = Mathf.Clamp(targetY, minAngleY, maxAngleY);
            }

            // 2. Focus Camera (F Key)
            if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
            {
                FocusOnTarget();
            }

            // 3. Zoom (Scroll Wheel)
            if (Mouse.current != null)
            {
                float scroll = Mouse.current.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    targetDistance -= Mathf.Sign(scroll) * zoomSpeed * 0.1f * targetDistance;
                    targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
                }
            }

            // 4. Smooth Interpolation
            currentX = Mathf.LerpAngle(currentX, targetX, Time.deltaTime * smoothSpeed);
            currentY = Mathf.LerpAngle(currentY, targetY, Time.deltaTime * smoothSpeed);
            currentDistance = Mathf.Lerp(currentDistance, targetDistance, Time.deltaTime * smoothSpeed);

            // 5. Position Calculation
            Quaternion rotation = Quaternion.Euler(currentY, currentX, 0f);
            Vector3 negDistance = new Vector3(0.0f, 0.0f, -currentDistance);
            Vector3 position = rotation * negDistance + focusPoint;

            // Prevent clipping through the workbench surface (Y = 0)
            if (position.y < minCameraY)
            {
                position.y = minCameraY;
            }

            transform.rotation = rotation;
            transform.position = position;
        }

        public void FocusOnTarget()
        {
            targetX = 0f;
            targetY = 45f;
            targetDistance = defaultDistance;
            Debug.Log("[OrbitCamera] Camera focused back to motherboard workspace center.");
        }

        public void ResetCamera()
        {
            targetX = 0f;
            targetY = 45f;
            targetDistance = defaultDistance;
            currentX = 0f;
            currentY = 45f;
            currentDistance = defaultDistance;
        }
    }
}