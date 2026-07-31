using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace AbpIoTemplateProject.Store;

public class Banner : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string DesktopImageUrl { get; private set; } = string.Empty;
    public string MobileImageUrl { get; private set; } = string.Empty;
    public string? ButtonText { get; private set; }
    public string? TargetUrl { get; private set; }
    public DateTime? StartTime { get; private set; }
    public DateTime? EndTime { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; }

    protected Banner()
    {
    }

    public Banner(
        Guid id,
        string title,
        string desktopImageUrl,
        string mobileImageUrl,
        int displayOrder,
        Guid? tenantId = null) : base(id)
    {
        TenantId = tenantId;
        Title = Check.NotNullOrWhiteSpace(title, nameof(title), StoreConsts.MaxNameLength);
        DesktopImageUrl = Check.NotNullOrWhiteSpace(desktopImageUrl, nameof(desktopImageUrl), StoreConsts.MaxUrlLength);
        MobileImageUrl = Check.NotNullOrWhiteSpace(mobileImageUrl, nameof(mobileImageUrl), StoreConsts.MaxUrlLength);
        DisplayOrder = displayOrder;
        IsActive = true;
    }

    public void UpdateContent(string? description, string? buttonText, string? targetUrl, DateTime? startTime, DateTime? endTime, bool isActive)
    {
        Description = description?.Trim();
        ButtonText = buttonText?.Trim();
        TargetUrl = targetUrl?.Trim();
        StartTime = startTime;
        EndTime = endTime;
        IsActive = isActive;
    }

    public void UpdateMedia(string title, string desktopImageUrl, string mobileImageUrl, int displayOrder)
    {
        Title = Check.NotNullOrWhiteSpace(title, nameof(title), StoreConsts.MaxNameLength);
        DesktopImageUrl = Check.NotNullOrWhiteSpace(desktopImageUrl, nameof(desktopImageUrl), StoreConsts.MaxUrlLength);
        MobileImageUrl = Check.NotNullOrWhiteSpace(mobileImageUrl, nameof(mobileImageUrl), StoreConsts.MaxUrlLength);
        DisplayOrder = displayOrder;
    }

    public bool IsVisibleAt(DateTime now)
    {
        return IsActive &&
               (!StartTime.HasValue || StartTime.Value <= now) &&
               (!EndTime.HasValue || EndTime.Value >= now);
    }
}

public class StoreLocation : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Address { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public string OpeningHours { get; private set; } = string.Empty;
    public string? MapUrl { get; private set; }
    public string? ImageUrl { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; }

    protected StoreLocation()
    {
    }

    public StoreLocation(
        Guid id,
        string name,
        string address,
        string phone,
        string openingHours,
        int displayOrder,
        Guid? tenantId = null) : base(id)
    {
        TenantId = tenantId;
        Name = name;
        Address = address;
        Phone = phone;
        OpeningHours = openingHours;
        DisplayOrder = displayOrder;
        IsActive = true;
    }

    public void SetMedia(string? mapUrl, string? imageUrl)
    {
        MapUrl = mapUrl;
        ImageUrl = imageUrl;
    }
}

public class ArticleCategory : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }

    protected ArticleCategory()
    {
    }

    public ArticleCategory(Guid id, string name, string slug, Guid? tenantId = null) : base(id)
    {
        TenantId = tenantId;
        Name = name;
        Slug = slug.ToLowerInvariant();
        IsActive = true;
    }
}

public class Article : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; private set; }
    public Guid ArticleCategoryId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string Summary { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public string? FeaturedImageUrl { get; private set; }
    public string AuthorName { get; private set; } = string.Empty;
    public ContentStatus Status { get; private set; }
    public DateTime? PublishTime { get; private set; }
    public bool IsFeatured { get; private set; }
    public string? MetaTitle { get; private set; }
    public string? MetaDescription { get; private set; }

    protected Article()
    {
    }

    public Article(
        Guid id,
        Guid articleCategoryId,
        string title,
        string slug,
        string summary,
        string content,
        string authorName,
        Guid? tenantId = null) : base(id)
    {
        TenantId = tenantId;
        ArticleCategoryId = articleCategoryId;
        Update(title, slug, summary, content, authorName, null, false);
        Status = ContentStatus.Draft;
    }

    public void Update(
        string title,
        string slug,
        string summary,
        string content,
        string authorName,
        string? featuredImageUrl,
        bool isFeatured)
    {
        Title = Check.NotNullOrWhiteSpace(title, nameof(title), StoreConsts.MaxNameLength);
        Slug = Check.NotNullOrWhiteSpace(slug, nameof(slug), StoreConsts.MaxSlugLength).ToLowerInvariant();
        Summary = Check.NotNullOrWhiteSpace(summary, nameof(summary), StoreConsts.MaxAddressLength);
        Content = Check.NotNullOrWhiteSpace(content, nameof(content));
        AuthorName = Check.NotNullOrWhiteSpace(authorName, nameof(authorName), StoreConsts.MaxNameLength);
        FeaturedImageUrl = featuredImageUrl?.Trim();
        IsFeatured = isFeatured;
    }

    public void Publish(DateTime publishTime)
    {
        Status = ContentStatus.Published;
        PublishTime = publishTime;
    }

    public void SetSeo(string? metaTitle, string? metaDescription)
    {
        MetaTitle = metaTitle?.Trim();
        MetaDescription = metaDescription?.Trim();
    }
}

public class HomePageSection : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public HomeSectionType Type { get; private set; }
    public Guid? CategoryId { get; private set; }
    public Guid? BrandId { get; private set; }
    public int ItemCount { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsVisible { get; private set; }

    protected HomePageSection()
    {
    }

    public HomePageSection(
        Guid id,
        string title,
        HomeSectionType type,
        int itemCount,
        int displayOrder,
        Guid? tenantId = null) : base(id)
    {
        TenantId = tenantId;
        Title = title;
        Type = type;
        ItemCount = itemCount;
        DisplayOrder = displayOrder;
        IsVisible = true;
    }
}

public class SiteSetting : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; private set; }
    public string Key { get; private set; } = string.Empty;
    public string Value { get; private set; } = string.Empty;
    public bool IsPublic { get; private set; }

    protected SiteSetting()
    {
    }

    public SiteSetting(Guid id, string key, string value, bool isPublic, Guid? tenantId = null) : base(id)
    {
        TenantId = tenantId;
        Key = Check.NotNullOrWhiteSpace(key, nameof(key), StoreConsts.MaxNameLength);
        Value = value;
        IsPublic = isPublic;
    }

    public void SetValue(string value)
    {
        Value = value;
    }
}
