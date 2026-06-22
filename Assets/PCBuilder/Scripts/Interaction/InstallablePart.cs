using UnityEngine;

namespace PCBuilder.Interaction
{
    [RequireComponent(typeof(DraggablePart))]
    [RequireComponent(typeof(SelectableObject))]
    public class InstallablePart : MonoBehaviour
    {
        [Header("Part Identity")]
        [SerializeField] private string componentId;

        private DraggablePart draggablePart;
        private SelectableObject selectableObject;
        private Rigidbody rb;
        private BoxCollider col;

        private bool isInstalled;
        private Vector3 originalPosition;
        private Quaternion originalRotation;

        public string ComponentId => componentId;
        public bool IsInstalled => isInstalled;
        public DraggablePart Draggable => draggablePart;

        public void Configure(string id, DraggablePart drag)
        {
            componentId = id;
            draggablePart = drag;
        }

        private void Awake()
        {
            draggablePart = GetComponent<DraggablePart>();
            selectableObject = GetComponent<SelectableObject>();
            rb = GetComponent<Rigidbody>();
            col = GetComponent<BoxCollider>();

            originalPosition = transform.position;
            originalRotation = transform.rotation;
        }

        /// <summary>
        /// Snaps the RAM module smoothly to a target snap point.
        /// </summary>
        public void SnapTo(Transform snapPoint)
        {
            isInstalled = true;
            DisableDragging();

            // Perform smooth visual snap
            StartCoroutine(LerpToTransform(snapPoint));
        }

        /// <summary>
        /// Returns the RAM module to its original workbench position.
        /// </summary>
        public void ReturnToStart()
        {
            isInstalled = false;
            if (draggablePart != null)
            {
                draggablePart.Release(); // Triggers the DraggablePart's smooth return to its original position
            }
        }

        /// <summary>
        /// Permanently disables dragging and raycasting for this module after installation.
        /// </summary>
        public void DisableDragging()
        {
            if (draggablePart != null) draggablePart.enabled = false;
            if (selectableObject != null) selectableObject.enabled = false;

            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            // We can keep collider active for trigger detection, or disable raycasting by changing layer or disabling collider.
            // Let's set it to ignore raycast layer, or just disable it since it's fully installed.
            gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
        }

        public void ResetPart()
        {
            StopAllCoroutines();
            isInstalled = false;
            gameObject.layer = LayerMask.NameToLayer("Default");

            if (draggablePart != null)
            {
                draggablePart.enabled = true;
                // Force DraggablePart state back to Idle
                var stateField = typeof(DraggablePart).GetField("currentState", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (stateField != null)
                {
                    stateField.SetValue(draggablePart, DraggablePart.DragState.Idle);
                }
            }

            if (selectableObject != null)
            {
                selectableObject.enabled = true;
                selectableObject.Deselect();
                selectableObject.HoverExit();
            }

            transform.position = originalPosition;
            transform.rotation = originalRotation;

            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }
        }

        private System.Collections.IEnumerator LerpToTransform(Transform target)
        {
            float elapsed = 0f;
            float duration = 0.4f;
            Vector3 startPos = transform.position;
            Quaternion startRot = transform.rotation;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                // Smooth step curve
                t = t * t * (3f - 2f * t);

                transform.position = Vector3.Lerp(startPos, target.position, t);
                transform.rotation = Quaternion.Slerp(startRot, target.rotation, t);
                yield return null;
            }

            transform.position = target.position;
            transform.rotation = target.rotation;
        }
    }
}