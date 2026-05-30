namespace Stylebazar.MVC;

/// <summary>
/// Marker type for the shared localization resource set. Lives at the project
/// ROOT namespace so that, combined with <c>ResourcesPath = "Resources"</c>, the
/// localizer resolves keys from <c>Resources/SharedResource.{culture}.resx</c>.
///
/// Inject <see cref="Microsoft.AspNetCore.Mvc.Localization.IHtmlLocalizer{T}"/>
/// of this type (aliased as <c>L</c> in _ViewImports) anywhere a translated
/// string is needed: <c>@L["Nav.Home"]</c>. Keys are semantic (e.g. "Footer.Shop")
/// and every supported culture has a value, so nothing ever falls back to a raw
/// key. The neutral (.resx, no suffix) file holds Azerbaijani — the default
/// culture — so an unresolved culture still renders proper AZ, never key names.
/// </summary>
public class SharedResource
{
}
