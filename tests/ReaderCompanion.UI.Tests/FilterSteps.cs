using Microsoft.AspNetCore.Components;

namespace ReaderCompanion.UI.Tests;

/// <summary>Drives a filter dropdown the way a user does: open it, tick or untick an option.</summary>
internal static class FilterSteps
{
    public static void Open<T>(IRenderedComponent<T> cut, string id) where T : IComponent
    {
        if (cut.FindAll($"#{id}-menu").Count == 0)
        {
            cut.Find($"#{id}").Click();
        }
    }

    public static void Pick<T>(IRenderedComponent<T> cut, string id, string option, bool on = true) where T : IComponent
    {
        Open(cut, id);
        cut.FindAll($"#{id}-menu .filter-option").Single(o => o.TextContent.Trim() == option).QuerySelector("input")!.Change(on);
    }

    public static IReadOnlyList<string> Options<T>(IRenderedComponent<T> cut, string id) where T : IComponent
    {
        Open(cut, id);
        return [.. cut.FindAll($"#{id}-menu .filter-option").Select(o => o.TextContent.Trim())];
    }

    public static IReadOnlyList<string> Ticked<T>(IRenderedComponent<T> cut, string id) where T : IComponent
    {
        Open(cut, id);
        return [.. cut.FindAll($"#{id}-menu .filter-option").Where(o => o.QuerySelector("input")!.HasAttribute("checked")).Select(o => o.TextContent.Trim())];
    }
}
