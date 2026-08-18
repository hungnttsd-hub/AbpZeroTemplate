using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebHoanTien.Affiliates;

namespace WebHoanTien.Web.Pages.Legal;

[Authorize]
public class ConsentModel : PageModel
{
    private readonly ICustomerProfileAppService _profile;
    [BindProperty] public bool Accepted { get; set; }
    [BindProperty(SupportsGet = true)] public string? ReturnUrl { get; set; }
    public string? Error { get; private set; }
    public ConsentModel(ICustomerProfileAppService profile) => _profile = profile;
    public async Task<IActionResult> OnPostAsync()
    {
        if (!Accepted) { Error = "Bạn cần chấp thuận để tiếp tục sử dụng dịch vụ."; return Page(); }
        await _profile.AcceptLegalAsync(new CreateLegalConsentInput { Accepted = true, Method = LegalConsentMethod.AccountPrompt });
        return LocalRedirect(IsLocal(ReturnUrl) ? ReturnUrl! : "/");
    }
    private static bool IsLocal(string? value) => !string.IsNullOrWhiteSpace(value) && value.StartsWith('/') && !value.StartsWith("//");
}
