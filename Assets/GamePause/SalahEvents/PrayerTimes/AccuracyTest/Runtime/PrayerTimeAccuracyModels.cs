using System;
using System.Collections.Generic;

namespace GamePause.SalahEvents.PrayerTimeAccuracy
{
    /// <summary>
    /// One reference record from the dataset (AlAdhan API, PrayTimes-style engine).
    /// Field names must match the JSON keys in Data/prayer_times_dataset.json
    /// (Unity JsonUtility requires public fields with exact key names).
    /// </summary>
    [Serializable]
    public class PrayerTimeAccuracyRecord
    {
        public string city;
        public string country;
        public double lat;
        public double lon;
        public string tz;
        public string date;          // "YYYY-MM-DD"
        public string method;        // CalculationMethod enum name
        public int method_id;
        public string asr_school;    // "Standard" | "Hanafi"
        public int hijri_day;
        public int hijri_month_number;
        public double utc_offset;    // UTC offset at local noon of record date (hours)
        public string fajr;          // "HH:MM" local reference times
        public string sunrise;
        public string dhuhr;
        public string asr;
        public string maghrib;
        public string isha;
        public bool ramadan;         // true when reference uses Isha = Maghrib + 120 min (UmmAlQura, Ramaḍān)
    }

    /// <summary>Root object of the accuracy dataset JSON.</summary>
    [Serializable]
    public class PrayerTimeAccuracyDataset
    {
        public string source;
        public int year;
        public int date_step_days;
        public int record_count;
        public string[] prayers_compared;
        public string time_format;
        public string note;
        public PrayerTimeAccuracyRecord[] records;
    }

    /// <summary>Per-prayer statistics.</summary>
    [Serializable]
    public class PerPrayerStat
    {
        public string prayer;
        public int n;
        public double mean_diff_min;
        public double std_diff_min;
        public int min_diff;
        public int max_diff;
        public double pct_within_1_min;
        public double pct_within_2_min;
        // internal accumulators (not part of the report contract)
        public double sq;
        public int within1;
        public int within2;
    }

    /// <summary>Per-method statistics.</summary>
    [Serializable]
    public class PerMethodStat
    {
        public string method;
        public int n;
        public double mean_abs_diff_min;
        public double pct_within_1_min;
        public double pct_within_2_min;
        public int max_abs_diff;
    }

    /// <summary>One comparison whose difference exceeds the outlier threshold.</summary>
    [Serializable]
    public class OutlierEntry
    {
        public string city;
        public string date;
        public string method;
        public string asr_school;
        public string prayer;
        public int reference_minutes;
        public int calculated_minutes;
        public int diff_minutes;
    }

    /// <summary>Full result of an accuracy run.</summary>
    [Serializable]
    public class AccuracyReport
    {
        public int total_records;
        public int total_comparisons;   // records x 6 prayers
        public int invalid_calculated_times;
        public int within_1_min;
        public int within_2_min;
        public int within_5_min;
        public double mean_abs_error_min;
        public double max_abs_error_min;

        public double PctWithin1Min => total_comparisons > 0 ? 100.0 * within_1_min / total_comparisons : 0.0;
        public double PctWithin2Min => total_comparisons > 0 ? 100.0 * within_2_min / total_comparisons : 0.0;
        public double PctWithin5Min => total_comparisons > 0 ? 100.0 * within_5_min / total_comparisons : 0.0;

        public PerPrayerStat[] per_prayer = new PerPrayerStat[6]; // Fajr..Isha order
        public List<PerMethodStat> per_method = new List<PerMethodStat>();
        public List<OutlierEntry> outliers = new List<OutlierEntry>(); // |diff| >= 2 min
    }
}
