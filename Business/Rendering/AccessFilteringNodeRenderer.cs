using System.Security.Principal;
using EPiServer.Security;
using EPiServer.VisualBuilder.Compositions;
using EPiServer.Web;
using EPiServer.Web.Mvc.TagHelpers.Experience.Internal;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Cms13Poc.Business.Rendering;

/// <summary>
/// POC: makes Visual Builder honour read access on SHARED blocks, which the built-in renderers do not (13.6).
/// Sorts before the built-in renderers and swallows shared components the visitor may not read.
/// Opt-in per request with ?vbfilter=1 so the unfiltered behaviour stays visible for comparison.
/// ponytail: hooks into an *.Internal namespace (unsupported API) and covers access rights only, not audiences.
/// </summary>
public class AccessFilteringNodeRenderer(IHttpContextAccessor http, IPrincipalAccessor principal, IContextModeResolver mode) : ICompositionNodeRenderer
{
    public int SortIndex => 0;

    public bool CanHandle(CompositionNode node) =>
        node is ComponentNode { Component: IContent content }
        && !Readable(content, principal.Principal)
        && http.HttpContext?.Request.Query["vbfilter"].ToString() == "1"
        && mode.CurrentMode == ContextMode.Default;

    public static bool Readable(IContent content, IPrincipal user) =>
        content is not ISecurable securable || securable.GetSecurityDescriptor().HasAccess(user, AccessLevel.Read);

    public Task RenderAsync(CompositionNode compositionNode, TagHelperContext tagHelperContext, TagHelperOutput output, ViewContext viewContext)
    {
        output.SuppressOutput();
        return Task.CompletedTask;
    }
}
