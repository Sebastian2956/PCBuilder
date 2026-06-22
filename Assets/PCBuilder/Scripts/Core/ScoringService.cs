using System;
using System.Collections.Generic;
using UnityEngine;

namespace PCBuilder.Core
{
    public class ScoringService : MonoBehaviour
    {
        private static ScoringService instance;
        public static ScoringService Instance => instance;

        [Header("Telemetry Session")]
        private float startTime;
        private float endTime;
        private int correctActionsCount = 0;
        private int incorrectActionsCount = 0;
        private int wrongSlotsCount = 0;
        private int orientationErrorsCount = 0;
        private int outOfOrderCount = 0;
        private int hintsUsedCount = 0;
        private int currentScore = 100;
        private bool isSessionActive = false;

        private List<string> actionLog = new List<string>();

        // Keeps track of already penalized actions per drag-attempt to prevent spamming penalties
        private HashSet<string> penalizedAttemptsThisDrag = new HashSet<string>();

        public int CurrentScore => currentScore;
        public float ElapsedTime => isSessionActive ? (Time.time - startTime) : (endTime - startTime);
        public int Mistakes => incorrectActionsCount;
        public int HintsUsed => hintsUsedCount;
        public bool Passed => currentScore >= 70;
        public List<string> ActionLog => actionLog;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void StartSession()
        {
            startTime = Time.time;
            correctActionsCount = 0;
            incorrectActionsCount = 0;
            wrongSlotsCount = 0;
            orientationErrorsCount = 0;
            outOfOrderCount = 0;
            hintsUsedCount = 0;
            currentScore = 100;
            isSessionActive = true;
            actionLog.Clear();
            penalizedAttemptsThisDrag.Clear();
            LogAction("Session started.");
        }

        public void EndSession()
        {
            endTime = Time.time;
            isSessionActive = false;
            LogAction($"Session completed. Final Score: {currentScore}, Time: {ElapsedTime:F1}s.");
        }

        public void ResetSession()
        {
            isSessionActive = false;
            correctActionsCount = 0;
            incorrectActionsCount = 0;
            wrongSlotsCount = 0;
            orientationErrorsCount = 0;
            outOfOrderCount = 0;
            hintsUsedCount = 0;
            currentScore = 100;
            actionLog.Clear();
            penalizedAttemptsThisDrag.Clear();
        }

        public void RecordCorrectAction(string details)
        {
            correctActionsCount++;
            LogAction($"CORRECT: {details}");
        }

        public void ClearPenaltiesForNewDrag()
        {
            penalizedAttemptsThisDrag.Clear();
        }

        public void RecordWrongSlot(string details)
        {
            // Only penalize once per placement attempt
            string key = $"WrongSlot_{details}";
            if (penalizedAttemptsThisDrag.Contains(key)) return;
            penalizedAttemptsThisDrag.Add(key);

            wrongSlotsCount++;
            incorrectActionsCount++;
            currentScore = Mathf.Clamp(currentScore - 8, 0, 100);
            LogAction($"ERROR (Wrong Slot): {details} (-8 points). Current Score: {currentScore}");
        }

        public void RecordOrientationError(string details)
        {
            string key = $"Orientation_{details}";
            if (penalizedAttemptsThisDrag.Contains(key)) return;
            penalizedAttemptsThisDrag.Add(key);

            orientationErrorsCount++;
            incorrectActionsCount++;
            currentScore = Mathf.Clamp(currentScore - 4, 0, 100);
            LogAction($"ERROR (Orientation): {details} (-4 points). Current Score: {currentScore}");
        }

        public void RecordOutOfOrderAction(string details)
        {
            string key = $"OutOfOrder_{details}";
            if (penalizedAttemptsThisDrag.Contains(key)) return;
            penalizedAttemptsThisDrag.Add(key);

            outOfOrderCount++;
            incorrectActionsCount++;
            currentScore = Mathf.Clamp(currentScore - 5, 0, 100);
            LogAction($"ERROR (Out of Order): {details} (-5 points). Current Score: {currentScore}");
        }

        public void RecordHintUsed(string details)
        {
            hintsUsedCount++;
            currentScore = Mathf.Clamp(currentScore - 3, 0, 100);
            LogAction($"HINT USED: {details} (-3 points). Current Score: {currentScore}");
        }

        public void LogAction(string message)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string entry = $"[{timestamp}] {message}";
            actionLog.Add(entry);
            Debug.Log(entry);
        }

        public SessionReportData GetReportData(string procedureName, string mode)
        {
            return new SessionReportData
            {
                dateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                procedureName = procedureName,
                mode = mode,
                score = currentScore,
                pass = Passed,
                duration = ElapsedTime,
                mistakes = incorrectActionsCount,
                hints = hintsUsedCount,
                completedSteps = correctActionsCount,
                actionLog = actionLog
            };
        }
    }

    [Serializable]
    public class SessionReportData
    {
        public string dateTime;
        public string procedureName;
        public string mode;
        public int score;
        public bool pass;
        public float duration;
        public int mistakes;
        public int hints;
        public int completedSteps;
        public List<string> actionLog;
    }
}