using System;
using GamePause.PrayerTimeCalculator.Enums;

namespace GamePause.PrayerTimeCalculator.Data
{
    /// <summary>
    /// Complete configuration for prayer time calculations.
    /// Immutable value object.
    /// </summary>
    public sealed class CalculationParameters : IEquatable<CalculationParameters>
    {
        public CalculationMethod Method { get; }
        public AsrJuristicMethod AsrMethod { get; }
        public HighLatitudeRule HighLatitudeRule { get; }
        public MethodParameters MethodParameters { get; }
        public PrayerTimeAdjustments Adjustments { get; }
        
        public CalculationParameters(
            CalculationMethod method,
            MethodParameters methodParameters,
            AsrJuristicMethod asrMethod = AsrJuristicMethod.Standard,
            HighLatitudeRule highLatitudeRule = HighLatitudeRule.MiddleOfNight,
            PrayerTimeAdjustments? adjustments = null)
        {
            Method = method;
            MethodParameters = methodParameters;
            AsrMethod = asrMethod;
            HighLatitudeRule = highLatitudeRule;
            Adjustments = adjustments ?? PrayerTimeAdjustments.None;
        }
        
        public bool Equals(CalculationParameters other)
        {
            if (other is null) return false;
            
            return Method == other.Method &&
                   AsrMethod == other.AsrMethod &&
                   HighLatitudeRule == other.HighLatitudeRule &&
                   MethodParameters.Equals(other.MethodParameters) &&
                   Adjustments.Equals(other.Adjustments);
        }
        
        public override bool Equals(object obj)
        {
            return obj is CalculationParameters other && Equals(other);
        }
        
        public override int GetHashCode()
        {
            return HashCode.Combine(
                Method,
                AsrMethod,
                HighLatitudeRule,
                MethodParameters,
                Adjustments
            );
        }
    }
}