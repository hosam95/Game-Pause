using System;
using UnityEngine;

namespace GamePause.Samples
{
    /// <summary>
    /// Debug tool for testing and visualizing prayer time calculations.
    /// Attach to a GameObject in a test scene to validate calculations.
    /// </summary>
    public class PrayerTimeDebugger : MonoBehaviour
    {
        [Header("=== INPUT CONFIGURATION ===")]
        
        [Tooltip("Latitude of the location (-90 to 90)")]
        [Range(-90f, 90f)]
        [SerializeField] private double latitude = 30.0444; // Cairo default
        
        [Tooltip("Longitude of the location (-180 to 180)")]
        [Range(-180f, 180f)]
        [SerializeField] private double longitude = 31.2357; // Cairo default
        
        [Header("Calculation Method")]
        [SerializeField] private CalculationMethodOption calculationMethod = CalculationMethodOption.EgyptianAuthority;
        
        [SerializeField] private AsrJuristicOption asrMethod = AsrJuristicOption.Standard;
        
        [SerializeField] private HighLatitudeOption highLatitudeRule = HighLatitudeOption.MiddleOfNight;
        
        [Header("Manual Adjustments (minutes)")]
        [SerializeField] private int fajrAdjustment = 0;
        [SerializeField] private int sunriseAdjustment = 0;
        [SerializeField] private int dhuhrAdjustment = 0;
        [SerializeField] private int asrAdjustment = 0;
        [SerializeField] private int maghribAdjustment = 0;
        [SerializeField] private int ishaAdjustment = 0;
        
        [Header("Date Override (leave empty for current UTC)")]
        [SerializeField] private bool useCustomDate = false;
        [SerializeField] private int customYear = 2024;
        [SerializeField] private int customMonth = 1;
        [SerializeField] private int customDay = 1;
        
        [Header("=== TEST CONTROLS ===")]
        [SerializeField] private bool calculateOnStart = true;
        [SerializeField] private bool autoRecalculateOnChange = true;
        
        [Header("=== PRESET TEST LOCATIONS ===")]
        [SerializeField] private TestLocation testLocationPreset = TestLocation.Custom;
        
        [Header("=== OUTPUT (Read Only) ===")]
        [SerializeField] private string currentUtcTime;
        [SerializeField] private string calculationDate;
        
        [Space(10)]
        [SerializeField] private string fajrTime;
        [SerializeField] private string sunriseTime;
        [SerializeField] private string dhuhrTime;
        [SerializeField] private string asrTime;
        [SerializeField] private string maghribTime;
        [SerializeField] private string ishaTime;
        
        [Space(10)]
        [SerializeField] private string nextPrayer;
        [SerializeField] private string timeUntilNext;
        [SerializeField] private string resultExpiry;
        
        [Space(10)]
        [SerializeField] private string calculationStatus;
        [SerializeField] private string highLatitudeApplied;
        
        // Cached values for change detection
        private double _lastLatitude;
        private double _lastLongitude;
        private CalculationMethodOption _lastMethod;
        private AsrJuristicOption _lastAsrMethod;
        private HighLatitudeOption _lastHighLatRule;
        private TestLocation _lastTestLocation;
        
        #region Unity Lifecycle
        
        private void Start()
        {
            CacheCurrentValues();
            
            if (calculateOnStart)
            {
                Calculate();
            }
        }
        
        private void Update()
        {
            // Update current UTC time display
            currentUtcTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + " UTC";
            
            // Auto-recalculate if values changed
            if (autoRecalculateOnChange && HasConfigurationChanged())
            {
                OnConfigurationChanged();
            }
        }
        
        private void OnValidate()
        {
            // Apply test location preset when changed in inspector
            if (testLocationPreset != _lastTestLocation && testLocationPreset != TestLocation.Custom)
            {
                ApplyTestLocationPreset();
            }
        }
        
        #endregion
        
        #region Public Methods (For Inspector Buttons / External Calls)
        
        /// <summary>
        /// Triggers prayer time calculation with current configuration.
        /// Can be called from inspector button or external scripts.
        /// </summary>
        [ContextMenu("Calculate Prayer Times")]
        public void Calculate()
        {
            Debug.Log("[PrayerTimeDebugger] Starting calculation...");
            
            try
            {
                ValidateInputs();
                DateTime dateToCalculate = GetCalculationDate();
                
                // TODO: Replace with actual implementation
                // This is placeholder output for testing the test system
                SimulateCalculation(dateToCalculate);
                
                calculationStatus = "Success";
                Debug.Log("[PrayerTimeDebugger] Calculation completed successfully.");
            }
            catch (Exception ex)
            {
                calculationStatus = $"Error: {ex.Message}";
                Debug.LogError($"[PrayerTimeDebugger] Calculation failed: {ex}");
                ClearOutputs();
            }
        }
        
