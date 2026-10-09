using GamePause.SalahEvents.PrayerTimes.Data;
using GamePause.SalahEvents.PrayerTimes.Enums;

namespace GamePause.SalahEvents.PrayerTimes.Methods
{
    /// <summary>
    /// Institute of Geophysics, University of Tehran calculation method.
    /// Uses specific Maghrib angle (4.5°).
    /// </summary>
    public sealed class TehranMethod : BaseCalculationMethod
    {
        public override CalculationMethod Method => CalculationMethod.Tehran;
        public override string Name => "Institute of Geophysics, University of Tehran";
        
        public override MethodParameters Parameters { get; } = new MethodParameters(
            fajrAngle: 17.7,
            ishaAngle: 14.0,
            maghribAngle: 4.5
        );
    }
}