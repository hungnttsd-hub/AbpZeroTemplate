using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AbpIoTemplateProject.Permissions;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;

namespace AbpIoTemplateProject.Store;

[Authorize]
public class StoreAdminAppService : AbpIoTemplateProjectAppService, IStoreAdminAppService
{
    private readonly IRepository<Category, Guid> _categoryRepository;
    private readonly IRepository<Brand, Guid> _brandRepository;
    private readonly IRepository<Supplier, Guid> _supplierRepository;
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IRepository<ProductVariant, Guid> _productVariantRepository;
    private readonly IRepository<Warehouse, Guid> _warehouseRepository;
    private readonly IRepository<InventoryItem, Guid> _inventoryRepository;
    private readonly IRepository<InventoryTransaction, Guid> _inventoryTransactionRepository;
    private readonly IRepository<Customer, Guid> _customerRepository;
    private readonly IRepository<Order, Guid> _orderRepository;
    private readonly IRepository<Payment, Guid> _paymentRepository;
    private readonly IRepository<Promotion, Guid> _promotionRepository;
    private readonly IRepository<Banner, Guid> _bannerRepository;
    private readonly IRepository<Article, Guid> _articleRepository;
    private readonly IRepository<ArticleCategory, Guid> _articleCategoryRepository;
    private readonly IStorefrontAppService _storefrontAppService;

    public StoreAdminAppService(
        IRepository<Category, Guid> categoryRepository,
        IRepository<Brand, Guid> brandRepository,
        IRepository<Supplier, Guid> supplierRepository,
        IRepository<Product, Guid> productRepository,
        IRepository<ProductVariant, Guid> productVariantRepository,
        IRepository<Warehouse, Guid> warehouseRepository,
        IRepository<InventoryItem, Guid> inventoryRepository,
        IRepository<InventoryTransaction, Guid> inventoryTransactionRepository,
        IRepository<Customer, Guid> customerRepository,
        IRepository<Order, Guid> orderRepository,
        IRepository<Payment, Guid> paymentRepository,
        IRepository<Promotion, Guid> promotionRepository,
        IRepository<Banner, Guid> bannerRepository,
        IRepository<Article, Guid> articleRepository,
        IRepository<ArticleCategory, Guid> articleCategoryRepository,
        IStorefrontAppService storefrontAppService)
    {
        _categoryRepository = categoryRepository;
        _brandRepository = brandRepository;
        _supplierRepository = supplierRepository;
        _productRepository = productRepository;
        _productVariantRepository = productVariantRepository;
        _warehouseRepository = warehouseRepository;
        _inventoryRepository = inventoryRepository;
        _inventoryTransactionRepository = inventoryTransactionRepository;
        _customerRepository = customerRepository;
        _orderRepository = orderRepository;
        _paymentRepository = paymentRepository;
        _promotionRepository = promotionRepository;
        _bannerRepository = bannerRepository;
        _articleRepository = articleRepository;
        _articleCategoryRepository = articleCategoryRepository;
        _storefrontAppService = storefrontAppService;
    }

    [Authorize(AbpIoTemplateProjectPermissions.Products.View)]
    public async Task<StoreAdminDashboardDto> GetDashboardAsync()
    {
        var inventories = await _inventoryRepository.GetListAsync();
        var orders = (await _orderRepository.GetListAsync())
            .OrderByDescending(x => x.CreationTime)
            .ToList();
        var recent = new List<OrderDto>();
        foreach (var order in orders.Take(8))
        {
            recent.Add(await MapOrderAsync(order.Id));
        }

        return new StoreAdminDashboardDto
        {
            ProductCount = (int)await _productRepository.GetCountAsync(),
            LowStockCount = inventories.Count(x => x.AvailableQuantity <= x.LowStockThreshold),
            PendingOrderCount = orders.Count(x =>
                x.Status is OrderStatus.Pending or OrderStatus.Confirmed or OrderStatus.Preparing),
            CustomerCount = (int)await _customerRepository.GetCountAsync(),
            RecentOrders = recent
        };
    }

