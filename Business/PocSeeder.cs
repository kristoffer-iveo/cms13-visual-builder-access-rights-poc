using EPiServer.Personalization.VisitorGroups;
using Cms13Poc.Models.Pages;
using EPiServer.Security;
using EPiServer.VisualBuilder;
using EPiServer.VisualBuilder.Compositions;
using EPiServer.Shell.Security;
using EPiServer.VisitorGroupsCriteriaPack;

namespace Cms13Poc.Business;

/// <summary>
/// Keys of the content that was created through the CMS MCP server (see scripts/seed_content.py + keys.json).
/// </summary>
public static class PocContent
{
    public static readonly Guid Experience = new("055993b8-8206-46d5-a4ee-687e4f785d08");
    public static readonly Guid ClassicPage = new("8c672c39-7fd1-4b93-8ea2-60544c85a086");
    public static readonly Guid PublicBlock = new("32d3a1b3-28b1-4dfc-b8cd-55bbbf3fda8b");
    public static readonly Guid MembersBlock = new("4a28a642-ba2b-46a2-a2af-95044b8dfab7");
    public static readonly Guid AdminLockedBlock = new("ddba4941-1a50-4557-b799-72aa519cd9d6");
    public static readonly Guid VipBlock = new("edbfaff6-0307-4ee0-879e-cdb7377ff2d6");
    public static readonly Guid DefaultBlock = new("2aef3939-e533-48c5-a6a6-b1135fb30246");
    public static readonly Guid MembersSectionBlock = new("bca09961-4a17-4042-a9fe-4e09b0bf60cc");
}

/// <summary>
/// Idempotent local test data that the CMS MCP server has no tools for: users, roles and audiences.
/// Runs at startup. POC only - passwords are hard coded on purpose.
/// </summary>
public static class PocSeeder
{
    public const string Password = "Test123!poc";
    public const string VipAudience = "POC VIP (?audience=vip)";
    public const string StudentAudience = "POC Student (?audience=student)";

    public static async Task RunAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var users = sp.GetRequiredService<UIUserProvider>();
        var roles = sp.GetRequiredService<UIRoleProvider>();

        // user -> roles. "VisitorGroupAdmins" lets the editor work with audiences.
        var accounts = new Dictionary<string, string[]>
        {
            ["editor"] = ["WebEditors", "VisitorGroupAdmins"],
            ["member"] = ["Members"],
        };

        foreach (var role in accounts.Values.SelectMany(r => r).Distinct())
        {
            if (!await roles.RoleExistsAsync(role)) await roles.CreateRoleAsync(role);
        }

        foreach (var (name, userRoles) in accounts)
        {
            if (await users.GetUserAsync(name) != null) continue;
            var result = await users.CreateUserAsync(name, Password, $"{name}@example.local", null, null, true);
            if (result.Status != UIUserCreateStatus.Success) throw new InvalidOperationException($"Could not create {name}: {string.Join(", ", result.Errors)}");
            await roles.AddUserToRolesAsync(name, userRoles);
        }

        // Alloy's root ACL only knows WebAdmins. Give the editor role normal editing rights, and the MCP
        // server's client-secret identity (role CmsAdmins) full access so it can create content.
        var security = sp.GetRequiredService<IContentSecurityRepository>();
        SetAcl(security, ContentReference.RootPage, replace: false,
            ("CmsAdmins", AccessLevel.FullAccess),
            ("WebEditors", Editor));

        var groups = sp.GetRequiredService<IVisitorGroupRepository>();
        foreach (var (groupName, value) in new[] { (VipAudience, "vip"), (StudentAudience, "student") })
        {
            // Upsert, so a changed definition reaches an existing database (the group id is kept).
            var group = groups.List().FirstOrDefault(g => g.Name == groupName) ?? new VisitorGroup { Name = groupName, IsSecurityRole = true };
            group.Notes = $"Matches requests with ?audience={value}";
            group.Criteria.Clear();
            group.Criteria.Add(new VisitorGroupCriterion
            {
                TypeName = typeof(QueryStringCriterion).AssemblyQualifiedName,
                Enabled = true,
                Model = new QueryStringModel { Key = "audience", Condition = QueryStringValueCondition.HasValue, Value = value },
            });
            groups.Save(group);
        }

        // Everything below needs the MCP-created content; on a fresh database it is simply skipped.
        var loader = sp.GetRequiredService<IContentLoader>();
        if (!loader.TryGet<ExperienceData>(PocContent.Experience, out var experience)) return;

        BlockData Shared(Guid key) => loader.Get<BlockData>(key);
        ContentReference Link(Guid key) => loader.Get<IContent>(key).ContentLink;

        // Access rights on shared blocks.
        foreach (var key in new[] { PocContent.MembersBlock, PocContent.MembersSectionBlock })
        {
            SetAcl(security, Link(key), replace: true, // no Everyone: anonymous visitors must not see it
                ("Members", AccessLevel.Read), ("WebEditors", Editor), ("WebAdmins", AccessLevel.FullAccess), ("CmsAdmins", AccessLevel.FullAccess));
        }

        SetAcl(security, Link(PocContent.AdminLockedBlock), replace: true, // editors can read but not change
            ("Everyone", AccessLevel.Read), ("WebEditors", AccessLevel.Read), ("WebAdmins", AccessLevel.FullAccess), ("CmsAdmins", AccessLevel.FullAccess));

        var repository = sp.GetRequiredService<IContentRepository>();
        var vipRole = groups.List().First(g => g.Name == VipAudience).Id.ToString();

