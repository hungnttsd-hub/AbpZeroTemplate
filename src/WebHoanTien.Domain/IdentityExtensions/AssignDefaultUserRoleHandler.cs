using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Entities.Events;
using Volo.Abp.EventBus;
using Volo.Abp.Identity;

namespace WebHoanTien.IdentityExtensions;

public class AssignDefaultUserRoleHandler : ILocalEventHandler<EntityCreatedEventData<IdentityUser>>, ITransientDependency
{
    private readonly IdentityUserManager _userManager;
    private readonly IdentityRoleManager _roleManager;
    public AssignDefaultUserRoleHandler(IdentityUserManager userManager, IdentityRoleManager roleManager)
    { _userManager = userManager; _roleManager = roleManager; }

    public async Task HandleEventAsync(EntityCreatedEventData<IdentityUser> eventData)
    {
        if (!string.Equals(eventData.Entity.UserName, "admin", System.StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(eventData.Entity.Email) &&
            !string.Equals(eventData.Entity.UserName, eventData.Entity.Email, System.StringComparison.OrdinalIgnoreCase))
            await _userManager.SetUserNameAsync(eventData.Entity, eventData.Entity.Email);

        if (await _roleManager.RoleExistsAsync("User") &&
            !await _userManager.IsInRoleAsync(eventData.Entity, "admin") &&
            !await _userManager.IsInRoleAsync(eventData.Entity, "User"))
            await _userManager.AddToRoleAsync(eventData.Entity, "User");
    }
}
