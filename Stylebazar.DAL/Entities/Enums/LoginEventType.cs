namespace Stylebazar.DAL.Entities.Enums;

/// <summary>
/// The kind of authentication event a LoginEvent row records.
/// Drives the colour + label of the badge in the user's Login History.
/// </summary>
public enum LoginEventType
{
    SignUp = 0,             // First-time registration
    SignIn = 1,             // Successful login
    SignOut = 2,            // Manual logout
    FailedSignIn = 3,       // Wrong password
    PasswordChanged = 4,    // Self-service change
    PasswordReset = 5,      // Reset via email token
    EmailVerified = 6,      // Email confirmation
    TwoFactorChallenge = 7, // 2FA prompt (future)
    Locked = 8              // Lockout triggered (5 consecutive fails)
}
