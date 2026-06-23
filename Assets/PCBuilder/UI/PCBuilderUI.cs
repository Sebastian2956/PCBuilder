using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PCBuilder.Core;
using PCBuilder.Interaction;

namespace PCBuilder.UI
{
    public class PCBuilderUI : MonoBehaviour
    {
        private static PCBuilderUI instance;
        public static PCBuilderUI Instance => instance;

        [Header("UI Visual Customization")]
        private Color bgColor = new Color(0.08f, 0.08f, 0.09f, 0.92f); // Dark translucent charcoal
        private Color accentColor = new Color(0.0f, 0.68f, 0.71f, 1f); // Muted teal accent
        private Color successColor = new Color(0.18f, 0.8f, 0.25f, 1f); // Clean green
        private Color warningColor = new Color(1.0f, 0.64f, 0.0f, 1f); // Amber / Warning
        private Color errorColor = new Color(0.86f, 0.2f, 0.18f, 1f); // Alert Red
        private Color buttonBgColor = new Color(0.16f, 0.16f, 0.18f, 1f);

        // UI Root Elements
        private GameObject canvasGo;
        private Canvas canvas;
        private CanvasScaler scaler;

        // UI Panels
        private GameObject startPanel;
        private GameObject inProcedurePanel;
        private GameObject completionPanel;

        // Start Panel Controls
        private TextMeshProUGUI subtitleText;
        private Button teachModeBtn;
        private Button practiceModeBtn;
        private Button assessModeBtn;
        private Button startBtn;

        // In-Procedure Panel Controls
        private TextMeshProUGUI modeText;
        private TextMeshProUGUI timerText;
        private TextMeshProUGUI mistakesText;
        private TextMeshProUGUI stepText;
        private TextMeshProUGUI instructionText;
        private TextMeshProUGUI feedbackText;
        private Image progressBarFill;
        private Button hintBtn;
        private Button resetBtn;
        private Button menuBtn;
        private Button verifyBtn;

        // Completion Panel Controls
        private TextMeshProUGUI compTitleText;
        private TextMeshProUGUI compScoreText;
        private TextMeshProUGUI compStatusText;
        private TextMeshProUGUI compDetailsText;
        private TextMeshProUGUI compSummaryText;
        private Button compRetryBtn;
        private Button compMenuBtn;

        // Loaded default TMP Font Asset
        private TMP_FontAsset defaultFont;

        private ProcedureRunner GetRunner()
        {
            var r = GetComponent<ProcedureRunner>();
            if (r == null) r = UnityEngine.Object.FindAnyObjectByType<ProcedureRunner>();
            return r;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;

            // Load default TMP Font
            defaultFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (defaultFont == null)
            {
                Debug.LogWarning("[PCBuilderUI] LiberationSans SDF font asset could not be loaded from Resources. Searching project...");
            }

            // Generate UI Canvas and EventSystem programmatically
            SetupCanvas();
            SetupEventSystem();
            BuildUI();

            // Register Event Listeners
            var runner = GetRunner();
            if (runner != null)
            {
                runner.OnStepChanged += RefreshInProcedureUI;
                runner.OnProcedureCompleted += ShowCompletionPanel;
                runner.OnFeedbackMessage += HandleFeedbackMessage;
            }
        }

        private void Start()
        {
            ShowStartPanel();
        }

        private void Update()
        {
            if (inProcedurePanel.activeSelf)
            {
                UpdateInProcedureStats();
            }
        }

        private void OnDestroy()
        {
            var runner = GetRunner();
            if (runner != null)
            {
                runner.OnStepChanged -= RefreshInProcedureUI;
                runner.OnProcedureCompleted -= ShowCompletionPanel;
                runner.OnFeedbackMessage -= HandleFeedbackMessage;
            }
        }

        #region Setup Canvas & Hierarchy
        private void SetupCanvas()
        {
            canvasGo = GameObject.Find("PCBuilderUI_Canvas");
            if (canvasGo == null)
            {
                canvasGo = new GameObject("PCBuilderUI_Canvas");
                canvasGo.transform.SetParent(transform);

                canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                scaler = canvasGo.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;

                canvasGo.AddComponent<GraphicRaycaster>();
            }
            else
            {
                canvas = canvasGo.GetComponent<Canvas>();
                scaler = canvasGo.GetComponent<CanvasScaler>();
            }
        }

