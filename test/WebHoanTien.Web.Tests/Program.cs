using Microsoft.AspNetCore.Builder;
using WebHoanTien;
using Volo.Abp.AspNetCore.TestBase;

var builder = WebApplication.CreateBuilder();
await builder.RunAbpModuleAsync<WebHoanTienWebTestModule>();

public partial class Program
{
}
