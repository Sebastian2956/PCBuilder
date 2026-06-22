using System;
using System.IO;
using System.Text;
using UnityEngine;
using PCBuilder.Core;

namespace PCBuilder.Reporting
{
    public static class ReportService
    {
        /// <summary>
        /// Generates both a JSON report and a human-readable HTML/Text report for the training session.
        /// </summary>
        public static void GenerateReport(SessionReportData data)
        {
            string folderPath = Path.Combine(Application.persistentDataPath, "Reports");
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string jsonFileName = $"Report_{timestamp}.json";
            string textFileName = $"Report_{timestamp}.html";

            string jsonPath = Path.Combine(folderPath, jsonFileName);
            string textPath = Path.Combine(folderPath, textFileName);

            // 1. Save JSON
            try
            {
                string jsonContent = JsonUtility.ToJson(data, true);
                File.WriteAllText(jsonPath, jsonContent);
                Debug.Log($"[ReportService] JSON Report saved to: {jsonPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ReportService] Failed to save JSON report: {ex.Message}");
            }

            // 2. Save HTML
            try
            {
                string htmlContent = BuildHtmlReport(data);
                File.WriteAllText(textPath, htmlContent);
                Debug.Log($"[ReportService] HTML Training Report successfully written to: {textPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ReportService] Failed to save HTML report: {ex.Message}");
            }
        }

        private static string BuildHtmlReport(SessionReportData data)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html>");
            sb.AppendLine("<head>");
            sb.AppendLine("<title>PCBuilder - Training Session Report</title>");
            sb.AppendLine("<style>");
            sb.AppendLine("body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #121212; color: #e0e0e0; margin: 40px; }");
            sb.AppendLine(".container { max-width: 800px; margin: auto; background-color: #1a1a1a; padding: 30px; border-radius: 8px; border: 1px solid #2a2a2a; box-shadow: 0 4px 15px rgba(0,0,0,0.5); }");
            sb.AppendLine("h1 { color: #00adb5; border-bottom: 2px solid #2a2a2a; padding-bottom: 10px; margin-top: 0; }");
            sb.AppendLine(".meta-table { width: 100%; border-collapse: collapse; margin-bottom: 30px; }");
            sb.AppendLine(".meta-table td { padding: 10px; border-bottom: 1px solid #252525; }");
            sb.AppendLine(".meta-table td.label { font-weight: bold; color: #888; width: 35%; }");
            sb.AppendLine(".status-pass { color: #393; font-weight: bold; background-color: rgba(51,153,51,0.15); padding: 5px 12px; border-radius: 4px; display: inline-block; }");
            sb.AppendLine(".status-fail { color: #f33; font-weight: bold; background-color: rgba(255,51,51,0.15); padding: 5px 12px; border-radius: 4px; display: inline-block; }");
            sb.AppendLine(".log-container { background-color: #0f0f0f; border: 1px solid #222; padding: 15px; border-radius: 4px; font-family: 'Courier New', Courier, monospace; font-size: 14px; line-height: 1.5; max-height: 300px; overflow-y: auto; }");
            sb.AppendLine(".log-entry { margin-bottom: 5px; color: #8bc34a; }");
            sb.AppendLine(".log-entry-error { margin-bottom: 5px; color: #f44336; }");
            sb.AppendLine(".log-entry-info { margin-bottom: 5px; color: #2196f3; }");
            sb.AppendLine("</style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("<div class='container'>");
            sb.AppendLine("<h1>PCBuilder - Training Session Report</h1>");

            sb.AppendLine("<table class='meta-table'>");
            sb.AppendLine($"<tr><td class='label'>Date & Time</td><td>{data.dateTime}</td></tr>");
            sb.AppendLine($"<tr><td class='label'>Procedure Name</td><td>{data.procedureName}</td></tr>");
            sb.AppendLine($"<tr><td class='label'>Training Mode</td><td>{data.mode}</td></tr>");
            sb.AppendLine($"<tr><td class='label'>Final Score</td><td><strong style='font-size: 1.2em;'>{data.score} / 100</strong></td></tr>");
            
            string statusClass = data.pass ? "status-pass" : "status-fail";
            string statusText = data.pass ? "PASS" : "FAIL (Min passing score: 70)";
            sb.AppendLine($"<tr><td class='label'>Status</td><td><span class='{statusClass}'>{statusText}</span></td></tr>");
            
            sb.AppendLine($"<tr><td class='label'>Duration</td><td>{data.duration:F1} seconds</td></tr>");
            sb.AppendLine($"<tr><td class='label'>Mistakes Committed</td><td>{data.mistakes}</td></tr>");
            sb.AppendLine($"<tr><td class='label'>Hints Used</td><td>{data.hints}</td></tr>");
            sb.AppendLine($"<tr><td class='label'>Completed Steps</td><td>{data.completedSteps}</td></tr>");
            sb.AppendLine("</table>");

            sb.AppendLine("<h2>Chronological Action Log</h2>");
            sb.AppendLine("<div class='log-container'>");
            foreach (var log in data.actionLog)
            {
                string cssClass = "log-entry";
                if (log.Contains("ERROR")) cssClass = "log-entry-error";
                else if (log.Contains("HINT") || log.Contains("Session")) cssClass = "log-entry-info";

                sb.AppendLine($"<div class='{cssClass}'>{EscapeHtml(log)}</div>");
            }
            sb.AppendLine("</div>");

            sb.AppendLine("</div>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return sb.ToString();
        }

        private static string EscapeHtml(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";
            return input.Replace("&", "&amp;")
                        .Replace("<", "&lt;")
                        .Replace(">", "&gt;")
                        .Replace("\"", "&quot;")
                        .Replace("'", "&#x27;");
        }
    }
}