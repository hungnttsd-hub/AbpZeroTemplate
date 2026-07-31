using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Timing;

namespace AbpIoTemplateProject.Store;

public class StoreDataSeedContributor : IDataSeedContributor, ITransientDependency
{
    private readonly IGuidGenerator _guidGenerator;
    private readonly IClock _clock;
    private readonly IRepository<Category, Guid> _categoryRepository;
    private readonly IRepository<Brand, Guid> _brandRepository;
    private readonly IRepository<Supplier, Guid> _supplierRepository;
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IRepository<Warehouse, Guid> _warehouseRepository;
    private readonly IRepository<InventoryItem, Guid> _inventoryRepository;
    private readonly IRepository<ShippingMethod, Guid> _shippingRepository;
    private readonly IRepository<Promotion, Guid> _promotionRepository;
    private readonly IRepository<Banner, Guid> _bannerRepository;
    private readonly IRepository<ArticleCategory, Guid> _articleCategoryRepository;
    private readonly IRepository<Article, Guid> _articleRepository;
    private readonly IRepository<StoreLocation, Guid> _storeRepository;
    private readonly IRepository<Customer, Guid> _customerRepository;
    private readonly IRepository<Order, Guid> _orderRepository;
    private readonly IRepository<Payment, Guid> _paymentRepository;
    private readonly IRepository<HomePageSection, Guid> _homeSectionRepository;
    private readonly IRepository<SiteSetting, Guid> _siteSettingRepository;

    public StoreDataSeedContributor(
        IGuidGenerator guidGenerator,
        IClock clock,
        IRepository<Category, Guid> categoryRepository,
        IRepository<Brand, Guid> brandRepository,
        IRepository<Supplier, Guid> supplierRepository,
        IRepository<Product, Guid> productRepository,
        IRepository<Warehouse, Guid> warehouseRepository,
        IRepository<InventoryItem, Guid> inventoryRepository,
        IRepository<ShippingMethod, Guid> shippingRepository,
        IRepository<Promotion, Guid> promotionRepository,
        IRepository<Banner, Guid> bannerRepository,
        IRepository<ArticleCategory, Guid> articleCategoryRepository,
        IRepository<Article, Guid> articleRepository,
        IRepository<StoreLocation, Guid> storeRepository,
        IRepository<Customer, Guid> customerRepository,
        IRepository<Order, Guid> orderRepository,
        IRepository<Payment, Guid> paymentRepository,
        IRepository<HomePageSection, Guid> homeSectionRepository,
        IRepository<SiteSetting, Guid> siteSettingRepository)
    {
        _guidGenerator = guidGenerator;
        _clock = clock;
        _categoryRepository = categoryRepository;
        _brandRepository = brandRepository;
        _supplierRepository = supplierRepository;
        _productRepository = productRepository;
        _warehouseRepository = warehouseRepository;
        _inventoryRepository = inventoryRepository;
        _shippingRepository = shippingRepository;
        _promotionRepository = promotionRepository;
        _bannerRepository = bannerRepository;
        _articleCategoryRepository = articleCategoryRepository;
        _articleRepository = articleRepository;
        _storeRepository = storeRepository;
        _customerRepository = customerRepository;
        _orderRepository = orderRepository;
        _paymentRepository = paymentRepository;
        _homeSectionRepository = homeSectionRepository;
        _siteSettingRepository = siteSettingRepository;
    }

    public async Task SeedAsync(DataSeedContext context)
    {
        if (await _productRepository.GetCountAsync() > 0)
        {
            return;
        }

        var tenantId = context.TenantId;
        var categories = await SeedCategoriesAsync(tenantId);
        var brands = await SeedBrandsAsync(tenantId);
        var suppliers = await SeedSuppliersAsync(tenantId);
        var warehouses = await SeedWarehousesAsync(tenantId);
        var products = await SeedProductsAsync(tenantId, categories, brands, suppliers);
        await SeedInventoryAsync(tenantId, products, warehouses);
        var shippingMethods = await SeedShippingAsync(tenantId);
        await SeedPromotionsAsync(tenantId);
        await SeedContentAsync(tenantId);
        var customers = await SeedCustomersAsync(tenantId);
        await SeedOrdersAsync(tenantId, customers, products, shippingMethods[0]);
        await SeedHomeConfigurationAsync(tenantId);
    }

