using GamePause.PrayerTimeCalculator.Data;
using GamePause.PrayerTimeCalculator.Enums;

namespace GamePause.PrayerTimeCalculator.Methods
{
    /// <summary>
    /// University of Islamic Sciences, Karachi calculation method.
    /// </summary>
    public sealed class KarachiMethod : BaseCalculationMethod
    {
        public override CalculationMethod Method => CalculationMethod.Karachi;
        public override string Name => "University of Islamic Sciences, Karachi";
        
        public override MethodParameters Parameters { get; } = new MethodParameters(
            fajrAngle: 18.0,
            ishaAngle: 18.0
        );
    }
}