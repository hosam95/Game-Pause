using GamePause.SalahEvents.PrayerTimes.Data;
using GamePause.SalahEvents.PrayerTimes.Enums;

namespace GamePause.SalahEvents.PrayerTimes.Methods
{
    /// <summary>
    /// Provides parameters for a specific prayer time calculation method.
    /// </summary>
    public interface ICalculationMethodProvider
    {
        /// <summary>
        /// Gets the calculation method identifier.
        /// </summary>
        CalculationMethod Method { get; }
        
        /// <summary>
        /// Gets the method parameters (angles and intervals).
        /// </summary>
        MethodParameters Parameters { get; }
        
        /// <summary>
        /// Gets the display name of the method.
        /// </summary>
        string Name { get; }
    }
}