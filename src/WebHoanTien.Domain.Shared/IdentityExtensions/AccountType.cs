namespace WebHoanTien.IdentityExtensions;

public enum AccountType { Anonymous = 0, Registered = 1 }

public static class CatBackAccountProperties
{
    public const string Type = "AccountType";
    public const string LoginEmail = "LoginEmail";
    public const string NormalizedLoginEmail = "NormalizedLoginEmail";
    public const string AnonymousClaim = "catback_anonymous";
    public const string CredentialClaim = "catback_device";
    public const string RegistrationRequired = "CatBack:RegistrationRequired";
}
