using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GamePause.Location;
using GamePause.Debugging;
using System.Collections.Generic;

namespace GamePause.UI
{
    /// <summary>
    /// UI handler for manual location selection with country and city dropdowns
    /// </summary>
    public class ManualLocationUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        [Tooltip("Reference to the LocationManager")]
        private LocationManager _locationManager;

        [Header("UI Elements")]
        [SerializeField]
        [Tooltip("Dropdown for country selection")]
        private TMP_Dropdown _countryDropdown;

        [SerializeField]
        [Tooltip("Dropdown for city selection")]
        private TMP_Dropdown _cityDropdown;

        [SerializeField]
        [Tooltip("Button to apply the selected location")]
        private Button _applyButton;

        [SerializeField]
        [Tooltip("Button to cancel/close the UI")]
        private Button _cancelButton;

        [SerializeField]
        [Tooltip("Text to display current location info")]
        private TextMeshProUGUI _currentLocationText;

        [SerializeField]
        [Tooltip("Text to display selected location preview")]
        private TextMeshProUGUI _previewText;

        [SerializeField]
        [Tooltip("Panel/container to show/hide the UI")]
        private GameObject _uiPanel;

        [Header("Settings")]
        [SerializeField]
        [Tooltip("Auto-hide UI after applying location")]
        private bool _autoHideOnApply = true;

        [SerializeField]
        [Tooltip("Show confirmation before applying")]
        private bool _showPreview = true;

        private List<string> _countryList = new List<string>();
        private List<string> _cityList = new List<string>();
        private string _selectedCountry;
        private string _selectedCity;
        private LocationDatabase _locationDatabase;

        #region Unity Lifecycle

        private void Awake()
        {
            ValidateReferences();
        }

        private void Start()
        {
            Initialize();
        }