        /// <summary>
        /// Resets all configuration to default values.
        /// </summary>
        [ContextMenu("Reset To Defaults")]
        public void ResetToDefaults()
        {
            latitude = 30.0444;
            longitude = 31.2357;
            calculationMethod = CalculationMethodOption.EgyptianAuthority;
            asrMethod = AsrJuristicOption.Standard;
            highLatitudeRule = HighLatitudeOption.MiddleOfNight;
            
            fajrAdjustment = 0;
            sunriseAdjustment = 0;
            dhuhrAdjustment = 0;
            asrAdjustment = 0;
            maghribAdjustment = 0;
            ishaAdjustment = 0;
            
            useCustomDate = false;
            testLocationPreset = TestLocation.Custom;
            
            CacheCurrentValues();
            ClearOutputs();
            
            Debug.Log("[PrayerTimeDebugger] Reset to defaults.");
        }
        
        /// <summary>
        /// Runs a suite of validation tests against known reference values.
        /// </summary>
        [ContextMenu("Run Validation Tests")]
        public void RunValidationTests()
        {
            Debug.Log("[PrayerTimeDebugger] === VALIDATION TESTS ===");
            Debug.Log("Running tests against known reference values...");
            
            // Test 1: Cairo - Egyptian Authority
            RunSingleValidationTest(
                "Cairo - Egyptian Authority",
                30.0444, 31.2357,
                CalculationMethodOption.EgyptianAuthority,
                new DateTime(2024, 1, 15)
            );
            
            // Test 2: Mecca - Umm Al-Qura
            RunSingleValidationTest(
                "Mecca - Umm Al-Qura",
                21.4225, 39.8262,
                CalculationMethodOption.UmmAlQura,
                new DateTime(2024, 1, 15)
            );
            
            // Test 3: Oslo - High Latitude
            RunSingleValidationTest(
                "Oslo - High Latitude (June)",
                59.9139, 10.7522,
                CalculationMethodOption.EgyptianAuthority,
                new DateTime(2024, 6, 21)
            );
            
            // Test 4: Reykjavik - Extreme Latitude
            RunSingleValidationTest(
                "Reykjavik - Extreme Latitude",
                64.1466, -21.9426,
                CalculationMethodOption.EgyptianAuthority,
                new DateTime(2024, 6, 21)
            );
            
            Debug.Log("[PrayerTimeDebugger] === TESTS COMPLETE ===");
        }
        
        #endregion
        
        #region Private Methods
        
        private void ValidateInputs()
        {
            if (latitude < -90 || latitude > 90)
            {
                throw new ArgumentOutOfRangeException(nameof(latitude), 
                    $"Latitude must be between -90 and 90. Got: {latitude}");
            }
            
            if (longitude < -180 || longitude > 180)
            {
                throw new ArgumentOutOfRangeException(nameof(longitude), 
                    $"Longitude must be between -180 and 180. Got: {longitude}");
            }
        }
        
        private DateTime GetCalculationDate()
        {
            if (useCustomDate)
            {
                try
                {
                    return new DateTime(customYear, customMonth, customDay, 0, 0, 0, DateTimeKind.Utc);
                }
                catch
                {
                    Debug.LogWarning("[PrayerTimeDebugger] Invalid custom date, using current UTC date.");
                }
            }
            
            return DateTime.UtcNow.Date;
        }
        
        private void SimulateCalculation(DateTime date)
        {
            // This simulates output for testing the debugger UI
            // Will be replaced with actual calculation calls
            
            calculationDate = date.ToString("yyyy-MM-dd") + " UTC";
            
            // Placeholder times (for testing the test system only)
            fajrTime = "05:15:00 UTC (placeholder)";
            sunriseTime = "06:45:00 UTC (placeholder)";
            dhuhrTime = "12:05:00 UTC (placeholder)";
            asrTime = "15:20:00 UTC (placeholder)";
            maghribTime = "17:30:00 UTC (placeholder)";
            ishaTime = "19:00:00 UTC (placeholder)";
            
            nextPrayer = "Dhuhr (placeholder)";
            timeUntilNext = "02:35:00 (placeholder)";
            resultExpiry = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd HH:mm:ss") + " UTC";
            
            highLatitudeApplied = latitude > 48.5 || latitude < -48.5 
                ? $"Yes - {highLatitudeRule}" 
                : "No - Normal calculation";
            
            Debug.Log($"[PrayerTimeDebugger] Calculated for: Lat={latitude}, Lng={longitude}");
            Debug.Log($"[PrayerTimeDebugger] Method: {calculationMethod}, Asr: {asrMethod}");
            Debug.Log($"[PrayerTimeDebugger] High Latitude Rule: {highLatitudeRule}");
        }
        
