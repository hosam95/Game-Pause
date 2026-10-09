using System;
using NUnit.Framework;
using GamePause.SalahEvents.PrayerTimes.Data;

namespace GamePause.SalahEvents.PrayerTimes.Tests
{
    [TestFixture]
    public class GeographicCoordinatesTests
    {
        [Test]
        public void Constructor_ValidCoordinates_CreatesInstance()
        {
            var coords = new GeographicCoordinates(30.0444, 31.2357);
            
            Assert.AreEqual(30.0444, coords.Latitude);
            Assert.AreEqual(31.2357, coords.Longitude);
        }
        
        [Test]
        public void Constructor_BoundaryValues_Succeeds()
        {
            Assert.DoesNotThrow(() => new GeographicCoordinates(-90, -180));
            Assert.DoesNotThrow(() => new GeographicCoordinates(90, 180));
            Assert.DoesNotThrow(() => new GeographicCoordinates(0, 0));
        }
        
        [Test]
        public void Equals_SameValues_ReturnsTrue()
        {
            var coords1 = new GeographicCoordinates(30.0444, 31.2357);
            var coords2 = new GeographicCoordinates(30.0444, 31.2357);
            
            Assert.IsTrue(coords1.Equals(coords2));
            Assert.IsTrue(coords1 == coords2);
        }
        
        [Test]
        public void Equals_DifferentValues_ReturnsFalse()
        {
            var coords1 = new GeographicCoordinates(30.0444, 31.2357);
            var coords2 = new GeographicCoordinates(21.4225, 39.8262);
            
            Assert.IsFalse(coords1.Equals(coords2));
            Assert.IsTrue(coords1 != coords2);
        }
        
        [Test]
        public void GetHashCode_SameValues_ReturnsSameHash()
        {
            var coords1 = new GeographicCoordinates(30.0444, 31.2357);
            var coords2 = new GeographicCoordinates(30.0444, 31.2357);
            
            Assert.AreEqual(coords1.GetHashCode(), coords2.GetHashCode());
        }
        
        [Test]
        public void ToString_ReturnsFormattedString()
        {
            var coords = new GeographicCoordinates(30.0444, 31.2357);
            var result = coords.ToString();
            
            Assert.IsTrue(result.Contains("30.0444"));
            Assert.IsTrue(result.Contains("31.2357"));
        }
    }
}