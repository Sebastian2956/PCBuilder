using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using PCBuilder.Core;
using PCBuilder.Interaction;
using PCBuilder.UI;

namespace PCBuilder.Editor
{
    public class SceneValidationUtility : EditorWindow
    {
        [MenuItem("Tools/PCBuilder/Validate Scene")]
        public static void ValidateScene()
        {
            Debug.Log("[SceneValidationUtility] Starting complete scene layout and configuration audit...");
            int errorsCount = 0;
            int warningsCount = 0;

            // 1. Check Key GameObjects
            string[] requiredObjects = {
                "Workbench", "Motherboard", "RamSlot_A1", "RamSlot_A2",
                "RamSlot_B1", "RamSlot_B2", "RamModule_1", "RamModule_2",
                "Main Camera", "Directional Light"
            };

            foreach (var name in requiredObjects)
            {
                var go = GameObject.Find(name);
                if (go == null)
                {
                    Debug.LogError($"[SceneValidation] ERROR: Required GameObject '{name}' is missing from the scene!");
                    errorsCount++;
                }
            }

            // 2. Audit Main Camera Configuration
            var camGo = GameObject.Find("Main Camera");
            if (camGo != null)
            {
                var ic = camGo.GetComponent<InteractionController>();
                if (ic == null)
                {
                    Debug.LogError("[SceneValidation] ERROR: Main Camera is missing 'InteractionController' component!");
                    errorsCount++;
                }
            }

            // 3. Audit RAM Modules
            string[] ramModules = { "RamModule_1", "RamModule_2" };
            HashSet<string> partIds = new HashSet<string>();

            foreach (var modName in ramModules)
            {
                var go = GameObject.Find(modName);
                if (go != null)
                {
                    // Audit Components
                    var sel = go.GetComponent<SelectableObject>();
                    var drag = go.GetComponent<DraggablePart>();
                    var hc = go.GetComponent<HighlightController>();
                    var part = go.GetComponent<InstallablePart>();
                    var rb = go.GetComponent<Rigidbody>();
                    var col = go.GetComponent<BoxCollider>();

                    if (sel == null) { Debug.LogError($"[SceneValidation] ERROR: {modName} is missing 'SelectableObject'."); errorsCount++; }
                    if (drag == null) { Debug.LogError($"[SceneValidation] ERROR: {modName} is missing 'DraggablePart'."); errorsCount++; }
                    if (hc == null) { Debug.LogError($"[SceneValidation] ERROR: {modName} is missing 'HighlightController'."); errorsCount++; }
                    if (part == null) { Debug.LogError($"[SceneValidation] ERROR: {modName} is missing 'InstallablePart'."); errorsCount++; }

                    // Rigidbody Setup
                    if (rb == null)
                    {
                        Debug.LogError($"[SceneValidation] ERROR: {modName} is missing 'Rigidbody'.");
                        errorsCount++;
                    }
                    else
                    {
                        if (!rb.isKinematic) { Debug.LogError($"[SceneValidation] ERROR: {modName} Rigidbody must be IsKinematic = TRUE."); errorsCount++; }
                        if (rb.useGravity) { Debug.LogError($"[SceneValidation] ERROR: {modName} Rigidbody must be UseGravity = FALSE."); errorsCount++; }
                    }

                    // BoxCollider Setup
                    if (col == null) { Debug.LogError($"[SceneValidation] ERROR: {modName} is missing BoxCollider."); errorsCount++; }

                    // ID Audit
                    if (part != null)
                    {
                        if (string.IsNullOrEmpty(part.ComponentId))
                        {
                            Debug.LogError($"[SceneValidation] ERROR: {modName} 'InstallablePart' ID is not configured (empty).");
                            errorsCount++;
                        }
                        else
                        {
                            if (partIds.Contains(part.ComponentId))
                            {
                                Debug.LogError($"[SceneValidation] ERROR: Duplicate InstallablePart ComponentID found: '{part.ComponentId}'!");
                                errorsCount++;
                            }
                            partIds.Add(part.ComponentId);
                        }
                    }
                }
            }

            // 4. Audit RAM Slots
            string[] ramSlots = { "RamSlot_A1", "RamSlot_A2", "RamSlot_B1", "RamSlot_B2" };
            HashSet<string> slotIds = new HashSet<string>();

            foreach (var slotName in ramSlots)
            {
                var go = GameObject.Find(slotName);
                if (go != null)
                {
                    var slot = go.GetComponent<InstallationSlot>();
                    var hc = go.GetComponent<HighlightController>();
                    var col = go.GetComponent<BoxCollider>();

                    if (slot == null) { Debug.LogError($"[SceneValidation] ERROR: {slotName} is missing 'InstallationSlot' component!"); errorsCount++; }
                    if (hc == null) { Debug.LogWarning($"[SceneValidation] WARNING: {slotName} is missing 'HighlightController' for visual feedback."); warningsCount++; }

                    // Trigger check
                    if (col == null)
                    {
                        Debug.LogError($"[SceneValidation] ERROR: {slotName} is missing 'BoxCollider'.");
                        errorsCount++;
                    }
                    else
                    {
                        if (!col.isTrigger) { Debug.LogError($"[SceneValidation] ERROR: {slotName} BoxCollider must be marked as 'Is Trigger' = TRUE."); errorsCount++; }
                    }

                    // Unique Slot ID Check
                    if (slot != null)
                    {
                        if (string.IsNullOrEmpty(slot.SlotId))
                        {
                            Debug.LogError($"[SceneValidation] ERROR: {slotName} slot ID is not configured (empty).");
                            errorsCount++;
                        }
                        else
                        {
                            if (slotIds.Contains(slot.SlotId))
                            {
                                Debug.LogError($"[SceneValidation] ERROR: Duplicate Slot ID found: '{slot.SlotId}'!");
                                errorsCount++;
                            }
                            slotIds.Add(slot.SlotId);
                        }

                        // Inspect SnapPoint reference
                        var snapPointProp = typeof(InstallationSlot).GetField("snapPoint", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (snapPointProp != null)
                        {
                            var snapVal = snapPointProp.GetValue(slot) as Transform;
                            if (snapVal == null)
                            {
                                Debug.LogError($"[SceneValidation] ERROR: {slotName} 'InstallationSlot' has no SnapPoint Transform assigned!");
                                errorsCount++;
                            }
                        }
                    }
                }
            }

            // 5. Audit EventSystem and Canvas
            var canvas = GameObject.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("[SceneValidation] WARNING: No UI Canvas detected in the scene active hierarchy.");
                warningsCount++;
            }

            var es = GameObject.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (es == null)
            {
                Debug.LogWarning("[SceneValidation] WARNING: No EventSystem found in the active hierarchy.");
                warningsCount++;
            }

            // 6. Report Summary
            if (errorsCount == 0 && warningsCount == 0)
            {
                Debug.Log("<color=green>[SceneValidationUtility] SUCCESS: 0 errors, 0 warnings! All GameObjects, scripts, colliders, rigidbodies, and unique IDs are perfectly aligned!</color>");
                EditorUtility.DisplayDialog("PCBuilder Scene Validation", "Perfect! 0 errors, 0 warnings. The scene is fully configured and ready for production!", "OK");
            }
            else
            {
                Debug.LogWarning($"[SceneValidationUtility] COMPLETED: Found {errorsCount} error(s) and {warningsCount} warning(s). Please review Console logs above.");
                EditorUtility.DisplayDialog("PCBuilder Scene Validation", $"Completed with {errorsCount} Error(s) and {warningsCount} Warning(s). Check the Console for detailed logs.", "OK");
            }
        }
    }
}