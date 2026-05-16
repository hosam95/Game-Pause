using UnityEngine;
using GamePause.PrayerTimeCalculator.Tests;

public class Tester : MonoBehaviour
{
    void Start()
    {
        TestCalculationMethods();
        //TestAstronomicalModule();
    }

    void TestAstronomicalModule()
    {
        // Julian Date Calculator Tests
        JulianDateCalculatorTests julianTester = new JulianDateCalculatorTests();
        julianTester.ToJulianDay_J2000Epoch_ReturnsCorrectValue();
        julianTester.ToJulianDay_KnownDate_ReturnsCorrectValue();
        julianTester.ToJulianDay_WithTime_IncludesTimeFraction();
        julianTester.FromJulianDay_J2000Epoch_ReturnsCorrectDate();
        julianTester.RoundTrip_PreservesDateTime();
        julianTester.ToJulianCentury_J2000_ReturnsZero();
        julianTester.ToJulianCentury_2100_ReturnsOne();
        julianTester.ToJulianDayStartOfDay_IgnoresTime();

        // Angle Utilities Tests
        AngleUtilitiesTests angleTester = new AngleUtilitiesTests();
        angleTester.DegreesToRadians_90Degrees_ReturnsHalfPi();
        angleTester.DegreesToRadians_180Degrees_ReturnsPi();
        angleTester.RadiansToDegrees_Pi_Returns180();
        angleTester.NormalizeDegrees_NegativeAngle_ReturnsPositive();
        angleTester.NormalizeDegrees_Over360_ReturnsNormalized();
        angleTester.SinDegrees_90_ReturnsOne();
        angleTester.CosDegrees_0_ReturnsOne();
        angleTester.TanDegrees_45_ReturnsOne();
        angleTester.AsinDegrees_One_Returns90();
        angleTester.AcosDegrees_Zero_Returns90();

        // Solar Calculator Tests
        SolarCalculatorTests solarTester = new SolarCalculatorTests();
        
        // Solar Position Tests
        solarTester.GetSolarPosition_SummerSolstice_DeclinationIsPositive();
        solarTester.GetSolarPosition_WinterSolstice_DeclinationIsNegative();
        solarTester.GetSolarPosition_Equinox_DeclinationNearZero();
        solarTester.GetSolarPosition_EquationOfTime_WithinExpectedRange();
        
        // Solar Noon Tests
        solarTester.GetSolarNoon_AtPrimeMeridian_NearTwelve();
        solarTester.GetSolarNoon_EastLongitude_BeforeTwelve();
        solarTester.GetSolarNoon_WestLongitude_AfterTwelve();
        
        // Sunrise/Sunset Tests
        solarTester.GetSunrise_BeforeSolarNoon();
        solarTester.GetSunset_AfterSolarNoon();
        solarTester.GetSunriseAndSunset_Symmetry_AroundSolarNoon();
        solarTester.GetSunrise_HighLatitudeSummer_MayReturnNaN();
        
        // Time For Angle Tests
        solarTester.GetTimeForAngle_Fajr_BeforeSunrise();
        solarTester.GetTimeForAngle_Isha_AfterSunset();
        
        // Asr Tests
        solarTester.GetAsrTime_Standard_AfterSolarNoon();
        solarTester.GetAsrTime_Hanafi_AfterStandard();
        solarTester.GetAsrTime_BeforeSunset();
        
        // Utility Tests
        solarTester.HoursToDateTime_WholeHours_ConvertsCorrectly();
        solarTester.HoursToDateTime_FractionalHours_ConvertsCorrectly();
        solarTester.HoursToDateTime_OverflowToNextDay_HandlesCorrectly();
        solarTester.HoursToDateTime_NaN_ThrowsException();
        solarTester.GetMidnight_CalculatesCorrectly();
        
        // Reference Value Tests
        solarTester.Cairo_January_SunriseNearReference();
        solarTester.Cairo_January_SunsetNearReference();
    }

    void TestCalculationMethods()
    {
        // CalculationMethodFactoryTests
        CalculationMethodFactoryTests factoryTests = new CalculationMethodFactoryTests();
        factoryTests.Create_EgyptianAuthority_ReturnsCorrectProvider();
        factoryTests.Create_UmmAlQura_ReturnsIntervalBasedIsha();
        factoryTests.Create_MuslimWorldLeague_ReturnsCorrectProvider();
        factoryTests.Create_ISNA_ReturnsCorrectProvider();
        factoryTests.Create_Karachi_ReturnsCorrectProvider();
        factoryTests.Create_Tehran_HasMaghribAngle();
        factoryTests.Create_Qom_HasMaghribAngle();
        factoryTests.Create_MuslimsOfFrance_ReturnsCorrectProvider();
        factoryTests.Create_Russia_ReturnsCorrectProvider();
        factoryTests.Create_Singapore_ReturnsCorrectProvider();
        factoryTests.GetAvailableMethods_ReturnsAllMethods();
        factoryTests.IsSupported_ValidMethod_ReturnsTrue();
        factoryTests.Create_AllMethods_HaveValidParameters();
        factoryTests.Create_AllMethods_HaveNonEmptyName();

        // MethodParametersTests
        MethodParametersTests paramTests = new MethodParametersTests();
        paramTests.Constructor_AngleBased_SetsProperties();
        paramTests.Constructor_IntervalBased_SetsProperties();
        paramTests.Constructor_WithMaghribAngle_SetsProperty();
        paramTests.Constructor_WithMaghribMinutes_SetsProperty();
        paramTests.Equals_SameAngleValues_ReturnsTrue();
        paramTests.Equals_SameIntervalValues_ReturnsTrue();
        paramTests.Equals_DifferentMaghribAngle_ReturnsFalse();
        paramTests.ToString_AngleBased_ContainsAngles();
        paramTests.ToString_WithMaghribAngle_ContainsMaghrib();
        paramTests.ToString_IntervalBased_ContainsMinutes();
    }
}
