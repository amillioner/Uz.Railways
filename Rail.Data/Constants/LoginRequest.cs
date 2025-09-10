namespace Rail.Data.Constants;

public static class Roles
{
    public const string Reader = "reader";
    public const string Uploader = "uploader";
    public const string Admin = "admin";

    public static readonly string[] All = { Reader, Uploader, Admin };
}