using UnityEngine;
using UnityEngine.Events;

public class SelectableObject : MonoBehaviour
{
    [Header("Hover Events")]
    public UnityEvent OnHoverEnter;
    public UnityEvent OnHoverExit;

    [Header("Selection Events")]
    public UnityEvent OnSelect;
    public UnityEvent OnDeselect;

    private bool isHovered;
    private bool isSelected;
    private HighlightController highlightController;

    public bool IsHovered => isHovered;
    public bool IsSelected => isSelected;

    private void Awake()
    {
        highlightController = GetComponent<HighlightController>();
        
        // Auto-wire local HighlightController if it is attached
        if (highlightController != null)
        {
            OnHoverEnter.AddListener(() => highlightController.SetHighlight(HighlightController.HighlightState.Hover));
            OnHoverExit.AddListener(() => highlightController.SetHighlight(HighlightController.HighlightState.None));
        }
    }

    public void HoverEnter()
    {
        if (isHovered) return;
        isHovered = true;
        OnHoverEnter?.Invoke();
    }

    public void HoverExit()
    {
        if (!isHovered) return;
        isHovered = false;
        OnHoverExit?.Invoke();
    }

    public void Select()
    {
        if (isSelected) return;
        isSelected = true;
        OnSelect?.Invoke();
    }

    public void Deselect()
    {
        if (!isSelected) return;
        isSelected = false;
        OnDeselect?.Invoke();
    }
}