    private async Task<List<Category>> SeedCategoriesAsync(Guid? tenantId)
    {
        var definitions = new[]
        {
            ("Cây thủy sinh", "aquatic-plants", true),
            ("Lọc & vật liệu lọc", "filtration", true),
            ("Đèn thủy sinh", "lighting", true),
            ("Nền & phân bón", "substrate", true),
            ("Thức ăn cho cá", "fish-food", true),
            ("Chăm sóc nước", "water-care", true),
            ("CO₂ & oxy", "oxygen-heating", false),
            ("Dụng cụ chăm hồ", "tools", false),
            ("Trang trí", "decorations", false),
            ("Bể & tủ", "aquariums", false)
        };
        var result = new List<Category>();
        for (var i = 0; i < definitions.Length; i++)
        {
            var item = definitions[i];
            var category = new Category(_guidGenerator.Create(), item.Item1, item.Item2, tenantId);
            category.Update(
                item.Item1,
                item.Item2,
                $"Tuyển chọn {item.Item1.ToLowerInvariant()} chất lượng cho hồ thủy sinh.",
                "/images/store/article-placeholder.svg",
                null,
                item.Item3,
                true,
                i);
            result.Add(await _categoryRepository.InsertAsync(category));
        }

        return result;
    }

    private async Task<List<Brand>> SeedBrandsAsync(Guid? tenantId)
    {
        var definitions = new[]
        {
            ("Aqua Garden", "aqua-garden"),
            ("ADA", "ada"),
            ("Chihiros", "chihiros"),
            ("Seachem", "seachem"),
            ("Oase", "oase")
        };
        var result = new List<Brand>();
        for (var i = 0; i < definitions.Length; i++)
        {
            var brand = new Brand(_guidGenerator.Create(), definitions[i].Item1, definitions[i].Item2, tenantId);
            brand.Update(brand.Name, brand.Slug, $"Thương hiệu {brand.Name}", null, i < 4, true);
            result.Add(await _brandRepository.InsertAsync(brand));
        }

        return result;
    }

    private async Task<List<Supplier>> SeedSuppliersAsync(Guid? tenantId)
    {
        var result = new List<Supplier>();
        for (var i = 1; i <= 5; i++)
        {
            var supplier = new Supplier(_guidGenerator.Create(), $"NCC{i:00}", $"Nhà cung cấp {i}", tenantId);
            supplier.Update($"Nhà cung cấp {i}", $"Đầu mối {i}", $"09000000{i:00}", $"ncc{i}@aquagarden.vn", $"Kho đối tác số {i}", true);
            result.Add(await _supplierRepository.InsertAsync(supplier));
        }

        return result;
    }

    private async Task<List<Warehouse>> SeedWarehousesAsync(Guid? tenantId)
    {
        var definitions = new[]
        {
            ("KHO-HCM", "Kho trung tâm TP.HCM", "12 Nguyễn Văn Linh, Quận 7, TP.HCM"),
            ("KHO-HN", "Kho Hà Nội", "88 Nguyễn Trãi, Thanh Xuân, Hà Nội"),
            ("KHO-DN", "Kho Đà Nẵng", "35 Nguyễn Hữu Thọ, Hải Châu, Đà Nẵng")
        };
        var result = new List<Warehouse>();
        foreach (var item in definitions)
        {
            result.Add(await _warehouseRepository.InsertAsync(new Warehouse(
                _guidGenerator.Create(), item.Item1, item.Item2, item.Item3, tenantId)));
        }

        return result;
    }

