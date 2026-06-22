using UnityEngine;

namespace PCBuilder.Interaction
{
    [RequireComponent(typeof(SelectableObject))]
    public class RamSlotClip : MonoBehaviour
    {
        [Header("Clip State")]
        [SerializeField] private bool isOpen = false;

        [Header("Animation Setup")]
        [Tooltip("The actual primitive object to rotate (visual clip).")]
        [SerializeField] private Transform clipPivot;
        [SerializeField] private Vector3 closedRotation = Vector3.zero;
        [SerializeField] private Vector3 openRotation = new Vector3(0f, 0f, -30f); // Rotate outwards
        [SerializeField] private float animationDuration = 0.25f;

        private SelectableObject selectable;
        private Coroutine activeAnimation;

        public bool IsOpen => isOpen;

        public void Configure(Transform pivot)
        {
            clipPivot = pivot;
        }

        private void Awake()
        {
            selectable = GetComponent<SelectableObject>();
            if (selectable != null)
            {
                selectable.OnSelect.AddListener(OnClipClicked);
            }

            // Initialize rotation
            if (clipPivot != null)
            {
                clipPivot.localEulerAngles = isOpen ? openRotation : closedRotation;
            }
        }

        private void OnClipClicked()
        {
            ToggleClip();
            // Deselect immediately so it acts as a click button trigger
            selectable.Deselect();
        }

        public void ToggleClip()
        {
            SetClipState(!isOpen);
        }

        public void OpenClip()
        {
            SetClipState(true);
        }

        public void CloseClip()
        {
            SetClipState(false);
        }

        public void ResetClip()
        {
            StopAllCoroutines();
            activeAnimation = null;
            isOpen = false;
            if (clipPivot != null)
            {
                clipPivot.localEulerAngles = closedRotation;
            }
        }

        private void SetClipState(bool targetOpen)
        {
            if (isOpen == targetOpen) return;
            isOpen = targetOpen;

            // Trigger notification to Procedure system or Audio
            if (isOpen)
            {
                Debug.Log($"[{gameObject.name}] Clip opened.");
                MaintSimAudio.PlaySound("ClipOpen");
            }
            else
            {
                Debug.Log($"[{gameObject.name}] Clip closed.");
                MaintSimAudio.PlaySound("ClipClose");
            }

            if (activeAnimation != null)
            {
                StopCoroutine(activeAnimation);
            }

            activeAnimation = StartCoroutine(AnimateClipRotation(isOpen ? openRotation : closedRotation));
        }

        private System.Collections.IEnumerator AnimateClipRotation(Vector3 targetEuler)
        {
            if (clipPivot == null) yield break;

            float elapsed = 0f;
            Quaternion startRot = clipPivot.localRotation;
            Quaternion targetRot = Quaternion.Euler(targetEuler);

            while (elapsed < animationDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / animationDuration;
                t = t * t * (3f - 2f * t); // Smooth interpolation
                clipPivot.localRotation = Quaternion.Slerp(startRot, targetRot, t);
                yield return null;
            }

            clipPivot.localRotation = targetRot;
            activeAnimation = null;
        }
    }
}