using UnityEngine;

namespace GamePause.Location
{
    /// <summary>
    /// Helper class for persisting location data
    /// </summary>
    public static class LocationPersistence
    {
        private const string LOCATION_DATA_KEY = "GamePause_LocationData";
        private const string LOCATION_DATABASE_KEY = "GamePause_LocationDatabase";

        public static void SaveLocation(LocationData location)
        {
            string json = JsonUtility.ToJson(location);
            PlayerPrefs.SetString(LOCATION_DATA_KEY, json);
            PlayerPrefs.Save();
        }

        public static LocationData LoadLocation()
        {
            if (PlayerPrefs.HasKey(LOCATION_DATA_KEY))
            {
                string json = PlayerPrefs.GetString(LOCATION_DATA_KEY);
                try
                {
                    return JsonUtility.FromJson<LocationData>(json);
                }
                catch
                {
                    return LocationData.Invalid;
                }
            }
            return LocationData.Invalid;
        }

        public static bool HasSavedLocation()
        {
            return PlayerPrefs.HasKey(LOCATION_DATA_KEY);
        }

        public static void ClearSavedLocation()
        {
            PlayerPrefs.DeleteKey(LOCATION_DATA_KEY);
            PlayerPrefs.Save();
        }

        public static void SaveLocationDatabase(LocationDatabase database)
        {
            string json = JsonUtility.ToJson(database);
            PlayerPrefs.SetString(LOCATION_DATABASE_KEY, json);
            PlayerPrefs.Save();
        }

        public static LocationDatabase LoadLocationDatabase()
        {
            if (PlayerPrefs.HasKey(LOCATION_DATABASE_KEY))
            {
                string json = PlayerPrefs.GetString(LOCATION_DATABASE_KEY);
                try
                {
                    return JsonUtility.FromJson<LocationDatabase>(json);
                }
                catch
                {
                    return LocationDatabase.CreateDefault();
                }
            }
            return LocationDatabase.CreateDefault();
        }
    }
}
