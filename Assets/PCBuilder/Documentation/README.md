# PCBuilder — Interactive PC Maintenance Training Simulation

A highly polished, modular, and professional C# training simulation in Unity 6 demonstrating dual-channel RAM installation procedure, scoring, and automated report generation. Designed as a high-quality technical portfolio prototype.

---

## 1. Project Purpose
The purpose of **PCBuilder** is to provide an interactive, data-driven, and highly precise training module for PC maintenance procedures. This prototype implements a guided 5-step dual-channel RAM installation sequence, validating physical constraints (such as retention clips, slot compatibility, proximity tolerances, and notch/rotation angles) while offering three operational training modes (Teach, Practice, Assess) and detailed scoring analytics.

---

## 2. Interactive Training Modes

### 🎓 Teach Mode
*   **Complete Instructions**: Shows full, detailed step-by-step guidance.
*   **Visual Highlights**: Actively pulses or highlights required components (orange) and correct targets (green).
*   **Orientation Helpers**: Displays real-time warnings and alignment feedback directly on the interface.
*   **Interaction Guardrail**: Prevents unrelated out-of-order interactions.

### 🛠️ Practice Mode
*   **Guided Steps**: Shows current instructions but removes active object/slot pulsing highlights.
*   **On-Demand Hints**: Allows requesting a visual hint (decreases final score by 3 points).
*   **Score Telemetry**: Silently records wrong slot attempts, rotation errors, and out-of-order actions.

### ⏱️ Assess Mode
*   **Assessment Objectives**: Only displays the overall high-level objective. No hints or automatic highlights are available.
*   **Score Penalty**: Deducts points on mistakes, wrong slots, and out-of-order steps.
*   **Written Feedback Summary**: Generates a rigorous diagnostic assessment and action log.

---

## 3. Controls Reference

*   **Left Mouse Button (Hold & Drag)**: Select and pick up RAM modules.
*   **Q & E Keys (While Dragging)**: Spin the held RAM module around its local Y-axis to align the notches.
*   **Escape Key (While Dragging)**: Cancel the active drag and smoothly return the RAM module to its original workbench position.
*   **Right Mouse Button (Hold & Drag)**: Smoothly orbit the camera around the motherboard workspace center.
*   **Mouse Scroll Wheel**: Zoom the camera closer to or farther from the motherboard.
*   **F Key**: Automatically focus the camera back to frame the primary workspace.

---

## 4. Architectural Overview

The project is structured with strict adherence to SOLID design principles, breaking responsibilities into separate lightweight, single-purpose components rather than a monolithic manager:

```
Assets/PCBuilder/
├── Documentation/
│   └── README.md (This file)
├── Scenes/
│   └── PCBuilderPrototype.unity (Main simulation scene)
├── Scripts/
│   ├── Core/
│   │   ├── ProcedureData.cs (Procedure definitions and step schema)
│   │   ├── ProcedureRunner.cs (State machine, step progression, and validation)
│   │   └── ScoringService.cs (Score tracking, mistake penalties, and session times)
│   ├── Interaction/
│   │   ├── InstallablePart.cs (Manages part IDs, visual lerp snapping, and locking)
│   │   ├── InstallationSlot.cs (Trigger detection, orientation check, and highlights)
│   │   ├── RamSlotClip.cs (Interactive retaining clips, click detection, rotation animations)
│   │   ├── DraggablePart.cs (Maintains draggable states, mouse tracking, and Q/E rotation)
│   │   ├── SelectableObject.cs (Hover/selection event triggers)
│   │   └── InteractionController.cs (Screen raycasting, clip clicking, and drop validation)
│   ├── Camera/
│   │   └── OrbitCamera.cs (Orbit controls, zoom, focus, and workbench clipping protection)
│   ├── UI/
│   │   └── PCBuilderUI.cs (Programmatic, self-scaling TextMeshPro Canvas UI builder)
│   ├── Reporting/
│   │   └── ReportService.cs (Saves JSON and human-readable HTML reports)
│   └── Editor/
│       └── SceneValidationUtility.cs (Automated scene configurations audit tool)
```

