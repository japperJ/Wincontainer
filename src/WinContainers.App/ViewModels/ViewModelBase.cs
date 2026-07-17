using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;
using WinContainers_App;

namespace WinContainers_App.ViewModels;

public abstract class ViewModelBase : ObservableObject
{
    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        if (!App.DispatcherQueue.HasThreadAccess)
            App.DispatcherQueue.TryEnqueue(() => base.OnPropertyChanged(e));
        else
            base.OnPropertyChanged(e);
    }
}
