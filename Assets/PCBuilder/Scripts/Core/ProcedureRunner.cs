using System;
using System.Collections.Generic;
using UnityEngine;
using PCBuilder.Interaction;
using PCBuilder.Reporting;

namespace PCBuilder.Core
{
    public enum TrainingMode
    {
        Teach,
        Practice,
        Assess
    }

    public class ProcedureRunner : MonoBehaviour
    {
        private static ProcedureRunner instance;
        public static ProcedureRunner Instance => instance;

        [Header("Setup")]
        [SerializeField] private TrainingMode currentMode = TrainingMode.Teach;
        [SerializeField] private ProcedureDefinition procedureAsset;

        private List<ProcedureStepDefinition> steps = new List<ProcedureStepDefinition>();
        private int currentStepIndex = 0;
        private bool isCompleted = false;

        public TrainingMode CurrentMode { get => currentMode; set => currentMode = value; }
        public int CurrentStepIndex => currentStepIndex;
        public int TotalSteps => steps.Count;
        public bool IsCompleted => isCompleted;
        public bool IsVerifyStep => !isCompleted && currentStepIndex < steps.Count && steps[currentStepIndex].actionType == ProcedureActionType.Verify;
        public string CurrentInstruction => isCompleted ? "Procedure Completed Successfully!" : (currentStepIndex < steps.Count ? steps[currentStepIndex].instruction : "");
        public string CurrentHint => currentStepIndex < steps.Count ? steps[currentStepIndex].hint : "No hints available.";

        public event Action OnStepChanged;
        public event Action OnProcedureCompleted;
        public event Action<string, bool> OnFeedbackMessage; // Message, IsError

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;

            InitializeProcedure();
        }

        private void Start()
        {
            StartTrainingSession();
        }

        private void Update()
        {
            if (currentMode == TrainingMode.Teach && !isCompleted)
            {
                UpdateTeachHighlights();
            }
        }

        public void InitializeProcedure()
        {
            steps.Clear();
            currentStepIndex = 0;
            isCompleted = false;

            if (procedureAsset != null)
            {
                foreach (var step in procedureAsset.steps)
                {
                    var copy = new ProcedureStepDefinition
                    {
                        stepId = step.stepId,
                        instruction = step.instruction,
                        hint = step.hint,
                        actionType = step.actionType,
                        objectId = step.objectId,
                        targetId = step.targetId,
                        prerequisiteStepIds = new List<string>(step.prerequisiteStepIds),
                        errorFeedback = step.errorFeedback
                    };
                    steps.Add(copy);
                }
            }
            else
            {
                // Auto-fallback generation of RAM Installation Procedure
                steps.Add(new ProcedureStepDefinition
                {
                    stepId = "Open_A2_Clip",
                    instruction = "Open the retaining clips for RamSlot_A2.",
                    hint = "Click on the clips at either end of the black Slot A2 on the motherboard.",
                    actionType = ProcedureActionType.OpenClip,
                    objectId = "RamSlot_A2",
                    targetId = "RamSlot_A2"
                });

                steps.Add(new ProcedureStepDefinition
                {
                    stepId = "Open_B2_Clip",
                    instruction = "Open the retaining clips for RamSlot_B2.",
                    hint = "Click on the clips at either end of the black Slot B2 on the motherboard.",
                    actionType = ProcedureActionType.OpenClip,
                    objectId = "RamSlot_B2",
                    targetId = "RamSlot_B2",
                    prerequisiteStepIds = new List<string> { "Open_A2_Clip" }
                });

                steps.Add(new ProcedureStepDefinition
                {
                    stepId = "Install_RAM_1",
                    instruction = "Install RamModule_1 into RamSlot_A2.",
                    hint = "Pick up RAM_1, make sure it is aligned (Q/E) and drag it to slot A2.",
                    actionType = ProcedureActionType.InstallPart,
                    objectId = "RAM_1",
                    targetId = "RamSlot_A2",
                    prerequisiteStepIds = new List<string> { "Open_A2_Clip", "Open_B2_Clip" }
                });

                steps.Add(new ProcedureStepDefinition
                {
                    stepId = "Install_RAM_2",
                    instruction = "Install RamModule_2 into RamSlot_B2.",
                    hint = "Pick up RAM_2, rotate it if necessary (Q/E) and drag it to slot B2.",
                    actionType = ProcedureActionType.InstallPart,
                    objectId = "RAM_2",
                    targetId = "RamSlot_B2",
                    prerequisiteStepIds = new List<string> { "Install_RAM_1" }
                });

                steps.Add(new ProcedureStepDefinition
                {
                    stepId = "Verify_Lock",
                    instruction = "Confirm both RAM modules are installed and locked.",
                    hint = "Ensure both RAM modules are seated flat and clips are secured.",
                    actionType = ProcedureActionType.Verify,
                    objectId = "Verify",
                    targetId = "Verify",
                    prerequisiteStepIds = new List<string> { "Install_RAM_2" }
                });
            }
        }

