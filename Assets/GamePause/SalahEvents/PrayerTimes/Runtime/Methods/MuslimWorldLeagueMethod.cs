using GamePause.SalahEvents.PrayerTimes.Data;
using GamePause.SalahEvents.PrayerTimes.Enums;

namespace GamePause.SalahEvents.PrayerTimes.Methods
{
    /// <summary>
    /// Muslim World League calculation method.
    /// </summary>
    public sealed class MuslimWorldLeagueMethod : BaseCalculationMethod
    {
        public override CalculationMethod Method => CalculationMethod.MuslimWorldLeague;
        public override string Name => "Muslim World League";
        
        public override MethodParameters Parameters { get; } = new MethodParameters(
            fajrAngle: 18.0,
            ishaAngle: 17.0
        );
    }
}