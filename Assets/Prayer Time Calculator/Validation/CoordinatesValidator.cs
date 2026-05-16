using GamePause.PrayerTimeCalculator.Data;

namespace GamePause.PrayerTimeCalculator.Validation
{
    /// <summary>
    /// Validates geographic coordinates.
    /// </summary>
    public sealed class CoordinatesValidator : IValidator<GeographicCoordinates>
    {
        public const double MinLatitude = -90.0;
        public const double MaxLatitude = 90.0;
        public const double MinLongitude = -180.0;
        public const double MaxLongitude = 180.0;
        
        public ValidationResult Validate(GeographicCoordinates input)
        {
            var errors = new System.Collections.Generic.List<string>();
            
            if (input.Latitude < MinLatitude || input.Latitude > MaxLatitude)
            {
                errors.Add($"Latitude must be between {MinLatitude} and {MaxLatitude}. Got: {input.Latitude}");
            }
            
            if (input.Longitude < MinLongitude || input.Longitude > MaxLongitude)
            {
                errors.Add($"Longitude must be between {MinLongitude} and {MaxLongitude}. Got: {input.Longitude}");
            }
            
            return errors.Count == 0 
                ? ValidationResult.Success() 
                : ValidationResult.Failure(errors);
        }
    }
}