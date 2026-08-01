namespace AbpIoTemplateProject.Education;

public enum PublicationStatus
{
    Draft = 0,
    Active = 1,
    Inactive = 2,
    Archived = 3
}

public enum CourseDeliveryMode
{
    Offline = 0,
    Online = 1,
    Hybrid = 2,
    OneToOne = 3,
    Group = 4,
    SelfPaced = 5
}

public enum CourseClassStatus
{
    Planned = 0,
    OpenForEnrollment = 1,
    InProgress = 2,
    Completed = 3,
    Cancelled = 4
}

public enum LeadStatus
{
    New = 0,
    Contacted = 1,
    Qualified = 2,
    Converted = 3,
    Lost = 4
}

public enum PlacementTestStatus
{
    Draft = 0,
    Published = 1,
    Closed = 2
}

public enum PlacementAttemptStatus
{
    Started = 0,
    Submitted = 1,
    Reviewed = 2,
    Expired = 3
}

public enum LessonType
{
    Video = 0,
    Reading = 1,
    Exercise = 2,
    LiveSession = 3
}

public enum DocumentAccessLevel
{
    Public = 0,
    RegisteredStudent = 1,
    EnrolledStudent = 2
}

public enum PaymentStatus
{
    Pending = 0,
    Paid = 1,
    Failed = 2,
    Refunded = 3,
    Cancelled = 4
}

public enum NotificationChannel
{
    Email = 0,
    Sms = 1
}

public enum NotificationStatus
{
    Pending = 0,
    Sent = 1,
    Failed = 2
}
