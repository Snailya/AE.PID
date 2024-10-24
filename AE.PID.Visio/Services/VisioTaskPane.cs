using System.Drawing;
using System.Windows.Forms;
using Avalonia.Interactivity;
using Avalonia.Win32.Interoperability;
using UserControl = System.Windows.Forms.UserControl;

namespace AE.PID.Visio.Services;

public class VisioTaskPane : UserControl
{
    private readonly WinFormsAvaloniaControlHost _avaloniaHost = new()
    {
        Dock = DockStyle.Fill,
    };

    public VisioTaskPane(Avalonia.Controls.UserControl control)
    {
        InitializeComponent();

        _avaloniaHost.Content = control;
    }

    private void InitializeComponent()
    {
        SuspendLayout();

        AutoSize = true;
        Size = new Size(460, 400);
        
        Controls.Add(_avaloniaHost);
        
        ResumeLayout(false);
    }
}