    [Authorize(AbpIoTemplateProjectPermissions.Categories.Default)]
    public async Task<List<CategoryDto>> GetCategoriesAsync()
    {
        var categories = (await _categoryRepository.GetListAsync())
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .ToList();
        var productQuery = await _productRepository.GetQueryableAsync();
        var counts = await AsyncExecuter.ToListAsync(
            productQuery.GroupBy(x => x.CategoryId).Select(x => new { Id = x.Key, Count = x.Count() }));
        var countMap = counts.ToDictionary(x => x.Id, x => x.Count);
        return categories.Select(x => MapCategory(x, countMap.GetValueOrDefault(x.Id))).ToList();
    }

    [Authorize(AbpIoTemplateProjectPermissions.Categories.Default)]
    public async Task<CategoryDto> SaveCategoryAsync(SaveCategoryInput input)
    {
        var slug = NormalizeSlug(input.Slug);
        if (await _categoryRepository.AnyAsync(x => x.Slug == slug && x.Id != input.Id))
        {
            throw new UserFriendlyException(L["Store:DuplicateCategorySlug"]);
        }

        if (input.ParentId.HasValue)
        {
            await EnsureCategoryHierarchyAsync(input.Id, input.ParentId.Value);
        }

        Category category;
        if (input.Id.HasValue)
        {
            category = await _categoryRepository.GetAsync(input.Id.Value);
        }
        else
        {
            category = new Category(GuidGenerator.Create(), input.Name.Trim(), slug, CurrentTenant.Id);
        }

        category.Update(
            input.Name.Trim(),
            slug,
            input.Description,
            input.ImageUrl,
            input.ParentId,
            input.IsFeatured,
            input.IsActive,
            input.DisplayOrder);

        if (input.Id.HasValue)
        {
            await _categoryRepository.UpdateAsync(category, autoSave: true);
        }
        else
        {
            await _categoryRepository.InsertAsync(category, autoSave: true);
        }

        return MapCategory(category, 0);
    }

    [Authorize(AbpIoTemplateProjectPermissions.Brands.Default)]
    public async Task<List<BrandDto>> GetBrandsAsync()
    {
        return (await _brandRepository.GetListAsync())
            .OrderBy(x => x.Name)
            .Select(x => new BrandDto
            {
                Id = x.Id,
                Name = x.Name,
                Slug = x.Slug,
                LogoUrl = x.LogoUrl,
                IsFeatured = x.IsFeatured
            }).ToList();
    }

    [Authorize(AbpIoTemplateProjectPermissions.Brands.Default)]
    public async Task<BrandDto> SaveBrandAsync(SaveBrandInput input)
    {
        var slug = NormalizeSlug(input.Slug);
        if (await _brandRepository.AnyAsync(x => x.Slug == slug && x.Id != input.Id))
        {
            throw new UserFriendlyException(L["Store:DuplicateBrandSlug"]);
        }

        Brand brand;
        if (input.Id.HasValue)
        {
            brand = await _brandRepository.GetAsync(input.Id.Value);
        }
        else
        {
            brand = new Brand(GuidGenerator.Create(), input.Name.Trim(), slug, CurrentTenant.Id);
        }

        brand.Update(input.Name.Trim(), slug, input.Description, input.LogoUrl, input.IsFeatured, input.IsActive);
        if (input.Id.HasValue)
        {
            await _brandRepository.UpdateAsync(brand, autoSave: true);
        }
        else
        {
            await _brandRepository.InsertAsync(brand, autoSave: true);
        }

        return new BrandDto
        {
            Id = brand.Id,
            Name = brand.Name,
            Slug = brand.Slug,
            LogoUrl = brand.LogoUrl,
            IsFeatured = brand.IsFeatured
        };
    }

    [Authorize(AbpIoTemplateProjectPermissions.Suppliers.Default)]
    public async Task<List<SupplierDto>> GetSuppliersAsync()
    {
        return (await _supplierRepository.GetListAsync())
            .OrderBy(x => x.Name)
            .Select(x => new SupplierDto { Id = x.Id, Code = x.Code, Name = x.Name })
            .ToList();
    }

