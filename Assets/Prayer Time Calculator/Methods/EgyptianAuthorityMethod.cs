using GamePause.PrayerTimeCalculator.Data;
using GamePause.PrayerTimeCalculator.Enums;

namespace GamePause.PrayerTimeCalculator.Methods
{
    /// <summary>
    /// Egyptian General Authority of Survey calculation method.
    /// </summary>
    public sealed class EgyptianAuthorityMethod : BaseCalculationMethod
    {
        public override CalculationMethod Method => CalculationMethod.EgyptianAuthority;
        public override string Name => "Egyptian General Authority of Survey";
        
        public override MethodParameters Parameters { get; } = new MethodParameters(
            fajrAngle: 19.5,
            ishaAngle: 17.5
        );
    }
}