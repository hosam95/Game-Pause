using NUnit.Framework;
using GamePause.PrayerTimeCalculator.Data;
using GamePause.PrayerTimeCalculator.Validation;

namespace GamePause.PrayerTimeCalculator.Tests
{
    [TestFixture]
    public class CoordinatesValidatorTests
    {
        private CoordinatesValidator _validator;
        
        [SetUp]
        public void SetUp()
        {
            _validator = new CoordinatesValidator();
        }
        
        [Test]
        public void Validate_ValidCoordinates_ReturnsSuccess()
        {
            var coords = new GeographicCoordinates(30.0444, 31.2357);
            
            var result = _validator.Validate(coords);
            
            Assert.IsTrue(result.IsValid);
            Assert.IsEmpty(result.Errors);
        }
        
        [Test]
        public void Validate_EquatorCoordinates_ReturnsSuccess()
        {
            var coords = new GeographicCoordinates(0, 0);
            
            var result = _validator.Validate(coords);
            
            Assert.IsTrue(result.IsValid);
        }
        
        [Test]
        public void Validate_BoundaryCoordinates_ReturnsSuccess()
        {
            var coords = new GeographicCoordinates(90, 180);
            
            var result = _validator.Validate(coords);
            
            Assert.IsTrue(result.IsValid);
        }
        
        [Test]
        public void Validate_NegativeBoundaryCoordinates_ReturnsSuccess()
        {
            var coords = new GeographicCoordinates(-90, -180);
            
            var result = _validator.Validate(coords);
            
            Assert.IsTrue(result.IsValid);
        }
        
        [Test]
        public void Validate_LatitudeTooHigh_ReturnsFailure()
        {
            var coords = new GeographicCoordinates(91, 0);
            
            var result = _validator.Validate(coords);
            
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(1, result.Errors.Count);
            StringAssert.Contains("Latitude", result.Errors[0]);
        }
        
        [Test]
        public void Validate_LatitudeTooLow_ReturnsFailure()
        {
            var coords = new GeographicCoordinates(-91, 0);
            
            var result = _validator.Validate(coords);
            
            Assert.IsFalse(result.IsValid);
            StringAssert.Contains("Latitude", result.Errors[0]);
        }
        
        [Test]
        public void Validate_LongitudeTooHigh_ReturnsFailure()
        {
            var coords = new GeographicCoordinates(0, 181);
            
            var result = _validator.Validate(coords);
            
            Assert.IsFalse(result.IsValid);
            StringAssert.Contains("Longitude", result.Errors[0]);
        }
        
        [Test]
        public void Validate_LongitudeTooLow_ReturnsFailure()
        {
            var coords = new GeographicCoordinates(0, -181);
            
            var result = _validator.Validate(coords);
            
            Assert.IsFalse(result.IsValid);
            StringAssert.Contains("Longitude", result.Errors[0]);
        }
        
        [Test]
        public void Validate_BothInvalid_ReturnsTwoErrors()
        {
            var coords = new GeographicCoordinates(100, 200);
            
            var result = _validator.Validate(coords);
            
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(2, result.Errors.Count);
        }
    }
}