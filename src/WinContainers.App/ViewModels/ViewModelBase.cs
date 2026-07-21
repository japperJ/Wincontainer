using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;
using WinContainers_App;

namespace WinContainers_App.ViewModels;

public abstract class ViewModelBase : ObservableObject
{
    private readonly DispatcherQueue? _dispatcherQueue;

    protected ViewModelBase()
    {
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread() ?? App.DispatcherQueue;
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        var dispatcherQueue = _dispatcherQueue;
        if (dispatcherQueue is null || dispatcherQueue.HasThreadAccess)
        {
            base.OnPropertyChanged(e);
            return;
        }

        if (!dispatcherQueue.TryEnqueue(() => base.OnPropertyChanged(e)))
        {
            // The dispatcher is shutting down, so the notification cannot be delivered safely.
        }
    }
}
