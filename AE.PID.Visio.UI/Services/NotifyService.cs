﻿using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Avalonia.Controls.Notifications;

namespace AE.PID.Visio.UI.Avalonia.Services;

public class NotifyService
{
    private readonly BehaviorSubject<NotificationExt?> _subject = new(default);

    public IObservable<NotificationExt?> Notifications => _subject.AsObservable();

    public void Error(string? title = null, string? message = null, string? route = null)
    {
        var notification = new NotificationExt
        {
            Route = route,
            Title = title,
            Message = message,
            Type = NotificationType.Error
        };
        _subject.OnNext(notification);
    }

    public void Warning(string? title = null, string? message = null, string? route = null)
    {
        var notification = new NotificationExt
        {
            Route = route,
            Title = title,
            Message = message,
            Type = NotificationType.Warning
        };
        _subject.OnNext(notification);
    }

    public abstract class Routes
    {
        public static string ProjectExplorer = nameof(ProjectExplorer);
        public static string SelectProject = nameof(SelectProject);
    }

    public class NotificationExt : Notification
    {
        public string? Route { get; set; }
    }
}