using Firebase.Auth;

namespace IronReign.Services;

public class AuthService
{
    private readonly FirebaseAuthClient _authClient;

    private const string SessionStartedAtKey = "auth_session_started_at_utc";
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(48);

    public string? CurrentUserId => _authClient.User?.Uid;
    public string? CurrentUserEmail => _authClient.User?.Info?.Email;
    public string? CurrentUserDisplayName => _authClient.User?.Info?.DisplayName;
    public bool IsLoggedIn => _authClient.User is not null;

    public AuthService(FirebaseAuthClient authClient)
    {
        _authClient = authClient;
    }

    public async Task<(bool Success, string? ErrorMessage)> LoginAsync(string email, string password)
    {
        try
        {
            await _authClient.SignInWithEmailAndPasswordAsync(email, password);
            MarkSessionStarted();
            return (true, null);
        }
        catch (FirebaseAuthException ex)
        {
            return (false, $"[{ex.Reason}] {ex.Message}");
        }
        catch (Exception ex)
        {
            return (false, $"Error genérico: {ex.Message}");
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> RegisterAsync(string email, string password, string displayName)
    {
        try
        {
            await _authClient.CreateUserWithEmailAndPasswordAsync(email, password, displayName);
            MarkSessionStarted();
            return (true, null);
        }
        catch (FirebaseAuthException ex)
        {
            return (false, $"[{ex.Reason}] {ex.Message}");
        }
        catch (Exception ex)
        {
            return (false, $"Error genérico: {ex.Message}");
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> ResetPasswordAsync(string email)
    {
        try
        {
            await _authClient.ResetEmailPasswordAsync(email);
            return (true, null);
        }
        catch (FirebaseAuthException ex)
        {
            return (false, $"[{ex.Reason}] {ex.Message}");
        }
        catch (Exception ex)
        {
            return (false, $"Error genérico: {ex.Message}");
        }
    }

    public bool IsSessionExpired()
    {
        if (!IsLoggedIn)
            return true;

        if (!Preferences.ContainsKey(SessionStartedAtKey))
            return true;

        var storedValue = Preferences.Get(SessionStartedAtKey, string.Empty);

        if (string.IsNullOrWhiteSpace(storedValue))
            return true;

        if (!DateTime.TryParse(
                storedValue,
                null,
                System.Globalization.DateTimeStyles.RoundtripKind,
                out var startedAtUtc))
        {
            return true;
        }

        return DateTime.UtcNow - startedAtUtc > SessionLifetime;
    }

    public void MarkSessionStarted()
    {
        Preferences.Set(SessionStartedAtKey, DateTime.UtcNow.ToString("O"));
    }

    public void Logout()
    {
        Preferences.Remove(SessionStartedAtKey);
        _authClient.SignOut();
    }
}