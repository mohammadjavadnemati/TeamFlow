namespace TeamFlow.Core.Enums;

public enum ActivityType
{
    TaskCreated = 1,
    TaskUpdated = 2,
    TaskDeleted = 3,
    TaskAssigned = 4,
    TaskStatusChanged = 5,
    TaskCompleted = 6,
    CommentAdded = 7,
    CommentDeleted = 8,
    FileUploaded = 9,
    FileDeleted = 10,
    SubtaskCreated = 11,
    SubtaskCompleted = 12,
    MemberInvited = 13,
    MemberRemoved = 14,
    SprintStarted = 15,
    SprintCompleted = 16
}