    [Authorize(AbpIoTemplateProjectPermissions.Suppliers.Default)]
    public async Task<SupplierDto> SaveSupplierAsync(SaveSupplierInput input)
    {
        var code = input.Code.Trim().ToUpperInvariant();
        if (await _supplierRepository.AnyAsync(x => x.Code == code && x.Id != input.Id))
        {
            throw new UserFriendlyException(L["Store:DuplicateSupplierCode"]);
        }

        Supplier supplier;
        if (input.Id.HasValue)
        {
            supplier = await _supplierRepository.GetAsync(input.Id.Value);
            if (supplier.Code != code)
            {
                throw new UserFriendlyException(L["Store:SupplierCodeCannotChange"]);
            }
        }
        else
        {
            supplier = new Supplier(GuidGenerator.Create(), code, input.Name.Trim(), CurrentTenant.Id);
        }

        supplier.Update(
            input.Name.Trim(),
            input.ContactName,
            input.Phone,
            input.Email,
            input.Address,
            input.IsActive);
        if (input.Id.HasValue)
        {
            await _supplierRepository.UpdateAsync(supplier, autoSave: true);
        }
        else
        {
            await _supplierRepository.InsertAsync(supplier, autoSave: true);
        }

        return new SupplierDto { Id = supplier.Id, Code = supplier.Code, Name = supplier.Name };
    }

    [Authorize(AbpIoTemplateProjectPermissions.Products.View)]
    public Task<PagedResultDto<ProductListItemDto>> GetProductsAsync(ProductListInput input)
    {
        return _storefrontAppService.GetProductsAsync(input);
    }

    [Authorize(AbpIoTemplateProjectPermissions.Products.Default)]
    public async Task<Guid> SaveProductAsync(SaveProductInput input)
    {
        var sku = input.Sku.Trim().ToUpperInvariant();
        var code = input.Code.Trim().ToUpperInvariant();
        var slug = NormalizeSlug(input.Slug);
        if (await _productRepository.AnyAsync(x =>
                x.Id != input.Id && (x.Sku == sku || x.Code == code || x.Slug == slug)))
        {
            throw new UserFriendlyException(L["Store:DuplicateProductIdentity"]);
        }

        if (!await _categoryRepository.AnyAsync(x => x.Id == input.CategoryId))
        {
            throw new UserFriendlyException(L["Store:CategoryNotFound"]);
        }

        Product product;
        if (input.Id.HasValue)
        {
            product = await _productRepository.GetAsync(input.Id.Value);
            if (product.Sku != sku || product.Code != code || product.Type != input.Type)
            {
                throw new UserFriendlyException(L["Store:ProductIdentityCannotChange"]);
            }
        }
        else
        {
            product = new Product(
                GuidGenerator.Create(),
                code,
                sku,
                input.Name.Trim(),
                slug,
                input.Type,
                input.CategoryId,
                CurrentTenant.Id);
        }

        product.UpdateDetails(
            input.Name.Trim(),
            slug,
            input.CategoryId,
            input.BrandId,
            input.SupplierId,
            input.Unit,
            input.ShortDescription,
            input.Description,
            input.Specifications,
            input.UsageInstructions,
            input.ThumbnailUrl,
            input.Weight,
            input.Warranty);
        product.ChangePrice(input.SalePrice, input.ListPrice, input.CostPrice, input.TaxRate);
        product.ConfigureSales(
            input.IsFeatured,
            input.IsNew,
            input.IsBestSeller,
            input.IsActive,
            input.IsVisible,
            input.AllowBackorder,
            input.MinimumOrderQuantity,
            input.MaximumOrderQuantity);
        product.SetSeo(input.MetaTitle, input.MetaDescription, null, null);

        if (input.Id.HasValue)
        {
            await _productRepository.UpdateAsync(product, autoSave: true);
        }
        else
        {
            await _productRepository.InsertAsync(product, autoSave: true);
        }

        return product.Id;
    }

    [Authorize(AbpIoTemplateProjectPermissions.Products.Update)]
    public async Task<Guid> AddProductVariantAsync(SaveProductVariantInput input)
    {
        var sku = input.Sku.Trim().ToUpperInvariant();
        if (await _productRepository.AnyAsync(x => x.Sku == sku) ||
            await _productVariantRepository.AnyAsync(x => x.Sku == sku))
        {
            throw new UserFriendlyException(L["Store:DuplicateProductIdentity"]);
        }

        var query = await _productRepository.WithDetailsAsync(x => x.Variants);
        var product = await AsyncExecuter.FirstAsync(query.Where(x => x.Id == input.ProductId));
        if (product.Variants.Any(x => x.Sku == sku))
        {
            throw new UserFriendlyException(L["Store:DuplicateProductIdentity"]);
        }

        var variant = product.AddVariant(
            GuidGenerator.Create(),
            input.Name.Trim(),
            sku,
            input.OptionSummary.Trim(),
            input.SalePrice,
            input.ListPrice,
            input.ImageUrl,
            input.Weight);
        await _productRepository.UpdateAsync(product, autoSave: true);
        return variant.Id;
    }

