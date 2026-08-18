using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Shouldly;
using WebHoanTien.EntityFrameworkCore;
using Xunit;

namespace WebHoanTien.Pages;

[Collection(WebHoanTienTestConsts.CollectionDefinitionName)]
public class Index_Tests : WebHoanTienWebTestBase
{
    [Fact]
    public async Task Customer_Home_Page_Should_Render()
    {
        var response = await GetResponseAsStringAsync("/");
        response.ShouldContain("<html lang=\"vi\"");
        response.ShouldContain("Mua sắm Shopee");
        response.ShouldContain("webHoanTien.com");
    }

    [Theory]
    [InlineData("/Account/Login", "Đăng nhập")]
    [InlineData("/Account/Register", "Tạo tài khoản")]
    [InlineData("/Account/ConfirmEmail", "Chưa thể xác minh email")]
    [InlineData("/Account/ConfirmEmailSent", "Kiểm tra hộp thư của bạn")]
    public async Task Customer_Account_Pages_Should_Use_Vietnamese_Brand_Layout(string url, string expectedHeading)
    {
        var response = await GetResponseAsStringAsync(url);
        response.ShouldContain("<html lang=\"vi\"");
        response.ShouldContain(expectedHeading);
        response.ShouldContain("account.css");
        response.ShouldNotContain("lpx-account");
    }

    [Fact]
    public async Task Email_Registration_Should_Require_And_Persist_Legal_Consent()
    {
        var rejectedEmail = $"missing-consent-{Guid.NewGuid():N}@example.test";
        var rejectedResponse = await PostRegistrationAsync(rejectedEmail, acceptedTerms: false);
        var rejectedHtml = await rejectedResponse.Content.ReadAsStringAsync();
        rejectedResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        rejectedResponse.RequestMessage!.RequestUri!.AbsolutePath.ShouldBe("/Account/Register");
        WebUtility.HtmlDecode(rejectedHtml).ShouldContain("Bạn cần đồng ý với Điều khoản và Chính sách riêng tư.");

        var acceptedEmail = $"accepted-consent-{Guid.NewGuid():N}@example.test";
        var acceptedResponse = await PostRegistrationAsync(acceptedEmail, acceptedTerms: true);
        acceptedResponse.StatusCode.ShouldBe(HttpStatusCode.Found);
        acceptedResponse.Headers.Location!.OriginalString.ShouldBe("/");

        var nextRequest = await Client.GetAsync("/");
        nextRequest.StatusCode.ShouldBe(HttpStatusCode.OK);
        nextRequest.RequestMessage!.RequestUri!.AbsolutePath.ShouldBe("/");
    }

    private async Task<HttpResponseMessage> PostRegistrationAsync(string email, bool acceptedTerms)
    {
        var registerPage = await Client.GetAsync("/Account/Register");
        var html = await registerPage.Content.ReadAsStringAsync();
        var match = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        match.Success.ShouldBeTrue("Registration page must emit an antiforgery token.");

        var values = new List<KeyValuePair<string, string>>
        {
            new("__RequestVerificationToken", WebUtility.HtmlDecode(match.Groups[1].Value)),
            new("Input.UserName", string.Empty),
            new("Input.EmailAddress", email),
            new("Input.Password", "Phase1!Test123"),
            new("ConfirmPassword", "Phase1!Test123")
        };
        if (acceptedTerms)
        {
            values.Add(new KeyValuePair<string, string>("AcceptedTerms", "true"));
        }

        return await Client.PostAsync("/Account/Register", new FormUrlEncodedContent(values));
    }
}