        private void SetupEventSystem()
        {
            var existingEventSystem = UnityEngine.Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (existingEventSystem == null)
            {
                GameObject esGo = new GameObject("EventSystem");
                esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esGo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                Debug.Log("[PCBuilderUI] EventSystem and InputSystemUIInputModule spawned programmatically.");
            }
        }
        #endregion

        #region UI Building Helpers
        private GameObject CreatePanel(string name, GameObject parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);

            RectTransform rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image img = go.AddComponent<Image>();
            img.color = color;

            return go;
        }

        private TextMeshProUGUI CreateText(string name, GameObject parent, string text, int fontSize, Color color, TextAlignmentOptions alignment, Vector4 margin = default)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);

            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null)
            {
                tmp.font = defaultFont;
            }
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.margin = margin;

            RectTransform rect = tmp.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            return tmp;
        }

        private Button CreateButton(string name, GameObject parent, string label, Vector2 size, Vector2 anchoredPosition, Action onClickCallback)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);

            RectTransform rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;

            Image img = go.AddComponent<Image>();
            img.color = buttonBgColor;

            Button btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            // Simple hover/pressed visual colors
            ColorBlock cb = btn.colors;
            cb.normalColor = buttonBgColor;
            cb.highlightedColor = buttonBgColor * 1.3f;
            cb.pressedColor = accentColor;
            cb.selectedColor = buttonBgColor;
            btn.colors = cb;

            btn.onClick.AddListener(() => onClickCallback?.Invoke());

            // Add text child
            GameObject textGo = new GameObject("Label");
            textGo.transform.SetParent(go.transform, false);

            TextMeshProUGUI tmp = textGo.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null)
            {
                tmp.font = defaultFont;
            }
            tmp.text = label;
            tmp.fontSize = 24;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;

            RectTransform tRect = tmp.GetComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.offsetMin = Vector2.zero;
            tRect.offsetMax = Vector2.zero;

            return btn;
        }
        #endregion

        #region Generate UI Hierarchy Programmatically
        private void BuildUI()
        {
            // 1. Build Start Panel
            startPanel = CreatePanel("StartPanel", canvasGo, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Color.clear);
            
            // Central Card for the Start Panel to leave the motherboard visible behind/beside it
            GameObject startCard = CreatePanel("StartCard", startPanel, new Vector2(0.25f, 0.15f), new Vector2(0.75f, 0.85f), new Vector2(0.5f, 0.5f), bgColor);
            VerticalLayoutGroup startVlg = startCard.AddComponent<VerticalLayoutGroup>();
            startVlg.padding = new RectOffset(45, 45, 45, 45);
            startVlg.spacing = 25;
            startVlg.childAlignment = TextAnchor.MiddleCenter;
            startVlg.childControlHeight = true;
            startVlg.childControlWidth = true;
            startVlg.childForceExpandHeight = false;
            startVlg.childForceExpandWidth = true;

            // Title and Subtitle with updated exact texts and sizes
            CreateText("Title", startCard, "PCBuilder", 80, accentColor, TextAlignmentOptions.Center);
            subtitleText = CreateText("Subtitle", startCard, "Interactive PC Assembly and Maintenance Training", 30, Color.white, TextAlignmentOptions.Center);

            // Horizontal container for Training Mode Buttons
            GameObject modeContainer = CreatePanel("ModeContainer", startCard, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Color.clear);
            LayoutElement modeLe = modeContainer.AddComponent<LayoutElement>();
            modeLe.preferredHeight = 70;
            modeLe.preferredWidth = 800;

            HorizontalLayoutGroup modeHlg = modeContainer.AddComponent<HorizontalLayoutGroup>();
            modeHlg.spacing = 20;
            modeHlg.childAlignment = TextAnchor.MiddleCenter;
            modeHlg.childControlHeight = false;
            modeHlg.childControlWidth = false;
            modeHlg.childForceExpandHeight = false;
            modeHlg.childForceExpandWidth = false;

            teachModeBtn = CreateButton("TeachModeBtn", modeContainer, "Teach Mode", new Vector2(240, 60), Vector2.zero, () => SetModeSelection(TrainingMode.Teach));
            practiceModeBtn = CreateButton("PracticeModeBtn", modeContainer, "Practice Mode", new Vector2(240, 60), Vector2.zero, () => SetModeSelection(TrainingMode.Practice));
            assessModeBtn = CreateButton("AssessModeBtn", modeContainer, "Assess Mode", new Vector2(240, 60), Vector2.zero, () => SetModeSelection(TrainingMode.Assess));

            // Default Highlight Teach Mode
            SetModeSelection(TrainingMode.Teach);

            // Center container for the Start Button to keep its preferred size
            GameObject startBtnContainer = CreatePanel("StartBtnContainer", startCard, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Color.clear);
            LayoutElement startBtnLe = startBtnContainer.AddComponent<LayoutElement>();
            startBtnLe.preferredHeight = 80;
            startBtnLe.preferredWidth = 800;

            HorizontalLayoutGroup startBtnHlg = startBtnContainer.AddComponent<HorizontalLayoutGroup>();
            startBtnHlg.childAlignment = TextAnchor.MiddleCenter;
            startBtnHlg.childControlHeight = false;
            startBtnHlg.childControlWidth = false;
            startBtnHlg.childForceExpandHeight = false;
            startBtnHlg.childForceExpandWidth = false;

            startBtn = CreateButton("StartBtn", startBtnContainer, "Start Procedure", new Vector2(300, 70), Vector2.zero, StartProcedureClicked);
            
            // Color start button teal
            Image startImg = startBtn.GetComponent<Image>();
            startImg.color = accentColor;
            ColorBlock startCb = startBtn.colors;
            startCb.normalColor = accentColor;
            startCb.highlightedColor = accentColor * 1.15f;
            startCb.pressedColor = accentColor * 0.8f;
            startBtn.colors = startCb;


            // 2. Build In-Procedure Panel
            inProcedurePanel = CreatePanel("InProcedurePanel", canvasGo, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Color.clear);

            // Left Side Panel: Step Details (charcoal background card) with VerticalLayoutGroup
            GameObject detailsCard = CreatePanel("DetailsCard", inProcedurePanel, new Vector2(0.05f, 0.35f), new Vector2(0.35f, 0.85f), new Vector2(0f, 1f), bgColor);
            VerticalLayoutGroup detailsVlg = detailsCard.AddComponent<VerticalLayoutGroup>();
            detailsVlg.padding = new RectOffset(30, 30, 30, 30);
            detailsVlg.spacing = 15;
            detailsVlg.childAlignment = TextAnchor.UpperLeft;
            detailsVlg.childControlHeight = true;
            detailsVlg.childControlWidth = true;
            detailsVlg.childForceExpandHeight = false;
            detailsVlg.childForceExpandWidth = true;

            modeText = CreateText("ModeText", detailsCard, "TEACH MODE", 22, warningColor, TextAlignmentOptions.TopLeft);
            stepText = CreateText("StepText", detailsCard, "Step 1 of 5", 26, accentColor, TextAlignmentOptions.TopLeft);
            instructionText = CreateText("InstructionText", detailsCard, "Preparing...", 24, Color.white, TextAlignmentOptions.TopLeft);
            
            // Bottom Action Log / Console Area with VerticalLayoutGroup
            GameObject feedbackConsole = CreatePanel("FeedbackConsole", inProcedurePanel, new Vector2(0.05f, 0.05f), new Vector2(0.65f, 0.30f), new Vector2(0f, 0f), bgColor);
            VerticalLayoutGroup feedbackVlg = feedbackConsole.AddComponent<VerticalLayoutGroup>();
            feedbackVlg.padding = new RectOffset(20, 20, 20, 20);
            feedbackVlg.childAlignment = TextAnchor.UpperLeft;
            feedbackVlg.childControlHeight = true;
            feedbackVlg.childControlWidth = true;
            feedbackVlg.childForceExpandHeight = false;
            feedbackVlg.childForceExpandWidth = true;

            feedbackText = CreateText("FeedbackText", feedbackConsole, "Status: Ready.", 20, new Color(0.8f, 0.8f, 0.8f, 1f), TextAlignmentOptions.TopLeft);

            // Right side stats card with VerticalLayoutGroup
            GameObject statsCard = CreatePanel("StatsCard", inProcedurePanel, new Vector2(0.70f, 0.65f), new Vector2(0.95f, 0.85f), new Vector2(0f, 1f), bgColor);
            VerticalLayoutGroup statsVlg = statsCard.AddComponent<VerticalLayoutGroup>();
            statsVlg.padding = new RectOffset(30, 30, 30, 30);
            statsVlg.spacing = 15;
            statsVlg.childAlignment = TextAnchor.UpperLeft;
            statsVlg.childControlHeight = true;
            statsVlg.childControlWidth = true;
            statsVlg.childForceExpandHeight = false;
            statsVlg.childForceExpandWidth = true;

            timerText = CreateText("TimerText", statsCard, "Timer: 00:00", 24, Color.white, TextAlignmentOptions.TopLeft);
            mistakesText = CreateText("MistakesText", statsCard, "Mistakes: 0", 24, Color.white, TextAlignmentOptions.TopLeft);

            // Controls Column Cards on the Right under Stats Card
            GameObject controlsCard = CreatePanel("ControlsCard", inProcedurePanel, new Vector2(0.70f, 0.15f), new Vector2(0.95f, 0.55f), new Vector2(0.5f, 0.5f), Color.clear);
            VerticalLayoutGroup controlsVlg = controlsCard.AddComponent<VerticalLayoutGroup>();
            controlsVlg.padding = new RectOffset(10, 10, 10, 10);
            controlsVlg.spacing = 15;
            controlsVlg.childAlignment = TextAnchor.UpperCenter;
            controlsVlg.childControlHeight = false;
            controlsVlg.childControlWidth = false;
            controlsVlg.childForceExpandHeight = false;
            controlsVlg.childForceExpandWidth = false;

            hintBtn = CreateButton("HintBtn", controlsCard, "Hint", new Vector2(240, 55), Vector2.zero, () => GetRunner()?.UseHint());
            resetBtn = CreateButton("ResetBtn", controlsCard, "Reset", new Vector2(240, 55), Vector2.zero, () => GetRunner()?.ResetProcedure());
            menuBtn = CreateButton("MenuBtn", controlsCard, "Return to Menu", new Vector2(240, 55), Vector2.zero, ReturnToMenuClicked);

            verifyBtn = CreateButton("VerifyBtn", controlsCard, "Verify", new Vector2(240, 55), Vector2.zero, VerifyClicked);
            Image verifyImg = verifyBtn.GetComponent<Image>();
            verifyImg.color = successColor;
            ColorBlock verifyCb = verifyBtn.colors;
            verifyCb.normalColor = successColor;
            verifyCb.highlightedColor = successColor * 1.15f;
            verifyCb.pressedColor = successColor * 0.8f;
            verifyBtn.colors = verifyCb;

            // Progress Bar (Slate bottom, teal fill)
            GameObject progressBarBg = CreatePanel("ProgressBarBg", inProcedurePanel, new Vector2(0.05f, 0.90f), new Vector2(0.95f, 0.93f), new Vector2(0.5f, 1f), new Color(0.2f, 0.2f, 0.2f, 1f));
            progressBarFill = CreatePanel("ProgressBarFill", progressBarBg, Vector2.zero, new Vector2(0f, 1f), Vector2.zero, accentColor).GetComponent<Image>();


            // 3. Build Completion Panel
            completionPanel = CreatePanel("CompletionPanel", canvasGo, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Color.clear);
            
            // Central Card for the Completion Panel
            GameObject compCard = CreatePanel("CompCard", completionPanel, new Vector2(0.20f, 0.10f), new Vector2(0.80f, 0.90f), new Vector2(0.5f, 0.5f), bgColor);
            VerticalLayoutGroup compVlg = compCard.AddComponent<VerticalLayoutGroup>();
            compVlg.padding = new RectOffset(45, 45, 45, 45);
            compVlg.spacing = 20;
            compVlg.childAlignment = TextAnchor.MiddleCenter;
            compVlg.childControlHeight = true;
            compVlg.childControlWidth = true;
            compVlg.childForceExpandHeight = false;
            compVlg.childForceExpandWidth = true;

            compTitleText = CreateText("CompTitle", compCard, "Procedure Complete", 45, accentColor, TextAlignmentOptions.Center);
            compScoreText = CreateText("CompScore", compCard, "SCORE: 100", 35, Color.white, TextAlignmentOptions.Center);
            compStatusText = CreateText("CompStatus", compCard, "PASS", 40, successColor, TextAlignmentOptions.Center);
            compDetailsText = CreateText("CompDetails", compCard, "Duration: 0s | Mistakes: 0 | Hints: 0", 22, Color.white, TextAlignmentOptions.Center);
            compSummaryText = CreateText("CompSummary", compCard, "Written feedback report summarized.", 20, new Color(0.7f, 0.7f, 0.7f, 1f), TextAlignmentOptions.Center);

            // Centered Horizontal buttons container for Retry and Return to Menu
            GameObject compButtonsContainer = CreatePanel("CompButtonsContainer", compCard, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Color.clear);
            LayoutElement compButtonsLe = compButtonsContainer.AddComponent<LayoutElement>();
            compButtonsLe.preferredHeight = 80;
            compButtonsLe.preferredWidth = 800;

            HorizontalLayoutGroup compButtonsHlg = compButtonsContainer.AddComponent<HorizontalLayoutGroup>();
            compButtonsHlg.spacing = 30;
            compButtonsHlg.childAlignment = TextAnchor.MiddleCenter;
            compButtonsHlg.childControlHeight = false;
            compButtonsHlg.childControlWidth = false;
            compButtonsHlg.childForceExpandHeight = false;
            compButtonsHlg.childForceExpandWidth = false;

            compRetryBtn = CreateButton("CompRetryBtn", compButtonsContainer, "Retry", new Vector2(240, 60), Vector2.zero, () => { GetRunner()?.ResetProcedure(); ShowInProcedurePanel(); });
            compMenuBtn = CreateButton("CompMenuBtn", compButtonsContainer, "Return to Menu", new Vector2(240, 60), Vector2.zero, ReturnToMenuClicked);
        }

        private void SetModeSelection(TrainingMode mode)
        {
            var runner = GetRunner();
            if (runner != null)
            {
                runner.CurrentMode = mode;
            }

            // Style buttons to reflect active toggle
            teachModeBtn.GetComponent<Image>().color = (mode == TrainingMode.Teach) ? accentColor : buttonBgColor;
            practiceModeBtn.GetComponent<Image>().color = (mode == TrainingMode.Practice) ? accentColor : buttonBgColor;
            assessModeBtn.GetComponent<Image>().color = (mode == TrainingMode.Assess) ? accentColor : buttonBgColor;
        }
        #endregion

        #region UI Button Event Callbacks
        private void StartProcedureClicked()
        {
            var runner = GetRunner();
            if (runner != null)
            {
                runner.ResetProcedure();
            }
            ShowInProcedurePanel();
        }

        private void ReturnToMenuClicked()
        {
            var runner = GetRunner();
            if (runner != null)
            {
                runner.ResetProcedure();
            }
            ShowStartPanel();
        }

        private void VerifyClicked()
        {
            var runner = GetRunner();
            if (runner != null)
            {
                string feedback;
                bool success = runner.ValidateAction(ProcedureActionType.Verify, "Verify", "Verify", out feedback);
                if (success)
                {
                    runner.TriggerFeedback(feedback, false);
                }
                else
                {
                    runner.TriggerFeedback(feedback, true);
                }
            }
        }
        #endregion

        #region UI States Toggling
        public void ShowStartPanel()
        {
            startPanel.SetActive(true);
            inProcedurePanel.SetActive(false);
            completionPanel.SetActive(false);
        }

        public void ShowInProcedurePanel()
        {
            startPanel.SetActive(false);
            inProcedurePanel.SetActive(true);
            completionPanel.SetActive(false);
            RefreshInProcedureUI();
        }

        public void ShowCompletionPanel()
        {
            startPanel.SetActive(false);
            inProcedurePanel.SetActive(false);
            completionPanel.SetActive(true);

            // Fetch session summary stats
            if (ScoringService.Instance != null && GetRunner() != null)
            {
                int finalScore = ScoringService.Instance.CurrentScore;
                bool passed = ScoringService.Instance.Passed;
                float duration = ScoringService.Instance.ElapsedTime;
                int mistakes = ScoringService.Instance.Mistakes;
                int hints = ScoringService.Instance.HintsUsed;

                compScoreText.text = $"FINAL SCORE: {finalScore} / 100";
                
                if (passed)
                {
                    compStatusText.text = "PASS";
                    compStatusText.color = successColor;
                }
                else
                {
                    compStatusText.text = "FAIL (Passing score: 70)";
                    compStatusText.color = errorColor;
                }

                compDetailsText.text = $"Duration: {duration:F1}s   |   Mistakes: {mistakes}   |   Hints Used: {hints}";

                // Build a short written feedback summary
                StringBuilder summary = new StringBuilder();
                summary.AppendLine("Feedback Summary:");
                if (mistakes == 0) summary.AppendLine("• Perfect performance! Outstanding speed and mechanical precision.");
                else if (passed) summary.AppendLine("• Good technical competence. Mind the tolerances and step prerequisites.");
                else summary.AppendLine("• Review the training steps in Teach Mode to practice precise slot placement and orientation alignment.");

                summary.AppendLine($"Reports saved automatically to local: {Application.persistentDataPath}/Reports/");
                compSummaryText.text = summary.ToString();
            }
        }
        #endregion

        #region In-Procedure UI Updates
        private void RefreshInProcedureUI()
        {
            var runner = GetRunner();
            if (runner == null) return;

            TrainingMode mode = runner.CurrentMode;
            modeText.text = mode.ToString().ToUpper() + " MODE";
            modeText.color = (mode == TrainingMode.Teach) ? warningColor : ((mode == TrainingMode.Practice) ? accentColor : errorColor);

            int currentStep = runner.CurrentStepIndex + 1;
            int total = runner.TotalSteps;

            if (runner.IsCompleted)
            {
                stepText.text = "Procedure Complete!";
                instructionText.text = "Confirm both RAM modules are installed and locked.";
                progressBarFill.rectTransform.anchorMax = new Vector2(1f, 1f);
            }
            else
            {
                stepText.text = $"Step {currentStep} of {total}";
                instructionText.text = runner.CurrentInstruction;

                float pct = (float)runner.CurrentStepIndex / total;
                progressBarFill.rectTransform.anchorMax = new Vector2(pct, 1f);
            }

            // Hide/Disable hints button in Assess mode
            hintBtn.gameObject.SetActive(mode != TrainingMode.Assess);

            // Dynamic Verify Button visibility
            if (verifyBtn != null)
            {
                verifyBtn.gameObject.SetActive(runner.IsVerifyStep);
            }
        }

        private void UpdateInProcedureStats()
        {
            if (ScoringService.Instance == null) return;

            float elapsed = ScoringService.Instance.ElapsedTime;
            int minutes = Mathf.FloorToInt(elapsed / 60F);
            int seconds = Mathf.FloorToInt(elapsed - minutes * 60);
            timerText.text = $"Timer: {minutes:00}:{seconds:00}";

            mistakesText.text = $"Mistakes: {ScoringService.Instance.Mistakes}";
        }

        private void HandleFeedbackMessage(string message, bool isError)
        {
            feedbackText.text = message;
            feedbackText.color = isError ? errorColor : Color.white;
        }
        #endregion
    }
}