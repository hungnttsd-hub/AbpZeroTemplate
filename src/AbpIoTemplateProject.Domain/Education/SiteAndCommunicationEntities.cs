using System;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Domain.Entities.Auditing;

namespace AbpIoTemplateProject.Education;

public class Banner : FullAuditedAggregateRoot<Guid>
{
    public Banner(Guid id) : base(id) { }
    [Required, StringLength(EducationConsts.NameMaxLength)] public string Name { get; set; } = null!;
    [StringLength(EducationConsts.NameMaxLength)] public string? Heading { get; set; }
    [StringLength(EducationConsts.ShortTextMaxLength)] public string? Description { get; set; }
    [StringLength(EducationConsts.UrlMaxLength)] public string? DesktopImageUrl { get; set; }
    [StringLength(EducationConsts.UrlMaxLength)] public string? MobileImageUrl { get; set; }
    [StringLength(EducationConsts.ShortTextMaxLength)] public string? CallToActionText { get; set; }
    [StringLength(EducationConsts.UrlMaxLength)] public string? CallToActionUrl { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public int DisplayOrder { get; set; }
    public PublicationStatus Status { get; set; } = PublicationStatus.Draft;
}

public class SiteSetting : FullAuditedAggregateRoot<Guid>
{
    public SiteSetting(Guid id) : base(id) { }
    [Required, StringLength(EducationConsts.CodeMaxLength)] public string Key { get; set; } = null!;
    [Required, StringLength(EducationConsts.TextMaxLength)] public string Value { get; set; } = null!;
    [StringLength(EducationConsts.ShortTextMaxLength)] public string? Description { get; set; }
}

public class PaymentTransaction : FullAuditedAggregateRoot<Guid>
{
    public PaymentTransaction(Guid id) : base(id) { }
    public Guid EnrollmentId { get; set; }
    [Required, StringLength(EducationConsts.CodeMaxLength)] public string ReferenceCode { get; set; } = null!;
    [Required, StringLength(EducationConsts.CodeMaxLength)] public string Provider { get; set; } = null!;
    [StringLength(EducationConsts.CodeMaxLength)] public string? ProviderTransactionId { get; set; }
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    [StringLength(EducationConsts.UrlMaxLength)] public string? PaymentUrl { get; set; }
    [StringLength(EducationConsts.TextMaxLength)] public string? ProviderPayload { get; set; }
    public DateTime? PaidAt { get; set; }
}

public class NotificationMessage : FullAuditedAggregateRoot<Guid>
{
    public NotificationMessage(Guid id) : base(id) { }
    public NotificationChannel Channel { get; set; }
    [Required, StringLength(EducationConsts.EmailMaxLength)] public string Recipient { get; set; } = null!;
    [Required, StringLength(EducationConsts.NameMaxLength)] public string Subject { get; set; } = null!;
    [Required, StringLength(EducationConsts.TextMaxLength)] public string Body { get; set; } = null!;
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;
    [StringLength(EducationConsts.TextMaxLength)] public string? FailureReason { get; set; }
    public DateTime? SentAt { get; set; }
}
