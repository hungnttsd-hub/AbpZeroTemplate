using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AbpIoTemplateProject.Education;
using AbpIoTemplateProject.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AbpIoTemplateProject.Web.Pages.Admin.Leads;

[Authorize(AbpIoTemplateProjectPermissions.Leads.Default)]
public class IndexModel : AbpIoTemplateProjectPageModel
{
    private readonly IAdminEducationAppService _adminEducationAppService;
    public List<AdminLeadDto> Leads { get; private set; } = new();
    [BindProperty] public Guid LeadId { get; set; }
    [BindProperty] public LeadStatus Status { get; set; }
    public IndexModel(IAdminEducationAppService adminEducationAppService) => _adminEducationAppService = adminEducationAppService;
    public async Task OnGetAsync() => Leads = await _adminEducationAppService.GetLeadsAsync();
    public async Task<IActionResult> OnPostStatusAsync() { await _adminEducationAppService.UpdateLeadStatusAsync(LeadId, new UpdateLeadStatusDto { Status = Status }); return RedirectToPage(); }
}
