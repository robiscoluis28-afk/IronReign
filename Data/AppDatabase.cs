using IronReign.Models;
using SQLite;

namespace IronReign.Data;

public class AppDatabase
{
    private SQLiteAsyncConnection? _database;

    private async Task Init()
    {
        if (_database is not null)
            return;

        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "ironreign.db3");
        _database = new SQLiteAsyncConnection(dbPath);

        await _database.CreateTableAsync<UserProfile>();
        await _database.CreateTableAsync<WorkoutSession>();
        await _database.CreateTableAsync<WorkoutExerciseBlock>();
        await _database.CreateTableAsync<WorkoutBlockEntry>();
        await _database.CreateTableAsync<RoutineTemplate>();
        await _database.CreateTableAsync<RoutineExercise>();
        await _database.CreateTableAsync<RoutineExerciseSetTechnique>(); // NUEVO
    }

    public async Task<UserProfile?> GetUserProfileByFirebaseUidAsync(string firebaseUid)
    {
        await Init();

        return await _database!.Table<UserProfile>()
            .FirstOrDefaultAsync(x => x.FirebaseUid == firebaseUid);
    }

    public async Task<List<UserProfile>> GetUserProfilesAsync()
    {
        await Init();

        return await _database!.Table<UserProfile>()
            .OrderBy(x => x.FirstName)
            .ThenBy(x => x.LastName)
            .ToListAsync();
    }

    public async Task<UserProfile?> GetUserProfileByIdAsync(int id)
    {
        await Init();

        return await _database!.Table<UserProfile>()
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<UserProfile?> GetActiveUserProfileAsync()
    {
        await Init();

        return await _database!.Table<UserProfile>()
            .FirstOrDefaultAsync(x => x.IsActive);
    }

    public async Task<int> SaveUserProfileAsync(UserProfile user)
    {
        await Init();

        if (user.IsActive)
        {
            var users = await _database!.Table<UserProfile>().ToListAsync();
            foreach (var existingUser in users.Where(x => x.Id != user.Id && x.IsActive))
            {
                existingUser.IsActive = false;
                await _database.UpdateAsync(existingUser);
            }
        }

        if (user.Id != 0)
            return await _database!.UpdateAsync(user);

        return await _database!.InsertAsync(user);
    }

    public async Task<int> DeleteUserProfileAsync(UserProfile user)
    {
        await Init();
        return await _database!.DeleteAsync(user);
    }

    public async Task<List<WorkoutSession>> GetWorkoutSessionsByUserAsync(int userProfileId)
    {
        await Init();

        return await _database!.Table<WorkoutSession>()
            .Where(x => x.UserProfileId == userProfileId)
            .OrderByDescending(x => x.SessionDateUtc)
            .ToListAsync();
    }

    public async Task<int> SaveWorkoutSessionAsync(WorkoutSession session)
    {
        await Init();

        if (session.Id != 0)
            return await _database!.UpdateAsync(session);

        return await _database!.InsertAsync(session);
    }

    public async Task DeleteWorkoutBlockEntriesByBlockIdAsync(int workoutExerciseBlockId)
    {
        await Init();

        var items = await _database!.Table<WorkoutBlockEntry>()
            .Where(x => x.WorkoutExerciseBlockId == workoutExerciseBlockId)
            .ToListAsync();

        foreach (var item in items)
            await _database.DeleteAsync(item);
    }

    public async Task DeleteWorkoutExerciseBlocksBySessionIdAsync(int workoutSessionId)
    {
        await Init();

        var blocks = await _database!.Table<WorkoutExerciseBlock>()
            .Where(x => x.WorkoutSessionId == workoutSessionId)
            .ToListAsync();

        foreach (var block in blocks)
        {
            await DeleteWorkoutBlockEntriesByBlockIdAsync(block.Id);
            await _database.DeleteAsync(block);
        }
    }

    public async Task DeleteWorkoutSessionAsync(WorkoutSession session)
    {
        await Init();

        await DeleteWorkoutExerciseBlocksBySessionIdAsync(session.Id);
        await _database!.DeleteAsync(session);
    }

    public async Task<List<WorkoutExerciseBlock>> GetWorkoutExerciseBlocksAsync(int workoutSessionId)
    {
        await Init();

        return await _database!.Table<WorkoutExerciseBlock>()
            .Where(x => x.WorkoutSessionId == workoutSessionId)
            .OrderBy(x => x.DisplayOrder)
            .ToListAsync();
    }

    public async Task<int> SaveWorkoutExerciseBlockAsync(WorkoutExerciseBlock block)
    {
        await Init();

        if (block.Id != 0)
            return await _database!.UpdateAsync(block);

        return await _database!.InsertAsync(block);
    }

    public async Task<List<WorkoutBlockEntry>> GetWorkoutBlockEntriesAsync(int workoutExerciseBlockId)
    {
        await Init();

        return await _database!.Table<WorkoutBlockEntry>()
            .Where(x => x.WorkoutExerciseBlockId == workoutExerciseBlockId)
            .OrderBy(x => x.EntryOrder)
            .ToListAsync();
    }

    public async Task<int> SaveWorkoutBlockEntryAsync(WorkoutBlockEntry entry)
    {
        await Init();

        if (entry.Id != 0)
            return await _database!.UpdateAsync(entry);

        return await _database!.InsertAsync(entry);
    }

    public async Task<int> SaveRoutineTemplateAsync(RoutineTemplate routine)
    {
        await Init();

        if (routine.Id != 0)
            return await _database!.UpdateAsync(routine);

        var existing = await _database!.Table<RoutineTemplate>()
            .Where(x => x.UserProfileId == routine.UserProfileId)
            .ToListAsync();

        routine.DisplayOrder = existing.Count == 0 ? 0 : existing.Max(x => x.DisplayOrder) + 1;

        return await _database!.InsertAsync(routine);
    }

    public async Task MoveRoutineUpAsync(int userProfileId, int routineTemplateId)
    {
        await Init();

        var routines = await _database!.Table<RoutineTemplate>()
            .Where(x => x.UserProfileId == userProfileId)
            .OrderBy(x => x.DisplayOrder)
            .ToListAsync();

        var index = routines.FindIndex(x => x.Id == routineTemplateId);

        if (index <= 0)
            return;

        var current = routines[index];
        var previous = routines[index - 1];

        (current.DisplayOrder, previous.DisplayOrder) = (previous.DisplayOrder, current.DisplayOrder);

        await _database.UpdateAsync(current);
        await _database.UpdateAsync(previous);
    }

    public async Task MoveRoutineDownAsync(int userProfileId, int routineTemplateId)
    {
        await Init();

        var routines = await _database!.Table<RoutineTemplate>()
            .Where(x => x.UserProfileId == userProfileId)
            .OrderBy(x => x.DisplayOrder)
            .ToListAsync();

        var index = routines.FindIndex(x => x.Id == routineTemplateId);

        if (index < 0 || index >= routines.Count - 1)
            return;

        var current = routines[index];
        var next = routines[index + 1];

        (current.DisplayOrder, next.DisplayOrder) = (next.DisplayOrder, current.DisplayOrder);

        await _database.UpdateAsync(current);
        await _database.UpdateAsync(next);
    }

    public async Task SetRoutineOrderAsync(List<RoutineTemplate> orderedRoutines)
    {
        await Init();

        for (var i = 0; i < orderedRoutines.Count; i++)
        {
            orderedRoutines[i].DisplayOrder = i;
            await _database!.UpdateAsync(orderedRoutines[i]);
        }
    }

    public async Task<int> SaveRoutineExerciseAsync(RoutineExercise exercise)
    {
        await Init();

        if (exercise.Id != 0)
            return await _database!.UpdateAsync(exercise);

        return await _database!.InsertAsync(exercise);
    }

    public async Task<List<RoutineExerciseSetTechnique>> GetSetTechniquesAsync(int routineExerciseId)
    {
        await Init();

        return await _database!.Table<RoutineExerciseSetTechnique>()
            .Where(x => x.RoutineExerciseId == routineExerciseId)
            .OrderBy(x => x.SetIndex)
            .ToListAsync();
    }

    public async Task<int> SaveSetTechniqueAsync(RoutineExerciseSetTechnique technique)
    {
        await Init();

        if (technique.Id != 0)
            return await _database!.UpdateAsync(technique);

        return await _database!.InsertAsync(technique);
    }

    public async Task DeleteSetTechniqueAsync(RoutineExerciseSetTechnique technique)
    {
        await Init();
        await _database!.DeleteAsync(technique);
    }

    public async Task DeleteSetTechniquesByExerciseIdAsync(int routineExerciseId)
    {
        await Init();

        var items = await _database!.Table<RoutineExerciseSetTechnique>()
            .Where(x => x.RoutineExerciseId == routineExerciseId)
            .ToListAsync();

        foreach (var item in items)
            await _database.DeleteAsync(item);
    }

    public async Task<List<RoutineTemplate>> GetRoutineTemplatesByUserAsync(int userProfileId)
    {
        await Init();

        return await _database!.Table<RoutineTemplate>()
            .Where(x => x.UserProfileId == userProfileId)
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .ToListAsync();
    }

    public async Task<RoutineTemplate?> GetRoutineTemplateByIdAsync(int routineTemplateId)
    {
        await Init();

        return await _database!.Table<RoutineTemplate>()
            .FirstOrDefaultAsync(x => x.Id == routineTemplateId);
    }

    public async Task<List<RoutineExercise>> GetRoutineExercisesAsync(int routineTemplateId)
    {
        await Init();

        return await _database!.Table<RoutineExercise>()
            .Where(x => x.RoutineTemplateId == routineTemplateId)
            .OrderBy(x => x.DisplayOrder)
            .ToListAsync();
    }

    public async Task DeleteRoutineExercisesByRoutineIdAsync(int routineTemplateId)
    {
        await Init();

        var items = await _database!.Table<RoutineExercise>()
            .Where(x => x.RoutineTemplateId == routineTemplateId)
            .ToListAsync();

        foreach (var item in items)
        {
            await DeleteSetTechniquesByExerciseIdAsync(item.Id); // NUEVO
            await _database.DeleteAsync(item);
        }
    }

    public async Task DeleteRoutineExerciseAsync(RoutineExercise exercise)
    {
        await Init();

        await DeleteSetTechniquesByExerciseIdAsync(exercise.Id);

        // Si otro ejercicio estaba enlazado a este en superserie, desenlázalo.
        var linkedExercises = await _database!.Table<RoutineExercise>()
            .Where(x => x.SupersetLinkedExerciseId == exercise.Id)
            .ToListAsync();

        foreach (var linked in linkedExercises)
        {
            linked.SupersetLinkedExerciseId = null;
            await _database.UpdateAsync(linked);
        }

        await _database.DeleteAsync(exercise);
    }

    public async Task<int> DeleteRoutineTemplateAsync(RoutineTemplate routine)
    {
        await Init();

        await DeleteRoutineExercisesByRoutineIdAsync(routine.Id);
        return await _database!.DeleteAsync(routine);
    }

    public async Task SetActiveRoutineAsync(int userProfileId, int routineTemplateId)
    {
        await Init();

        var routines = await _database!.Table<RoutineTemplate>()
            .Where(x => x.UserProfileId == userProfileId)
            .ToListAsync();

        foreach (var routine in routines)
        {
            routine.IsActive = routine.Id == routineTemplateId;
            await _database.UpdateAsync(routine);
        }
    }

    public async Task<RoutineTemplate?> GetActiveRoutineAsync(int userProfileId)
    {
        await Init();

        return await _database!.Table<RoutineTemplate>()
            .FirstOrDefaultAsync(x => x.UserProfileId == userProfileId && x.IsActive);
    }

    public async Task<List<RoutineTemplate>> GetAllRoutineTemplatesAsync()
    {
        await Init();

        return await _database!.Table<RoutineTemplate>()
            .OrderBy(x => x.Name)
            .ToListAsync();
    }

    public async Task DeleteWorkoutSessionsByUserAsync(int userProfileId)
    {
        await Init();

        var sessions = await _database!.Table<WorkoutSession>()
            .Where(x => x.UserProfileId == userProfileId)
            .ToListAsync();

        foreach (var session in sessions)
            await DeleteWorkoutSessionAsync(session);
    }

    public async Task DeleteRoutineTemplatesByUserAsync(int userProfileId)
    {
        await Init();

        var routines = await _database!.Table<RoutineTemplate>()
            .Where(x => x.UserProfileId == userProfileId)
            .ToListAsync();

        foreach (var routine in routines)
            await DeleteRoutineTemplateAsync(routine);
    }

    public async Task ResetAllUserDataAsync(int userProfileId)
    {
        await Init();

        await DeleteWorkoutSessionsByUserAsync(userProfileId);
        await DeleteRoutineTemplatesByUserAsync(userProfileId);
    }
}