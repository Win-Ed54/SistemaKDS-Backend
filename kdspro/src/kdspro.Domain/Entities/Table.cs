using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace kdspro.Domain.Entities;

public class Table
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public int Number { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int Capacity { get; set; } = 4;
    public bool IsOccupied { get; set; } = false;
    public int? CurrentPartySize { get; set; }
    public DateTime? OccupiedSince { get; set; }
    public int? EstimatedDiningMinutes { get; set; }
    public string HostNotes { get; set; } = string.Empty;
    public string AssignedByName { get; set; } = string.Empty;
    public string AssignedWaiterId { get; set; } = string.Empty;
    public string AssignedWaiterName { get; set; } = string.Empty;
    public bool IsBeingCleaned { get; set; } = false;
    public DateTime? CleaningStartedAt { get; set; }
    public int? EstimatedCleaningMinutes { get; set; }
}
