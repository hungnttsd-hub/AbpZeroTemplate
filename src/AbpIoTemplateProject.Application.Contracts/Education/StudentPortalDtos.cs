using System;
using System.ComponentModel.DataAnnotations;

namespace AbpIoTemplateProject.Education;

public class StudentProfileDto
{
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? CurrentLevel { get; set; }
    public string? Target { get; set; }
}

public class UpdateStudentProfileDto
{
    [Required, StringLength(EducationConsts.NameMaxLength)] public string FullName { get; set; } = null!;
    [StringLength(EducationConsts.PhoneMaxLength)] public string? PhoneNumber { get; set; }
    [EmailAddress, StringLength(EducationConsts.EmailMaxLength)] public string? Email { get; set; }
    [StringLength(EducationConsts.ShortTextMaxLength)] public string? CurrentLevel { get; set; }
    [StringLength(EducationConsts.ShortTextMaxLength)] public string? Target { get; set; }
}

public class StudentEnrollmentDto
{
    public string CourseName { get; set; } = null!;
    public string ClassCode { get; set; } = null!;
    public string? ScheduleText { get; set; }
    public DateTime StartDate { get; set; }
    public string? Status { get; set; }
    public decimal AgreedTuitionFee { get; set; }
    public decimal PaidAmount { get; set; }
}
