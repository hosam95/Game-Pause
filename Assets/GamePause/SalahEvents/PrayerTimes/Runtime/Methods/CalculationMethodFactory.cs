using System;
using System.Collections.Generic;
using GamePause.SalahEvents.PrayerTimes.Enums;

namespace GamePause.SalahEvents.PrayerTimes.Methods
{
    /// <summary>
    /// Factory for creating calculation method providers.
    /// </summary>
    public static class CalculationMethodFactory
    {
        private static readonly Dictionary<CalculationMethod, Func<ICalculationMethodProvider>> Providers =
            new Dictionary<CalculationMethod, Func<ICalculationMethodProvider>>
            {
                { CalculationMethod.EgyptianAuthority, () => new EgyptianAuthorityMethod() },
                { CalculationMethod.UmmAlQura, () => new UmmAlQuraMethod() },
                { CalculationMethod.MuslimWorldLeague, () => new MuslimWorldLeagueMethod() },
                { CalculationMethod.ISNA, () => new IsnaMethod() },
                { CalculationMethod.Karachi, () => new KarachiMethod() },
                { CalculationMethod.Tehran, () => new TehranMethod() },
                { CalculationMethod.Qom, () => new QomMethod() },
                { CalculationMethod.MuslimsOfFrance, () => new MuslimsOfFranceMethod() },
                { CalculationMethod.Russia, () => new RussiaMethod() },
                { CalculationMethod.Singapore, () => new SingaporeMethod() }
            };
        
        /// <summary>
        /// Creates a calculation method provider for the specified method.
        /// </summary>
        /// <param name="method">The calculation method.</param>
        /// <returns>The method provider.</returns>
        /// <exception cref="ArgumentException">Thrown when method is not supported.</exception>
        public static ICalculationMethodProvider Create(CalculationMethod method)
        {
            if (Providers.TryGetValue(method, out var factory))
            {
                return factory();
            }
            
            throw new ArgumentException($"Calculation method '{method}' is not supported.", nameof(method));
        }
        
        /// <summary>
        /// Gets all available calculation methods.
        /// </summary>
        public static IEnumerable<CalculationMethod> GetAvailableMethods()
        {
            return Providers.Keys;
        }
        
        /// <summary>
        /// Checks if a calculation method is supported.
        /// </summary>
        public static bool IsSupported(CalculationMethod method)
        {
            return Providers.ContainsKey(method);
        }
    }
}