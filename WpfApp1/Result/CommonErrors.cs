namespace WpfApp1.Result;

public static class CatalogsErrors
{
    public static readonly Error CodeNotUnique = Error.Conflict(
        "Catalogs.CodeNotUnique",
        "The provided code is not unique");

    public static readonly Error NameNotUnique = Error.Conflict(
        "Catalogs.NameNotUnique",
        "The provided name is not unique");

    public static readonly Error ServiceUnavailable = Error.ServiceUnavailable(
        "Database.Unavailable",
        "The database is temporarily unavailable.");

    public static readonly Error ServerUnavailable = Error.ServiceUnavailable(
        "Server.Unavailable",
        "The server is temporarily unavailable.");

    public static Error NotFound(Guid catalogId)
    {
        return Error.NotFound(
            "Catalogs.NotFound",
            "The requested record was not found. It may have been deleted.");
    }

    public static Error NotFound(string catalogCodeOrId)
    {
        return Error.NotFound(
            "Catalogs.NotFound",
            $"The record with code '{catalogCodeOrId}' was not found");
    }

    public static Error InvalidSettings(Guid catalogId, string settings)
    {
        return Error.Validation(
            "General.InvalidSettings",
            $"The requested record has invalid settings = '{settings}'");
    }
}