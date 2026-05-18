namespace GLMS.Enterprise.Core.Models;

public class ValidationResult
{
    public bool IsValid { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();

    public static ValidationResult Success() => new() { IsValid = true };

    public static ValidationResult Failure(string error) => new()
    {
        IsValid = false,
        ErrorMessage = error,
        Errors = new List<string> { error }
    };

    public static ValidationResult Failure(IEnumerable<string> errors)
    {
        var errorList = errors.ToList();
        return new ValidationResult
        {
            IsValid = false,
            ErrorMessage = errorList.FirstOrDefault() ?? "Validation failed",
            Errors = errorList
        };
    }
}
