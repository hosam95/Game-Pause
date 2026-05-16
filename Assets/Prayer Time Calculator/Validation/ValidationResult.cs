using System.Collections.Generic;
using System.Linq;

namespace GamePause.PrayerTimeCalculator.Validation
{
    /// <summary>
    /// Result of a validation operation.
    /// </summary>
    public sealed class ValidationResult
    {
        public bool IsValid { get; }
        public IReadOnlyList<string> Errors { get; }
        
        private ValidationResult(bool isValid, IReadOnlyList<string> errors)
        {
            IsValid = isValid;
            Errors = errors;
        }
        
        public static ValidationResult Success()
        {
            return new ValidationResult(true, new List<string>());
        }
        
        public static ValidationResult Failure(string error)
        {
            return new ValidationResult(false, new List<string> { error });
        }
        
        public static ValidationResult Failure(IEnumerable<string> errors)
        {
            return new ValidationResult(false, errors.ToList());
        }
        
        public string GetErrorMessage()
        {
            return string.Join("; ", Errors);
        }
    }
}