---

## 5. Procedure Validation & Mechanics

### How Procedure Validation Works
The `ProcedureRunner` operates as a central state-driven manager. Any interaction (such as toggling a clip or releasing a RAM module) calls `ProcedureRunner.Instance.ValidateAction()`. The validator:
1.  Verifies if the action matches the type and targets of the current step.
2.  Ensures all prerequisite step IDs are marked completed.
3.  If successful: advances the step index, updates progress, and signals UI and Audio.
4.  If failed: records mistake telemetry, plays warning audio, returns components to starting coordinates, and outputs detailed error logs.

### Proximity & Orientation Detection
The `InstallationSlot` trigger zone tracks overlapping RAM modules. It calculates:
1.  **Proximity**: Distance from the module's transform to the target snap point. Must be within `1.5` units.
2.  **Orientation**: Angle between the module's rotation and the slot's snap transform. Must be within `25°`.
3.  If misaligned (such as a 180° rotation error), the slot highlights **Amber (Warning)** and guides the user to rotate the part.
4.  If correct, it highlights **Green (Valid)** and snaps smoothly on release.

---

## 6. Training Reports

At the end of each completed simulation, the `ReportService` automatically packages all session statistics and chronological action logs into two formats:
1.  **JSON Report (`Report_YYYYMMDD_HHMMSS.json`)**: Ideal for database ingestion, telemetry tracking, or class-wide performance analytics.
2.  **HTML Report (`Report_YYYYMMDD_HHMMSS.html`)**: A beautiful, fully styled dark-themed report sheet with collapsible logs, stats breakdown, and a pass/fail certificate.

**Output Location**: Saved to the platform's local application data folder:
`%userprofile%\AppData\LocalLow\DefaultCompany\PCBuilder\Reports\`

---

## 7. How to Extend the Simulation

### Adding a New Installable Component (e.g. CPU or GPU)
1.  Attach **`SelectableObject`**, **`DraggablePart`**, **`HighlightController`**, and **`InstallablePart`** components to your new mesh.
2.  In the inspector, configure a unique `Component ID` (e.g., `CPU_1`).
3.  Configure a kinematic `Rigidbody` and a `BoxCollider` for raycasting.

### Adding a New Installation Slot
1.  Create a target slot GameObject, attach **`InstallationSlot`**, **`HighlightController`**, and a **`BoxCollider`** (set to *Is Trigger*).
2.  Set `Accepted Component ID` to match your new part ID (e.g., `CPU_1`).
3.  Add a child Transform named `SnapPoint`, rotated and seated exactly where the component should fit. Assign it in the slot's Inspector reference.

### Adding a New Procedure Step
Open `ProcedureRunner.cs` and add a new step inside `InitializeProcedure()`:
```csharp
steps.Add(new ProcedureStepDefinition
{
    stepId = "Install_CPU",
    instruction = "Install CPU_1 into CPU_Socket_1.",
    hint = "Check gold notch alignment and lower the socket lever.",
    actionType = ProcedureActionType.InstallPart,
    objectId = "CPU_1",
    targetId = "CPU_Socket_1",
    prerequisiteStepIds = new List<string> { "Previous_Step_ID" }
});
```

---

## 8. Scene Validation Audit Utility
The prototype contains an editor-integrated scene validation utility. It acts as an automated "Linter" to ensure your training scenes are built perfectly with 0 runtime errors:
*   **To run validation**: Go to **Tools > PCBuilder > Validate Scene** in the top Unity menu bar.
*   **What it audits**: Verifies required objects, attached scripts, correct Rigidbody gravity/kinematics, trigger flags, non-empty and unique IDs, SnapPoint references, and event-system Canvas bindings.
