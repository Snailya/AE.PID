using System;
using System.Linq;
using System.Windows.Forms;
using AE.PID.Client.Core;
using AE.PID.Client.Core.VisioExt;
using AE.PID.Client.UI.Avalonia.VisioExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Office.Core;
using Microsoft.Office.Interop.Visio;
using Splat;
using Path = System.IO.Path;

namespace AE.PID.Client.VisioAddIn;

internal sealed class UpdateDocumentCommand : RibbonCommandBase
{
    public override string Id { get; } = nameof(UpdateDocumentCommand);

    public override async void Execute(IRibbonControl control)
    {
        var doc = Globals.ThisAddIn.Application.ActiveDocument;

        //remove hidden information to reduce size
        doc.RemoveHiddenInformation((int)VisRemoveHiddenInfoItems.visRHIMasters);

        var service = ThisAddIn.Services.GetRequiredService<IDocumentUpdateService>();

        // 2024.12.9更新：在更新时，一部分用户希望更新所有的模具，但一部分用户希望保留自己修改后的模具，此处弹框要求用户选择哪些模具需要被更新
        var mastersNeedUpdate = service.GetObsoleteMasters(Globals.ThisAddIn.Application.ActiveDocument)
            .Select(x =>
                new DocumentMasterViewModel(x)
                {
                    IsSelected = true
                })
            .ToArray();
        // 2025.02.05： 用户如果点击了取消按钮，则返回null，用户如果点击了确定按钮，则获得待更新的清单
        var ui = ThisAddIn.Services.GetRequiredService<IUserInteractionService>();
        var mastersToUpdate = await ui
            .ShowDialog<ConfirmUpdateDocumentWindowViewModel, VisioMaster[]?>(
                new ConfirmUpdateDocumentWindowViewModel(mastersNeedUpdate), ThisAddIn.GetApplicationHandle());

        // 如果用户取消了，或者待更新清单为空，则取消操作
        if (mastersToUpdate == null || !mastersToUpdate.Any()) return;


        // 文档可能有两种情况：
        // 1. 文档是新建的，此时没有FullName，但是这种情况不应该发生，因为没有检查一个新建的文档，因为该文档随时可被丢弃。
        // 2. 文档曾经被保存过，此时才有检查更新的必要
        // 所以此处假定文档一定有fullname。
        var filePath = doc.FullName;
        doc.Close();

        try
        {
            // do update
            await service.UpdateAsync(filePath, mastersToUpdate);

            // 2025.02.06: 此处增加一个更新成功提示
            MessageBox.Show("更新成功", "文档更新");
        }
        catch (DocumentFailedToUpdateException e)
        {
            MessageBox.Show($"文档更新遇到了些问题，但是很难说是什么问题，请联系李婧雅。错误信息：{e.Message}", "文档更新");
        }
        catch (Exception e)
        {
            MessageBox.Show($"更新失败，{e.Message}", "文档更新");
        }

        // reopen after updated
        var document = Globals.ThisAddIn.Application.Documents.Open(filePath);
    }

    public override bool CanExecute(IRibbonControl control)
    {
        if (Globals.ThisAddIn.Application.ActiveDocument == null) return false;

        // check if the document is the visDrawing, not the stencil or other type
        if (Globals.ThisAddIn.Application.ActiveDocument.Type != VisDocumentTypes.visTypeDrawing) return false;

        // 如果文档从来没有被存储过，则不检查
        if (!Path.IsPathRooted(Globals.ThisAddIn.Application.ActiveDocument.FullName)) return false;

        // check if the AE style exists, if the AE style exists, means this is a target drawing.
        if (Globals.ThisAddIn.Application.ActiveDocument.Styles.OfType<IVStyle>()
                .SingleOrDefault(x => x.Name == StyleDict.Normal) ==
            null) return false;

        LogHost.Default.Info(
            $"Checking if the masters in {Globals.ThisAddIn.Application.ActiveDocument.FullName} is up to date...");

        // check if the version is out of date

        var documentUpdateService = ThisAddIn.Services.GetRequiredService<IDocumentUpdateService>();

        var isObsolete = documentUpdateService.IsObsolete(Globals.ThisAddIn.Application.ActiveDocument);
        LogHost.Default.Info(isObsolete
            ? $"{Globals.ThisAddIn.Application.ActiveDocument.FullName} need update."
            : $"{Globals.ThisAddIn.Application.ActiveDocument.FullName} is up to date.");

        return isObsolete;
    }

    public override string GetLabel(IRibbonControl control)
    {
        return "更新";
    }
}