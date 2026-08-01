using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace AbpIoTemplateProject.Education;

public interface IPublicEducationAppService : IApplicationService
{
    Task<List<CourseCardDto>> GetCoursesAsync();
    Task<List<CourseCardDto>> GetFeaturedCoursesAsync();
    Task<CourseDetailDto?> GetCourseBySlugAsync(string slug);
    Task<List<TeacherCardDto>> GetTeachersAsync();
    Task<List<TeacherCardDto>> GetFeaturedTeachersAsync();
    Task<TeacherDetailDto?> GetTeacherBySlugAsync(string slug);
    Task<List<CourseClassDto>> GetUpcomingClassesAsync();
    Task<Guid> SubmitLeadAsync(SubmitLeadDto input);
    Task<PlacementTestDto?> GetPublishedPlacementTestAsync();
    Task<Guid> StartPlacementAttemptAsync(StartPlacementAttemptDto input);
    Task<List<PlacementQuestionDto>> GetPlacementQuestionsAsync(Guid placementAttemptId);
    Task<PlacementResultDto> SubmitPlacementAttemptAsync(SubmitPlacementAttemptDto input);
}
