using System.Reactive;
using AE.PID.Client.Core;
using AE.PID.Client.UI.Avalonia.Shared;
using AE.PID.Core;
using ReactiveUI;

namespace AE.PID.Client.UI.Avalonia;

public class SelectMaterialWindowViewModel : WindowViewModelBase
{
    private int _viewIndex;

    public int ViewIndex
    {
        get => _viewIndex;
        set => this.RaiseAndSetIfChanged(ref _viewIndex, value);
    }

    public StandardMaterialViewModel StandardMaterials { get; set; }
    public RecommendMaterialViewModel RecommendMaterials { get; set; }

    #region -- Commands --

    public ReactiveCommand<MaterialViewModel?, MaterialViewModel?> Confirm { get; } =
        ReactiveCommand.Create<MaterialViewModel?, MaterialViewModel?>(material => material);

    public ReactiveCommand<Unit, Unit> Cancel { get; } = ReactiveCommand.Create(() => { });

    #endregion

    #region -- Constructors --

    public SelectMaterialWindowViewModel(NotificationHelper notificationHelper,
        IMaterialService materialService, MaterialLocationContext context
    ) : base(notificationHelper,
        NotificationHelper.Routes.SelectMaterial)
    {
        StandardMaterials = new StandardMaterialViewModel(notificationHelper, materialService, context);
        RecommendMaterials = new RecommendMaterialViewModel(notificationHelper, materialService, context);

        this.WhenAnyObservable(x => x.StandardMaterials.Confirm, x => x.RecommendMaterials.Confirm)
            .InvokeCommand(Confirm);
        this.WhenAnyObservable(x => x.StandardMaterials.Cancel, x => x.RecommendMaterials.Cancel)
            .InvokeCommand(Cancel);
    }

    #endregion
}