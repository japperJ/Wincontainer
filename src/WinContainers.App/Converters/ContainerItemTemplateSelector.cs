using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinContainers.Runtime.Models;

namespace WinContainers_App.Converters;

public sealed class ContainerItemTemplateSelector : DataTemplateSelector
{
    public DataTemplate? GroupHeaderTemplate { get; set; }
    public DataTemplate? ContainerTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item)
    {
        if (item is ContainerGroup)
            return GroupHeaderTemplate!;
        return ContainerTemplate!;
    }
}
