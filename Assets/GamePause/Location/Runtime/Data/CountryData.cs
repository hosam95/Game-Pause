using System;
using System.Collections.Generic;

namespace GamePause.Location
{
    /// <summary>
    /// Represents a country with its cities
    /// </summary>
    [Serializable]
    public class CountryData
    {
        public string Name;
        public string Code;
        public string Timezone;
        public List<CityData> Cities = new List<CityData>();
    }
}