    private async Task<List<Product>> SeedProductsAsync(
        Guid? tenantId,
        IReadOnlyList<Category> categories,
        IReadOnlyList<Brand> brands,
        IReadOnlyList<Supplier> suppliers)
    {
        var prefixes = new[]
        {
            "Ráy Nana", "Lọc thùng", "Đèn WRGB", "Nền dinh dưỡng", "Thức ăn premium",
            "Vi sinh nước", "Bộ CO₂", "Kéo tỉa cây", "Đá nham thạch", "Bể kính siêu trong"
        };
        var result = new List<Product>();
        for (var i = 1; i <= 50; i++)
        {
            var categoryIndex = (i - 1) % categories.Count;
            var category = categories[categoryIndex];
            var isVariant = i <= 10;
            var name = $"{prefixes[categoryIndex]} {i:00}";
            var product = new Product(
                _guidGenerator.Create(),
                $"SP{i:000}",
                $"AQ-{i:000}",
                name,
                $"san-pham-thuy-sinh-{i:00}",
                isVariant ? ProductType.Variant : ProductType.Simple,
                category.Id,
                tenantId);
            product.UpdateDetails(
                name,
                product.Slug,
                category.Id,
                brands[(i - 1) % brands.Count].Id,
                suppliers[(i - 1) % suppliers.Count].Id,
                "Sản phẩm",
                $"{name} được Aqua Garden tuyển chọn cho người chơi thủy sinh.",
                $"<p><strong>{name}</strong> có nguồn gốc rõ ràng, dễ sử dụng và phù hợp với nhiều kích thước hồ.</p><p>Đội ngũ Aqua Garden kiểm tra sản phẩm trước khi đóng gói.</p>",
                "<ul><li>Chất lượng tuyển chọn</li><li>Phù hợp hồ thủy sinh</li><li>Hỗ trợ tư vấn sử dụng</li></ul>",
                "<p>Đọc kỹ hướng dẫn trên bao bì và liên hệ Aqua Garden nếu cần hỗ trợ.</p>",
                "/images/store/product-placeholder.svg",
                decimal.Round(0.1m + i * 0.02m, 2),
                i % 3 == 0 ? "Bảo hành 12 tháng" : null);
            var salePrice = 65000m + i * 27000m;
            var listPrice = i % 3 == 0 ? salePrice * 1.15m : salePrice;
            product.ChangePrice(salePrice, listPrice, salePrice * .62m, i % 4 == 0 ? 8 : 0);
            product.ConfigureSales(i <= 12, i > 38, i % 5 == 0, true, true, false, 1, 20);
            product.SetSeo(name, $"Mua {name} chính hãng tại Aqua Garden.", "thủy sinh,aqua garden", $"/products/{product.Slug}");
            product.AddImage(_guidGenerator.Create(), "/images/store/product-placeholder.svg", name, 0, true);
            if (isVariant)
            {
                product.AddVariant(_guidGenerator.Create(), $"{name} - Nhỏ", $"AQ-{i:000}-S", "Kích thước: Nhỏ", salePrice, listPrice, null, null);
                product.AddVariant(_guidGenerator.Create(), $"{name} - Lớn", $"AQ-{i:000}-L", "Kích thước: Lớn", salePrice * 1.35m, listPrice * 1.35m, null, null);
            }

            result.Add(await _productRepository.InsertAsync(product));
        }

        return result;
    }

    private async Task SeedInventoryAsync(Guid? tenantId, IReadOnlyList<Product> products, IReadOnlyList<Warehouse> warehouses)
    {
        for (var i = 0; i < products.Count; i++)
        {
            var product = products[i];
            var warehouse = warehouses[i % warehouses.Count];
            if (product.Type == ProductType.Variant)
            {
                foreach (var variant in product.Variants)
                {
                    await _inventoryRepository.InsertAsync(new InventoryItem(
                        _guidGenerator.Create(), warehouse.Id, product.Id, variant.Id, 18 + i, 5, tenantId));
                }
            }
            else
            {
                await _inventoryRepository.InsertAsync(new InventoryItem(
                    _guidGenerator.Create(), warehouse.Id, product.Id, null, 20 + i, 5, tenantId));
            }
        }
    }

