using System.ComponentModel.DataAnnotations;
using EPiServer.Web;

namespace Cms13Poc.Models.Blocks;

// Simple Visual Builder elements. TeaserBlock and EditorialBlock are also element-enabled (see those files).

[SiteContentType(GUID = "0B4E7C7A-6B0E-4B7A-9E53-1F3D2C6A9A01", DisplayName = "Heading element",
    CompositionBehaviors = [CompositionBehavior.ElementEnabledKey])]
public class HeadingElement : SiteBlockData
{
    [CultureSpecific]
    [Display(Order = 1)]
    public virtual string Heading { get; set; }
}

[SiteContentType(GUID = "0B4E7C7A-6B0E-4B7A-9E53-1F3D2C6A9A02", DisplayName = "Text element",
    CompositionBehaviors = [CompositionBehavior.ElementEnabledKey])]
public class TextElement : SiteBlockData
{
    // Plain text on purpose: in CMS 13.6 the MCP composition write fails on XhtmlString inside inline elements
    // ("The XhtmlString instance is read-only").
    [CultureSpecific]
    [Display(Order = 1)]
    [UIHint(UIHint.Textarea)]
    public virtual string Body { get; set; }
}

// Hybrid: a Visual Builder section that holds a classic Content Area. Shared blocks placed in it are rendered by
// the normal content area renderer, which filters on access rights and audiences (the Visual Builder outline does not).
// It has to be a section: CMS 13.6 refuses to start if an ElementEnabled type has a ContentArea property
// ("The property 'Items' is not allowed when content type has ElementEnabled").
[SiteContentType(GUID = "0B4E7C7A-6B0E-4B7A-9E53-1F3D2C6A9A03", DisplayName = "Content area section",
    CompositionBehaviors = [CompositionBehavior.SectionEnabledKey])]
public class ContentAreaSection : SiteBlockData
{
    [Display(Order = 1)]
    public virtual ContentArea Items { get; set; }
}