        public void StartTrainingSession()
        {
            ScoringService.Instance.StartSession();
            currentStepIndex = 0;
            isCompleted = false;

            foreach (var step in steps)
            {
                step.isCompleted = false;
            }

            OnStepChanged?.Invoke();
            string startMsg = $"Training started in {currentMode} Mode.";
            OnFeedbackMessage?.Invoke(startMsg, false);
        }

        /// <summary>
        /// Validates if an action is valid for the current step.
        /// </summary>
        public bool ValidateAction(ProcedureActionType actionType, string objectId, string targetId, out string errorExplanation)
        {
            errorExplanation = "";

            if (isCompleted)
            {
                errorExplanation = "The procedure is already completed!";
                return false;
            }

            ProcedureStepDefinition currentStep = steps[currentStepIndex];

            // 1. Check if this specific action matches the current active step's signature
            bool isMatch = currentStep.actionType == actionType &&
                           currentStep.objectId == objectId &&
                           currentStep.targetId == targetId;

            if (isMatch)
            {
                // Prerequisites double-check (just in case)
                foreach (var preReqId in currentStep.prerequisiteStepIds)
                {
                    var preReqStep = steps.Find(s => s.stepId == preReqId);
                    if (preReqStep != null && !preReqStep.isCompleted)
                    {
                        errorExplanation = $"Prerequisite step '{preReqStep.instruction}' is not completed yet!";
                        ScoringService.Instance.RecordOutOfOrderAction(objectId);
                        return false;
                    }
                }

                // Action is fully correct!
                currentStep.isCompleted = true;
                ScoringService.Instance.RecordCorrectAction(currentStep.instruction);
                AdvanceStep();
                return true;
            }

            // 2. Action is incorrect or out of order. Generate smart technical explanation.
            GenerateSmartErrorExplanation(actionType, objectId, targetId, out errorExplanation);
            return false;
        }

        private void GenerateSmartErrorExplanation(ProcedureActionType actionType, string objectId, string targetId, out string explanation)
        {
            explanation = "Invalid action for the current step.";

            // Specific validation failures
            if (actionType == ProcedureActionType.InstallPart)
            {
                // Out of order clip requirements
                if (targetId == "RamSlot_A2")
                {
                    var slotGo = GameObject.Find("RamSlot_A2");
                    if (slotGo != null)
                    {
                        var slot = slotGo.GetComponent<InstallationSlot>();
                        if (slot != null && slot.RetainingClip != null && !slot.RetainingClip.IsOpen)
                        {
                            explanation = "Open the A2 retaining clip before installing the module.";
                            ScoringService.Instance.RecordOutOfOrderAction("RamModule_1");
                            return;
                        }
                    }
                }
                if (targetId == "RamSlot_B2")
                {
                    var slotGo = GameObject.Find("RamSlot_B2");
                    if (slotGo != null)
                    {
                        var slot = slotGo.GetComponent<InstallationSlot>();
                        if (slot != null && slot.RetainingClip != null && !slot.RetainingClip.IsOpen)
                        {
                            explanation = "Open the B2 retaining clip before installing the module.";
                            ScoringService.Instance.RecordOutOfOrderAction("RamModule_2");
                            return;
                        }
                    }
                }

                // Incompatible slots
                if (targetId == "RamSlot_A1" || targetId == "RamSlot_B1")
                {
                    explanation = "That is not the correct dual-channel slot.";
                    ScoringService.Instance.RecordWrongSlot(objectId);
                    return;
                }

                if (objectId == "RamModule_1" && targetId == "RamSlot_B2")
                {
                    explanation = "RamModule_1 belongs in slot A2.";
                    ScoringService.Instance.RecordWrongSlot(objectId);
                    return;
                }
                if (objectId == "RamModule_2" && targetId == "RamSlot_A2")
                {
                    explanation = "RamModule_2 belongs in slot B2.";
                    ScoringService.Instance.RecordWrongSlot(objectId);
                    return;
                }
            }
            else if (actionType == ProcedureActionType.OpenClip)
            {
                explanation = $"Opening clips on {objectId} is not the current step.";
                ScoringService.Instance.RecordOutOfOrderAction(objectId);
            }
        }