    [Authorize(AbpIoTemplateProjectPermissions.Products.ManageImages)]
    public async Task<Guid> AddProductImageAsync(SaveProductImageInput input)
    {
        var query = await _productRepository.WithDetailsAsync(x => x.Images);
        var product = await AsyncExecuter.FirstAsync(query.Where(x => x.Id == input.ProductId));
        var imageId = GuidGenerator.Create();
        product.AddImage(
            imageId,
            input.Url.Trim(),
            input.AltText.Trim(),
            input.DisplayOrder,
            input.IsPrimary);
        await _productRepository.UpdateAsync(product, autoSave: true);
        return imageId;
    }

    [Authorize(AbpIoTemplateProjectPermissions.Inventory.View)]
    public async Task<List<InventoryItemDto>> GetInventoryAsync()
    {
        var inventory = await _inventoryRepository.GetListAsync();
        var productIds = inventory.Select(x => x.ProductId).Distinct().ToList();
        var productQuery = await _productRepository.WithDetailsAsync(x => x.Variants);
        var products = await AsyncExecuter.ToListAsync(productQuery.Where(x => productIds.Contains(x.Id)));
        var productMap = products.ToDictionary(x => x.Id);
        var warehouses = (await _warehouseRepository.GetListAsync()).ToDictionary(x => x.Id, x => x.Name);

        return inventory.Select(x =>
        {
            productMap.TryGetValue(x.ProductId, out var product);
            var variant = x.ProductVariantId.HasValue
                ? product?.Variants.FirstOrDefault(v => v.Id == x.ProductVariantId.Value)
                : null;
            return new InventoryItemDto
            {
                Id = x.Id,
                ProductId = x.ProductId,
                ProductVariantId = x.ProductVariantId,
                WarehouseName = warehouses.GetValueOrDefault(x.WarehouseId, string.Empty),
                ProductName = product?.Name ?? string.Empty,
                Sku = variant?.Sku ?? product?.Sku ?? string.Empty,
                OnHandQuantity = x.OnHandQuantity,
                ReservedQuantity = x.ReservedQuantity,
                AvailableQuantity = x.AvailableQuantity,
                LowStockThreshold = x.LowStockThreshold
            };
        }).OrderBy(x => x.ProductName).ThenBy(x => x.WarehouseName).ToList();
    }

    [Authorize(AbpIoTemplateProjectPermissions.Inventory.Adjust)]
    public async Task AdjustInventoryAsync(AdjustInventoryInput input)
    {
        if (input.QuantityDelta == 0)
        {
            throw new UserFriendlyException(L["Store:InventoryAdjustmentCannotBeZero"]);
        }

        var inventory = await _inventoryRepository.GetAsync(input.InventoryItemId);
        var (before, after) = inventory.Adjust(input.QuantityDelta);
        await _inventoryRepository.UpdateAsync(inventory);
        await _inventoryTransactionRepository.InsertAsync(new InventoryTransaction(
            GuidGenerator.Create(),
            inventory.Id,
            InventoryTransactionType.Adjust,
            before,
            input.QuantityDelta,
            after,
            "ManualAdjustment",
            null,
            input.Note,
            CurrentTenant.Id), autoSave: true);
    }

