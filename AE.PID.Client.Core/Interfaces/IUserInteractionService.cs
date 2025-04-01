using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace AE.PID.Client.Core;

public interface IUserInteractionService
{
    void Show<TViewModel>(TViewModel vm, IntPtr? parent = null, Action? onClosed = null)
        where TViewModel : INotifyPropertyChanged;

    Task<TResult> ShowDialog<TViewModel, TResult>(TViewModel vm, IntPtr? parent = null)
        where TViewModel : INotifyPropertyChanged;

    Task<bool> SimpleDialog(string message, string? title);
}