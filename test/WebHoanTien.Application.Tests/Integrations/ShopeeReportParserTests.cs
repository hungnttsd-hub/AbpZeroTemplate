using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shouldly;
using WebHoanTien.Affiliates;
using WebHoanTien.Integrations.Shopee;
using Xunit;

namespace WebHoanTien.Tests.Integrations;

public class ShopeeReportParserTests
{
    [Fact]
    public async Task ParseAsync_Should_Aggregate_Order_Items_And_Use_SubId_For_Attribution()
    {
        const string trackingToken = "Tm9uU2Vuc2l0aXZlVHJhY2tpbmdUb2tlbjEy";
        var csv = $"order_id;sub_id;purchase_time;net_commission;order_status;item_id;model_id;product_name;quantity;purchase_amount\n" +
                  $"ORDER-1;sub1: {trackingToken};2026-08-18 10:30:00;70000;COMPLETED;ITEM-1;MODEL-1;Áo;1;100000\n" +
                  $"ORDER-1;sub1: {trackingToken};2026-08-18 10:30:00;30000;COMPLETED;ITEM-2;MODEL-2;Quần;2;50000";
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = await new ShopeeReportParser().ParseAsync(stream);

        result.RowCount.ShouldBe(2);
        result.Conversions.Count.ShouldBe(1);
        var conversion = result.Conversions[0];
        conversion.AttributionValue.ShouldBe(trackingToken);
        conversion.Status.ShouldBe(AffiliateConversionStatus.Approved);
        conversion.NetCommission.ShouldBe(100_000m);
        conversion.Orders.Count.ShouldBe(1);
        conversion.Orders[0].Items.Count.ShouldBe(2);
        conversion.Orders[0].PurchaseAmount.ShouldBe(150_000m);
    }

    [Fact]
    public async Task ParseAsync_Should_Parse_Actual_Shopee_Conversion_Report()
    {
        const string trackingToken = "6V-Der1p6oTMTqPBlSwbqVn4hAPs3gd5";
        var csv = "ID đơn hàng,Trạng thái đặt hàng,Thời Gian Đặt Hàng,Item id,Tên Item,ID Model,Số lượng,Giá trị đơn hàng (₫),Số tiền hoàn trả (₫),Hoa hồng ròng tiếp thị liên kết(₫),Sub_id1,Sub_id2,Sub_id3,Sub_id4,Sub_id5\n" +
                  $"ORDER-2,Đang chờ xử lý,2026-08-21 10:06:07,ITEM-2,Sản phẩm Shopee,MODEL-2,2,107695,,5923.225,6V,{trackingToken[3..]},,,";
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = await new ShopeeReportParser().ParseAsync(stream);

        result.RowCount.ShouldBe(1);
        var conversion = result.Conversions[0];
        conversion.AttributionValue.ShouldBe(trackingToken);
        conversion.Status.ShouldBe(AffiliateConversionStatus.Pending);
        conversion.NetCommission.ShouldBe(5923.225m);
        conversion.Orders[0].PurchaseAmount.ShouldBe(107695m);
        conversion.Orders[0].Items[0].ProductName.ShouldBe("Sản phẩm Shopee");
    }

    [Fact]
    public async Task ParseAsync_Should_Reconstruct_Shop_Token_From_All_SubId_Columns()
    {
        const string trackingToken = "shop-tracking-token-12345678901234567890";
        var csv = "ID đơn hàng,Trạng thái đặt hàng,Thời Gian Đặt Hàng,Item id,Tên Item,ID Model,Số lượng,Giá trị đơn hàng (₫),Hoa hồng ròng tiếp thị liên kết(₫),Sub_id1,Sub_id2,Sub_id3,Sub_id4,Sub_id5\n" +
                  "ORDER-SHOP,Đang chờ xử lý,2026-08-21 10:06:07,ITEM-SHOP,Sản phẩm trong shop,MODEL-SHOP,1,100000,5000,shop,tracking,token,12345678901234567890,";
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var conversion = (await new ShopeeReportParser().ParseAsync(stream)).Conversions.Single();

        conversion.AttributionValue.ShouldBe(trackingToken);
        conversion.Orders.Single().Items.Single().Attributions.Single().AttributionValue.ShouldBe(trackingToken);
    }

    [Fact]
    public async Task Unknown_Order_Status_Should_Remain_Pending()
    {
        var csv = "order_id,sub_id,purchase_time,net_commission,order_status\n" +
                  "ORDER-UNKNOWN,tracking-token-123456789012345678901234,2026-08-21 10:06:07,5000,Trạng thái mới";
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = await new ShopeeReportParser().ParseAsync(stream);

        result.Conversions[0].Orders[0].Status.ShouldBe(AffiliateOrderStatus.Pending);
    }

