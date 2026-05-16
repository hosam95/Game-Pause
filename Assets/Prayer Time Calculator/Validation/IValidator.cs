namespace GamePause.PrayerTimeCalculator.Validation
{
    /// <summary>
    /// Generic validation contract.
    /// </summary>
    /// <typeparam name="T">Type to validate.</typeparam>
    public interface IValidator<in T>
    {
        /// <summary>
        /// Validates the input and returns the result.
        /// </summary>
        ValidationResult Validate(T input);
    }
}