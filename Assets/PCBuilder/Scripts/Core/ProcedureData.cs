using System;
using System.Collections.Generic;
using UnityEngine;

namespace PCBuilder.Core
{
    public enum ProcedureActionType
    {
        OpenClip,
        InstallPart,
        Verify
    }

    [Serializable]
    public class ProcedureStepDefinition
    {
        public string stepId;
        public string instruction;
        public string hint;
        public ProcedureActionType actionType;
        public string objectId; // e.g. "RAM_1", "A2" (for clip)
        public string targetId; // e.g. "A2" (for slot)
        public List<string> prerequisiteStepIds = new List<string>();
        public string errorFeedback;

        [HideInInspector] public bool isCompleted = false;
    }

    [CreateAssetMenu(fileName = "NewProcedure", menuName = "PCBuilder/Procedure Definition")]
    public class ProcedureDefinition : ScriptableObject
    {
        public string procedureTitle = "Dual-Channel RAM Installation";
        public List<ProcedureStepDefinition> steps = new List<ProcedureStepDefinition>();
    }
}