using GamePause.PrayerTimeCalculator.Data;
using GamePause.PrayerTimeCalculator.Enums;

namespace GamePause.PrayerTimeCalculator.Methods
{
    /// <summary>
    /// Islamic Society of North America calculation method.
    /// </summary>
    public sealed class IsnaMethod : BaseCalculationMethod
    {
        public override CalculationMethod Method => CalculationMethod.ISNA;
        public override string Name => "Islamic Society of North America";
        
        public override MethodParameters Parameters { get; } = new MethodParameters(
            fajrAngle: 15.0,
            ishaAngle: 15.0
        );
    }
}