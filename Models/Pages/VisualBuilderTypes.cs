using EPiServer.VisualBuilder;

namespace Cms13Poc.Models.Pages;

// CMS 13 PaaS ships no experience/section types out of the box, so the POC defines the two "blank" ones.

[ContentType(GUID = "0B4E7C7A-6B0E-4B7A-9E53-1F3D2C6A9A10", DisplayName = "Blank experience",
    Description = "Visual Builder experience (outline of sections)", GroupName = Globals.GroupNames.Default)]
public class BlankExperience : ExperienceData
{
}

[ContentType(GUID = "0B4E7C7A-6B0E-4B7A-9E53-1F3D2C6A9A11", DisplayName = "Blank section",
    Description = "Visual Builder section (grid of rows/columns/elements)", GroupName = Globals.GroupNames.Default)]
public class BlankSection : SectionData
{
}
