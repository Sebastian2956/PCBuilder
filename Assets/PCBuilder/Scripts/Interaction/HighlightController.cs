using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class HighlightController : MonoBehaviour
{
    public enum HighlightState
    {
        None,
        Hover,
        Valid,
        Invalid,
        Warning
    }

    [Header("Highlight Visuals")]
    [SerializeField] private Color hoverColor = new Color(0.1f, 1.8f, 1.8f, 1f); // Vibrant Cyan glow
    [SerializeField] private Color validColor = Color.green;
    [SerializeField] private Color invalidColor = Color.red;
    [SerializeField] private Color warningColor = new Color(1.0f, 0.6f, 0.0f, 1f); // Amber / Warning
    [SerializeField] private string colorPropertyName = "_BaseColor"; // Standard URP Lit diffuse color property

    private Renderer targetRenderer;
    private MaterialPropertyBlock propBlock;
    private Color[] originalColors;
    private HighlightState currentState = HighlightState.None;

    private void Awake()
    {
        targetRenderer = GetComponent<Renderer>();
        propBlock = new MaterialPropertyBlock();
        
        int materialCount = targetRenderer.sharedMaterials.Length;
        originalColors = new Color[materialCount];
        for (int i = 0; i < materialCount; i++)
        {
            var mat = targetRenderer.sharedMaterials[i];
            if (mat != null && mat.HasProperty(colorPropertyName))
            {
                originalColors[i] = mat.GetColor(colorPropertyName);
            }
            else
            {
                originalColors[i] = Color.white;
            }
        }
    }

    public void SetHighlight(HighlightState state)
    {
        if (currentState == state) return;
        currentState = state;

        for (int i = 0; i < originalColors.Length; i++)
        {
            targetRenderer.GetPropertyBlock(propBlock, i);
            Color targetColor = originalColors[i];

            switch (state)
            {
                case HighlightState.Hover:
                    // Multiplicative highlight to tint and brighten the existing color
                    targetColor = originalColors[i] * hoverColor;
                    break;
                case HighlightState.Valid:
                    targetColor = Color.Lerp(originalColors[i], validColor, 0.5f);
                    break;
                case HighlightState.Invalid:
                    targetColor = Color.Lerp(originalColors[i], invalidColor, 0.5f);
                    break;
                case HighlightState.Warning:
                    targetColor = Color.Lerp(originalColors[i], warningColor, 0.5f);
                    break;
                case HighlightState.None:
                default:
                    targetColor = originalColors[i];
                    break;
            }

            propBlock.SetColor(colorPropertyName, targetColor);
            targetRenderer.SetPropertyBlock(propBlock, i);
        }
    }
}