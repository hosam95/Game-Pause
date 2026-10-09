using GamePause.SalahEvents.PrayerTimes.Data;
using GamePause.SalahEvents.PrayerTimes.Enums;

namespace GamePause.SalahEvents.PrayerTimes.Methods
{
    /// <summary>
    /// Base class for calculation method providers.
    /// </summary>
    public abstract class BaseCalculationMethod : ICalculationMethodProvider
    {
        public abstract CalculationMethod Method { get; }
        public abstract MethodParameters Parameters { get; }
        public abstract string Name { get; }
    }
}