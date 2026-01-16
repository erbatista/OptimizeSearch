namespace WpfApp1.Result;

public record Error
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);

    public static readonly Error NullValue = new(
      "General.Null",
      "Null value was provided",
      ErrorType.Failure);

    public Error(string code, string description, ErrorType type)
    {
        Code = code;
        Description = description;
        Type = type;
    }

    public string Code { get; }

    public string Description { get; }

    public ErrorType Type { get; }

    public static Error Failure(string code, string description)
    {
        return new Error(code, description, ErrorType.Failure);
    }

    public static Error NotFound(string code, string description)
    {
        return new Error(code, description, ErrorType.NotFound);
    }

    public static Error Problem(string code, string description)
    {
        return new Error(code, description, ErrorType.Problem);
    }

    public static Error Conflict(string code, string description)
    {
        return new Error(code, description, ErrorType.Conflict);
    }

    public static Error ServiceUnavailable(string code, string description)
    {
        return new Error(code, description, ErrorType.ServiceUnavailable);
    }

    public static Error Unauthorized(string code = "General.Unauthorized",
      string description = "An 'Unauthorized' error has occurred.", Dictionary<string, object>? metadata = null)
    {
        return new Error(code, description, ErrorType.Unauthorized);
    }

    public static Error Validation(string code = "Validation.General",
      string description = "One or more validation errors occurred.", Dictionary<string, object>? metadata = null)
    {
        return new Error(code, description, ErrorType.Validation);
    }

    public static Error Forbidden(string code = "General.Forbidden",
      string description = "You are not authorized to perform this action.", Dictionary<string, object>? metadata = null)
    {
        return new Error(code, description, ErrorType.Forbidden);
    }

    public static Error OperationCancelled(string code = "General.OperationCancelled",
      string description = "The operation was cancelled.", Dictionary<string, object>? metadata = null)
    {
        return new Error(code, description, ErrorType.OperationCancelled);
    }
}
