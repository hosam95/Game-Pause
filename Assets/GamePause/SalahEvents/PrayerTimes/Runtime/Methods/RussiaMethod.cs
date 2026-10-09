using GamePause.SalahEvents.PrayerTimes.Data;
using GamePause.SalahEvents.PrayerTimes.Enums;

namespace GamePause.SalahEvents.PrayerTimes.Methods
{
    /// <summary>
    /// Spiritual Administration of Muslims of Russia calculation method.
    /// </summary>
    public sealed class RussiaMethod : BaseCalculationMethod
    {
        public override CalculationMethod Method => CalculationMethod.Russia;
        public override string Name => "Spiritual Administration of Muslims of Russia";
        
        public override MethodParameters Parameters { get; } = new MethodParameters(
            fajrAngle: 16.0,
            ishaAngle: 15.0
        );
    }
}