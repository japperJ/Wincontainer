using CommunityToolkit.Mvvm.ComponentModel;

namespace WinContainers.Runtime.Models;

public sealed class FileEntryData : ObservableObject
{
    private string _name = "";
    private string _type = "file";
    private string _icon = "\uE996";
    private string _permissions = "";

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string Type
    {
        get => _type;
        set => SetProperty(ref _type, value);
    }

    public string Icon
    {
        get => _icon;
        set => SetProperty(ref _icon, value);
    }

    public string Permissions
    {
        get => _permissions;
        set => SetProperty(ref _permissions, value);
    }
}
