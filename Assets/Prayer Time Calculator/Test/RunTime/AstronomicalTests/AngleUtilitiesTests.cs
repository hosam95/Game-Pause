using System;
using NUnit.Framework;
using GamePause.PrayerTimeCalculator.Astronomical;

namespace GamePause.PrayerTimeCalculator.Tests
{
    [TestFixture]
    public class AngleUtilitiesTests
    {
        private const double Tolerance = 0.0001;
        
        [Test]
        public void DegreesToRadians_90Degrees_ReturnsHalfPi()
        {
            double result = AngleUtilities.DegreesToRadians(90);
            
            Assert.AreEqual(Math.PI / 2, result, Tolerance);
        }
        
        [Test]
        public void DegreesToRadians_180Degrees_ReturnsPi()
        {
            double result = AngleUtilities.DegreesToRadians(180);
            
            Assert.AreEqual(Math.PI, result, Tolerance);
        }
        
        [Test]
        public void RadiansToDegrees_Pi_Returns180()
        {
            double result = AngleUtilities.RadiansToDegrees(Math.PI);
            
            Assert.AreEqual(180, result, Tolerance);
        }
        
        [Test]
        public void NormalizeDegrees_NegativeAngle_ReturnsPositive()
        {
            double result = AngleUtilities.NormalizeDegrees(-90);
            
            Assert.AreEqual(270, result, Tolerance);
        }
        
        [Test]
        public void NormalizeDegrees_Over360_ReturnsNormalized()
        {
            double result = AngleUtilities.NormalizeDegrees(450);
            
            Assert.AreEqual(90, result, Tolerance);
        }
        
        [Test]
        public void SinDegrees_90_ReturnsOne()
        {
            double result = AngleUtilities.SinDegrees(90);
            
            Assert.AreEqual(1, result, Tolerance);
        }
        
        [Test]
        public void CosDegrees_0_ReturnsOne()
        {
            double result = AngleUtilities.CosDegrees(0);
            
            Assert.AreEqual(1, result, Tolerance);
        }
        
        [Test]
        public void TanDegrees_45_ReturnsOne()
        {
            double result = AngleUtilities.TanDegrees(45);
            
            Assert.AreEqual(1, result, Tolerance);
        }
        
        [Test]
        public void AsinDegrees_One_Returns90()
        {
            double result = AngleUtilities.AsinDegrees(1);
            
            Assert.AreEqual(90, result, Tolerance);
        }
        
        [Test]
        public void AcosDegrees_Zero_Returns90()
        {
            double result = AngleUtilities.AcosDegrees(0);
            
            Assert.AreEqual(90, result, Tolerance);
        }
    }
}