    [Authorize(AbpIoTemplateProjectPermissions.Customers.View)]
    public async Task<List<AdminCustomerDto>> GetCustomersAsync()
    {
        var customers = await _customerRepository.GetListAsync();
        var orderQuery = await _orderRepository.GetQueryableAsync();
        var aggregates = await AsyncExecuter.ToListAsync(orderQuery
            .GroupBy(x => x.CustomerId)
            .Select(x => new
            {
                CustomerId = x.Key,
                Count = x.Count(),
                Total = x.Where(o => o.Status != OrderStatus.Cancelled).Sum(o => o.GrandTotal)
            }));
        var aggregateMap = aggregates.ToDictionary(x => x.CustomerId);
        return customers.OrderByDescending(x => x.CreationTime).Select(x =>
        {
            aggregateMap.TryGetValue(x.Id, out var aggregate);
            return new AdminCustomerDto
            {
                Id = x.Id,
                FullName = x.FullName,
                Phone = x.Phone,
                Email = x.Email,
                OrderCount = aggregate?.Count ?? 0,
                TotalSpent = aggregate?.Total ?? 0
            };
        }).ToList();
    }

    [Authorize(AbpIoTemplateProjectPermissions.Payments.View)]
    public async Task<List<AdminPaymentDto>> GetPaymentsAsync()
    {
        var payments = (await _paymentRepository.GetListAsync())
            .OrderByDescending(x => x.CreationTime)
            .ToList();
        var orderIds = payments.Select(x => x.OrderId).Distinct().ToList();
        var orders = (await _orderRepository.GetListAsync(x => orderIds.Contains(x.Id)))
            .ToDictionary(x => x.Id);
        return payments.Select(x => new AdminPaymentDto
        {
            Id = x.Id,
            OrderId = x.OrderId,
            OrderNumber = orders.GetValueOrDefault(x.OrderId)?.OrderNumber ?? string.Empty,
            Method = x.Method,
            Status = x.Status,
            Amount = x.Amount,
            ReferenceNumber = x.ReferenceNumber,
            CreationTime = x.CreationTime
        }).ToList();
    }

    [Authorize(AbpIoTemplateProjectPermissions.Payments.Confirm)]
    public async Task ConfirmPaymentAsync(ConfirmPaymentInput input)
    {
        var payment = await _paymentRepository.GetAsync(input.PaymentId);
        payment.Confirm(input.ReferenceNumber);
        var order = await _orderRepository.GetAsync(payment.OrderId);
        order.MarkPayment(PaymentStatus.Paid);
        await _paymentRepository.UpdateAsync(payment);
        await _orderRepository.UpdateAsync(order, autoSave: true);
    }

    [Authorize(AbpIoTemplateProjectPermissions.Orders.View)]
    public async Task<PagedResultDto<OrderDto>> GetOrdersAsync(PagedAndSortedResultRequestDto input)
    {
        var query = await _orderRepository.GetQueryableAsync();
        var totalCount = await AsyncExecuter.CountAsync(query);
        var max = Math.Clamp(input.MaxResultCount, 1, StoreConsts.MaxPageSize);
        var orders = await AsyncExecuter.ToListAsync(
            query.OrderByDescending(x => x.CreationTime).Skip(input.SkipCount).Take(max));
        var result = new List<OrderDto>();
        foreach (var order in orders)
        {
            result.Add(await MapOrderAsync(order.Id));
        }

        return new PagedResultDto<OrderDto>(totalCount, result);
    }

    [Authorize(AbpIoTemplateProjectPermissions.Orders.Default)]
    public async Task ChangeOrderStatusAsync(ChangeOrderStatusInput input)
    {
        var orderQuery = await _orderRepository.WithDetailsAsync(x => x.Items);
        var order = await AsyncExecuter.FirstAsync(orderQuery.Where(x => x.Id == input.OrderId));
        switch (input.TargetStatus)
        {
            case OrderStatus.Confirmed:
                order.Confirm(GuidGenerator.Create(), input.Note);
                break;
            case OrderStatus.Preparing:
                order.StartPreparing(GuidGenerator.Create(), input.Note);
                break;
            case OrderStatus.ReadyToShip:
                order.MarkReadyToShip(GuidGenerator.Create(), input.Note);
                break;
            case OrderStatus.Shipping:
                order.MarkAsShipped(
                    GuidGenerator.Create(),
                    Check.NotNullOrWhiteSpace(input.TrackingCode, nameof(input.TrackingCode)),
                    input.Note);
                break;
            case OrderStatus.Completed:
                order.Complete(GuidGenerator.Create(), input.Note);
                await SettleInventoryAsync(order, completeSale: true);
                if (order.PaymentMethod == PaymentMethod.CashOnDelivery)
                {
                    order.MarkPayment(PaymentStatus.Paid);
                }
                break;
            case OrderStatus.Cancelled:
                order.Cancel(
                    GuidGenerator.Create(),
                    input.Note.IsNullOrWhiteSpace() ? "Huỷ bởi quản trị viên" : input.Note!);
                await SettleInventoryAsync(order, completeSale: false);
                break;
            default:
                throw new UserFriendlyException(L["Store:UnsupportedOrderStatus"]);
        }

        await _orderRepository.UpdateAsync(order, autoSave: true);
    }

