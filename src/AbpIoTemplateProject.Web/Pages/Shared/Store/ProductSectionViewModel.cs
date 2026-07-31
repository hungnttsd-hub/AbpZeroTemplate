using System.Collections.Generic;
using AbpIoTemplateProject.Store;

namespace AbpIoTemplateProject.Web.Pages.Shared.Store;

public sealed record ProductSectionViewModel(
    string Title,
    string Subtitle,
    IReadOnlyList<ProductListItemDto> Products);