        private void ClearOutputs()
        {
            fajrTime = "-";
            sunriseTime = "-";
            dhuhrTime = "-";
            asrTime = "-";
            maghribTime = "-";
            ishaTime = "-";
            nextPrayer = "-";
            timeUntilNext = "-";
            resultExpiry = "-";
            highLatitudeApplied = "-";
            calculationDate = "-";
        }
        
        private bool HasConfigurationChanged()
        {
            return Math.Abs(_lastLatitude - latitude) > 0.0001 ||
                   Math.Abs(_lastLongitude - longitude) > 0.0001 ||
                   _lastMethod != calculationMethod ||
                   _lastAsrMethod != asrMethod ||
                   _lastHighLatRule != highLatitudeRule;
        }
        
        private void OnConfigurationChanged()
        {
            CacheCurrentValues();
            Calculate();
        }
        
        private void CacheCurrentValues()
        {
            _lastLatitude = latitude;
            _lastLongitude = longitude;
            _lastMethod = calculationMethod;
            _lastAsrMethod = asrMethod;
            _lastHighLatRule = highLatitudeRule;
            _lastTestLocation = testLocationPreset;
        }
        
        private void ApplyTestLocationPreset()
        {
            switch (testLocationPreset)
            {
                case TestLocation.Cairo:
                    latitude = 30.0444;
                    longitude = 31.2357;
                    calculationMethod = CalculationMethodOption.EgyptianAuthority;
                    break;
                    
                case TestLocation.Mecca:
                    latitude = 21.4225;
                    longitude = 39.8262;
                    calculationMethod = CalculationMethodOption.UmmAlQura;
                    break;
                    
                case TestLocation.London:
                    latitude = 51.5074;
                    longitude = -0.1278;
                    break;
                    
                case TestLocation.Oslo:
                    latitude = 59.9139;
                    longitude = 10.7522;
                    break;
                    
                case TestLocation.Reykjavik:
                    latitude = 64.1466;
                    longitude = -21.9426;
                    break;
                    
                case TestLocation.NewYork:
                    latitude = 40.7128;
                    longitude = -74.0060;
                    break;
                    
                case TestLocation.Tokyo:
                    latitude = 35.6762;
                    longitude = 139.6503;
                    break;
                    
                case TestLocation.Sydney:
                    latitude = -33.8688;
                    longitude = 151.2093;
                    break;
                    
                case TestLocation.CapeTown:
                    latitude = -33.9249;
                    longitude = 18.4241;
                    break;
                    
                case TestLocation.NorthPole:
                    latitude = 89.0;
                    longitude = 0.0;
                    break;
                    
                case TestLocation.Equator:
                    latitude = 0.0;
                    longitude = 0.0;
                    break;
            }
            
            _lastTestLocation = testLocationPreset;
            CacheCurrentValues();
            
            Debug.Log($"[PrayerTimeDebugger] Applied preset: {testLocationPreset}");
        }
        
        private void RunSingleValidationTest(
            string testName, 
            double lat, 
            double lng, 
            CalculationMethodOption method,
            DateTime date)
        {
            Debug.Log($"[Test] {testName}");
            Debug.Log($"       Location: ({lat}, {lng})");
            Debug.Log($"       Method: {method}");
            Debug.Log($"       Date: {date:yyyy-MM-dd}");
            
            // TODO: Perform actual calculation and compare against reference
            Debug.Log($"       Status: PENDING (implementation required)");
            Debug.Log("       ---");
        }
        
        #endregion
        
        #region Nested Types
        
        public enum CalculationMethodOption
        {
            EgyptianAuthority,
            UmmAlQura
        }
        
        public enum AsrJuristicOption
        {
            Standard,   // Shafi'i, Maliki, Hanbali
            Hanafi      // Hanafi
        }
        
        public enum HighLatitudeOption
        {
            None,
            MiddleOfNight,
            SeventhOfNight,
            AngleBased
        }
        
        public enum TestLocation
        {
            Custom,
            Cairo,
            Mecca,
            London,
            Oslo,
            Reykjavik,
            NewYork,
            Tokyo,
            Sydney,
            CapeTown,
            NorthPole,
            Equator
        }
        
        #endregion
    }
}