    [Authorize(AbpIoTemplateProjectPermissions.Promotions.Default)]
    public async Task<List<PromotionDto>> GetPromotionsAsync()
    {
        return (await _promotionRepository.GetListAsync())
            .OrderByDescending(x => x.CreationTime)
            .Select(x => new PromotionDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                Type = x.Type,
                Value = x.Value,
                MinimumOrderAmount = x.MinimumOrderAmount,
                StartTime = x.StartTime,
                EndTime = x.EndTime,
                UsageCount = x.UsageCount,
                IsActive = x.IsActive
            }).ToList();
    }

    [Authorize(AbpIoTemplateProjectPermissions.Promotions.Default)]
    public async Task<Guid> SavePromotionAsync(SavePromotionInput input)
    {
        var code = input.Code.Trim().ToUpperInvariant();
        if (await _promotionRepository.AnyAsync(x => x.Code == code && x.Id != input.Id))
        {
            throw new UserFriendlyException(L["Store:DuplicatePromotionCode"]);
        }

        Promotion promotion;
        if (input.Id.HasValue)
        {
            promotion = await _promotionRepository.GetAsync(input.Id.Value);
            if (promotion.Code != code)
            {
                throw new UserFriendlyException(L["Store:PromotionCodeCannotChange"]);
            }

            promotion.Update(
                input.Name.Trim(),
                input.Type,
                input.Value,
                input.MinimumOrderAmount,
                input.MaximumDiscountAmount,
                input.StartTime,
                input.EndTime);
        }
        else
        {
            promotion = new Promotion(
                GuidGenerator.Create(),
                code,
                input.Name.Trim(),
                input.Type,
                input.Value,
                input.MinimumOrderAmount,
                input.MaximumDiscountAmount,
                input.StartTime,
                input.EndTime,
                CurrentTenant.Id);
        }

        promotion.ConfigureLimits(input.UsageLimit, null, input.IsAutomatic, false, input.IsActive);
        if (input.Id.HasValue)
        {
            await _promotionRepository.UpdateAsync(promotion, autoSave: true);
        }
        else
        {
            await _promotionRepository.InsertAsync(promotion, autoSave: true);
        }

        return promotion.Id;
    }

    [Authorize(AbpIoTemplateProjectPermissions.Banners.Default)]
    public async Task<List<BannerDto>> GetBannersAsync()
    {
        return (await _bannerRepository.GetListAsync())
            .OrderBy(x => x.DisplayOrder)
            .Select(x => new BannerDto
            {
                Id = x.Id,
                Title = x.Title,
                Description = x.Description,
                DesktopImageUrl = x.DesktopImageUrl,
                MobileImageUrl = x.MobileImageUrl,
                ButtonText = x.ButtonText,
                TargetUrl = x.TargetUrl
            }).ToList();
    }

    [Authorize(AbpIoTemplateProjectPermissions.Banners.Default)]
    public async Task<Guid> SaveBannerAsync(SaveBannerInput input)
    {
        Banner banner;
        if (input.Id.HasValue)
        {
            banner = await _bannerRepository.GetAsync(input.Id.Value);
            banner.UpdateMedia(
                input.Title.Trim(),
                input.DesktopImageUrl.Trim(),
                input.MobileImageUrl.Trim(),
                input.DisplayOrder);
        }
        else
        {
            banner = new Banner(
                GuidGenerator.Create(),
                input.Title.Trim(),
                input.DesktopImageUrl.Trim(),
                input.MobileImageUrl.Trim(),
                input.DisplayOrder,
                CurrentTenant.Id);
        }

        banner.UpdateContent(
            input.Description,
            input.ButtonText,
            input.TargetUrl,
            input.StartTime,
            input.EndTime,
            input.IsActive);
        if (input.Id.HasValue)
        {
            await _bannerRepository.UpdateAsync(banner, autoSave: true);
        }
        else
        {
            await _bannerRepository.InsertAsync(banner, autoSave: true);
        }

        return banner.Id;
    }

    [Authorize(AbpIoTemplateProjectPermissions.Articles.Default)]
    public async Task<List<ArticleCategoryDto>> GetArticleCategoriesAsync()
    {
        return (await _articleCategoryRepository.GetListAsync(x => x.IsActive))
            .OrderBy(x => x.Name)
            .Select(x => new ArticleCategoryDto { Id = x.Id, Name = x.Name, Slug = x.Slug })
            .ToList();
    }

    [Authorize(AbpIoTemplateProjectPermissions.Articles.Default)]
    public async Task<PagedResultDto<ArticleSummaryDto>> GetArticlesAsync(PagedAndSortedResultRequestDto input)
    {
        var query = await _articleRepository.GetQueryableAsync();
        var total = await AsyncExecuter.CountAsync(query);
        var articles = await AsyncExecuter.ToListAsync(query
            .OrderByDescending(x => x.CreationTime)
            .Skip(input.SkipCount)
            .Take(Math.Clamp(input.MaxResultCount, 1, StoreConsts.MaxPageSize)));
        var categories = (await _articleCategoryRepository.GetListAsync())
            .ToDictionary(x => x.Id, x => x.Name);
        return new PagedResultDto<ArticleSummaryDto>(
            total,
            articles.Select(x => new ArticleSummaryDto
            {
                Id = x.Id,
                Title = x.Title,
                Slug = x.Slug,
                Summary = x.Summary,
                CategoryName = categories.GetValueOrDefault(x.ArticleCategoryId, string.Empty),
                FeaturedImageUrl = x.FeaturedImageUrl,
                PublishTime = x.PublishTime,
                IsFeatured = x.IsFeatured
            }).ToList());
    }

    [Authorize(AbpIoTemplateProjectPermissions.Articles.Default)]
    public async Task<Guid> SaveArticleAsync(SaveArticleInput input)
    {
        var slug = NormalizeSlug(input.Slug);
        if (!await _articleCategoryRepository.AnyAsync(x => x.Id == input.ArticleCategoryId))
        {
            throw new UserFriendlyException(L["Store:ArticleCategoryNotFound"]);
        }

        if (await _articleRepository.AnyAsync(x => x.Slug == slug && x.Id != input.Id))
        {
            throw new UserFriendlyException(L["Store:DuplicateArticleSlug"]);
        }

        Article article;
        if (input.Id.HasValue)
        {
            article = await _articleRepository.GetAsync(input.Id.Value);
            if (article.ArticleCategoryId != input.ArticleCategoryId)
            {
                throw new UserFriendlyException(L["Store:ArticleCategoryCannotChange"]);
            }
        }
        else
        {
            article = new Article(
                GuidGenerator.Create(),
                input.ArticleCategoryId,
                input.Title.Trim(),
                slug,
                input.Summary.Trim(),
                input.Content,
                input.AuthorName.Trim(),
                CurrentTenant.Id);
        }

        article.Update(
            input.Title.Trim(),
            slug,
            input.Summary.Trim(),
            input.Content,
            input.AuthorName.Trim(),
            input.FeaturedImageUrl,
            input.IsFeatured);
        if (input.Publish)
        {
            article.Publish(Clock.Now);
        }

        if (input.Id.HasValue)
        {
            await _articleRepository.UpdateAsync(article, autoSave: true);
        }
        else
        {
            await _articleRepository.InsertAsync(article, autoSave: true);
        }

        return article.Id;
    }

    private async Task EnsureCategoryHierarchyAsync(Guid? categoryId, Guid parentId)
    {
        var parent = await _categoryRepository.FindAsync(parentId)
                     ?? throw new UserFriendlyException(L["Store:CategoryNotFound"]);
        var visited = new HashSet<Guid>();
        while (parent is not null)
        {
            if (!visited.Add(parent.Id) || parent.Id == categoryId)
            {
                throw new UserFriendlyException(L["Store:CategoryHierarchyCycle"]);
            }

            parent = parent.ParentId.HasValue
                ? await _categoryRepository.FindAsync(parent.ParentId.Value)
                : null;
        }
    }

    private async Task SettleInventoryAsync(Order order, bool completeSale)
    {
        foreach (var orderItem in order.Items)
        {
            var remaining = orderItem.Quantity;
            var inventories = (await _inventoryRepository.GetListAsync(x =>
                    x.ProductId == orderItem.ProductId &&
                    x.ProductVariantId == orderItem.ProductVariantId &&
                    x.ReservedQuantity > 0))
                .OrderByDescending(x => x.ReservedQuantity)
                .ToList();
            foreach (var inventory in inventories)
            {
                if (remaining == 0)
                {
                    break;
                }

                var quantity = Math.Min(remaining, inventory.ReservedQuantity);
                var before = completeSale ? inventory.OnHandQuantity : inventory.AvailableQuantity;
                if (completeSale)
                {
                    inventory.CompleteSale(quantity);
                }
                else
                {
                    inventory.Release(quantity);
                }

                var after = completeSale ? inventory.OnHandQuantity : inventory.AvailableQuantity;
                await _inventoryRepository.UpdateAsync(inventory);
                await _inventoryTransactionRepository.InsertAsync(new InventoryTransaction(
                    GuidGenerator.Create(),
                    inventory.Id,
                    completeSale ? InventoryTransactionType.Sale : InventoryTransactionType.Release,
                    before,
                    completeSale ? -quantity : quantity,
                    after,
                    "Order",
                    order.OrderNumber,
                    completeSale ? "Xuất kho khi hoàn tất đơn" : "Hoàn tồn khi huỷ đơn",
                    CurrentTenant.Id));
                remaining -= quantity;
            }
        }
    }

    private async Task<OrderDto> MapOrderAsync(Guid id)
    {
        var query = await _orderRepository.WithDetailsAsync(x => x.Items, x => x.StatusHistory);
        var order = await AsyncExecuter.FirstAsync(query.Where(x => x.Id == id));
        return new OrderDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            Status = order.Status,
            PaymentStatus = order.PaymentStatus,
            PaymentMethod = order.PaymentMethod,
            CustomerName = order.CustomerName,
            Phone = order.Phone,
            Email = order.Email,
            FullAddress = $"{order.AddressLine}, {order.Ward}, {order.District}, {order.Province}",
            ShippingMethodName = order.ShippingMethodName,
            TrackingCode = order.TrackingCode,
            CancellationReason = order.CancellationReason,
            Subtotal = order.Subtotal,
            DiscountAmount = order.DiscountAmount,
            ShippingFee = order.ShippingFee,
            TaxAmount = order.TaxAmount,
            GrandTotal = order.GrandTotal,
            PromotionCode = order.PromotionCode,
            CreationTime = order.CreationTime,
            Items = order.Items.Select(x => new OrderItemDto
            {
                Id = x.Id,
                ProductName = x.ProductName,
                Sku = x.Sku,
                OptionSummary = x.OptionSummary,
                ImageUrl = x.ImageUrl,
                Quantity = x.Quantity,
                UnitPrice = x.UnitPrice,
                LineTotal = x.UnitPrice * x.Quantity
            }).ToList(),
            History = order.StatusHistory.OrderBy(x => x.CreationTime).Select(x => new OrderHistoryDto
            {
                FromStatus = x.FromStatus,
                ToStatus = x.ToStatus,
                Note = x.Note,
                CreationTime = x.CreationTime
            }).ToList()
        };
    }

    private static CategoryDto MapCategory(Category category, int productCount)
    {
        return new CategoryDto
        {
            Id = category.Id,
            ParentId = category.ParentId,
            Name = category.Name,
            Slug = category.Slug,
            Description = category.Description,
            ImageUrl = category.ImageUrl,
            IsFeatured = category.IsFeatured,
            DisplayOrder = category.DisplayOrder,
            ProductCount = productCount
        };
    }

    private static string NormalizeSlug(string value)
    {
        return Check.NotNullOrWhiteSpace(value, nameof(value), StoreConsts.MaxSlugLength)
            .Trim()
            .ToLowerInvariant();
    }
}
