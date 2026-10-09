using System;
using System.Text;
using UnityEngine;
using UnityEngine.Events;

namespace GamePause.SalahEvents.PrayerTimeAccuracy
{
    /// <summary>
    /// MonoBehaviour wrapper for the prayer-time accuracy test.
    ///
    /// Single click: the "Run Accuracy Test" inspector button evaluates the full
    /// reference dataset (4048 records, AlAdhan API / PrayTimes-style engine)
    /// against the Game Pause calculation module, fills the public counters,
    /// logs a formatted report and invokes OnTestingDone.
    ///
    /// The same logic is reachable headlessly:
    ///     PrayerTimeAccuracyTester.LoadDataset()  ->  PrayerTimeAccuracyEngine.Evaluate(dataset)
    /// </summary>
    public class PrayerTimeAccuracyTester : MonoBehaviour
    {
        [Tooltip("Optional dataset asset. If null, loads 'Resources/prayer_times_dataset'.")]
        [SerializeField] private TextAsset datasetOverride;

        // ---- results (visible in the inspector after a run) ----
        [Header("Results")]
        public int totalRecords;
        public int totalComparisons;
        public int within1Min;
        public int within2Min;
        public int within5Min;
        public float meanAbsErrorMin;
        public int maxAbsErrorMin;
        public float pctWithin1Min;
        public float pctWithin2Min;
        public float pctWithin5Min;
        public string lastReport;

        [Tooltip("Invoked after a test run completes.")]
        public UnityEvent onTestingDone;

        [Button("Run Accuracy Test")]
        public void RunAccuracyTest()
        {
            AccuracyReport report = EvaluateDataset(LoadDataset(datasetOverride));
            ApplyReport(report);
            Debug.Log(lastReport);
            if (onTestingDone != null)
                onTestingDone.Invoke();
        }

        /// <summary>Loads the dataset (override asset, or the bundled Resources copy).</summary>
        public static PrayerTimeAccuracyDataset LoadDataset(TextAsset overrideAsset = null)
        {
            TextAsset text = overrideAsset;
            if (text == null)
            {
                text = Resources.Load<TextAsset>("prayer_times_dataset");
                if (text == null)
                    throw new InvalidOperationException(
                        "Accuracy dataset not found. Expected 'Resources/prayer_times_dataset' " +
                        "(Assets/GamePause/SalahEvents/PrayerTimes/AccuracyTest/Resources/prayer_times_dataset.json).");
            }
            PrayerTimeAccuracyDataset dataset = JsonUtility.FromJson<PrayerTimeAccuracyDataset>(text.text);
            if (dataset == null || dataset.records == null || dataset.records.Length == 0)
                throw new InvalidOperationException("Accuracy dataset failed to deserialize (no records).");
            return dataset;
        }

        /// <summary>Runs the engine over a dataset and returns the report.</summary>
        public static AccuracyReport EvaluateDataset(PrayerTimeAccuracyDataset dataset)
        {
            return PrayerTimeAccuracyEngine.Evaluate(dataset);
        }

        private void ApplyReport(AccuracyReport report)
        {
            totalRecords = report.total_records;
            totalComparisons = report.total_comparisons;
            within1Min = report.within_1_min;
            within2Min = report.within_2_min;
            within5Min = report.within_5_min;
            meanAbsErrorMin = (float)report.mean_abs_error_min;
            maxAbsErrorMin = (int)Math.Round(report.max_abs_error_min);
            pctWithin1Min = (float)report.PctWithin1Min;
            pctWithin2Min = (float)report.PctWithin2Min;
            pctWithin5Min = (float)report.PctWithin5Min;
            lastReport = FormatReport(report);
        }

        /// <summary>Formats a report as a human-readable multi-line string.</summary>
        public static string FormatReport(AccuracyReport report)
        {
            var sb = new StringBuilder(4096);
            sb.AppendLine("=== Prayer Time Accuracy Test ===");
            sb.AppendLine($"Records: {report.total_records}, comparisons: {report.total_comparisons}" +
                          (report.invalid_calculated_times > 0 ? $" (invalid raw times handled by high-latitude rule: {report.invalid_calculated_times})" : ""));
            sb.AppendLine($"Within +/-1 min : {report.within_1_min} ({report.PctWithin1Min:0.00}%)");
            sb.AppendLine($"Within +/-2 min : {report.within_2_min} ({report.PctWithin2Min:0.00}%)");
            sb.AppendLine($"Within +/-5 min : {report.within_5_min} ({report.PctWithin5Min:0.00}%)");
            sb.AppendLine($"Mean abs error: {report.mean_abs_error_min:0.000} min, max: {report.max_abs_error_min} min");
            sb.AppendLine();
            sb.AppendLine("Per prayer (diff = calculated - reference, in minutes):");
            sb.AppendLine($"  {"prayer",-9}{"n",6}{"mean",8}{"std",7}{"min",5}{"max",5}{"+/-1%",8}{"+/-2%",8}");
            foreach (var ps in report.per_prayer)
                sb.AppendLine($"  {ps.prayer,-9}{ps.n,6}{ps.mean_diff_min,8:F2}{ps.std_diff_min,7:F2}{ps.min_diff,5}{ps.max_diff,5}{ps.pct_within_1_min,7:F1}%{ps.pct_within_2_min,7:F1}%");
            sb.AppendLine();
            sb.AppendLine("Per method:");
            sb.AppendLine($"  {"method",-20}{"n",6}{"mean|d|",9}{"+/-1%",8}{"+/-2%",8}{"max",5}");
            foreach (var ms in report.per_method)
                sb.AppendLine($"  {ms.method,-20}{ms.n,6}{ms.mean_abs_diff_min,9:F3}{ms.pct_within_1_min,7:F1}%{ms.pct_within_2_min,7:F1}%{ms.max_abs_diff,5}");
            sb.AppendLine();
            int beyond2 = 0;
            foreach (var o in report.outliers)
                if (Math.Abs(o.diff_minutes) > 2) beyond2++;
            sb.AppendLine($"Deviations of 2+ min: {report.outliers.Count} (of which {beyond2} exceed +/-2 min)");
            int shown = 0;
            foreach (var o in report.outliers)
            {
                if (shown >= 25) { sb.AppendLine("  ... (see report CSV for the full list)"); break; }
                if (Math.Abs(o.diff_minutes) > 5)
                {
                    sb.AppendLine($"  {o.city} {o.date} {o.method}/{o.asr_school} {o.prayer}: " +
                                  $"ref {ToHhMm(o.reference_minutes)} vs calc {ToHhMm(o.calculated_minutes)} ({o.diff_minutes:+0} min)");
                    shown++;
                }
            }
            return sb.ToString();
        }

        private static string ToHhMm(int minutes) =>
            $"{minutes / 60:00}:{minutes % 60:00}";
    }
}
