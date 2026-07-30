using Microsoft.AspNetCore.Builder;
using AbpIoTemplateProject;
using Volo.Abp.AspNetCore.TestBase;

var builder = WebApplication.CreateBuilder();
await builder.RunAbpModuleAsync<AbpIoTemplateProjectWebTestModule>();

public partial class Program
{
}
