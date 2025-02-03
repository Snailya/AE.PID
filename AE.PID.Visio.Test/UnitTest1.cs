using System.Runtime.InteropServices;
using FluentAssertions;
using Microsoft.Office.Interop.Visio;

namespace AE.PID.Visio.Test;

public class IntegrationTest : IDisposable
{
    private readonly Document _document;
    private readonly Document _equipmentStencil;
    private readonly Document _logicalStencil;
    private readonly Application _visioApp;

    public IntegrationTest()
    {
        // 初始化 Visio 应用程序
        _visioApp = new Application();

        // open stencil
        _equipmentStencil = _visioApp.Documents.OpenEx(@"C:\Users\lijin\Documents\我的形状\AE基础.vssx",
            (short)VisOpenSaveArgs.visOpenDocked);
        _logicalStencil = _visioApp.Documents.OpenEx(@"C:\Users\lijin\Documents\我的形状\AE逻辑.vssx",
            (short)VisOpenSaveArgs.visOpenDocked);

        var fileName = @"C:\Users\lijin\Desktop\Drawing1.vsdx";
        _document = _visioApp.Documents.Open(fileName);
    }

    public void Dispose()
    {
        _visioApp.Quit();
        Marshal.ReleaseComObject(_visioApp);
    }

    [Fact]
    public void Test1()
    {
        // create a new document
        var document = _visioApp.Documents.Add("");


        document.Close();
    }

    [Fact]
    public void GetVersion()
    {
        var count = _document.SolutionXMLElementCount;
        count.Should().NotBe(0);
    }
}