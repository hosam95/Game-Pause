using System;
using System.Collections.Generic;
using UnityEngine;
using GamePause.PrayerTimeCalculator.Tests;

public class Tester : MonoBehaviour
{
    int _passed;
    int _failed;
    List<string> _failures = new List<string>();

    void Start()
    {
        _passed = 0;
        _failed = 0;
        _failures.Clear();

        TestCalculationMethods();
        TestAstronomicalModule();

        Debug.Log($"Test run complete: {_passed} passed, {_failed} failed, {_passed + _failed} total");
        foreach (string failure in _failures)
            Debug.LogError(failure);
    }

    void Run(string name, Action test)
    {
        try
        {
            test();
            _passed++;
        }
        catch (Exception e)
        {
            _failed++;
            _failures.Add($"FAIL: {name} - {e.Message}");
            Debug.LogWarning($"FAIL: {name}\n{e}");
        }
    }

    void TestAstronomicalModule()
    {
        // Julian Date Calculator Tests
        JulianDateCalculatorTests julianTester = new JulianDateCalculatorTests();
        Run(nameof(julianTester.ToJulianDay_J2000Epoch_ReturnsCorrectValue), julianTester.ToJulianDay_J2000Epoch_ReturnsCorrectValue);
        Run(nameof(julianTester.ToJulianDay_KnownDate_ReturnsCorrectValue), julianTester.ToJulianDay_KnownDate_ReturnsCorrectValue);
        Run(nameof(julianTester.ToJulianDay_WithTime_IncludesTimeFraction), julianTester.ToJulianDay_WithTime_IncludesTimeFraction);
        Run(nameof(julianTester.FromJulianDay_J2000Epoch_ReturnsCorrectDate), julianTester.FromJulianDay_J2000Epoch_ReturnsCorrectDate);
        Run(nameof(julianTester.RoundTrip_PreservesDateTime), julianTester.RoundTrip_PreservesDateTime);
        Run(nameof(julianTester.ToJulianCentury_J2000_ReturnsZero), julianTester.ToJulianCentury_J2000_ReturnsZero);
        Run(nameof(julianTester.ToJulianCentury_2100_ReturnsOne), julianTester.ToJulianCentury_2100_ReturnsOne);
        Run(nameof(julianTester.ToJulianDayStartOfDay_IgnoresTime), julianTester.ToJulianDayStartOfDay_IgnoresTime);

        // Angle Utilities Tests
        AngleUtilitiesTests angleTester = new AngleUtilitiesTests();
        Run(nameof(angleTester.DegreesToRadians_90Degrees_ReturnsHalfPi), angleTester.DegreesToRadians_90Degrees_ReturnsHalfPi);
        Run(nameof(angleTester.DegreesToRadians_180Degrees_ReturnsPi), angleTester.DegreesToRadians_180Degrees_ReturnsPi);
        Run(nameof(angleTester.RadiansToDegrees_Pi_Returns180), angleTester.RadiansToDegrees_Pi_Returns180);
        Run(nameof(angleTester.NormalizeDegrees_NegativeAngle_ReturnsPositive), angleTester.NormalizeDegrees_NegativeAngle_ReturnsPositive);
        Run(nameof(angleTester.NormalizeDegrees_Over360_ReturnsNormalized), angleTester.NormalizeDegrees_Over360_ReturnsNormalized);
        Run(nameof(angleTester.SinDegrees_90_ReturnsOne), angleTester.SinDegrees_90_ReturnsOne);
        Run(nameof(angleTester.CosDegrees_0_ReturnsOne), angleTester.CosDegrees_0_ReturnsOne);
        Run(nameof(angleTester.TanDegrees_45_ReturnsOne), angleTester.TanDegrees_45_ReturnsOne);
        Run(nameof(angleTester.AsinDegrees_One_Returns90), angleTester.AsinDegrees_One_Returns90);
        Run(nameof(angleTester.AcosDegrees_Zero_Returns90), angleTester.AcosDegrees_Zero_Returns90);

        // Solar Calculator Tests
        SolarCalculatorTests solarTester = new SolarCalculatorTests();

        // Solar Position Tests
        Run(nameof(solarTester.GetSolarPosition_SummerSolstice_DeclinationIsPositive), solarTester.GetSolarPosition_SummerSolstice_DeclinationIsPositive);
        Run(nameof(solarTester.GetSolarPosition_WinterSolstice_DeclinationIsNegative), solarTester.GetSolarPosition_WinterSolstice_DeclinationIsNegative);
        Run(nameof(solarTester.GetSolarPosition_Equinox_DeclinationNearZero), solarTester.GetSolarPosition_Equinox_DeclinationNearZero);
        Run(nameof(solarTester.GetSolarPosition_EquationOfTime_WithinExpectedRange), solarTester.GetSolarPosition_EquationOfTime_WithinExpectedRange);

        // Solar Noon Tests
        Run(nameof(solarTester.GetSolarNoon_AtPrimeMeridian_NearTwelve), solarTester.GetSolarNoon_AtPrimeMeridian_NearTwelve);
        Run(nameof(solarTester.GetSolarNoon_EastLongitude_BeforeTwelve), solarTester.GetSolarNoon_EastLongitude_BeforeTwelve);
        Run(nameof(solarTester.GetSolarNoon_WestLongitude_AfterTwelve), solarTester.GetSolarNoon_WestLongitude_AfterTwelve);

        // Sunrise/Sunset Tests
        Run(nameof(solarTester.GetSunrise_BeforeSolarNoon), solarTester.GetSunrise_BeforeSolarNoon);
        Run(nameof(solarTester.GetSunset_AfterSolarNoon), solarTester.GetSunset_AfterSolarNoon);
        Run(nameof(solarTester.GetSunriseAndSunset_Symmetry_AroundSolarNoon), solarTester.GetSunriseAndSunset_Symmetry_AroundSolarNoon);
        Run(nameof(solarTester.GetSunrise_HighLatitudeSummer_MayReturnNaN), solarTester.GetSunrise_HighLatitudeSummer_MayReturnNaN);

        // Time For Angle Tests
        Run(nameof(solarTester.GetTimeForAngle_Fajr_BeforeSunrise), solarTester.GetTimeForAngle_Fajr_BeforeSunrise);
        Run(nameof(solarTester.GetTimeForAngle_Isha_AfterSunset), solarTester.GetTimeForAngle_Isha_AfterSunset);

        // Asr Tests
        Run(nameof(solarTester.GetAsrTime_Standard_AfterSolarNoon), solarTester.GetAsrTime_Standard_AfterSolarNoon);
        Run(nameof(solarTester.GetAsrTime_Hanafi_AfterStandard), solarTester.GetAsrTime_Hanafi_AfterStandard);
        Run(nameof(solarTester.GetAsrTime_BeforeSunset), solarTester.GetAsrTime_BeforeSunset);

        // Utility Tests
        Run(nameof(solarTester.HoursToDateTime_WholeHours_ConvertsCorrectly), solarTester.HoursToDateTime_WholeHours_ConvertsCorrectly);
        Run(nameof(solarTester.HoursToDateTime_FractionalHours_ConvertsCorrectly), solarTester.HoursToDateTime_FractionalHours_ConvertsCorrectly);
        Run(nameof(solarTester.HoursToDateTime_OverflowToNextDay_HandlesCorrectly), solarTester.HoursToDateTime_OverflowToNextDay_HandlesCorrectly);
        Run(nameof(solarTester.HoursToDateTime_NaN_ThrowsException), solarTester.HoursToDateTime_NaN_ThrowsException);
        Run(nameof(solarTester.GetMidnight_CalculatesCorrectly), solarTester.GetMidnight_CalculatesCorrectly);

        // Reference Value Tests
        Run(nameof(solarTester.Cairo_January_SunriseNearReference), solarTester.Cairo_January_SunriseNearReference);
        Run(nameof(solarTester.Cairo_January_SunsetNearReference), solarTester.Cairo_January_SunsetNearReference);
    }

