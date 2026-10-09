using UnityEditor;
using UnityEngine;
using System.IO;
using System.Text;
using GamePause.SalahEvents.PrayerTimeAccuracy;

namespace GamePause.SalahEvents.PrayerTimeAccuracy.EditorTools
{
    /// <summary>
    /// Editor window for the prayer-time accuracy test.
    /// Menu: Game Pause / Prayer Time Accuracy Test
    /// </summary>
    public class PrayerTimeAccuracyTestWindow : EditorWindow
    {
        private AccuracyReport report;
        private string reportText = "No test run yet.";
        private string statusText = "";
        private Vector2 scroll;
        private float runningSeconds = -1f;

        [MenuItem("Game Pause/Prayer Time Accuracy Test")]
        private static void Open()
        {
            var window = GetWindow<PrayerTimeAccuracyTestWindow>("Prayer Accuracy");
            window.minSize = new Vector2(720, 480);
        }

        private void OnGUI()
        {
            GUILayout.Label("Prayer Time Accuracy Test", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Compares the Game Pause prayer-time calculation module against the bundled " +
                "AlAdhan API reference dataset (4048 records: 22 city/method combinations x " +
                "2 Asr schools x 92 dates in 2026). Reference engine: PrayTimes.org algorithm " +
                "family (AlAdhan/Meezaan).", MessageType.Info);

            if (runningSeconds < 0f)
            {
                if (GUILayout.Button("Run Full Accuracy Test (4048 records)", GUILayout.Height(36)))
                    RunTest();
            }
            else
            {
                GUILayout.Label($"Running... {runningSeconds:0.0}s elapsed");
            }

            if (report != null && GUILayout.Button("Save Outlier CSV to Editor.log path (Temp)"))
                SaveOutlierCsv();

            if (!string.IsNullOrEmpty(statusText))
            {
                EditorGUILayout.LabelField(statusText, EditorStyles.miniLabel);
            }

            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.TextArea(reportText, GUILayout.MinHeight(200));
            EditorGUILayout.EndScrollView();
        }

        private void RunTest()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            runningSeconds = 0f;
            Repaint();
            try
            {
                var dataset = PrayerTimeAccuracyTester.LoadDataset();
                report = PrayerTimeAccuracyEngine.Evaluate(dataset);
                reportText = PrayerTimeAccuracyTester.FormatReport(report);
                sw.Stop();
                statusText = $"Done in {sw.ElapsedMilliseconds} ms - {dataset.records.Length} records, " +
                             $"{report.total_comparisons} prayer-time comparisons. " +
                             $"{report.PctWithin2Min:0.00}% within +/-2 min.";
            }
            catch (System.Exception e)
            {
                statusText = "FAILED: " + e.Message;
                reportText = e.ToString();
            }
            runningSeconds = -1f;
            Repaint();
        }

        private void SaveOutlierCsv()
        {
            if (report == null) return;
            string path = Path.Combine(Application.dataPath, "../Temp/prayer_accuracy_outliers.csv");
            var sb = new StringBuilder();
            sb.AppendLine("city,date,method,asr_school,prayer,reference,calculated,diff_min");
            foreach (var o in report.outliers)
                sb.AppendLine($"{o.city},{o.date},{o.method},{o.asr_school},{o.prayer}," +
                              $"{o.reference_minutes},{o.calculated_minutes},{o.diff_minutes}");
            File.WriteAllText(path, sb.ToString());
            statusText = $"CSV written: {path} ({report.outliers.Count} rows)";
            Debug.Log(statusText);
            Repaint();
        }
    }
}