    [Fact]
    public async Task ParseAsync_Should_Preserve_Different_Tracking_Tokens_Per_Item()
    {
        const string tokenA = "tracking-token-aaaaaaaaaaaaaaaaaaaaaaaa";
        const string tokenB = "tracking-token-bbbbbbbbbbbbbbbbbbbbbbbb";
        var csv = "order_id,sub_id,purchase_time,net_commission,item_id,model_id,product_name,quantity,purchase_amount\n" +
                  $"ORDER-MULTI,{tokenA},2026-08-21 10:06:07,4000,ITEM-A,MODEL-A,Sản phẩm A,1,50000\n" +
                  $"ORDER-MULTI,{tokenB},2026-08-21 10:06:07,6000,ITEM-B,MODEL-B,Sản phẩm B,1,70000";
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var conversion = (await new ShopeeReportParser().ParseAsync(stream)).Conversions[0];

        conversion.AttributionValue.ShouldBeNull();
        conversion.Orders.Count.ShouldBe(1);
        conversion.Orders[0].Items.Count.ShouldBe(2);
        conversion.Orders[0].Items[0].Attributions.Single().AttributionValue.ShouldBe(tokenA);
        conversion.Orders[0].Items[1].Attributions.Single().AttributionValue.ShouldBe(tokenB);
    }

    [Fact]
    public async Task ParseAsync_Should_Keep_Two_Attributions_For_The_Same_Item_And_Order_Them_Stably()
    {
        const string tokenA = "tracking-token-aaaaaaaaaaaaaaaaaaaaaaaa";
        const string tokenB = "tracking-token-bbbbbbbbbbbbbbbbbbbbbbbb";
        var csv = "order_id,sub_id,purchase_time,net_commission,item_id,model_id,product_name,quantity,purchase_amount\n" +
                  $"ORDER-SAME-ITEM,{tokenB},2026-08-21 10:06:07,3000,ITEM-A,MODEL-A,Sản phẩm A,1,40000\n" +
                  $"ORDER-SAME-ITEM,{tokenA},2026-08-21 10:06:07,2000,ITEM-A,MODEL-A,Sản phẩm A,1,30000";
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var item = (await new ShopeeReportParser().ParseAsync(stream)).Conversions[0].Orders[0].Items.Single();

        item.Attributions.Count.ShouldBe(2);
        item.Attributions.Select(x => x.AttributionValue).ShouldBe(new[] { tokenA, tokenB });
        item.ItemTotalCommission.ShouldBe(5000m);
        item.PurchaseAmount.ShouldBe(70000m);
    }

    [Fact]
    public async Task Settlement_Report_Should_Group_Order_Rows_And_Read_Actual_Payment()
    {
        var csv = "ID đơn hàng,Số tiền Shopee thực trả,Mã bảng kê,Ngày thanh toán\n" +
                  "ORDER-PAID,1200.5,BK-001,2026-08-24\n" +
                  "ORDER-PAID,300.25,BK-001,2026-08-24";
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = await new ShopeeSettlementReportParser().ParseAsync(stream);

        result.RowCount.ShouldBe(2);
        result.Rows.Count.ShouldBe(1);
        result.Rows[0].ActualPaidCommission.ShouldBe(1500.75m);
        result.Rows[0].PaymentReference.ShouldBe("BK-001");
    }

    [Fact]
    public void Build_Should_Create_AnRedir_Link_From_A_Canonical_Origin()
    {
        var builder = new ShopeeAffiliateLinkBuilder();

        var result = builder.Build("https://shopee.vn/product/1/2?affiliate_id=old&sub_id=old&foo=bar",
            "tracking-token", "123456789");

        result.ShouldBe("https://s.shopee.vn/an_redir?origin_link=https%3A%2F%2Fshopee.vn%2Fproduct%2F1%2F2&affiliate_id=123456789&sub_id=tracking-token");
    }

    [Fact]
    public void Build_Should_Explain_When_AffiliateId_Is_Not_Configured()
    {
        var builder = new ShopeeAffiliateLinkBuilder();

        var exception = Should.Throw<Volo.Abp.UserFriendlyException>(() =>
            builder.Build("https://shopee.vn/product/1/2", "tracking-token", string.Empty));

        exception.Message.ShouldContain("Shopee Affiliate ID");
    }
}
