using Microsoft.AspNetCore.Identity;

namespace SocialNetworkPlatformProject.Persistence.Identity;

// ASP.NET Identity's own error texts are English only; the ones a person can actually run into (password rules, taken
// names) come from the language files here, in the language of the request (see Loc). Anything not listed keeps
// Identity's default text.
public class LocalizedIdentityErrorDescriber : IdentityErrorDescriber
{
    public override IdentityError DefaultError() => Error(nameof(DefaultError), "identity.default");

    public override IdentityError ConcurrencyFailure() => Error(nameof(ConcurrencyFailure), "identity.concurrency");

    public override IdentityError PasswordMismatch() => Error(nameof(PasswordMismatch), "identity.passwordMismatch");

    public override IdentityError InvalidToken() => Error(nameof(InvalidToken), "identity.invalidToken");

    public override IdentityError InvalidUserName(string? userName) => Error(nameof(InvalidUserName), "identity.invalidUserName");

    public override IdentityError InvalidEmail(string? email) => Error(nameof(InvalidEmail), "identity.invalidEmail");

    public override IdentityError DuplicateUserName(string userName) => Error(nameof(DuplicateUserName), "auth.userNameTaken");

    public override IdentityError DuplicateEmail(string email) => Error(nameof(DuplicateEmail), "auth.emailTaken");

    public override IdentityError PasswordTooShort(int length) => Error(nameof(PasswordTooShort), "identity.passwordTooShort", length);

    public override IdentityError PasswordRequiresNonAlphanumeric() => Error(nameof(PasswordRequiresNonAlphanumeric), "identity.passwordNeedsSpecial");

    public override IdentityError PasswordRequiresDigit() => Error(nameof(PasswordRequiresDigit), "identity.passwordNeedsDigit");

    public override IdentityError PasswordRequiresLower() => Error(nameof(PasswordRequiresLower), "identity.passwordNeedsLower");

    public override IdentityError PasswordRequiresUpper() => Error(nameof(PasswordRequiresUpper), "identity.passwordNeedsUpper");

    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) => Error(nameof(PasswordRequiresUniqueChars), "identity.passwordNeedsUnique", uniqueChars);

    private static IdentityError Error(string code, string key, params object[] args) => new() { Code = code, Description = Loc.T(key, args) };
}
