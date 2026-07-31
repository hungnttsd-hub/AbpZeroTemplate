using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;

namespace AbpIoTemplateProject.Store;

[AllowAnonymous]
public class StorefrontAppService : AbpIoTemplateProjectAppService, IStorefrontAppService
{
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IRepository<Category, Guid> _categoryRepository;
    private readonly IRepository<Brand, Guid> _brandRepository;
    private readonly IRepository<InventoryItem, Guid> _inventoryRepository;
    private readonly IRepository<Banner, Guid> _bannerRepository;
    private readonly IRepository<Article, Guid> _articleRepository;
    private readonly IRepository<ArticleCategory, Guid> _articleCategoryRepository;
    private readonly IRepository<StoreLocation, Guid> _storeRepository;

    public StorefrontAppService(
        IRepository<Product, Guid> productRepository,
        IRepository<Category, Guid> categoryRepository,
        IRepository<Brand, Guid> brandRepository,
        IRepository<InventoryItem, Guid> inventoryRepository,
        IRepository<Banner, Guid> bannerRepository,
        IRepository<Article, Guid> articleRepository,
        IRepository<ArticleCategory, Guid> articleCategoryRepository,
        IRepository<StoreLocation, Guid> storeRepository)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _brandRepository = brandRepository;
        _inventoryRepository = inventoryRepository;
        _bannerRepository = bannerRepository;
        _articleRepository = articleRepository;
        _articleCategoryRepository = articleCategoryRepository;
        _storeRepository = storeRepository;
    }

    public async Task<HomePageDto> GetHomeAsync()
    {
        var now = Clock.Now;
        var banners = (await _bannerRepository.GetListAsync())
            .Where(x => x.IsVisibleAt(now))
            .OrderBy(x => x.DisplayOrder)
            .Take(8)
            .Select(MapBanner)
            .ToList();

        var categories = await GetCategoriesAsync();
        var featured = await GetProductsAsync(new ProductListInput { Featured = true, MaxResultCount = 8 });
        var bestSellers = await GetProductsAsync(new ProductListInput { BestSelling = true, MaxResultCount = 8 });
        var newest = await GetProductsAsync(new ProductListInput { Newest = true, MaxResultCount = 8 });
        var promotions = await GetProductsAsync(new ProductListInput { OnSale = true, MaxResultCount = 8 });
        var articles = await GetArticlesAsync(new PagedAndSortedResultRequestDto { MaxResultCount = 4 });
        var stores = await GetStoresAsync();

        return new HomePageDto
        {
            Banners = banners,
            FeaturedCategories = categories.Where(x => x.IsFeatured).Take(10).ToList(),
            FeaturedProducts = featured.Items.ToList(),
            BestSellers = bestSellers.Items.ToList(),
            NewProducts = newest.Items.ToList(),
            PromotionProducts = promotions.Items.ToList(),
            Articles = articles.Items.ToList(),
            Stores = stores.Take(4).ToList()
        };
    }

    public async Task<PagedResultDto<ProductListItemDto>> GetProductsAsync(ProductListInput input)
    {
        var categories = await _categoryRepository.GetListAsync();
        var categoryIds = ResolveCategoryIds(categories, input.Category);
        var query = await _productRepository.GetQueryableAsync();
        query = query.Where(x => x.IsActive && x.IsVisible);

        if (!input.Filter.IsNullOrWhiteSpace())
        {
            var filter = input.Filter!.Trim().ToLower();
            query = query.Where(x =>
                x.Name.ToLower().Contains(filter) ||
                x.Sku.ToLower().Contains(filter) ||
                x.Code.ToLower().Contains(filter));
        }

        if (categoryIds.Count > 0)
        {
            query = query.Where(x => categoryIds.Contains(x.CategoryId));
        }
        else if (!input.Category.IsNullOrWhiteSpace())
        {
            return new PagedResultDto<ProductListItemDto>(0, Array.Empty<ProductListItemDto>());
        }

        query = query
            .WhereIf(input.BrandId.HasValue, x => x.BrandId == input.BrandId)
            .WhereIf(input.SupplierId.HasValue, x => x.SupplierId == input.SupplierId)
            .WhereIf(input.MinPrice.HasValue, x => (x.SalePrice ?? x.ListPrice ?? 0) >= input.MinPrice)
            .WhereIf(input.MaxPrice.HasValue, x => (x.SalePrice ?? x.ListPrice ?? 0) <= input.MaxPrice)
            .WhereIf(input.OnSale, x => x.SalePrice.HasValue && x.ListPrice.HasValue && x.SalePrice < x.ListPrice)
            .WhereIf(input.Featured, x => x.IsFeatured)
            .WhereIf(input.Newest, x => x.IsNew)
            .WhereIf(input.BestSelling, x => x.IsBestSeller);

        var inventory = await _inventoryRepository.GetListAsync();
        var availableByProduct = inventory
            .GroupBy(x => x.ProductId)
            .ToDictionary(x => x.Key, x => x.Sum(y => y.AvailableQuantity));

        if (input.OnlyInStock)
        {
            var inStockIds = availableByProduct.Where(x => x.Value > 0).Select(x => x.Key).ToList();
            query = query.Where(x => x.AllowBackorder || inStockIds.Contains(x.Id));
        }

        var totalCount = await AsyncExecuter.CountAsync(query);
        query = ApplyProductSorting(query, input.Sorting);
        var maxResultCount = Math.Clamp(input.MaxResultCount, 1, StoreConsts.MaxPageSize);
        var products = await AsyncExecuter.ToListAsync(query.Skip(input.SkipCount).Take(maxResultCount));
        var brands = await _brandRepository.GetListAsync();

        var categoryNames = categories.ToDictionary(x => x.Id, x => x.Name);
        var brandNames = brands.ToDictionary(x => x.Id, x => x.Name);
        var items = products.Select(x => MapProduct(
            x,
            categoryNames.GetValueOrDefault(x.CategoryId, string.Empty),
            x.BrandId.HasValue ? brandNames.GetValueOrDefault(x.BrandId.Value) : null,
            x.AllowBackorder || availableByProduct.GetValueOrDefault(x.Id) > 0)).ToList();

        return new PagedResultDto<ProductListItemDto>(totalCount, items);
    }

    public async Task<ProductDetailDto> GetProductAsync(string slug)
    {
        Check.NotNullOrWhiteSpace(slug, nameof(slug));
        var query = await _productRepository.WithDetailsAsync(x => x.Variants, x => x.Images);
        var product = await AsyncExecuter.FirstOrDefaultAsync(
            query.Where(x => x.Slug == slug.ToLower() && x.IsActive && x.IsVisible));

        if (product is null)
        {
            throw new UserFriendlyException(L["Store:ProductNotFound"]);
        }

        var category = await _categoryRepository.GetAsync(product.CategoryId);
        var brand = product.BrandId.HasValue
            ? await _brandRepository.FindAsync(product.BrandId.Value)
            : null;
        var inventory = await _inventoryRepository.GetListAsync(x => x.ProductId == product.Id);
        var availableByVariant = inventory
            .Where(x => x.ProductVariantId.HasValue)
            .GroupBy(x => x.ProductVariantId!.Value)
            .ToDictionary(x => x.Key, x => x.Sum(y => y.AvailableQuantity));
        var isInStock = product.AllowBackorder || inventory.Sum(x => x.AvailableQuantity) > 0;
        var dto = MapProductDetail(product, category.Name, brand?.Name, isInStock, availableByVariant);

        var related = await GetProductsAsync(new ProductListInput
        {
            Category = category.Slug,
            MaxResultCount = 5
        });
        dto.RelatedProducts = related.Items.Where(x => x.Id != product.Id).Take(4).ToList();
        return dto;
    }

    public async Task<List<SearchSuggestionDto>> SearchSuggestionsAsync(string query)
    {
        if (query.IsNullOrWhiteSpace() || query.Trim().Length < 2)
        {
            return new List<SearchSuggestionDto>();
        }

        var result = await GetProductsAsync(new ProductListInput
        {
            Filter = query,
            MaxResultCount = StoreConsts.SearchSuggestionLimit
        });

        return result.Items.Select(x => new SearchSuggestionDto
        {
            ProductId = x.Id,
            Name = x.Name,
            Slug = x.Slug,
            Sku = x.Sku,
            ImageUrl = x.ThumbnailUrl,
            SalePrice = x.SalePrice,
            ListPrice = x.ListPrice,
            IsInStock = x.IsInStock
        }).ToList();
    }

    public async Task<List<CategoryDto>> GetCategoriesAsync()
    {
        var categories = (await _categoryRepository.GetListAsync(x => x.IsActive))
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .ToList();
        var productQuery = await _productRepository.GetQueryableAsync();
        var counts = await AsyncExecuter.ToListAsync(
            productQuery.Where(x => x.IsActive && x.IsVisible)
                .GroupBy(x => x.CategoryId)
                .Select(x => new { CategoryId = x.Key, Count = x.Count() }));
        var countMap = counts.ToDictionary(x => x.CategoryId, x => x.Count);

        return categories.Select(x => new CategoryDto
        {
            Id = x.Id,
            ParentId = x.ParentId,
            Name = x.Name,
            Slug = x.Slug,
            Description = x.Description,
            ImageUrl = x.ImageUrl,
            IsFeatured = x.IsFeatured,
            DisplayOrder = x.DisplayOrder,
            ProductCount = countMap.GetValueOrDefault(x.Id)
        }).ToList();
    }

    public async Task<List<BrandDto>> GetBrandsAsync()
    {
        return (await _brandRepository.GetListAsync(x => x.IsActive))
            .OrderByDescending(x => x.IsFeatured)
            .ThenBy(x => x.Name)
            .Select(x => new BrandDto
            {
                Id = x.Id,
                Name = x.Name,
                Slug = x.Slug,
                LogoUrl = x.LogoUrl,
                IsFeatured = x.IsFeatured
            }).ToList();
    }

    public async Task<PagedResultDto<ArticleSummaryDto>> GetArticlesAsync(PagedAndSortedResultRequestDto input)
    {
        var query = await _articleRepository.GetQueryableAsync();
        query = query.Where(x => x.Status == ContentStatus.Published && x.PublishTime <= Clock.Now);
        var totalCount = await AsyncExecuter.CountAsync(query);
        var maxResultCount = Math.Clamp(input.MaxResultCount, 1, StoreConsts.MaxPageSize);
        var articles = await AsyncExecuter.ToListAsync(
            query.OrderByDescending(x => x.PublishTime).Skip(input.SkipCount).Take(maxResultCount));
        var categories = (await _articleCategoryRepository.GetListAsync())
            .ToDictionary(x => x.Id, x => x.Name);

        return new PagedResultDto<ArticleSummaryDto>(
            totalCount,
            articles.Select(x => MapArticle(x, categories.GetValueOrDefault(x.ArticleCategoryId, string.Empty))).ToList());
    }

    public async Task<ArticleDetailDto> GetArticleAsync(string slug)
    {
        Check.NotNullOrWhiteSpace(slug, nameof(slug));
        var article = await _articleRepository.FindAsync(x =>
            x.Slug == slug.ToLower() &&
            x.Status == ContentStatus.Published &&
            x.PublishTime <= Clock.Now);
        if (article is null)
        {
            throw new UserFriendlyException(L["Store:ArticleNotFound"]);
        }

        var category = await _articleCategoryRepository.GetAsync(article.ArticleCategoryId);
        var dto = new ArticleDetailDto
        {
            Id = article.Id,
            Title = article.Title,
            Slug = article.Slug,
            Summary = article.Summary,
            Content = article.Content,
            AuthorName = article.AuthorName,
            CategoryName = category.Name,
            FeaturedImageUrl = article.FeaturedImageUrl,
            PublishTime = article.PublishTime,
            IsFeatured = article.IsFeatured,
            MetaTitle = article.MetaTitle,
            MetaDescription = article.MetaDescription
        };

        var related = await _articleRepository.GetListAsync(x =>
            x.Id != article.Id &&
            x.ArticleCategoryId == article.ArticleCategoryId &&
            x.Status == ContentStatus.Published);
        dto.RelatedArticles = related.OrderByDescending(x => x.PublishTime).Take(3)
            .Select(x => MapArticle(x, category.Name)).ToList();
        return dto;
    }

    public async Task<List<StoreLocationDto>> GetStoresAsync()
    {
        return (await _storeRepository.GetListAsync(x => x.IsActive))
            .OrderBy(x => x.DisplayOrder)
            .Select(x => new StoreLocationDto
            {
                Id = x.Id,
                Name = x.Name,
                Address = x.Address,
                Phone = x.Phone,
                OpeningHours = x.OpeningHours,
                MapUrl = x.MapUrl,
                ImageUrl = x.ImageUrl
            }).ToList();
    }

    private static IQueryable<Product> ApplyProductSorting(IQueryable<Product> query, string? sorting)
    {
        return sorting?.Trim().ToLowerInvariant() switch
        {
            "price" or "price asc" => query.OrderBy(x => x.SalePrice ?? x.ListPrice),
            "price desc" => query.OrderByDescending(x => x.SalePrice ?? x.ListPrice),
            "name" or "name asc" => query.OrderBy(x => x.Name),
            "name desc" => query.OrderByDescending(x => x.Name),
            "bestselling" => query.OrderByDescending(x => x.IsBestSeller).ThenByDescending(x => x.CreationTime),
            _ => query.OrderByDescending(x => x.IsFeatured).ThenByDescending(x => x.CreationTime)
        };
    }

    private static HashSet<Guid> ResolveCategoryIds(List<Category> categories, string? slug)
    {
        if (slug.IsNullOrWhiteSpace())
        {
            return new HashSet<Guid>();
        }

        var root = categories.FirstOrDefault(x => x.Slug == slug.Trim().ToLowerInvariant());
        if (root is null)
        {
            return new HashSet<Guid>();
        }

        var result = new HashSet<Guid> { root.Id };
        var changed = true;
        while (changed)
        {
            changed = false;
            foreach (var category in categories.Where(x => x.ParentId.HasValue && result.Contains(x.ParentId.Value)))
            {
                changed |= result.Add(category.Id);
            }
        }

        return result;
    }

    private static ProductListItemDto MapProduct(Product product, string categoryName, string? brandName, bool isInStock)
    {
        return new ProductListItemDto
        {
            Id = product.Id,
            Code = product.Code,
            Sku = product.Sku,
            Name = product.Name,
            Slug = product.Slug,
            CategoryName = categoryName,
            BrandName = brandName,
            SalePrice = product.SalePrice,
            ListPrice = product.ListPrice,
            ThumbnailUrl = product.ThumbnailUrl ?? product.Images.OrderBy(x => x.DisplayOrder).FirstOrDefault()?.Url,
            HoverImageUrl = product.Images.OrderBy(x => x.DisplayOrder).Skip(1).FirstOrDefault()?.Url,
            IsFeatured = product.IsFeatured,
            IsNew = product.IsNew,
            IsBestSeller = product.IsBestSeller,
            IsInStock = isInStock,
            HasVariants = product.Type == ProductType.Variant
        };
    }

    private static ProductDetailDto MapProductDetail(
        Product product,
        string categoryName,
        string? brandName,
        bool isInStock,
        IReadOnlyDictionary<Guid, int> availableByVariant)
    {
        var list = MapProduct(product, categoryName, brandName, isInStock);
        return new ProductDetailDto
        {
            Id = list.Id,
            Code = list.Code,
            Sku = list.Sku,
            Name = list.Name,
            Slug = list.Slug,
            CategoryName = list.CategoryName,
            BrandName = list.BrandName,
            SalePrice = list.SalePrice,
            ListPrice = list.ListPrice,
            ThumbnailUrl = list.ThumbnailUrl,
            HoverImageUrl = list.HoverImageUrl,
            IsFeatured = list.IsFeatured,
            IsNew = list.IsNew,
            IsBestSeller = list.IsBestSeller,
            IsInStock = list.IsInStock,
            HasVariants = list.HasVariants,
            Type = product.Type,
            CategoryId = product.CategoryId,
            BrandId = product.BrandId,
            Unit = product.Unit,
            ShortDescription = product.ShortDescription,
            Description = product.Description,
            Specifications = product.Specifications,
            UsageInstructions = product.UsageInstructions,
            TaxRate = product.TaxRate,
            Weight = product.Weight,
            Warranty = product.Warranty,
            MinimumOrderQuantity = product.MinimumOrderQuantity,
            MaximumOrderQuantity = product.MaximumOrderQuantity,
            MetaTitle = product.MetaTitle,
            MetaDescription = product.MetaDescription,
            CanonicalUrl = product.CanonicalUrl,
            Images = product.Images.OrderBy(x => x.DisplayOrder).Select(x => new ProductImageDto
            {
                Id = x.Id,
                Url = x.Url,
                AltText = x.AltText,
                DisplayOrder = x.DisplayOrder,
                IsPrimary = x.IsPrimary
            }).ToList(),
            Variants = product.Variants.Where(x => x.IsActive).Select(x =>
            {
                var available = availableByVariant.GetValueOrDefault(x.Id);
                return new ProductVariantDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Sku = x.Sku,
                    OptionSummary = x.OptionSummary,
                    SalePrice = x.SalePrice ?? product.SalePrice,
                    ListPrice = x.ListPrice ?? product.ListPrice,
                    ImageUrl = x.ImageUrl,
                    Weight = x.Weight,
                    IsActive = x.IsActive,
                    IsInStock = product.AllowBackorder || available > 0,
                    MaximumPurchasableQuantity = product.AllowBackorder
                        ? product.MaximumOrderQuantity
                        : Math.Min(product.MaximumOrderQuantity, available)
                };
            }).ToList()
        };
    }

    private static BannerDto MapBanner(Banner banner)
    {
        return new BannerDto
        {
            Id = banner.Id,
            Title = banner.Title,
            Description = banner.Description,
            DesktopImageUrl = banner.DesktopImageUrl,
            MobileImageUrl = banner.MobileImageUrl,
            ButtonText = banner.ButtonText,
            TargetUrl = banner.TargetUrl
        };
    }

    private static ArticleSummaryDto MapArticle(Article article, string categoryName)
    {
        return new ArticleSummaryDto
        {
            Id = article.Id,
            Title = article.Title,
            Slug = article.Slug,
            Summary = article.Summary,
            CategoryName = categoryName,
            FeaturedImageUrl = article.FeaturedImageUrl,
            PublishTime = article.PublishTime,
            IsFeatured = article.IsFeatured
        };
    }
}
