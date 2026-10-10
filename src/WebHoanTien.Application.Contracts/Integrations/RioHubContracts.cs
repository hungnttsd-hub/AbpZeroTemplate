using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace WebHoanTien.Integrations.RioHub;

// Server-side client, deliberately not an IApplicationService / public proxy.
public interface IRioHubAffiliateClient
{
    Task<string> CreateAffiliateLinkAsync(string productUrlOrId, string subId, CancellationToken cancellationToken = default);
    Task<JsonElement> CreateLinkAsync(RioHubCreateLinkInput input, bool includeProduct = false, CancellationToken cancellationToken = default);
    Task<JsonElement> CreateGeneralLinksAsync(RioHubGeneralLinksInput input, CancellationToken cancellationToken = default);
    Task<JsonElement> GetLinksAsync(RioHubLinksQuery input, CancellationToken cancellationToken = default);
    Task<JsonElement> GetOrdersAsync(RioHubOrdersQuery input, CancellationToken cancellationToken = default);
    Task<JsonElement> GetProductsAsync(RioHubProductsQuery input, CancellationToken cancellationToken = default);
}

public class RioHubCreatorInput
{
    [Required, StringLength(100), RegularExpression(@"[A-Za-z0-9._]+")]
    public string CreatorUsername { get; set; } = string.Empty;
}

public sealed class RioHubCreateLinkInput : RioHubCreatorInput, IValidatableObject
{
    [StringLength(4096)] public string? ProductUrl { get; set; }
    [RegularExpression(@"[0-9]+"), StringLength(30)] public string? ProductId { get; set; }
    [Required, StringLength(128), RegularExpression(@"[A-Za-z0-9_]*(?:-[A-Za-z0-9_]*){0,3}")]
    public string SubId { get; set; } = string.Empty;
    [StringLength(64, MinimumLength = 1), RegularExpression(@"[A-Za-z0-9_-]+")]
    public string? Channel { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(ProductUrl) == string.IsNullOrWhiteSpace(ProductId))
            yield return new ValidationResult("Cung cấp đúng một trong ProductUrl hoặc ProductId.");
        if (!string.IsNullOrWhiteSpace(ProductUrl) &&
            (!Uri.TryCreate(ProductUrl, UriKind.Absolute, out var uri) || uri.Scheme != "https" ||
             !uri.IsDefaultPort || uri.UserInfo.Length > 0 ||
             !(uri.IdnHost.Equals("tiktok.com", StringComparison.OrdinalIgnoreCase) ||
               uri.IdnHost.EndsWith(".tiktok.com", StringComparison.OrdinalIgnoreCase))))
            yield return new ValidationResult("ProductUrl phải là URL HTTPS thuộc tiktok.com.");
    }
}

public sealed class RioHubGeneralLinksInput : RioHubCreatorInput, IValidatableObject
{
    [Required, MinLength(1), MaxLength(50)] public string[] ProductIds { get; set; } = Array.Empty<string>();
    [StringLength(100)] public string? CampaignId { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ProductIds is not null)
            foreach (var id in ProductIds)
                if (string.IsNullOrEmpty(id) || id.Length > 30 || !System.Text.RegularExpressions.Regex.IsMatch(id, @"\A[0-9]+\z"))
                    yield return new ValidationResult("ProductIds chỉ chứa mã sản phẩm dạng chuỗi số.");
    }
}

public class RioHubPagedQuery : RioHubCreatorInput, IValidatableObject
{
    [Range(1, int.MaxValue)] public int Page { get; set; } = 1;
    [Range(1, 200)] public int PageSize { get; set; } = 50;
    [StringLength(128)] public string? SubId { get; set; }
    [StringLength(128), RegularExpression(@"[A-Za-z0-9_]*")] public string? Sub1 { get; set; }
    [StringLength(128), RegularExpression(@"[A-Za-z0-9_]*")] public string? Sub2 { get; set; }
    [StringLength(128), RegularExpression(@"[A-Za-z0-9_]*")] public string? Sub3 { get; set; }
    [StringLength(128), RegularExpression(@"[A-Za-z0-9_]*")] public string? Sub4 { get; set; }
    [Range(0, long.MaxValue)] public long? TimeStart { get; set; }
    [Range(0, long.MaxValue)] public long? TimeEnd { get; set; }

    public virtual IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (TimeStart.HasValue && TimeEnd.HasValue && TimeStart >= TimeEnd)
            yield return new ValidationResult("TimeEnd phải lớn hơn TimeStart (Unix giây, cận cuối loại trừ).");
    }
}

public sealed class RioHubLinksQuery : RioHubPagedQuery
{
    [StringLength(100)] public string? Channel { get; set; }
}

public sealed class RioHubOrdersQuery : RioHubPagedQuery
{
    [Range(0, long.MaxValue)] public long? UpdateTimeStart { get; set; }
    [Range(0, long.MaxValue)] public long? UpdateTimeEnd { get; set; }
    [StringLength(6200)] public string? OrderId { get; set; }
    [StringLength(3100)] public string? ProductId { get; set; }
    [StringLength(26000)] public string? TraceId { get; set; }
    [RegularExpression(@"[123](,[123])*")] public string? Status { get; set; }
    [StringLength(200)] public string? SettlementStatus { get; set; }
    [StringLength(200)] public string? ContentType { get; set; }
    [Range(0, 1)] public int? FullyRefunded { get; set; }

    public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var error in base.Validate(validationContext)) yield return error;
        if (UpdateTimeStart.HasValue && UpdateTimeEnd.HasValue && UpdateTimeStart >= UpdateTimeEnd)
            yield return new ValidationResult("UpdateTimeEnd phải lớn hơn UpdateTimeStart.");
        foreach (var (value, maximum, name, numeric) in new[]
                 { (OrderId, 200, "OrderId", true), (ProductId, 100, "ProductId", true), (TraceId, 200, "TraceId", false) })
        {
            if (value is null) continue;
            var parts = value.Split(',');
            if (parts.Length > maximum || Array.Exists(parts, p => string.IsNullOrWhiteSpace(p) ||
                (numeric && (p.Length > 30 || !System.Text.RegularExpressions.Regex.IsMatch(p, @"\A[0-9]+\z")))))
                yield return new ValidationResult($"{name}: danh sách không hợp lệ hoặc vượt {maximum} phần tử.");
        }
    }
}

public sealed class RioHubProductsQuery : RioHubCreatorInput
{
    [Required, StringLength(3100), RegularExpression(@"[0-9]{1,30}(,[0-9]{1,30}){0,99}")]
    public string ProductId { get; set; } = string.Empty;
}

public sealed class RioHubApiException : Exception
{
    public HttpStatusCode? UpstreamStatus { get; }
    public TimeSpan? RetryAfter { get; }
    public string? ErrorCode { get; }
    public RioHubApiException(string message, HttpStatusCode? upstreamStatus = null, TimeSpan? retryAfter = null, string? errorCode = null)
        : base(message) { UpstreamStatus = upstreamStatus; RetryAfter = retryAfter; ErrorCode = errorCode; }
}
