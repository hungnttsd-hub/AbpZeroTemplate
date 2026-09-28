using System;
using WebHoanTien.Affiliates;

namespace WebHoanTien.Admin;

internal sealed record ShopeeSettlementAmounts(decimal Gross, decimal Fee, decimal Tax, decimal Net,
    bool DefaultFee = false, bool DefaultTax = false)
{
    public static ShopeeSettlementAmounts For(ShopeeSettlementRecord row, ShopeeSettlementBill bill)
    {
        if (row.Status == ShopeeSettlementRecordStatus.Approved ||
            row.ExtraProperties.ContainsKey("ManualGrossCommission"))
            return new(row.EligibleCommission, row.AllocatedServiceFee, row.AllocatedTax, row.ActualPaidCommission);

        var legacyPendingTax = !bill.IsShopeePaid && bill.PaidCommission == 0m &&
            row.ActualPaidCommission == 0m && row.AllocatedTax > 0m;
        // Legacy CSV contains NET proceeds, not a known gross amount. Never tax that net again.
        // Use bill-level deductions so a rounded zero allocation does not get charged twice.
        var defaultFee = bill.HasAuthoritativeEligibleCommission && bill.ServiceFeeAmount == 0m &&
            row.AllocatedServiceFee == 0m;
        var defaultTax = bill.HasAuthoritativeEligibleCommission &&
            (legacyPendingTax || bill.TaxAmount == 0m && row.AllocatedTax == 0m);
        var fee = defaultFee ? Round(row.EligibleCommission * AdminShopeeSettlementManualInput.DefaultServiceFeePercent / 100m)
            : row.AllocatedServiceFee;
        var tax = defaultTax
            ? Math.Min(Math.Max(0m, row.EligibleCommission - fee), Round(row.EligibleCommission * AdminShopeeSettlementManualInput.DefaultTaxPercent / 100m))
            : legacyPendingTax ? 0m : row.AllocatedTax;
        var net = defaultFee || defaultTax || legacyPendingTax
            ? Math.Max(0m, row.EligibleCommission - fee - tax) : row.ActualPaidCommission;
        return new(row.EligibleCommission, fee, tax, net, defaultFee, defaultTax);
    }

    public static ShopeeSettlementAmounts Manual(AdminShopeeSettlementManualInput input)
    {
        var gross = Round(input.GrossCommission!.Value);
        var fee = Round(gross * input.ServiceFeePercent!.Value / 100m);
        var tax = Math.Min(gross - fee, Round(gross * input.TaxPercent!.Value / 100m));
        return new(gross, fee, tax, gross - fee - tax);
    }

    public void Apply(ShopeeSettlementRecord row)
    {
        if (row.Status == ShopeeSettlementRecordStatus.Approved) return;
        if (!row.ExtraProperties.ContainsKey("OriginalEligibleCommission"))
        {
            row.ExtraProperties["OriginalEligibleCommission"] = row.EligibleCommission;
            row.ExtraProperties["OriginalAllocatedServiceFee"] = row.AllocatedServiceFee;
            row.ExtraProperties["OriginalAllocatedTax"] = row.AllocatedTax;
            row.ExtraProperties["OriginalActualPaidCommission"] = row.ActualPaidCommission;
        }
        if (DefaultFee) row.ExtraProperties["DefaultServiceFeePercent"] = AdminShopeeSettlementManualInput.DefaultServiceFeePercent;
        if (DefaultTax) row.ExtraProperties["DefaultTaxPercent"] = AdminShopeeSettlementManualInput.DefaultTaxPercent;
        row.UpdateAmounts(Gross, Fee, Tax, Net);
    }

    private static decimal Round(decimal amount) => decimal.Round(amount, 4, MidpointRounding.AwayFromZero);
}