    private async Task<List<ShippingMethod>> SeedShippingAsync(Guid? tenantId)
    {
        var result = new List<ShippingMethod>
        {
            new(_guidGenerator.Create(), "STANDARD", "Giao hàng tiêu chuẩn", 30000, 3, tenantId),
            new(_guidGenerator.Create(), "EXPRESS", "Giao nhanh", 55000, 1, tenantId),
            new(_guidGenerator.Create(), "PICKUP", "Nhận tại cửa hàng", 0, 0, tenantId)
        };
        foreach (var method in result)
        {
            await _shippingRepository.InsertAsync(method);
        }
        return result;
    }

    private async Task SeedPromotionsAsync(Guid? tenantId)
    {
        for (var i = 1; i <= 5; i++)
        {
            var promotion = new Promotion(
                _guidGenerator.Create(),
                $"AQUA{i * 5}",
                $"Ưu đãi Aqua {i}",
                i % 2 == 0 ? PromotionType.FixedAmount : PromotionType.Percentage,
                i % 2 == 0 ? i * 25000m : i * 5m,
                250000m,
                200000m,
                _clock.Now.AddDays(-7),
                _clock.Now.AddMonths(3),
                tenantId);
            promotion.ConfigureLimits(500, 2, i == 1, false, true);
            await _promotionRepository.InsertAsync(promotion);
        }
    }

    private async Task SeedContentAsync(Guid? tenantId)
    {
        for (var i = 1; i <= 5; i++)
        {
            var banner = new Banner(
                _guidGenerator.Create(),
                $"Kiến tạo hồ xanh – Ưu đãi số {i}",
                "/images/store/article-placeholder.svg",
                "/images/store/article-placeholder.svg",
                i,
                tenantId);
            banner.UpdateContent(
                "Sản phẩm thủy sinh chính hãng, tư vấn tận tâm.",
                "Khám phá ngay",
                "/products",
                _clock.Now.AddDays(-10),
                _clock.Now.AddMonths(6),
                true);
            await _bannerRepository.InsertAsync(banner);
        }

        var articleCategory = await _articleCategoryRepository.InsertAsync(new ArticleCategory(
            _guidGenerator.Create(), "Cẩm nang thủy sinh", "cam-nang-thuy-sinh", tenantId));
        for (var i = 1; i <= 10; i++)
        {
            var article = new Article(
                _guidGenerator.Create(),
                articleCategory.Id,
                $"Bí quyết chăm hồ thủy sinh khỏe đẹp #{i}",
                $"bi-quyet-cham-ho-thuy-sinh-{i}",
                "Hướng dẫn ngắn gọn, thực tế dành cho người mới và người chơi lâu năm.",
                $"<h2>Nền tảng của một hồ khỏe</h2><p>Bài viết số {i} tổng hợp các bước kiểm tra nước, ánh sáng và dinh dưỡng.</p><h2>Lịch chăm sóc</h2><ul><li>Thay nước định kỳ</li><li>Kiểm tra thiết bị</li><li>Cắt tỉa cây</li></ul>",
                "Đội ngũ Aqua Garden",
                tenantId);
            article.Update(
                article.Title,
                article.Slug,
                article.Summary,
                article.Content,
                article.AuthorName,
                "/images/store/article-placeholder.svg",
                i <= 4);
            article.SetSeo(article.Title, article.Summary);
            article.Publish(_clock.Now.AddDays(-i));
            await _articleRepository.InsertAsync(article);
        }

        var cities = new[] { "TP.HCM", "Hà Nội", "Đà Nẵng", "Cần Thơ", "Hải Phòng" };
        for (var i = 0; i < cities.Length; i++)
        {
            var store = new StoreLocation(
                _guidGenerator.Create(),
                $"Aqua Garden {cities[i]}",
                $"{12 + i * 7} Đường Trung Tâm, {cities[i]}",
                $"0909 000 0{i + 1}0",
                "08:00 – 21:00, Thứ 2 – Chủ nhật",
                i,
                tenantId);
            store.SetMedia("https://maps.google.com", "/images/store/store-placeholder.svg");
            await _storeRepository.InsertAsync(store);
        }
    }

