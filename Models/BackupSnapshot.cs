namespace IronReign.Models;

public class BackupSnapshot
{
    public int SchemaVersion { get; set; } = 1;

    public DateTime BackedUpAtUtc { get; set; }

    public BackupProfile Profile { get; set; } = new();

    public List<BackupRoutine> Routines { get; set; } = new();

    public List<BackupSession> Sessions { get; set; } = new();
}

public class BackupProfile
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public int Age { get; set; }

    public string Sex { get; set; } = string.Empty;

    public string PreferredWeightUnit { get; set; } = "kg";
}

public class BackupRoutine
{
    public string Name { get; set; } = string.Empty;

    public string Notes { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public int DisplayOrder { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public List<BackupExercise> Exercises { get; set; } = new();
}

public class BackupExercise
{
    // Id local en el dispositivo de origen. Solo se usa para volver a enlazar
    // superseries dentro de este mismo snapshot; no tiene significado fuera de él.
    public int LocalId { get; set; }

    public int DisplayOrder { get; set; }

    public string ExerciseName { get; set; } = string.Empty;

    public string BlockType { get; set; } = "Normal";

    public int PlannedSets { get; set; }

    public string TargetReps { get; set; } = string.Empty;

    public double SuggestedWeight { get; set; }

    public int RestSeconds { get; set; }

    public string Notes { get; set; } = string.Empty;

    public int? SupersetLinkedLocalId { get; set; }

    public List<BackupSetTechnique> SetTechniques { get; set; } = new();
}

public class BackupSetTechnique
{
    public int SetIndex { get; set; }

    public string TechniqueType { get; set; } = "Normal";

    public double DropWeight { get; set; }

    public string DropReps { get; set; } = string.Empty;

    public double DropWeight2 { get; set; }

    public string DropReps2 { get; set; } = string.Empty;

    public double ClusterWeight { get; set; }

    public string ClusterRepsPerMiniSet { get; set; } = string.Empty;

    public int ClusterMiniSetCount { get; set; }

    public int ClusterRestSeconds { get; set; }

    public string MyoActivationReps { get; set; } = string.Empty;

    public string MyoRepsPerMiniSet { get; set; } = string.Empty;

    public int MyoRestSeconds { get; set; }
}

public class BackupSession
{
    public string Name { get; set; } = string.Empty;

    public DateTime SessionDateUtc { get; set; }

    public int DurationMinutes { get; set; }

    public string Notes { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public List<BackupBlock> Blocks { get; set; } = new();
}

public class BackupBlock
{
    public int DisplayOrder { get; set; }

    public string ExerciseName { get; set; } = string.Empty;

    public string BlockType { get; set; } = "Normal";

    public int PlannedSets { get; set; }

    public string TargetReps { get; set; } = string.Empty;

    public double SuggestedWeight { get; set; }

    public int RestSeconds { get; set; }

    public string Notes { get; set; } = string.Empty;

    public bool IsCompleted { get; set; }

    public List<BackupEntry> Entries { get; set; } = new();
}

public class BackupEntry
{
    public int EntryOrder { get; set; }

    public string EntryType { get; set; } = "Main";

    public double Weight { get; set; }

    public int Reps { get; set; }

    public int RestSeconds { get; set; }

    public string Notes { get; set; } = string.Empty;
}
