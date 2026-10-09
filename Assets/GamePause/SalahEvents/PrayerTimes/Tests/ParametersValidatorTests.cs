using NUnit.Framework;
using GamePause.SalahEvents.PrayerTimes.Data;
using GamePause.SalahEvents.PrayerTimes.Enums;
using GamePause.SalahEvents.PrayerTimes.Validation;

namespace GamePause.SalahEvents.PrayerTimes.Tests
{
    [TestFixture]
    public class ParametersValidatorTests
    {
        private ParametersValidator _validator;
        
        [SetUp]
        public void SetUp()
        {
            _validator = new ParametersValidator();
        }
        
        [Test]
        public void Validate_ValidAngleBasedParameters_ReturnsSuccess()
        {
            var methodParams = new MethodParameters(19.5, 17.5);
            var parameters = new CalculationParameters(
                CalculationMethod.EgyptianAuthority,
                methodParams
            );
            
            var result = _validator.Validate(parameters);
            
            Assert.IsTrue(result.IsValid);
        }
        
        [Test]
        public void Validate_ValidIntervalBasedParameters_ReturnsSuccess()
        {
            var methodParams = new MethodParameters(18.5, ishaIntervalMinutes: 90);
            var parameters = new CalculationParameters(
                CalculationMethod.UmmAlQura,
                methodParams
            );
            
            var result = _validator.Validate(parameters);
            
            Assert.IsTrue(result.IsValid);
        }
        
        [Test]
        public void Validate_NullParameters_ReturnsFailure()
        {
            var result = _validator.Validate(null);
            
            Assert.IsFalse(result.IsValid);
            StringAssert.Contains("null", result.Errors[0]);
        }
        
        [Test]
        public void Validate_FajrAngleTooLow_ReturnsFailure()
        {
            var methodParams = new MethodParameters(5.0, 17.5);
            var parameters = new CalculationParameters(
                CalculationMethod.EgyptianAuthority,
                methodParams
            );
            
            var result = _validator.Validate(parameters);
            
            Assert.IsFalse(result.IsValid);
            StringAssert.Contains("Fajr", result.Errors[0]);
        }
        
        [Test]
        public void Validate_FajrAngleTooHigh_ReturnsFailure()
        {
            var methodParams = new MethodParameters(30.0, 17.5);
            var parameters = new CalculationParameters(
                CalculationMethod.EgyptianAuthority,
                methodParams
            );
            
            var result = _validator.Validate(parameters);
            
            Assert.IsFalse(result.IsValid);
            StringAssert.Contains("Fajr", result.Errors[0]);
        }
        
        [Test]
        public void Validate_IshaAngleTooLow_ReturnsFailure()
        {
            var methodParams = new MethodParameters(18.0, 5.0);
            var parameters = new CalculationParameters(
                CalculationMethod.EgyptianAuthority,
                methodParams
            );
            
            var result = _validator.Validate(parameters);
            
            Assert.IsFalse(result.IsValid);
            StringAssert.Contains("Isha", result.Errors[0]);
        }
        
        [Test]
        public void Validate_IshaIntervalTooLow_ReturnsFailure()
        {
            var methodParams = new MethodParameters(18.5, ishaIntervalMinutes: 20);
            var parameters = new CalculationParameters(
                CalculationMethod.UmmAlQura,
                methodParams
            );
            
            var result = _validator.Validate(parameters);
            
            Assert.IsFalse(result.IsValid);
            StringAssert.Contains("Isha interval", result.Errors[0]);
        }
        
        [Test]
        public void Validate_IshaIntervalTooHigh_ReturnsFailure()
        {
            var methodParams = new MethodParameters(18.5, ishaIntervalMinutes: 200);
            var parameters = new CalculationParameters(
                CalculationMethod.UmmAlQura,
                methodParams
            );
            
            var result = _validator.Validate(parameters);
            
            Assert.IsFalse(result.IsValid);
            StringAssert.Contains("Isha interval", result.Errors[0]);
        }
        
        [Test]
        public void Validate_BoundaryValues_ReturnsSuccess()
        {
            var methodParams = new MethodParameters(10.0, 10.0);
            var parameters = new CalculationParameters(
                CalculationMethod.EgyptianAuthority,
                methodParams
            );
            
            var result = _validator.Validate(parameters);
            
            Assert.IsTrue(result.IsValid);
        }
    }
}