        private void AdvanceStep()
        {
            currentStepIndex++;

            if (currentStepIndex >= steps.Count)
            {
                isCompleted = true;
                ScoringService.Instance.EndSession();
                OnFeedbackMessage?.Invoke("Procedure completed! Click Verify to finish.", false);
                OnProcedureCompleted?.Invoke();

                // Trigger Report Generation automatically
                var reportData = ScoringService.Instance.GetReportData(
                    procedureAsset != null ? procedureAsset.procedureTitle : "Dual-Channel RAM Installation",
                    currentMode.ToString()
                );
                ReportService.GenerateReport(reportData);
                MaintSimAudio.PlaySound("Complete");
            }
            else
            {
                OnFeedbackMessage?.Invoke($"Step Advanced: {steps[currentStepIndex].instruction}", false);
                OnStepChanged?.Invoke();
            }
        }

        public void UseHint()
        {
            if (isCompleted || currentStepIndex >= steps.Count) return;

            if (currentMode == TrainingMode.Assess)
            {
                OnFeedbackMessage?.Invoke("Hints are not available in Assessment Mode!", true);
                return;
            }

            string hint = steps[currentStepIndex].hint;
            ScoringService.Instance.RecordHintUsed(steps[currentStepIndex].stepId);
            OnFeedbackMessage?.Invoke($"HINT: {hint}", false);
        }

        private void UpdateTeachHighlights()
        {
            if (isCompleted) return;

            ProcedureStepDefinition currentStep = steps[currentStepIndex];

            // In Teach Mode, pulse or highlight the exact requested object/slot
            if (currentStep.actionType == ProcedureActionType.OpenClip)
            {
                HighlightGameObject(currentStep.objectId, HighlightController.HighlightState.Warning);
            }
            else if (currentStep.actionType == ProcedureActionType.InstallPart)
            {
                HighlightGameObject(currentStep.objectId, HighlightController.HighlightState.Warning);
                HighlightGameObject(currentStep.targetId, HighlightController.HighlightState.Valid);
            }
        }

        private void HighlightGameObject(string name, HighlightController.HighlightState state)
        {
            GameObject go = GameObject.Find(name);
            if (go != null)
            {
                var hc = go.GetComponent<HighlightController>();
                if (hc != null)
                {
                    hc.SetHighlight(state);
                }
            }
        }

        public void TriggerFeedback(string message, bool isError)
        {
            OnFeedbackMessage?.Invoke(message, isError);
        }

        public void ResetProcedure()
        {
            // Clear highlights of all selectables
            var selectables = UnityEngine.Object.FindObjectsByType<SelectableObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var s in selectables)
            {
                var hc = s.GetComponent<HighlightController>();
                if (hc != null) hc.SetHighlight(HighlightController.HighlightState.None);
            }

            // Reset RAM parts
            var parts = UnityEngine.Object.FindObjectsByType<InstallablePart>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var part in parts)
            {
                part.ResetPart();
            }

            // Reset slots
            var slots = UnityEngine.Object.FindObjectsByType<InstallationSlot>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var slot in slots)
            {
                slot.ResetSlot();
            }

            InitializeProcedure();
            StartTrainingSession();
        }
    }
}