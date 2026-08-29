using System.Net.Http.Json;
using Firebase.Auth;
using IronReign.Data;
using IronReign.Models;

namespace IronReign.Services;

public class CloudBackupService
{
    private const string DatabaseUrl = "https://ironreign-f618e-default-rtdb.europe-west1.firebasedatabase.app";

    private readonly FirebaseAuthClient _authClient;
    private readonly AppDatabase _database;
    private readonly HttpClient _httpClient;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(DatabaseUrl);

    public CloudBackupService(FirebaseAuthClient authClient, AppDatabase database)
    {
        _authClient = authClient;
        _database = database;
        _httpClient = new HttpClient();
    }

    public async Task<(bool Success, string? ErrorMessage)> BackupAsync(UserProfile user)
    {
        if (!IsConfigured)
            return (false, "El backup en la nube no está configurado todavía.");

        if (string.IsNullOrWhiteSpace(user.FirebaseUid))
            return (false, "Este perfil no está enlazado a una cuenta.");

        try
        {
            var snapshot = await BuildSnapshotAsync(user);
            var idToken = await _authClient.User.GetIdTokenAsync(false);

            var url = $"{DatabaseUrl}/backups/{user.FirebaseUid}.json?auth={idToken}";
            var response = await _httpClient.PutAsJsonAsync(url, snapshot);

            return response.IsSuccessStatusCode
                ? (true, null)
                : (false, $"El servidor respondió {(int)response.StatusCode}.");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> RestoreAsync(string firebaseUid, UserProfile targetProfile)
    {
        if (!IsConfigured)
            return (false, "El backup en la nube no está configurado todavía.");

        try
        {
            var idToken = await _authClient.User.GetIdTokenAsync(false);
            var url = $"{DatabaseUrl}/backups/{firebaseUid}.json?auth={idToken}";

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return (false, $"El servidor respondió {(int)response.StatusCode}.");

            var snapshot = await response.Content.ReadFromJsonAsync<BackupSnapshot>();

            if (snapshot is null)
                return (false, "No hay ningún backup guardado para esta cuenta.");

            await RestoreSnapshotAsync(snapshot, targetProfile);
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private async Task<BackupSnapshot> BuildSnapshotAsync(UserProfile user)
    {
        var snapshot = new BackupSnapshot
        {
            BackedUpAtUtc = DateTime.UtcNow,
            Profile = new BackupProfile
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Age = user.Age,
                Sex = user.Sex,
                PreferredWeightUnit = user.PreferredWeightUnit
            }
        };

        var routines = await _database.GetRoutineTemplatesByUserAsync(user.Id);

        foreach (var routine in routines)
        {
            var backupRoutine = new BackupRoutine
            {
                Name = routine.Name,
                Notes = routine.Notes,
                ScheduledDays = routine.ScheduledDays,
                DisplayOrder = routine.DisplayOrder,
                CreatedAtUtc = routine.CreatedAtUtc
            };

            var exercises = await _database.GetRoutineExercisesAsync(routine.Id);

            foreach (var exercise in exercises)
            {
                var backupExercise = new BackupExercise
                {
                    LocalId = exercise.Id,
                    DisplayOrder = exercise.DisplayOrder,
                    ExerciseName = exercise.ExerciseName,
                    BlockType = exercise.BlockType,
                    PlannedSets = exercise.PlannedSets,
                    TargetReps = exercise.TargetReps,
                    SuggestedWeight = exercise.SuggestedWeight,
                    RestSeconds = exercise.RestSeconds,
                    Notes = exercise.Notes,
                    SupersetLinkedLocalId = exercise.SupersetLinkedExerciseId
                };

                var techniques = await _database.GetSetTechniquesAsync(exercise.Id);

                foreach (var technique in techniques)
                {
                    backupExercise.SetTechniques.Add(new BackupSetTechnique
                    {
                        SetIndex = technique.SetIndex,
                        TechniqueType = technique.TechniqueType,
                        DropWeight = technique.DropWeight,
                        DropReps = technique.DropReps,
                        DropWeight2 = technique.DropWeight2,
                        DropReps2 = technique.DropReps2,
                        ClusterWeight = technique.ClusterWeight,
                        ClusterRepsPerMiniSet = technique.ClusterRepsPerMiniSet,
                        ClusterMiniSetCount = technique.ClusterMiniSetCount,
                        ClusterRestSeconds = technique.ClusterRestSeconds,
                        MyoActivationReps = technique.MyoActivationReps,
                        MyoRepsPerMiniSet = technique.MyoRepsPerMiniSet,
                        MyoRestSeconds = technique.MyoRestSeconds
                    });
                }

                backupRoutine.Exercises.Add(backupExercise);
            }

            snapshot.Routines.Add(backupRoutine);
        }

        var sessions = await _database.GetWorkoutSessionsByUserAsync(user.Id);

        foreach (var session in sessions)
        {
            var backupSession = new BackupSession
            {
                Name = session.Name,
                SessionDateUtc = session.SessionDateUtc,
                DurationMinutes = session.DurationMinutes,
                Notes = session.Notes,
                CreatedAtUtc = session.CreatedAtUtc
            };

            var blocks = await _database.GetWorkoutExerciseBlocksAsync(session.Id);

            foreach (var block in blocks)
            {
                var backupBlock = new BackupBlock
                {
                    DisplayOrder = block.DisplayOrder,
                    ExerciseName = block.ExerciseName,
                    BlockType = block.BlockType,
                    PlannedSets = block.PlannedSets,
                    TargetReps = block.TargetReps,
                    SuggestedWeight = block.SuggestedWeight,
                    RestSeconds = block.RestSeconds,
                    Notes = block.Notes,
                    IsCompleted = block.IsCompleted
                };

                var entries = await _database.GetWorkoutBlockEntriesAsync(block.Id);

                foreach (var entry in entries)
                {
                    backupBlock.Entries.Add(new BackupEntry
                    {
                        EntryOrder = entry.EntryOrder,
                        EntryType = entry.EntryType,
                        Weight = entry.Weight,
                        Reps = entry.Reps,
                        RestSeconds = entry.RestSeconds,
                        Notes = entry.Notes
                    });
                }

                backupSession.Blocks.Add(backupBlock);
            }

            snapshot.Sessions.Add(backupSession);
        }

        return snapshot;
    }

    private async Task RestoreSnapshotAsync(BackupSnapshot snapshot, UserProfile targetProfile)
    {
        if (!string.IsNullOrWhiteSpace(snapshot.Profile.FirstName))
            targetProfile.FirstName = snapshot.Profile.FirstName;

        targetProfile.LastName = snapshot.Profile.LastName;
        targetProfile.Age = snapshot.Profile.Age;
        targetProfile.Sex = snapshot.Profile.Sex;

        if (!string.IsNullOrWhiteSpace(snapshot.Profile.PreferredWeightUnit))
            targetProfile.PreferredWeightUnit = snapshot.Profile.PreferredWeightUnit;

        await _database.SaveUserProfileAsync(targetProfile);

        var exerciseIdMap = new Dictionary<int, int>();
        var insertedExercises = new Dictionary<int, RoutineExercise>();
        var pendingSupersetLinks = new List<(int NewExerciseId, int OldLinkedLocalId)>();

        foreach (var backupRoutine in snapshot.Routines)
        {
            var routine = new RoutineTemplate
            {
                UserProfileId = targetProfile.Id,
                Name = backupRoutine.Name,
                Notes = backupRoutine.Notes,
                ScheduledDays = backupRoutine.ScheduledDays,
                CreatedAtUtc = backupRoutine.CreatedAtUtc,
                DisplayOrder = backupRoutine.DisplayOrder
            };

            await _database.SaveRoutineTemplateAsync(routine);

            foreach (var backupExercise in backupRoutine.Exercises)
            {
                var exercise = new RoutineExercise
                {
                    RoutineTemplateId = routine.Id,
                    UserProfileId = targetProfile.Id,
                    DisplayOrder = backupExercise.DisplayOrder,
                    ExerciseName = backupExercise.ExerciseName,
                    BlockType = backupExercise.BlockType,
                    PlannedSets = backupExercise.PlannedSets,
                    TargetReps = backupExercise.TargetReps,
                    SuggestedWeight = backupExercise.SuggestedWeight,
                    RestSeconds = backupExercise.RestSeconds,
                    Notes = backupExercise.Notes
                };

                await _database.SaveRoutineExerciseAsync(exercise);

                exerciseIdMap[backupExercise.LocalId] = exercise.Id;
                insertedExercises[exercise.Id] = exercise;

                if (backupExercise.SupersetLinkedLocalId is int linkedLocalId)
                    pendingSupersetLinks.Add((exercise.Id, linkedLocalId));

                foreach (var technique in backupExercise.SetTechniques)
                {
                    await _database.SaveSetTechniqueAsync(new RoutineExerciseSetTechnique
                    {
                        RoutineExerciseId = exercise.Id,
                        SetIndex = technique.SetIndex,
                        TechniqueType = technique.TechniqueType,
                        DropWeight = technique.DropWeight,
                        DropReps = technique.DropReps,
                        DropWeight2 = technique.DropWeight2,
                        DropReps2 = technique.DropReps2,
                        ClusterWeight = technique.ClusterWeight,
                        ClusterRepsPerMiniSet = technique.ClusterRepsPerMiniSet,
                        ClusterMiniSetCount = technique.ClusterMiniSetCount,
                        ClusterRestSeconds = technique.ClusterRestSeconds,
                        MyoActivationReps = technique.MyoActivationReps,
                        MyoRepsPerMiniSet = technique.MyoRepsPerMiniSet,
                        MyoRestSeconds = technique.MyoRestSeconds
                    });
                }
            }
        }

        foreach (var (newExerciseId, oldLinkedLocalId) in pendingSupersetLinks)
        {
            if (!exerciseIdMap.TryGetValue(oldLinkedLocalId, out var newLinkedId))
                continue;

            if (!insertedExercises.TryGetValue(newExerciseId, out var exerciseEntity))
                continue;

            exerciseEntity.SupersetLinkedExerciseId = newLinkedId;
            await _database.SaveRoutineExerciseAsync(exerciseEntity);
        }

        foreach (var backupSession in snapshot.Sessions)
        {
            var session = new WorkoutSession
            {
                UserProfileId = targetProfile.Id,
                Name = backupSession.Name,
                SessionDateUtc = backupSession.SessionDateUtc,
                DurationMinutes = backupSession.DurationMinutes,
                Notes = backupSession.Notes,
                CreatedAtUtc = backupSession.CreatedAtUtc
            };

            await _database.SaveWorkoutSessionAsync(session);

            foreach (var backupBlock in backupSession.Blocks)
            {
                var block = new WorkoutExerciseBlock
                {
                    WorkoutSessionId = session.Id,
                    DisplayOrder = backupBlock.DisplayOrder,
                    ExerciseName = backupBlock.ExerciseName,
                    BlockType = backupBlock.BlockType,
                    PlannedSets = backupBlock.PlannedSets,
                    TargetReps = backupBlock.TargetReps,
                    SuggestedWeight = backupBlock.SuggestedWeight,
                    RestSeconds = backupBlock.RestSeconds,
                    Notes = backupBlock.Notes,
                    IsCompleted = backupBlock.IsCompleted
                };

                await _database.SaveWorkoutExerciseBlockAsync(block);

                foreach (var backupEntry in backupBlock.Entries)
                {
                    await _database.SaveWorkoutBlockEntryAsync(new WorkoutBlockEntry
                    {
                        WorkoutExerciseBlockId = block.Id,
                        WorkoutSessionId = session.Id,
                        UserProfileId = targetProfile.Id,
                        EntryOrder = backupEntry.EntryOrder,
                        EntryType = backupEntry.EntryType,
                        Weight = backupEntry.Weight,
                        Reps = backupEntry.Reps,
                        RestSeconds = backupEntry.RestSeconds,
                        Notes = backupEntry.Notes
                    });
                }
            }
        }
    }
}