    void TestCalculationMethods()
    {
        // CalculationMethodFactoryTests
        CalculationMethodFactoryTests factoryTests = new CalculationMethodFactoryTests();
        Run(nameof(factoryTests.Create_EgyptianAuthority_ReturnsCorrectProvider), factoryTests.Create_EgyptianAuthority_ReturnsCorrectProvider);
        Run(nameof(factoryTests.Create_UmmAlQura_ReturnsIntervalBasedIsha), factoryTests.Create_UmmAlQura_ReturnsIntervalBasedIsha);
        Run(nameof(factoryTests.Create_MuslimWorldLeague_ReturnsCorrectProvider), factoryTests.Create_MuslimWorldLeague_ReturnsCorrectProvider);
        Run(nameof(factoryTests.Create_ISNA_ReturnsCorrectProvider), factoryTests.Create_ISNA_ReturnsCorrectProvider);
        Run(nameof(factoryTests.Create_Karachi_ReturnsCorrectProvider), factoryTests.Create_Karachi_ReturnsCorrectProvider);
        Run(nameof(factoryTests.Create_Tehran_HasMaghribAngle), factoryTests.Create_Tehran_HasMaghribAngle);
        Run(nameof(factoryTests.Create_Qom_HasMaghribAngle), factoryTests.Create_Qom_HasMaghribAngle);
        Run(nameof(factoryTests.Create_MuslimsOfFrance_ReturnsCorrectProvider), factoryTests.Create_MuslimsOfFrance_ReturnsCorrectProvider);
        Run(nameof(factoryTests.Create_Russia_ReturnsCorrectProvider), factoryTests.Create_Russia_ReturnsCorrectProvider);
        Run(nameof(factoryTests.Create_Singapore_ReturnsCorrectProvider), factoryTests.Create_Singapore_ReturnsCorrectProvider);
        Run(nameof(factoryTests.GetAvailableMethods_ReturnsAllMethods), factoryTests.GetAvailableMethods_ReturnsAllMethods);
        Run(nameof(factoryTests.IsSupported_ValidMethod_ReturnsTrue), factoryTests.IsSupported_ValidMethod_ReturnsTrue);
        Run(nameof(factoryTests.Create_AllMethods_HaveValidParameters), factoryTests.Create_AllMethods_HaveValidParameters);
        Run(nameof(factoryTests.Create_AllMethods_HaveNonEmptyName), factoryTests.Create_AllMethods_HaveNonEmptyName);

        // MethodParametersTests
        MethodParametersTests paramTests = new MethodParametersTests();
        Run(nameof(paramTests.Constructor_AngleBased_SetsProperties), paramTests.Constructor_AngleBased_SetsProperties);
        Run(nameof(paramTests.Constructor_IntervalBased_SetsProperties), paramTests.Constructor_IntervalBased_SetsProperties);
        Run(nameof(paramTests.Constructor_WithMaghribAngle_SetsProperty), paramTests.Constructor_WithMaghribAngle_SetsProperty);
        Run(nameof(paramTests.Constructor_WithMaghribMinutes_SetsProperty), paramTests.Constructor_WithMaghribMinutes_SetsProperty);
        Run(nameof(paramTests.Equals_SameAngleValues_ReturnsTrue), paramTests.Equals_SameAngleValues_ReturnsTrue);
        Run(nameof(paramTests.Equals_SameIntervalValues_ReturnsTrue), paramTests.Equals_SameIntervalValues_ReturnsTrue);
        Run(nameof(paramTests.Equals_DifferentMaghribAngle_ReturnsFalse), paramTests.Equals_DifferentMaghribAngle_ReturnsFalse);
        Run(nameof(paramTests.ToString_AngleBased_ContainsAngles), paramTests.ToString_AngleBased_ContainsAngles);
        Run(nameof(paramTests.ToString_WithMaghribAngle_ContainsMaghrib), paramTests.ToString_WithMaghribAngle_ContainsMaghrib);
        Run(nameof(paramTests.ToString_IntervalBased_ContainsMinutes), paramTests.ToString_IntervalBased_ContainsMinutes);
    }
}
