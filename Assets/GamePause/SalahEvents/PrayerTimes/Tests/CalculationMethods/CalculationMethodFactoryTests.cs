using System.Linq;
using NUnit.Framework;
using GamePause.SalahEvents.PrayerTimes.Enums;
using GamePause.SalahEvents.PrayerTimes.Methods;

namespace GamePause.SalahEvents.PrayerTimes.Tests
{
    [TestFixture]
    public class CalculationMethodFactoryTests
    {
        [Test]
        public void Create_EgyptianAuthority_ReturnsCorrectProvider()
        {
            var provider = CalculationMethodFactory.Create(CalculationMethod.EgyptianAuthority);
            
            Assert.IsNotNull(provider);
            Assert.AreEqual(CalculationMethod.EgyptianAuthority, provider.Method);
            Assert.AreEqual(19.5, provider.Parameters.FajrAngle);
            Assert.AreEqual(17.5, provider.Parameters.IshaAngle);
        }
        
        [Test]
        public void Create_UmmAlQura_ReturnsIntervalBasedIsha()
        {
            var provider = CalculationMethodFactory.Create(CalculationMethod.UmmAlQura);
            
            Assert.IsNotNull(provider);
            Assert.IsTrue(provider.Parameters.UsesIshaInterval);
            Assert.AreEqual(90, provider.Parameters.IshaIntervalMinutes);
        }
        
        [Test]
        public void Create_MuslimWorldLeague_ReturnsCorrectProvider()
        {
            var provider = CalculationMethodFactory.Create(CalculationMethod.MuslimWorldLeague);
            
            Assert.AreEqual(18.0, provider.Parameters.FajrAngle);
            Assert.AreEqual(17.0, provider.Parameters.IshaAngle);
        }
        
        [Test]
        public void Create_ISNA_ReturnsCorrectProvider()
        {
            var provider = CalculationMethodFactory.Create(CalculationMethod.ISNA);
            
            Assert.AreEqual(15.0, provider.Parameters.FajrAngle);
            Assert.AreEqual(15.0, provider.Parameters.IshaAngle);
        }
        
        [Test]
        public void Create_Karachi_ReturnsCorrectProvider()
        {
            var provider = CalculationMethodFactory.Create(CalculationMethod.Karachi);
            
            Assert.AreEqual(18.0, provider.Parameters.FajrAngle);
            Assert.AreEqual(18.0, provider.Parameters.IshaAngle);
        }
        
        [Test]
        public void Create_Tehran_HasMaghribAngle()
        {
            var provider = CalculationMethodFactory.Create(CalculationMethod.Tehran);
            
            Assert.AreEqual(17.7, provider.Parameters.FajrAngle);
            Assert.AreEqual(14.0, provider.Parameters.IshaAngle);
            Assert.AreEqual(4.5, provider.Parameters.MaghribAngle);
        }
        
        [Test]
        public void Create_Qom_HasMaghribAngle()
        {
            var provider = CalculationMethodFactory.Create(CalculationMethod.Qom);
            
            Assert.AreEqual(16.0, provider.Parameters.FajrAngle);
            Assert.AreEqual(14.0, provider.Parameters.IshaAngle);
            Assert.AreEqual(4.0, provider.Parameters.MaghribAngle);
        }
        
        [Test]
        public void Create_MuslimsOfFrance_ReturnsCorrectProvider()
        {
            var provider = CalculationMethodFactory.Create(CalculationMethod.MuslimsOfFrance);
            
            Assert.AreEqual(12.0, provider.Parameters.FajrAngle);
            Assert.AreEqual(12.0, provider.Parameters.IshaAngle);
        }
        
        [Test]
        public void Create_Russia_ReturnsCorrectProvider()
        {
            var provider = CalculationMethodFactory.Create(CalculationMethod.Russia);
            
            Assert.AreEqual(16.0, provider.Parameters.FajrAngle);
            Assert.AreEqual(15.0, provider.Parameters.IshaAngle);
        }
        
        [Test]
        public void Create_Singapore_ReturnsCorrectProvider()
        {
            var provider = CalculationMethodFactory.Create(CalculationMethod.Singapore);
            
            Assert.AreEqual(20.0, provider.Parameters.FajrAngle);
            Assert.AreEqual(18.0, provider.Parameters.IshaAngle);
        }
        
        [Test]
        public void GetAvailableMethods_ReturnsAllMethods()
        {
            var methods = CalculationMethodFactory.GetAvailableMethods().ToList();
            
            Assert.AreEqual(10, methods.Count);
            Assert.Contains(CalculationMethod.EgyptianAuthority, methods);
            Assert.Contains(CalculationMethod.UmmAlQura, methods);
            Assert.Contains(CalculationMethod.MuslimWorldLeague, methods);
            Assert.Contains(CalculationMethod.ISNA, methods);
            Assert.Contains(CalculationMethod.Karachi, methods);
            Assert.Contains(CalculationMethod.Tehran, methods);
            Assert.Contains(CalculationMethod.Qom, methods);
            Assert.Contains(CalculationMethod.MuslimsOfFrance, methods);
            Assert.Contains(CalculationMethod.Russia, methods);
            Assert.Contains(CalculationMethod.Singapore, methods);
        }
        
        [Test]
        public void IsSupported_ValidMethod_ReturnsTrue()
        {
            Assert.IsTrue(CalculationMethodFactory.IsSupported(CalculationMethod.EgyptianAuthority));
            Assert.IsTrue(CalculationMethodFactory.IsSupported(CalculationMethod.Singapore));
        }
        
        [Test]
        public void Create_AllMethods_HaveValidParameters()
        {
            foreach (var method in CalculationMethodFactory.GetAvailableMethods())
            {
                var provider = CalculationMethodFactory.Create(method);
                
                Assert.IsNotNull(provider, $"Provider for {method} is null");
                Assert.IsNotNull(provider.Name, $"Name for {method} is null");
                Assert.Greater(provider.Parameters.FajrAngle, 0, $"Fajr angle for {method} is invalid");
                
                // Either Isha angle or interval must be set
                Assert.IsTrue(
                    provider.Parameters.IshaAngle.HasValue || provider.Parameters.IshaIntervalMinutes.HasValue,
                    $"Isha configuration for {method} is invalid"
                );
            }
        }
        
        [Test]
        public void Create_AllMethods_HaveNonEmptyName()
        {
            foreach (var method in CalculationMethodFactory.GetAvailableMethods())
            {
                var provider = CalculationMethodFactory.Create(method);
                
                Assert.IsFalse(string.IsNullOrWhiteSpace(provider.Name));
            }
        }
    }
}