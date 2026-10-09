using GamePause.SalahEvents.PrayerTimes.Data;
using GamePause.SalahEvents.PrayerTimes.Enums;

namespace GamePause.SalahEvents.PrayerTimes.Methods
{
    /// <summary>
    /// Umm Al-Qura University, Mecca calculation method.
    /// Note: Uses 90 minutes after Maghrib for Isha (120 during Ramadan).
    /// </summary>
    public sealed class UmmAlQuraMethod : BaseCalculationMethod
    {
        public override CalculationMethod Method => CalculationMethod.UmmAlQura;
        public override string Name => "Umm Al-Qura University, Mecca";
        
        public override MethodParameters Parameters { get; } = new MethodParameters(
            fajrAngle: 18.5,
            ishaIntervalMinutes: 90
        );
    }
}