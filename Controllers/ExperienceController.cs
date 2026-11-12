using EPiServer.Framework.DataAnnotations;
using EPiServer.VisualBuilder;
using EPiServer.Web.Mvc;
using Microsoft.AspNetCore.Mvc;

namespace Cms13Poc.Controllers;

/// <summary>
/// Renders every Visual Builder experience (built-in BlankExperience and any custom type).
/// </summary>
[TemplateDescriptor(Inherited = true)]
public class ExperienceController : PageController<ExperienceData>
{
    public IActionResult Index(ExperienceData currentPage) => View("~/Views/Experience/Index.cshtml", currentPage);
}
