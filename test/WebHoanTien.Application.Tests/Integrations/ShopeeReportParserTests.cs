using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
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
    public async Task Unknown_Order_Status_Should_Remain_Pending()
    {
        var csv = "order_id,sub_id,purchase_time,net_commission,order_status\n" +
                  "ORDER-UNKNOWN,tracking-token-123456789012345678901234,2026-08-21 10:06:07,5000,Trạng thái mới";
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = await new ShopeeReportParser().ParseAsync(stream);

        result.Conversions[0].Orders[0].Status.ShouldBe(AffiliateOrderStatus.Pending);
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
        var builder = new ShopeeAffiliateLinkBuilder(Options.Create(new ShopeeAffiliateOptions
        {
            AffiliateId = "123456789"
        }));

        var result = builder.Build("https://shopee.vn/product/1/2?affiliate_id=old&sub_id=old&foo=bar", "tracking-token");

        result.ShouldBe("https://s.shopee.vn/an_redir?origin_link=https%3A%2F%2Fshopee.vn%2Fproduct%2F1%2F2&affiliate_id=123456789&sub_id=tracking-token");
    }

    [Fact]
    public void Build_Should_Explain_When_AffiliateId_Is_Not_Configured()
    {
        var builder = new ShopeeAffiliateLinkBuilder(Options.Create(new ShopeeAffiliateOptions()));

        var exception = Should.Throw<Volo.Abp.UserFriendlyException>(() =>
            builder.Build("https://shopee.vn/product/1/2", "tracking-token"));

        exception.Message.ShouldContain("Shopee Affiliate ID");
    }
}
