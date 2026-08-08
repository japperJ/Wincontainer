using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinContainers_App.Models;

namespace WinContainers_App.Pages;

/// <summary>Selects the chat bubble template for each message type.</summary>
public sealed class MessageTemplateSelector : DataTemplateSelector
{
    public DataTemplate? UserTemplate { get; set; }
    public DataTemplate? AssistantTemplate { get; set; }
    public DataTemplate? StepTemplate { get; set; }
    public DataTemplate? SystemTemplate { get; set; }
    public DataTemplate? RetryWaitTemplate { get; set; }
    public DataTemplate? ThinkingTemplate { get; set; }

    protected override DataTemplate? SelectTemplateCore(object item) => item switch
    {
        UserChatMessage => UserTemplate,
        AssistantChatMessage => AssistantTemplate,
        StepCardMessage => StepTemplate,
        SystemChatMessage => SystemTemplate,
        RetryWaitChatMessage => RetryWaitTemplate,
        ThinkingChatMessage => ThinkingTemplate,
        _ => null,
    };
}
