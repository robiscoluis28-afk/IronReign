namespace IronReign.Services;

public sealed class WorkoutSessionState
{
    public bool IsRunning { get; private set; }

    public int ActiveRoutineId { get; private set; }

    public DateTime StartedAtUtc { get; private set; }

    public bool HasActiveWorkout =>
        IsRunning && ActiveRoutineId > 0 && StartedAtUtc != default;

    public void Start(int routineId)
    {
        ActiveRoutineId = routineId;
        StartedAtUtc = DateTime.UtcNow;
        IsRunning = true;
    }

    public void Stop()
    {
        IsRunning = false;
        ActiveRoutineId = 0;
        StartedAtUtc = default;
    }

    public int GetElapsedSeconds()
    {
        if (!HasActiveWorkout)
            return 0;

        return Math.Max(0, (int)(DateTime.UtcNow - StartedAtUtc).TotalSeconds);
    }
}