        private void OnEnable()
        {
            if (_locationManager != null && _locationManager.IsLocationAvailable)
            {
                UpdateCurrentLocationDisplay();
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Show the manual location selection UI
        /// </summary>
        public void Show()
        {
            if (_uiPanel != null)
            {
                _uiPanel.SetActive(true);
            }

            RefreshCountryList();
            UpdateCurrentLocationDisplay();
            ClearPreview();

            DebugLogger.Log("<color=#00FFFF>[ManualLocationUI]</color> UI opened.");
        }

        /// <summary>
        /// Hide the manual location selection UI
        /// </summary>
        public void Hide()
        {
            if (_uiPanel != null)
            {
                _uiPanel.SetActive(false);
            }

            DebugLogger.Log("<color=#00FFFF>[ManualLocationUI]</color> UI closed.");
        }

        /// <summary>
        /// Toggle the UI visibility
        /// </summary>
        public void Toggle()
        {
            if (_uiPanel != null && _uiPanel.activeSelf)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }

        /// <summary>
        /// Apply the currently selected location
        /// </summary>
        public void ApplySelectedLocation()
        {
            if (string.IsNullOrEmpty(_selectedCountry))
            {
                DebugLogger.LogWarning("[ManualLocationUI] Please select a country.");
                return;
            }

            if (string.IsNullOrEmpty(_selectedCity))
            {
                DebugLogger.LogWarning("[ManualLocationUI] Please select a city.");
                return;
            }

            if (_locationManager != null)
            {
                _locationManager.SetManualLocation(_selectedCountry, _selectedCity);
                DebugLogger.Log($"<color=#00FFFF>[ManualLocationUI]</color> Applied location: {_selectedCity}, {_selectedCountry}");

                if (_autoHideOnApply)
                {
                    Hide();
                }

                UpdateCurrentLocationDisplay();
            }
            else
            {
                DebugLogger.LogError("[ManualLocationUI] LocationManager reference is missing.");
            }
        }

        /// <summary>
        /// Refresh the UI with current data
        /// </summary>
        public void Refresh()
        {
            RefreshCountryList();
            UpdateCurrentLocationDisplay();
        }

        #endregion

        #region Private Methods

        private void ValidateReferences()
        {
            if (_locationManager == null)
            {
                _locationManager = FindFirstObjectByType<LocationManager>();
                
                if (_locationManager == null)
                {
                    DebugLogger.LogError("[ManualLocationUI] LocationManager not found. Please assign it in the inspector.");
                }
            }
        }

        private void Initialize()
        {
            // Get location database
            if (_locationManager != null && _locationManager.LocationDatabase != null)
            {
                _locationDatabase = _locationManager.LocationDatabase;
            }
            else
            {
                _locationDatabase = LocationDatabase.CreateDefault();
            }

            // Setup button listeners
            if (_applyButton != null)
            {
                _applyButton.onClick.AddListener(ApplySelectedLocation);
            }

            if (_cancelButton != null)
            {
                _cancelButton.onClick.AddListener(Hide);
            }

            // Setup dropdown listeners
            if (_countryDropdown != null)
            {
                _countryDropdown.onValueChanged.AddListener(OnCountrySelected);
            }

            if (_cityDropdown != null)
            {
                _cityDropdown.onValueChanged.AddListener(OnCitySelected);
            }

            // Initial population
            RefreshCountryList();
            UpdateCurrentLocationDisplay();

            // Start hidden if panel exists
            if (_uiPanel != null && _uiPanel.activeSelf)
            {
                _uiPanel.SetActive(false);
            }
        }

        private void RefreshCountryList()
        {
            _countryList.Clear();

            if (_locationDatabase != null)
            {
                foreach (var country in _locationDatabase.Countries)
                {
                    _countryList.Add(country.Name);
                }
            }

            if (_countryDropdown != null)
            {
                _countryDropdown.ClearOptions();
                
                List<string> options = new List<string> { "Select Country..." };
                options.AddRange(_countryList);
                
                _countryDropdown.AddOptions(options);
                _countryDropdown.value = 0;
            }

            // Clear city dropdown
            ClearCityDropdown();
            _selectedCountry = null;
            _selectedCity = null;
        }

        private void RefreshCityList(string countryName)
        {
            _cityList.Clear();

            if (_locationDatabase != null)
            {
                var country = _locationDatabase.Countries.Find(c => c.Name == countryName);
                if (country != null)
                {
                    foreach (var city in country.Cities)
                    {
                        _cityList.Add(city.Name);
                    }
                }
            }

            if (_cityDropdown != null)
            {
                _cityDropdown.ClearOptions();
                
                List<string> options = new List<string> { "Select City..." };
                options.AddRange(_cityList);
                
                _cityDropdown.AddOptions(options);
                _cityDropdown.value = 0;
                _cityDropdown.interactable = _cityList.Count > 0;
            }

            _selectedCity = null;
            ClearPreview();
        }

        private void ClearCityDropdown()
        {
            if (_cityDropdown != null)
            {
                _cityDropdown.ClearOptions();
                _cityDropdown.AddOptions(new List<string> { "Select City..." });
                _cityDropdown.value = 0;
                _cityDropdown.interactable = false;
            }
        }

        private void OnCountrySelected(int index)
        {
            // Index 0 is "Select Country..." placeholder
            if (index <= 0 || index > _countryList.Count)
            {
                _selectedCountry = null;
                ClearCityDropdown();
                ClearPreview();
                return;
            }

            _selectedCountry = _countryList[index - 1];
            RefreshCityList(_selectedCountry);
            
            DebugLogger.Log($"<color=#00FFFF>[ManualLocationUI]</color> Country selected: {_selectedCountry}");
        }

        private void OnCitySelected(int index)
        {
            // Index 0 is "Select City..." placeholder
            if (index <= 0 || index > _cityList.Count)
            {
                _selectedCity = null;
                ClearPreview();
                return;
            }

            _selectedCity = _cityList[index - 1];
            
            DebugLogger.Log($"<color=#00FFFF>[ManualLocationUI]</color> City selected: {_selectedCity}");
            
            if (_showPreview)
            {
                UpdatePreview();
            }
        }

        private void UpdatePreview()
        {
            if (_previewText == null)
                return;

            if (string.IsNullOrEmpty(_selectedCountry) || string.IsNullOrEmpty(_selectedCity))
            {
                ClearPreview();
                return;
            }

            // Find city data for preview
            if (_locationDatabase != null)
            {
                var country = _locationDatabase.Countries.Find(c => c.Name == _selectedCountry);
                if (country != null)
                {
                    var city = country.Cities.Find(c => c.Name == _selectedCity);
                    if (city != null)
                    {
                        _previewText.text = $"<b>Preview:</b>\n" +
                                           $"Location: {city.Name}, {country.Name}\n" +
                                           $"Coordinates: ({city.Latitude:F4}, {city.Longitude:F4})\n" +
                                           $"Timezone: {city.Timezone}";
                        return;
                    }
                }
            }

            _previewText.text = $"<b>Preview:</b>\n{_selectedCity}, {_selectedCountry}";
        }

        private void ClearPreview()
        {
            if (_previewText != null)
            {
                _previewText.text = "";
            }
        }

        private void UpdateCurrentLocationDisplay()
        {
            if (_currentLocationText == null)
                return;

            if (_locationManager != null && _locationManager.IsLocationAvailable)
            {
                var location = _locationManager.CurrentLocation;
                string source = location.IsManuallySet ? "(Manual)" : "(Auto)";
                _currentLocationText.text = $"<b>Current Location {source}:</b>\n" +
                                           $"{location.City}, {location.Country}\n" +
                                           $"({location.Latitude:F4}, {location.Longitude:F4})";
            }
            else
            {
                _currentLocationText.text = "<b>Current Location:</b>\nNot set";
            }
        }

        #endregion

        #region Static Helper

        /// <summary>
        /// Find the first ManualLocationUI in the scene using the new API
        /// </summary>
        private static T FindFirstObjectByType<T>() where T : Object
        {
            #if UNITY_2023_1_OR_NEWER
                return Object.FindAnyObjectByType<T>();
            #elif UNITY_2020_1_OR_NEWER
                return Object.FindObjectOfType<T>(true);
            #else
                return Object.FindObjectOfType<T>();
            #endif
        }

        #endregion
    }
}