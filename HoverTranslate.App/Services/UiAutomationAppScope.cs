using System.Windows.Automation;

namespace HoverTranslate.App.Services;

/// <summary>UI Automation 作用域：不读取本进程炫译界面上的文字（历史/译文/设置等）。</summary>
internal static class UiAutomationAppScope
{
    private static readonly int ThisProcessId = Environment.ProcessId;

    public static bool IsFromThisProcess(AutomationElement? element)
    {
        if (element is null)
            return false;

        try
        {
            return element.Current.ProcessId == ThisProcessId;
        }
        catch (ElementNotAvailableException)
        {
            return true;
        }
    }
}
