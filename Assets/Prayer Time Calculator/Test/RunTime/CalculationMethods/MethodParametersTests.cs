using NUnit.Framework;
using GamePause.PrayerTimeCalculator.Data;

namespace GamePause.PrayerTimeCalculator.Tests
{
    [TestFixture]
    public class MethodParametersTests
    {
        [Test]
        public void Constructor_AngleBased_SetsProperties()
        {
            var methodParams = new MethodParameters(19.5, 17.5);
            
            Assert.AreEqual(19.5, methodParams.FajrAngle);
            Assert.AreEqual(17.5, methodParams.IshaAngle);
            Assert.IsNull(methodParams.IshaIntervalMinutes);
            Assert.IsFalse(methodParams.UsesIshaInterval);
            Assert.AreEqual(0, methodParams.MaghribAngle);
        }
        
        [Test]
        public void Constructor_IntervalBased_SetsProperties()
        {
            var methodParams = new MethodParameters(18.5, ishaIntervalMinutes: 90);
            
            Assert.AreEqual(18.5, methodParams.FajrAngle);
            Assert.IsNull(methodParams.IshaAngle);
            Assert.AreEqual(90, methodParams.IshaIntervalMinutes);
            Assert.IsTrue(methodParams.UsesIshaInterval);
        }
        
        [Test]
        public void Constructor_WithMaghribAngle_SetsProperty()
        {
            var methodParams = new MethodParameters(17.7, 14.0, maghribAngle: 4.5);
            
            Assert.AreEqual(4.5, methodParams.MaghribAngle);
            Assert.IsNull(methodParams.MaghribMinutes);
            Assert.IsFalse(methodParams.UsesMaghribInterval);
        }
        
        [Test]
        public void Constructor_WithMaghribMinutes_SetsProperty()
        {
            var methodParams = new MethodParameters(18.0, 17.0, maghribMinutes: 5);
            
            Assert.AreEqual(5, methodParams.MaghribMinutes);
            Assert.IsTrue(methodParams.UsesMaghribInterval);
        }
        
        [Test]
        public void Equals_SameAngleValues_ReturnsTrue()
        {
            var params1 = new MethodParameters(19.5, 17.5);
            var params2 = new MethodParameters(19.5, 17.5);
            
            Assert.IsTrue(params1.Equals(params2));
        }
        
        [Test]
        public void Equals_SameIntervalValues_ReturnsTrue()
        {
            var params1 = new MethodParameters(18.5, ishaIntervalMinutes: 90);
            var params2 = new MethodParameters(18.5, ishaIntervalMinutes: 90);
            
            Assert.IsTrue(params1.Equals(params2));
        }
        
        [Test]
        public void Equals_DifferentMaghribAngle_ReturnsFalse()
        {
            var params1 = new MethodParameters(17.7, 14.0, maghribAngle: 4.5);
            var params2 = new MethodParameters(17.7, 14.0, maghribAngle: 4.0);
            
            Assert.IsFalse(params1.Equals(params2));
        }
        
        [Test]
        public void ToString_AngleBased_ContainsAngles()
        {
            var methodParams = new MethodParameters(19.5, 17.5);
            
            var result = methodParams.ToString();
            
            StringAssert.Contains("19.5", result);
            StringAssert.Contains("17.5", result);
            StringAssert.Contains("°", result);
        }
        
        [Test]
        public void ToString_WithMaghribAngle_ContainsMaghrib()
        {
            var methodParams = new MethodParameters(17.7, 14.0, maghribAngle: 4.5);
            
            var result = methodParams.ToString();
            
            StringAssert.Contains("Maghrib", result);
            StringAssert.Contains("4.5", result);
        }
        
        [Test]
        public void ToString_IntervalBased_ContainsMinutes()
        {
            var methodParams = new MethodParameters(18.5, ishaIntervalMinutes: 90);
            
            var result = methodParams.ToString();
            
            StringAssert.Contains("90", result);
            StringAssert.Contains("min", result);
        }
    }
}