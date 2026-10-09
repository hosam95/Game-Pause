using System;
using GamePause.SalahEvents.PrayerTimes.Enums;
using GamePause.SalahEvents.PrayerTimes.Methods;

namespace GamePause.SalahEvents.PrayerTimes.Data
{
    /// <summary>
    /// Complete configuration for prayer time calculations.
    /// Immutable value object.
    /// </summary>
    public sealed class CalculationParameters : IEquatable<CalculationParameters>
    {
        public CalculationMethod Method { get; }

        /// <summary>Tehran and Qom follow Shia rulings at high latitudes (see <see cref="ShiaMarja"/>).</summary>
        public bool IsShiaMethod => Method == CalculationMethod.Tehran || Method == CalculationMethod.Qom;
        public AsrJuristicMethod AsrMethod { get; }
        public HighLatitudeRule HighLatitudeRule { get; }
        public MethodParameters MethodParameters { get; }
        public PrayerTimeAdjustments Adjustments { get; }
        public PolarEstimationRule PolarEstimationRule { get; }

        /// <summary>Only used by the Shia methods (Tehran, Qom).</summary>
        public ShiaMarja ShiaMarja { get; }
        
        public CalculationParameters(
            CalculationMethod method,
            MethodParameters methodParameters,
            AsrJuristicMethod asrMethod = AsrJuristicMethod.Standard,
            HighLatitudeRule highLatitudeRule = HighLatitudeRule.AngleBased,
            PrayerTimeAdjustments? adjustments = null,
            PolarEstimationRule polarEstimationRule = PolarEstimationRule.ReferenceLatitude,
            ShiaMarja shiaMarja = ShiaMarja.Khamenei)
        {
            Method = method;
            MethodParameters = methodParameters;
            AsrMethod = asrMethod;
            HighLatitudeRule = highLatitudeRule;
            Adjustments = adjustments ?? PrayerTimeAdjustments.None;
            PolarEstimationRule = polarEstimationRule;
            ShiaMarja = shiaMarja;
        }
        
        /// <summary>
        /// Creates parameters using the angles of a predefined calculation method.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when the method is not supported.</exception>
        public static CalculationParameters ForMethod(
            CalculationMethod method,
            AsrJuristicMethod asrMethod = AsrJuristicMethod.Standard,
            HighLatitudeRule highLatitudeRule = HighLatitudeRule.AngleBased,
            PrayerTimeAdjustments? adjustments = null,
            PolarEstimationRule polarEstimationRule = PolarEstimationRule.ReferenceLatitude,
            ShiaMarja shiaMarja = ShiaMarja.Khamenei)
        {
            MethodParameters methodParameters = CalculationMethodFactory.Create(method).Parameters;
            return new CalculationParameters(
                method, methodParameters, asrMethod, highLatitudeRule, adjustments, polarEstimationRule, shiaMarja);
        }
        
        public bool Equals(CalculationParameters other)
        {
            if (other is null) return false;
            
            return Method == other.Method &&
                   AsrMethod == other.AsrMethod &&
                   HighLatitudeRule == other.HighLatitudeRule &&
                   MethodParameters.Equals(other.MethodParameters) &&
                   Adjustments.Equals(other.Adjustments) &&
                   PolarEstimationRule == other.PolarEstimationRule &&
                   ShiaMarja == other.ShiaMarja;
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
                Adjustments,
                PolarEstimationRule,
                ShiaMarja
            );
        }
    }
}