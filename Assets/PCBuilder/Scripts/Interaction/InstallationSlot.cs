using UnityEngine;

namespace PCBuilder.Interaction
{
    [RequireComponent(typeof(BoxCollider))]
    public class InstallationSlot : MonoBehaviour
    {
        [Header("Slot Identity")]
        [SerializeField] private string slotId;
        [SerializeField] private string acceptedComponentId;

        [Header("References")]
        [SerializeField] private Transform snapPoint;
        [SerializeField] private RamSlotClip retainingClip;
        [SerializeField] private HighlightController highlight;

        [Header("Tolerances")]
        [SerializeField] private float distanceTolerance = 1.5f;
        [SerializeField] private float orientationTolerance = 25f;

        private InstallablePart overlappingPart;
        private InstallablePart installedPart;
        private BoxCollider triggerCol;

        public string SlotId => slotId;
        public string AcceptedComponentId => acceptedComponentId;
        public InstallablePart InstalledPart => installedPart;
        public InstallablePart OverlappingPart => overlappingPart;
        public bool IsOccupied => installedPart != null;
        public RamSlotClip RetainingClip => retainingClip;

        public void Configure(string id, string acceptedId, Transform snap, RamSlotClip clip)
        {
            slotId = id;
            acceptedComponentId = acceptedId;
            snapPoint = snap;
            retainingClip = clip;
        }

        private void Awake()
        {
            triggerCol = GetComponent<BoxCollider>();
            triggerCol.isTrigger = true;

            if (highlight == null)
            {
                highlight = GetComponent<HighlightController>();
            }
        }

        private void Update()
        {
            if (installedPart != null)
            {
                // Slot is occupied, keep normal highlight
                if (highlight != null) highlight.SetHighlight(HighlightController.HighlightState.None);
                return;
            }

            if (overlappingPart != null && overlappingPart.Draggable != null && overlappingPart.Draggable.IsDragging)
            {
                float distance = Vector3.Distance(overlappingPart.transform.position, snapPoint.position);

                if (distance <= distanceTolerance)
                {
                    // Check compatibility
                    bool isCompatible = overlappingPart.ComponentId == acceptedComponentId;

                    if (!isCompatible)
                    {
                        if (highlight != null) highlight.SetHighlight(HighlightController.HighlightState.Invalid);
                        return;
                    }

                    // Check clip
                    if (retainingClip != null && !retainingClip.IsOpen)
                    {
                        if (highlight != null) highlight.SetHighlight(HighlightController.HighlightState.Invalid);
                        return;
                    }

                    // Check orientation alignment (rotation around Y-axis)
                    float angle = Quaternion.Angle(overlappingPart.transform.rotation, snapPoint.rotation);
                    bool isOriented = angle <= orientationTolerance;

                    if (isOriented)
                    {
                        if (highlight != null) highlight.SetHighlight(HighlightController.HighlightState.Valid);
                    }
                    else
                    {
                        if (highlight != null) highlight.SetHighlight(HighlightController.HighlightState.Warning);
                    }
                }
                else
                {
                    if (highlight != null) highlight.SetHighlight(HighlightController.HighlightState.None);
                }
            }
            else
            {
                if (highlight != null) highlight.SetHighlight(HighlightController.HighlightState.None);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            InstallablePart part = other.GetComponentInParent<InstallablePart>();
            if (part != null && part != installedPart)
            {
                overlappingPart = part;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            InstallablePart part = other.GetComponentInParent<InstallablePart>();
            if (part != null && part == overlappingPart)
            {
                overlappingPart = null;
                if (highlight != null) highlight.SetHighlight(HighlightController.HighlightState.None);
            }
        }

        /// <summary>
        /// Attempts to install a part into this slot. Returns true if successful.
        /// </summary>
        public bool TryInstall(InstallablePart part, out string feedbackMessage)
        {
            feedbackMessage = "";

            if (installedPart != null)
            {
                feedbackMessage = "This slot is already occupied.";
                MaintSimAudio.PlaySound("Reject");
                return false;
            }

            if (part.ComponentId != acceptedComponentId)
            {
                feedbackMessage = $"{part.gameObject.name} does not belong in slot {slotId}.";
                MaintSimAudio.PlaySound("Reject");
                return false;
            }

            if (retainingClip != null && !retainingClip.IsOpen)
            {
                feedbackMessage = $"Open the {slotId} retaining clip before installing the module.";
                MaintSimAudio.PlaySound("Reject");
                return false;
            }

            float distance = Vector3.Distance(part.transform.position, snapPoint.position);
            if (distance > distanceTolerance)
            {
                feedbackMessage = "Bring the module closer to the slot to install.";
                MaintSimAudio.PlaySound("Reject");
                return false;
            }

            float angle = Quaternion.Angle(part.transform.rotation, snapPoint.rotation);
            if (angle > orientationTolerance)
            {
                feedbackMessage = "Rotate the RAM module so the notch aligns with the slot.";
                MaintSimAudio.PlaySound("Reject");
                return false;
            }

            // Success! Install it.
            installedPart = part;
            part.SnapTo(snapPoint);

            // Automatically lock retaining clip after installation
            if (retainingClip != null)
            {
                retainingClip.CloseClip();
            }

            overlappingPart = null;
            if (highlight != null) highlight.SetHighlight(HighlightController.HighlightState.None);

            feedbackMessage = $"Successfully installed {part.gameObject.name} into slot {slotId}!";
            MaintSimAudio.PlaySound("Snap");
            return true;
        }

        public void ResetSlot()
        {
            installedPart = null;
            overlappingPart = null;
            if (highlight != null) highlight.SetHighlight(HighlightController.HighlightState.None);

            if (retainingClip != null)
            {
                retainingClip.ResetClip();
            }
        }
    }
}