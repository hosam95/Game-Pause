using GamePause.PrayerTimeCalculator.Data;
using GamePause.PrayerTimeCalculator.Enums;

namespace GamePause.PrayerTimeCalculator.Methods
{
    /// <summary>
    /// Grand Mosque of Paris / Muslims of France calculation method.
    /// </summary>
    public sealed class MuslimsOfFranceMethod : BaseCalculationMethod
    {
        public override CalculationMethod Method => CalculationMethod.MuslimsOfFrance;
        public override string Name => "Muslims of France (Grand Mosque of Paris)";
        
        public override MethodParameters Parameters { get; } = new MethodParameters(
            fajrAngle: 12.0,
            ishaAngle: 12.0
        );
    }
}