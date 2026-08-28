using IronReign.Data;
using IronReign.Models;

namespace IronReign.Services;

public class UserSessionService
{
    private readonly AppDatabase _database;
    private readonly CloudBackupService _cloudBackupService;

    public UserProfile? CurrentUser { get; private set; }

    public UserSessionService(AppDatabase database, CloudBackupService cloudBackupService)
    {
        _database = database;
        _cloudBackupService = cloudBackupService;
    }

    public async Task<UserProfile?> LoadActiveUserAsync()
    {
        var users = await _database.GetUserProfilesAsync();
        CurrentUser = users.FirstOrDefault(x => x.IsActive);
        return CurrentUser;
    }

    public async Task SetActiveUserAsync(int userId)
    {
        var users = await _database.GetUserProfilesAsync();

        foreach (var user in users)
        {
            user.IsActive = user.Id == userId;
            await _database.SaveUserProfileAsync(user);
        }

        CurrentUser = users.FirstOrDefault(x => x.Id == userId);
    }

    public async Task<UserProfile> EnsureLocalProfileForFirebaseUserAsync(string firebaseUid, string email, string? displayName)
    {
        var existingProfile = await _database.GetUserProfileByFirebaseUidAsync(firebaseUid);

        if (existingProfile is not null)
        {
            if (!string.IsNullOrWhiteSpace(displayName))
            {
                var (firstName, lastName) = SplitDisplayName(displayName, email);

                if (existingProfile.FirstName != firstName || existingProfile.LastName != lastName)
                {
                    existingProfile.FirstName = firstName;
                    existingProfile.LastName = lastName;
                    await _database.SaveUserProfileAsync(existingProfile);
                }
            }

            if (!existingProfile.IsActive)
            {
                await SetActiveUserAsync(existingProfile.Id);
            }
            else
            {
                CurrentUser = existingProfile;
            }

            return existingProfile;
        }

        var (newFirstName, newLastName) = SplitDisplayName(displayName, email);

        var newProfile = new UserProfile
        {
            FirebaseUid = firebaseUid,
            FirstName = newFirstName,
            LastName = newLastName,
            IsActive = true
        };

        var newId = await _database.SaveUserProfileAsync(newProfile);
        newProfile.Id = newId;

        // Dispositivo nuevo para esta cuenta: si hay un backup en la nube, lo restauramos
        // en vez de dejar el perfil recién creado vacío.
        if (_cloudBackupService.IsConfigured)
            await _cloudBackupService.RestoreAsync(firebaseUid, newProfile);

        CurrentUser = newProfile;
        return newProfile;
    }

    public async Task ClearActiveUserAsync()
    {
        var users = await _database.GetUserProfilesAsync();

        foreach (var user in users.Where(x => x.IsActive))
        {
            user.IsActive = false;
            await _database.SaveUserProfileAsync(user);
        }

        CurrentUser = null;
    }

    private static (string FirstName, string LastName) SplitDisplayName(string? displayName, string email)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            var localPart = email.Split('@').FirstOrDefault() ?? "Usuario";
            return (localPart, string.Empty);
        }

        var parts = displayName.Trim().Split(' ', 2);
        var firstName = parts[0];
        var lastName = parts.Length > 1 ? parts[1] : string.Empty;

        return (firstName, lastName);
    }

    public void Clear()
    {
        CurrentUser = null;
    }
}