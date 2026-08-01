using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace AbpIoTemplateProject.Education;

[Authorize]
public class StudentPortalAppService : ApplicationService, IStudentPortalAppService
{
    private readonly IRepository<Student, Guid> _studentRepository;
    private readonly IRepository<Enrollment, Guid> _enrollmentRepository;
    private readonly IRepository<CourseClass, Guid> _classRepository;
    private readonly IRepository<Course, Guid> _courseRepository;

    public StudentPortalAppService(IRepository<Student, Guid> studentRepository, IRepository<Enrollment, Guid> enrollmentRepository, IRepository<CourseClass, Guid> classRepository, IRepository<Course, Guid> courseRepository)
    {
        _studentRepository = studentRepository;
        _enrollmentRepository = enrollmentRepository;
        _classRepository = classRepository;
        _courseRepository = courseRepository;
    }

    public async Task<StudentProfileDto> GetMyProfileAsync()
    {
        var student = await FindMyStudentAsync();
        return student == null
            ? new StudentProfileDto { FullName = CurrentUser.Name ?? string.Empty, Email = CurrentUser.Email }
            : new StudentProfileDto { FullName = student.FullName, PhoneNumber = student.PhoneNumber, Email = student.Email, CurrentLevel = student.CurrentLevel, Target = student.Target };
    }

    public async Task UpdateMyProfileAsync(UpdateStudentProfileDto input)
    {
        EnsureUser();
        var student = await FindMyStudentAsync();
        if (student == null)
        {
            student = new Student(GuidGenerator.Create()) { IdentityUserId = CurrentUser.Id };
            Apply(student, input);
            await _studentRepository.InsertAsync(student, autoSave: true);
            return;
        }
        Apply(student, input);
        await _studentRepository.UpdateAsync(student, autoSave: true);
    }

    public async Task<List<StudentEnrollmentDto>> GetMyEnrollmentsAsync()
    {
        var student = await FindMyStudentAsync();
        if (student == null) return new List<StudentEnrollmentDto>();
        var enrollments = await _enrollmentRepository.GetQueryableAsync();
        var classes = await _classRepository.GetQueryableAsync();
        var courses = await _courseRepository.GetQueryableAsync();
        return await AsyncExecuter.ToListAsync(
            from enrollment in enrollments
            join courseClass in classes on enrollment.CourseClassId equals courseClass.Id
            join course in courses on courseClass.CourseId equals course.Id
            where enrollment.StudentId == student.Id
            orderby courseClass.StartDate descending
            select new StudentEnrollmentDto
            {
                CourseName = course.Name, ClassCode = courseClass.Code, ScheduleText = courseClass.ScheduleText,
                StartDate = courseClass.StartDate, Status = enrollment.Status, AgreedTuitionFee = enrollment.AgreedTuitionFee,
                PaidAmount = enrollment.PaidAmount
            });
    }

    private async Task<Student?> FindMyStudentAsync()
    {
        EnsureUser();
        var query = await _studentRepository.GetQueryableAsync();
        return await AsyncExecuter.FirstOrDefaultAsync(query.Where(x => x.IdentityUserId == CurrentUser.Id));
    }

    private void EnsureUser()
    {
        if (!CurrentUser.Id.HasValue) throw new AbpException("Người dùng chưa đăng nhập.");
    }

    private static void Apply(Student student, UpdateStudentProfileDto input)
    {
        student.FullName = input.FullName.Trim(); student.PhoneNumber = input.PhoneNumber?.Trim(); student.Email = input.Email?.Trim();
        student.CurrentLevel = input.CurrentLevel?.Trim(); student.Target = input.Target?.Trim();
    }
}
