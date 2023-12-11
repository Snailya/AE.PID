namespace AE.PID
{
	partial class Ribbon : Microsoft.Office.Tools.Ribbon.RibbonBase
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		public Ribbon()
			: base(Globals.Factory.GetRibbonFactory())
		{
			InitializeComponent();
		}

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Component Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this.tab1 = this.Factory.CreateRibbonTab();
            this.grpEdit = this.Factory.CreateRibbonGroup();
            this.btnLibrary = this.Factory.CreateRibbonButton();
            this.btnSelectTool = this.Factory.CreateRibbonButton();
            this.btnSynchronizeToLibrary = this.Factory.CreateRibbonButton();
            this.grpExport = this.Factory.CreateRibbonGroup();
            this.btnExportAsBOM = this.Factory.CreateRibbonButton();
            this.btnFlatten = this.Factory.CreateRibbonButton();
            this.grpAbout = this.Factory.CreateRibbonGroup();
            this.btnSettings = this.Factory.CreateRibbonButton();
            this.grpDebug = this.Factory.CreateRibbonGroup();
            this.button1 = this.Factory.CreateRibbonButton();
            this.button3 = this.Factory.CreateRibbonButton();
            this.button2 = this.Factory.CreateRibbonButton();
            this.tab1.SuspendLayout();
            this.grpEdit.SuspendLayout();
            this.grpExport.SuspendLayout();
            this.grpAbout.SuspendLayout();
            this.grpDebug.SuspendLayout();
            this.SuspendLayout();
            // 
            // tab1
            // 
            this.tab1.ControlId.ControlIdType = Microsoft.Office.Tools.Ribbon.RibbonControlIdType.Office;
            this.tab1.Groups.Add(this.grpEdit);
            this.tab1.Groups.Add(this.grpExport);
            this.tab1.Groups.Add(this.grpAbout);
            this.tab1.Groups.Add(this.grpDebug);
            this.tab1.Label = "AE PID";
            this.tab1.Name = "tab1";
            // 
            // grpEdit
            // 
            this.grpEdit.Items.Add(this.btnLibrary);
            this.grpEdit.Items.Add(this.btnSelectTool);
            this.grpEdit.Items.Add(this.btnSynchronizeToLibrary);
            this.grpEdit.Label = "编辑";
            this.grpEdit.Name = "grpEdit";
            // 
            // btnLibrary
            // 
            this.btnLibrary.ControlSize = Microsoft.Office.Core.RibbonControlSize.RibbonControlSizeLarge;
            this.btnLibrary.Label = "库";
            this.btnLibrary.Name = "btnLibrary";
            this.btnLibrary.ScreenTip = "打开AE原理图库，请勿使用My Shapes中的模具库";
            this.btnLibrary.ShowImage = true;
            this.btnLibrary.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.btnOpenLibraries_Click);
            // 
            // btnSelectTool
            // 
            this.btnSelectTool.Label = "选择";
            this.btnSelectTool.Name = "btnSelectTool";
            this.btnSelectTool.ScreenTip = "选择文档中的设备对象";
            this.btnSelectTool.SuperTip = "支持ID选择与类型选择";
            this.btnSelectTool.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.btnSelectTool_Click);
            // 
            // btnSynchronizeToLibrary
            // 
            this.btnSynchronizeToLibrary.Label = "更新";
            this.btnSynchronizeToLibrary.Name = "btnSynchronizeToLibrary";
            this.btnSynchronizeToLibrary.ScreenTip = "更新图纸中的模具至最新版";
            this.btnSynchronizeToLibrary.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.btnUpdateDocumentMasters_Click);
            // 
            // grpExport
            // 
            this.grpExport.Items.Add(this.btnExportAsBOM);
            this.grpExport.Items.Add(this.btnFlatten);
            this.grpExport.Label = "导出";
            this.grpExport.Name = "grpExport";
            // 
            // btnExportAsBOM
            // 
            this.btnExportAsBOM.ControlSize = Microsoft.Office.Core.RibbonControlSize.RibbonControlSizeLarge;
            this.btnExportAsBOM.Label = "BOM";
            this.btnExportAsBOM.Name = "btnExportAsBOM";
            this.btnExportAsBOM.ScreenTip = "导出完整BOM表";
            this.btnExportAsBOM.ShowImage = true;
            this.btnExportAsBOM.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.btnExport_Click);
            // 
            // btnFlatten
            // 
            this.btnFlatten.ControlSize = Microsoft.Office.Core.RibbonControlSize.RibbonControlSizeLarge;
            this.btnFlatten.Label = "简化";
            this.btnFlatten.Name = "btnFlatten";
            this.btnFlatten.ScreenTip = "删除设备对象携带的属性（不可逆）";
            this.btnFlatten.ShowImage = true;
            this.btnFlatten.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.btnFlatten_Click);
            // 
            // grpAbout
            // 
            this.grpAbout.Items.Add(this.btnSettings);
            this.grpAbout.Label = "关于";
            this.grpAbout.Name = "grpAbout";
            // 
            // btnSettings
            // 
            this.btnSettings.Label = "设置";
            this.btnSettings.Name = "btnSettings";
            this.btnSettings.ScreenTip = "打开配置文件";
            this.btnSettings.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.btnSettings_Click);
            // 
            // grpDebug
            // 
            this.grpDebug.Items.Add(this.button1);
            this.grpDebug.Items.Add(this.button3);
            this.grpDebug.Label = "group1";
            this.grpDebug.Name = "grpDebug";
            // 
            // button1
            // 
            this.button1.Label = "button1";
            this.button1.Name = "button1";
            this.button1.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.btnTest);
            // 
            // button3
            // 
            this.button3.Label = "button1";
            this.button3.Name = "button3";
            this.button3.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.btnTest2);
            // 
            // button2
            // 
            this.button2.ControlSize = Microsoft.Office.Core.RibbonControlSize.RibbonControlSizeLarge;
            this.button2.Label = "删除敏感信息";
            this.button2.Name = "button2";
            this.button2.ScreenTip = "更新图纸中的形状对象为模具库中的最新版";
            this.button2.ShowImage = true;
            // 
            // Ribbon
            // 
            this.Name = "Ribbon";
            this.RibbonType = "Microsoft.Visio.Drawing";
            this.Tabs.Add(this.tab1);
            this.Close += new System.EventHandler(this.Ribbon_Close);
            this.Load += new Microsoft.Office.Tools.Ribbon.RibbonUIEventHandler(this.Ribbon_Load);
            this.tab1.ResumeLayout(false);
            this.tab1.PerformLayout();
            this.grpEdit.ResumeLayout(false);
            this.grpEdit.PerformLayout();
            this.grpExport.ResumeLayout(false);
            this.grpExport.PerformLayout();
            this.grpAbout.ResumeLayout(false);
            this.grpAbout.PerformLayout();
            this.grpDebug.ResumeLayout(false);
            this.grpDebug.PerformLayout();
            this.ResumeLayout(false);

		}

		#endregion

		internal Microsoft.Office.Tools.Ribbon.RibbonTab tab1;
		internal Microsoft.Office.Tools.Ribbon.RibbonGroup grpEdit;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton btnFlatten;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton btnExportAsBOM;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton button2;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton btnSelectTool;
        internal Microsoft.Office.Tools.Ribbon.RibbonGroup grpExport;
        internal Microsoft.Office.Tools.Ribbon.RibbonGroup grpAbout;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton btnLibrary;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton btnSettings;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton btnSynchronizeToLibrary;
        internal Microsoft.Office.Tools.Ribbon.RibbonGroup grpDebug;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton button1;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton button3;
    }

    partial class ThisRibbonCollection
	{
		internal Ribbon Ribbon
		{
			get { return this.GetRibbon<Ribbon>(); }
		}
	}
}
