using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebHoanTien.Web.Pages.Guide;

[AllowAnonymous]
public class VideoModel : PageModel
{
    public GuideVideoDefinition Video { get; private set; } = null!;

    public IReadOnlyList<GuideVideoDefinition> RelatedVideos { get; private set; } = [];

    public string BackUrl => $"/huong-dan#{Video.AnchorId}";

    public string CtaUrl => Video.NextSlug is not null
        ? $"/huong-dan/video/{Video.NextSlug}"
        : User.Identity?.IsAuthenticated == true ? "/" : "/Account/Register";

    public IActionResult OnGet(string? slug)
    {
        var video = GuideVideoCatalog.Find(slug);
        if (video is null)
        {
            return Redirect("/huong-dan");
        }

        Video = video;
        RelatedVideos = GuideVideoCatalog.All.Where(item => item.Slug != video.Slug).ToArray();
        return Page();
    }
}
