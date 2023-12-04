using System;
using System.Windows.Forms;

namespace AE.PID.Views;

public partial class ProgressBar : Form
{
    public ProgressBar()
    {
        InitializeComponent();
    }

    private void Form1_Load(object sender, EventArgs e)
    {
    }

    public void SetValue(int value)
    {
        progressBar1.Value = value;
    }
}