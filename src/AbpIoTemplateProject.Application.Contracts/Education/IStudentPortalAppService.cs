using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace AbpIoTemplateProject.Education;

public interface IStudentPortalAppService : IApplicationService
{
    Task<StudentProfileDto> GetMyProfileAsync();
    Task UpdateMyProfileAsync(UpdateStudentProfileDto input);
    Task<List<StudentEnrollmentDto>> GetMyEnrollmentsAsync();
}
