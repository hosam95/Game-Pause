using GamePause.SalahEvents.PrayerTimes.Data;
using GamePause.SalahEvents.PrayerTimes.Enums;

namespace GamePause.SalahEvents.PrayerTimes.Methods
{
    /// <summary>
    /// Majlis Ugama Islam Singapura calculation method.
    /// </summary>
    public sealed class SingaporeMethod : BaseCalculationMethod
    {
        public override CalculationMethod Method => CalculationMethod.Singapore;
        public override string Name => "Majlis Ugama Islam Singapura";
        
        public override MethodParameters Parameters { get; } = new MethodParameters(
            fajrAngle: 20.0,
            ishaAngle: 18.0
        );
    }
}