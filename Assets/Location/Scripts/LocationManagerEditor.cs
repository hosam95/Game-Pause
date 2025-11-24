#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using GamePause.Location;

namespace GamePause.Editor
{
    [CustomEditor(typeof(LocationManager))]
    public class LocationManagerEditor : UnityEditor.Editor
    {
        private LocationManager _locationManager;
        private int _selectedCountryIndex = 0;
        private int _selectedCityIndex = 0;
        private string[] _countryNames;
        private string[] _cityNames;
        private LocationDatabase _previewDatabase;

        private void OnEnable()
        {
            _locationManager = (LocationManager)target;
            _previewDatabase = LocationDatabase.CreateDefault();
            RefreshCountryList();
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Manual Location Selection", EditorStyles.boldLabel);

            if (_countryNames != null && _countryNames.Length > 0)
            {
                EditorGUI.BeginChangeCheck();
                _selectedCountryIndex = EditorGUILayout.Popup("Country", _selectedCountryIndex, _countryNames);
                if (EditorGUI.EndChangeCheck())
                {
                    RefreshCityList();
                    serializedObject.FindProperty("_selectedCountry").stringValue = _countryNames[_selectedCountryIndex];
                    serializedObject.ApplyModifiedProperties();
                }

                if (_cityNames != null && _cityNames.Length > 0)
                {
                    EditorGUI.BeginChangeCheck();
                    _selectedCityIndex = EditorGUILayout.Popup("City", _selectedCityIndex, _cityNames);
                    if (EditorGUI.EndChangeCheck())
                    {
                        serializedObject.FindProperty("_selectedCity").stringValue = _cityNames[_selectedCityIndex];
                        serializedObject.ApplyModifiedProperties();
                    }
                }
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Runtime Controls", EditorStyles.boldLabel);

            GUI.enabled = Application.isPlaying;

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Fetch Location"))
            {
                _locationManager.FetchLocation(false);
            }
            if (GUILayout.Button("Force Fetch"))
            {
                _locationManager.FetchLocation(true);
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Apply Manual Location"))
            {
                _locationManager.ApplySelectedManualLocation();
            }

            if (GUILayout.Button("Clear Cache"))
            {
                _locationManager.ClearCachedLocation();
            }

            GUI.enabled = true;

            // Display current status
            if (Application.isPlaying)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("Current Status", EditorStyles.boldLabel);
                
                EditorGUILayout.LabelField("Status:", _locationManager.Status.ToString());
                EditorGUILayout.LabelField("Provider:", _locationManager.ActiveProviderType.ToString());
                EditorGUILayout.LabelField("Has Location:", _locationManager.IsLocationAvailable.ToString());
                
                if (_locationManager.IsLocationAvailable)
                {
                    var loc = _locationManager.CurrentLocation;
                    EditorGUILayout.LabelField("Location:", $"{loc.City}, {loc.Country}");
                    EditorGUILayout.LabelField("Coordinates:", $"({loc.Latitude:F4}, {loc.Longitude:F4})");
                }

                if (_locationManager.PersistenceSettings.EnableRateLimiting)
                {
                    EditorGUILayout.LabelField("Rate Limited:", _locationManager.IsRateLimited.ToString());
                    if (_locationManager.IsRateLimited)
                    {
                        EditorGUILayout.LabelField("Next Update:", $"{_locationManager.HoursUntilNextUpdate:F1} hours");
                    }
                }

                Repaint();
            }
        }

        private void RefreshCountryList()
        {
            if (_previewDatabase != null)
            {
                _countryNames = new string[_previewDatabase.Countries.Count];
                for (int i = 0; i < _previewDatabase.Countries.Count; i++)
                {
                    _countryNames[i] = _previewDatabase.Countries[i].Name;
                }
                RefreshCityList();
            }
        }

        private void RefreshCityList()
        {
            if (_previewDatabase != null && _selectedCountryIndex < _previewDatabase.Countries.Count)
            {
                var country = _previewDatabase.Countries[_selectedCountryIndex];
                _cityNames = new string[country.Cities.Count];
                for (int i = 0; i < country.Cities.Count; i++)
                {
                    _cityNames[i] = country.Cities[i].Name;
                }
                _selectedCityIndex = 0;
            }
        }
    }
}
#endif