using Hangfire.Dashboard;

namespace WebHoanTien.Web.Operations;

public class AdminHangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        return httpContext.User.Identity?.IsAuthenticated == true && httpContext.User.IsInRole("admin");
    }
}