    private async Task<List<Customer>> SeedCustomersAsync(Guid? tenantId)
    {
        var result = new List<Customer>();
        for (var i = 1; i <= 10; i++)
        {
            var customer = new Customer(
                _guidGenerator.Create(),
                $"Khách hàng {i:00}",
                $"09010000{i:00}",
                $"khach{i}@example.com",
                null,
                tenantId);
            customer.AddAddress(
                _guidGenerator.Create(),
                customer.FullName,
                customer.Phone,
                "TP.HCM",
                "Quận 7",
                "Phường Tân Phong",
                $"{20 + i} Nguyễn Văn Linh",
                true);
            result.Add(await _customerRepository.InsertAsync(customer));
        }
        return result;
    }

    private async Task SeedOrdersAsync(
        Guid? tenantId,
        IReadOnlyList<Customer> customers,
        IReadOnlyList<Product> products,
        ShippingMethod shippingMethod)
    {
        var simpleProducts = products.Where(x => x.Type == ProductType.Simple).ToList();
        for (var i = 1; i <= 20; i++)
        {
            var customer = customers[(i - 1) % customers.Count];
            var product = simpleProducts[(i - 1) % simpleProducts.Count];
            var order = new Order(
                _guidGenerator.Create(),
                $"AQ-SEED-{i:000}",
                $"seed-order-{i:000}",
                customer.Id,
                null,
                customer.FullName,
                customer.Phone,
                customer.Email,
                "TP.HCM",
                "Quận 7",
                "Phường Tân Phong",
                $"{20 + i} Nguyễn Văn Linh",
                null,
                shippingMethod.Id,
                shippingMethod.Name,
                PaymentMethod.CashOnDelivery,
                tenantId);
            order.AddItem(
                _guidGenerator.Create(),
                product.Id,
                null,
                product.Name,
                product.Sku,
                null,
                product.ThumbnailUrl,
                1 + i % 2,
                product.SalePrice ?? product.ListPrice ?? 0,
                product.TaxRate);
            order.CalculateTotals(0, shippingMethod.Fee, null);
            if (i % 4 != 0)
            {
                order.Confirm(_guidGenerator.Create(), "Đơn dữ liệu mẫu");
            }
            if (i % 4 == 2)
            {
                order.StartPreparing(_guidGenerator.Create());
            }
            if (i % 4 == 3)
            {
                order.StartPreparing(_guidGenerator.Create());
                order.MarkReadyToShip(_guidGenerator.Create());
            }

            await _orderRepository.InsertAsync(order);
            await _paymentRepository.InsertAsync(new Payment(
                _guidGenerator.Create(), order.Id, order.PaymentMethod, order.GrandTotal, tenantId));
        }
    }

    private async Task SeedHomeConfigurationAsync(Guid? tenantId)
    {
        var sections = new[]
        {
            ("Sản phẩm nổi bật", HomeSectionType.FeaturedProducts, 8),
            ("Bán chạy", HomeSectionType.BestSellers, 8),
            ("Sản phẩm mới", HomeSectionType.NewProducts, 8),
            ("Ưu đãi", HomeSectionType.Promotions, 8),
            ("Kiến thức", HomeSectionType.Articles, 4),
            ("Cửa hàng", HomeSectionType.Stores, 4)
        };
        for (var i = 0; i < sections.Length; i++)
        {
            await _homeSectionRepository.InsertAsync(new HomePageSection(
                _guidGenerator.Create(), sections[i].Item1, sections[i].Item2, sections[i].Item3, i, tenantId));
        }

        await _siteSettingRepository.InsertAsync(new SiteSetting(
            _guidGenerator.Create(), "Store.Seo.DefaultTitle", "Aqua Garden – Thủy sinh chính hãng", true, tenantId));
        await _siteSettingRepository.InsertAsync(new SiteSetting(
            _guidGenerator.Create(), "Store.Contact.Hotline", "0909 123 456", true, tenantId));
    }
}
