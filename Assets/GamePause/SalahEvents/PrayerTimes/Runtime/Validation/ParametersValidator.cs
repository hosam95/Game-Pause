using GamePause.SalahEvents.PrayerTimes.Data;

namespace GamePause.SalahEvents.PrayerTimes.Validation
{
    /// <summary>
    /// Validates calculation parameters.
    /// </summary>
    public sealed class ParametersValidator : IValidator<CalculationParameters>
    {
        public const double MinFajrAngle = 10.0;
        public const double MaxFajrAngle = 25.0;
        public const double MinIshaAngle = 10.0;
        public const double MaxIshaAngle = 25.0;
        public const int MinIshaInterval = 30;
        public const int MaxIshaInterval = 150;
        
        public ValidationResult Validate(CalculationParameters input)
        {
            if (input == null)
            {
                return ValidationResult.Failure("Calculation parameters cannot be null.");
            }
            
            var errors = new System.Collections.Generic.List<string>();
            var methodParams = input.MethodParameters;
            
            // Validate Fajr angle
            if (methodParams.FajrAngle < MinFajrAngle || methodParams.FajrAngle > MaxFajrAngle)
            {
                errors.Add($"Fajr angle must be between {MinFajrAngle} and {MaxFajrAngle}. Got: {methodParams.FajrAngle}");
            }
            
            // Validate Isha (angle or interval)
            if (methodParams.UsesIshaInterval)
            {
                var interval = methodParams.IshaIntervalMinutes.Value;
                if (interval < MinIshaInterval || interval > MaxIshaInterval)
                {
                    errors.Add($"Isha interval must be between {MinIshaInterval} and {MaxIshaInterval} minutes. Got: {interval}");
                }
            }
            else if (methodParams.IshaAngle.HasValue)
            {
                var angle = methodParams.IshaAngle.Value;
                if (angle < MinIshaAngle || angle > MaxIshaAngle)
                {
                    errors.Add($"Isha angle must be between {MinIshaAngle} and {MaxIshaAngle}. Got: {angle}");
                }
            }
            else
            {
                errors.Add("Either Isha angle or interval must be specified.");
            }
            
            return errors.Count == 0 
                ? ValidationResult.Success() 
                : ValidationResult.Failure(errors);
        }
    }
}