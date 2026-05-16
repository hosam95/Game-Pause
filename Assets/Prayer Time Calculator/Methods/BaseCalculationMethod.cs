using GamePause.PrayerTimeCalculator.Data;
using GamePause.PrayerTimeCalculator.Enums;

namespace GamePause.PrayerTimeCalculator.Methods
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