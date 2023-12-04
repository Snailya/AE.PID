using System;
using System.Reactive;
using Microsoft.Office.Interop.Visio;

namespace AE.PID.Interfaces;

public interface IBackgroundTaskService
{
    public IObservable<Unit> UpdateAppObservable();
    public IObservable<Unit> UpdateLibrariesObservable();
    public IObservable<Unit> UpdateDocumentMastersObservable();

    public void InvokeUpdateDocumentMasters(IVDocument document);
}