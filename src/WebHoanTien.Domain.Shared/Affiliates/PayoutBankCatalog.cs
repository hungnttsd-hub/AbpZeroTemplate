using System;
using System.Collections.Generic;
using System.Linq;

namespace WebHoanTien.Affiliates;

public sealed record PayoutBank(string Code, string Name);

public static class PayoutBankCatalog
{
    public static IReadOnlyList<PayoutBank> Banks { get; } = new[]
    {
        new PayoutBank("VCB", "Vietcombank"),
        new PayoutBank("BIDV", "BIDV"),
        new PayoutBank("CTG", "VietinBank"),
        new PayoutBank("AGR", "Agribank"),
        new PayoutBank("TCB", "Techcombank"),
        new PayoutBank("MB", "MBBank"),
        new PayoutBank("ACB", "ACB"),
        new PayoutBank("VPB", "VPBank"),
        new PayoutBank("TPB", "TPBank"),
        new PayoutBank("VIB", "VIB"),
        new PayoutBank("STB", "Sacombank"),
        new PayoutBank("HDB", "HDBank"),
        new PayoutBank("MSB", "MSB"),
        new PayoutBank("SHB", "SHB"),
        new PayoutBank("OCB", "OCB"),
        new PayoutBank("EIB", "Eximbank"),
        new PayoutBank("LPB", "LPBank"),
        new PayoutBank("NAB", "Nam A Bank"),
        new PayoutBank("SEAB", "SeABank"),
        new PayoutBank("BAB", "Bac A Bank")
    };

    public static bool IsSupported(string? code) => !string.IsNullOrWhiteSpace(code) &&
        Banks.Any(bank => bank.Code.Equals(code.Trim(), StringComparison.OrdinalIgnoreCase));
}
