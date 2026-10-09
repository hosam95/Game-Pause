using GamePause.SalahEvents.PrayerTimes.Data;
using GamePause.SalahEvents.PrayerTimes.Enums;

namespace GamePause.SalahEvents.PrayerTimes.Methods
{
    /// <summary>
    /// Shia Ithna-Ashari, Leva Institute, Qom calculation method.
    /// Uses specific Maghrib angle (4°).
    /// </summary>
    public sealed class QomMethod : BaseCalculationMethod
    {
        public override CalculationMethod Method => CalculationMethod.Qom;
        public override string Name => "Shia Ithna-Ashari, Leva Institute, Qom";
        
        public override MethodParameters Parameters { get; } = new MethodParameters(
            fajrAngle: 16.0,
            ishaAngle: 14.0,
            maghribAngle: 4.0
        );
    }
}