        // Visual Builder experience: add sections that hold REAL shared block references. The MCP server cannot do
        // this - its "reference" nodes are stored as inline copies - so the public Composition API is used here.
        const string sharedSection = "3. Shared blocks with ACL";
        const string personalizedSection = "5. Audience set in data (not via the UI)";
        if (experience.Layout?.Nodes.All(n => n.Name != sharedSection) == true)
        {
            var mapper = sp.GetRequiredService<ICompositionMapper>();
            var blocks = sp.GetRequiredService<IBlockPropertyFactory>();
            var page = experience.CreateWritableClone();
            var composition = (Composition)mapper.ToComposition(page, false).CreateWritableClone();

            StructureNode NewRow(string name) => composition.AddSection(blocks.Create<BlankSection>(), name).AddRow();

            var row = NewRow(sharedSection);
            row.AddColumn().AddComponent(Shared(PocContent.PublicBlock), "Public");
            row.AddColumn().AddComponent(Shared(PocContent.MembersBlock), "Members only");
            row.AddColumn().AddComponent(Shared(PocContent.AdminLockedBlock), "Locked for editors");

            row = NewRow("4. Audience test (shared blocks, no audience set)");
            row.AddColumn().AddComponent(Shared(PocContent.VipBlock), "VIP");
            row.AddColumn().AddComponent(Shared(PocContent.DefaultBlock), "Standard");

            row = NewRow(personalizedSection);
            row.AddColumn().AddComponent(Shared(PocContent.VipBlock), "VIP");
            row.AddColumn().AddComponent(Shared(PocContent.DefaultBlock), "Standard");

            // A shared, section-enabled block placed directly as a section.
            composition.AddComponent(Shared(PocContent.MembersSectionBlock), "6. Shared block as section (ACL)");

            mapper.Populate(page, composition);

            // Section 5: Visual Builder stores elements in a ContentArea (UnstructuredData), so personalization can be
            // written at data level even though the Visual Builder UI offers no way to set it. Does rendering honour it?
            var section = page.UnstructuredData.Items
                .Where(i => i.RenderSettings.TryGetValue("data-inlineblockname", out var n) && Equals(n, personalizedSection))
                .Select(i => (SectionData)i.InlineBlock).Single();
            Personalize(section.UnstructuredData, Link(PocContent.VipBlock), Link(PocContent.DefaultBlock), vipRole);

            repository.Save(page, EPiServer.DataAccess.SaveAction.Publish, AccessLevel.NoAccess);
        }

        // Hybrid (section 7): an inline section block with a classic Content Area holding the same shared blocks.
        const string hybridSection = "7. Hybrid: Content Area section (ACL + audience)";
        experience = loader.Get<ExperienceData>(PocContent.Experience);
        if (experience.Layout?.Nodes.All(n => n.Name != hybridSection) == true)
        {
            var mapper = sp.GetRequiredService<ICompositionMapper>();
            var blocks = sp.GetRequiredService<IBlockPropertyFactory>();
            var page = experience.CreateWritableClone();
            var composition = (Composition)mapper.ToComposition(page, false).CreateWritableClone();

            var element = blocks.Create<Models.Blocks.ContentAreaSection>();
            element.Items = new ContentArea();
            foreach (var key in new[] { PocContent.PublicBlock, PocContent.MembersBlock, PocContent.VipBlock, PocContent.DefaultBlock })
            {
                element.Items.Items.Add(new ContentAreaItem { ContentLink = Link(key) });
            }
            Personalize(element.Items, Link(PocContent.VipBlock), Link(PocContent.DefaultBlock), vipRole);

            composition.AddComponent(element, hybridSection);
            mapper.Populate(page, composition);
            repository.Save(page, EPiServer.DataAccess.SaveAction.Publish, AccessLevel.NoAccess);
        }

        // Classic page: VIP + default block as a normal personalization group in the content area.
        if (loader.TryGet<StandardPage>(PocContent.ClassicPage, out var classic)
            && classic.MainContentArea.Items.All(i => i.AllowedRoles == null || !i.AllowedRoles.Any()))
        {
            var page = (StandardPage)classic.CreateWritableClone();
            Personalize(page.MainContentArea, Link(PocContent.VipBlock), Link(PocContent.DefaultBlock), vipRole);
            repository.Save(page, EPiServer.DataAccess.SaveAction.Publish, AccessLevel.NoAccess);
        }
    }

    /// <summary>Puts the two items in one personalization group: <paramref name="vip"/> for the audience, <paramref name="fallback"/> for everyone else.</summary>
    private static void Personalize(ContentArea area, ContentReference vip, ContentReference fallback, string audienceId)
    {
        foreach (var item in area.Items)
        {
            if (item.ContentLink?.CompareToIgnoreWorkID(vip) == true)
            {
                item.AllowedRoles = [audienceId];
                item.ContentGroup = "poc-message";
            }
            else if (item.ContentLink?.CompareToIgnoreWorkID(fallback) == true)
            {
                item.ContentGroup = "poc-message";
            }
        }
    }

    private const AccessLevel Editor = AccessLevel.Read | AccessLevel.Create | AccessLevel.Edit | AccessLevel.Delete | AccessLevel.Publish;

    /// <summary>Adds entries to a content item's ACL, or (replace) breaks inheritance and sets exactly these entries.</summary>
    private static void SetAcl(IContentSecurityRepository security, ContentReference link, bool replace, params (string Role, AccessLevel Level)[] entries)
    {
        var acl = (IContentSecurityDescriptor)security.Get(link).CreateWritableClone();
        if (replace)
        {
            acl.IsInherited = false;
            acl.Clear();
        }

        foreach (var (role, level) in entries)
        {
            acl.AddEntry(new AccessControlEntry(role, level, SecurityEntityType.Role));
        }

        security.Save(link, acl, SecuritySaveType.Replace);
    }
}
