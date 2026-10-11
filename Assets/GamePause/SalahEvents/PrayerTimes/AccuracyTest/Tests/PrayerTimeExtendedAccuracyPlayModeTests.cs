using System;
using NUnit.Framework;
using UnityEngine;

namespace GamePause.SalahEvents.PrayerTimeAccuracy.Tests
{
    /// <summary>
    /// Extended accuracy tests (issue #3): the AlAdhan latitude ladder, non-default high-latitude rules and
    /// daily solstice / clamp windows (prayer_times_dataset_extended), and the independent NASA/JPL DE421
    /// reference (prayer_times_dataset_jpl). Baseline numbers are in AccuracyTest/README.md; thresholds here
    /// sit just under the measured values so a regression fails the run.
    /// </summary>
    [TestFixture]
    public class PrayerTimeExtendedAccuracyPlayModeTests
    {
        private PrayerTimeAccuracyDataset extended;
        private PrayerTimeAccuracyDataset jpl;

        [OneTimeSetUp]
        public void Load()
        {
            extended = LoadDataset("prayer_times_dataset_extended");
            jpl = LoadDataset("prayer_times_dataset_jpl");
            Assert.AreEqual(3492, extended.records.Length, "Unexpected extended dataset size");
            Assert.AreEqual(1272, jpl.records.Length, "Unexpected JPL dataset size");
        }

        private static PrayerTimeAccuracyDataset LoadDataset(string name)
        {
            TextAsset text = Resources.Load<TextAsset>(name);
            Assert.That(text, Is.Not.Null, $"Dataset not found at Resources/{name}");
            var ds = JsonUtility.FromJson<PrayerTimeAccuracyDataset>(text.text);
            Assert.That(ds, Is.Not.Null);
            Assert.That(ds.records, Is.Not.Empty);
            return ds;
        }

        private static bool Rule(PrayerTimeAccuracyRecord r, string rule) => r.high_lat_rule == rule;

        [Test]
        public void Extended_Overall_WithinThresholds()
        {
            AccuracyReport r = PrayerTimeAccuracyEngine.Evaluate(extended);
            Assert.Greater(r.PctWithin1Min, 84.0, $"+/-1 min: {r.PctWithin1Min:0.00}%");
            Assert.Greater(r.PctWithin2Min, 95.0, $"+/-2 min: {r.PctWithin2Min:0.00}%");
            Assert.Greater(r.PctWithin5Min, 99.0, $"+/-5 min: {r.PctWithin5Min:0.00}%");
            Assert.Less(r.mean_abs_error_min, 0.8, $"MAE {r.mean_abs_error_min:0.000} min");
            Assert.LessOrEqual(r.max_abs_error_min, 25, "max abs error");
        }

        [Test]
        public void LatitudeLadder_UpTo56N_Within2Min()
        {
            // Madrid 40, Milan 45, Frankfurt 50, Copenhagen 56: method (MWL) is fixed, so any
            // drop here is a latitude effect, not a method effect.
            foreach (string city in new[] { "Madrid", "Milan", "Frankfurt", "Copenhagen" })
            {
                AccuracyReport r = PrayerTimeAccuracyEngine.Evaluate(extended,
                    x => x.city == city && Rule(x, "AngleBased"));
                Assert.Greater(r.PctWithin2Min, 98.0, $"{city}: +/-2 min {r.PctWithin2Min:0.00}%");
                Assert.LessOrEqual(r.max_abs_error_min, 3, $"{city}: max abs error");
            }
        }

        [Test]
        public void LatitudeLadder_Above60N_Within5Min()
        {
            foreach (string city in new[] { "Bergen", "Trondheim", "Reykjavik" })
            {
                AccuracyReport r = PrayerTimeAccuracyEngine.Evaluate(extended,
                    x => x.city == city && Rule(x, "AngleBased"));
                Assert.Greater(r.PctWithin5Min, 96.0, $"{city}: +/-5 min {r.PctWithin5Min:0.00}%");
            }
        }

        [Test]
        public void SeventhOfNight_MatchesReference_Within2Min_At50N()
        {
            AccuracyReport r = PrayerTimeAccuracyEngine.Evaluate(extended,
                x => x.city == "Frankfurt" && Rule(x, "SeventhOfNight"));
            Assert.Greater(r.total_records, 0);
            Assert.AreEqual(100.0, r.PctWithin2Min, $"Frankfurt SeventhOfNight +/-2 min {r.PctWithin2Min:0.00}%");
        }

        [Test]
        public void NonDefaultRules_Within5Min_ForAtLeast95Percent()
        {
            foreach (string rule in new[] { "MiddleOfNight", "SeventhOfNight" })
            {
                AccuracyReport r = PrayerTimeAccuracyEngine.Evaluate(extended, x => Rule(x, rule));
                Assert.Greater(r.total_records, 0, rule);
                Assert.Greater(r.PctWithin5Min, 96.0, $"{rule}: +/-5 min {r.PctWithin5Min:0.00}%");
            }
        }

        [Test]
        public void Extended_Sunrise_AndDhuhr_Within1Min()
        {
            AccuracyReport r = PrayerTimeAccuracyEngine.Evaluate(extended);
            Assert.Greater(r.per_prayer[Array.IndexOf(Names, "dhuhr")].pct_within_1_min, 99.5);
            Assert.Greater(r.per_prayer[Array.IndexOf(Names, "sunrise")].pct_within_1_min, 99.5);
        }

        [Test]
        public void Jpl_Overall_WithinThresholds()
        {
            AccuracyReport r = PrayerTimeAccuracyEngine.Evaluate(jpl);
            Assert.Greater(r.PctWithin1Min, 88.0, $"+/-1 min: {r.PctWithin1Min:0.00}%");
            Assert.Greater(r.PctWithin2Min, 96.5, $"+/-2 min: {r.PctWithin2Min:0.00}%");
            Assert.Greater(r.PctWithin5Min, 99.0, $"+/-5 min: {r.PctWithin5Min:0.00}%");
            Assert.Less(r.mean_abs_error_min, 0.8, $"MAE {r.mean_abs_error_min:0.000} min");
            Assert.LessOrEqual(r.max_abs_error_min, 10, "max abs error");
        }

        [Test]
        public void Jpl_Fajr_Dhuhr_Sunrise_Within1Min()
        {
            AccuracyReport r = PrayerTimeAccuracyEngine.Evaluate(jpl);
            Assert.AreEqual(100.0, r.per_prayer[Array.IndexOf(Names, "fajr")].pct_within_1_min);
            Assert.AreEqual(100.0, r.per_prayer[Array.IndexOf(Names, "dhuhr")].pct_within_1_min);
            Assert.Greater(r.per_prayer[Array.IndexOf(Names, "sunrise")].pct_within_1_min, 99.0);
        }

        private static readonly string[] Names = { "fajr", "sunrise", "dhuhr", "asr", "maghrib", "isha" };
    }
}
