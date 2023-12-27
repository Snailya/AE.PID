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
            this.grpExport = this.Factory.CreateRibbonGroup();
            this.grpAbout = this.Factory.CreateRibbonGroup();
            this.btnLibrary = this.Factory.CreateRibbonButton();
            this.btnInitialize = this.Factory.CreateRibbonButton();
            this.btnSelectTool = this.Factory.CreateRibbonButton();
            this.btnSynchronizeToLibrary = this.Factory.CreateRibbonButton();
            this.btnExportAsBOM = this.Factory.CreateRibbonButton();
            this.btnFlatten = this.Factory.CreateRibbonButton();
            this.btnSettings = this.Factory.CreateRibbonButton();
            this.button2 = this.Factory.CreateRibbonButton();
            this.tab1.SuspendLayout();
            this.grpEdit.SuspendLayout();
            this.grpExport.SuspendLayout();
            this.grpAbout.SuspendLayout();
            this.SuspendLayout();
            // 
            // tab1
            // 
            this.tab1.ControlId.ControlIdType = Microsoft.Office.Tools.Ribbon.RibbonControlIdType.Office;
            this.tab1.Groups.Add(this.grpEdit);
            this.tab1.Groups.Add(this.grpExport);
            this.tab1.Groups.Add(this.grpAbout);
            this.tab1.Label = "AE PID";
            this.tab1.Name = "tab1";
            // 
            // grpEdit
            // 
            this.grpEdit.Items.Add(this.btnLibrary);
            this.grpEdit.Items.Add(this.btnInitialize);
            this.grpEdit.Items.Add(this.btnSelectTool);
            this.grpEdit.Items.Add(this.btnSynchronizeToLibrary);
            this.grpEdit.Label = "编辑";
            this.grpEdit.Name = "grpEdit";
            // 
            // grpExport
            // 
            this.grpExport.Items.Add(this.btnExportAsBOM);
            this.grpExport.Items.Add(this.btnFlatten);
            this.grpExport.Label = "导出";
            this.grpExport.Name = "grpExport";
            // 
            // grpAbout
            // 
            this.grpAbout.Items.Add(this.btnSettings);
            this.grpAbout.Label = "关于";
            this.grpAbout.Name = "grpAbout";
            // 
            // btnLibrary
            // 
            this.btnLibrary.ControlSize = Microsoft.Office.Core.RibbonControlSize.RibbonControlSizeLarge;
            this.btnLibrary.Image = global::AE.PID.Properties.Resources.library_32x32;
            this.btnLibrary.Label = "库";
            this.btnLibrary.Name = "btnLibrary";
            this.btnLibrary.ScreenTip = "打开AE原理图库，请勿使用My Shapes中的模具库";
            this.btnLibrary.ShowImage = true;
            this.btnLibrary.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.btnOpenLibraries_Click);
            // 
            // btnInitialize
            // 
            this.btnInitialize.Image = global::AE.PID.Properties.Resources.format_16x16;
            this.btnInitialize.Label = "初始化";
            this.btnInitialize.Name = "btnInitialize";
            this.btnInitialize.ScreenTip = "设置文档的字体和网格";
            this.btnInitialize.ShowImage = true;
            this.btnInitialize.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.btnInitialize_Click);
            // 
            // btnSelectTool
            // 
            this.btnSelectTool.Image = global::AE.PID.Properties.Resources.select_16x16;
            this.btnSelectTool.Label = "选择";
            this.btnSelectTool.Name = "btnSelectTool";
            this.btnSelectTool.ScreenTip = "选择文档中的设备对象";
            this.btnSelectTool.ShowImage = true;
            this.btnSelectTool.SuperTip = "支持ID选择与类型选择";
            this.btnSelectTool.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.btnSelectTool_Click);
            // 
            // btnSynchronizeToLibrary
            // 
            this.btnSynchronizeToLibrary.Image = global::AE.PID.Properties.Resources.synchronize_16x16;
            this.btnSynchronizeToLibrary.Label = "更新";
            this.btnSynchronizeToLibrary.Name = "btnSynchronizeToLibrary";
            this.btnSynchronizeToLibrary.ScreenTip = "更新图纸中的模具至最新版";
            this.btnSynchronizeToLibrary.ShowImage = true;
            this.btnSynchronizeToLibrary.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.btnUpdateDocumentMasters_Click);
            // 
            // btnExportAsBOM
            // 
            this.btnExportAsBOM.ControlSize = Microsoft.Office.Core.RibbonControlSize.RibbonControlSizeLarge;
            this.btnExportAsBOM.Image = global::AE.PID.Properties.Resources.bom_32x32;
            this.btnExportAsBOM.Label = "BOM";
            this.btnExportAsBOM.Name = "btnExportAsBOM";
            this.btnExportAsBOM.ScreenTip = "导出完整BOM表";
            this.btnExportAsBOM.ShowImage = true;
            this.btnExportAsBOM.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.btnExport_Click);
            // 
            // btnFlatten
            // 
            this.btnFlatten.Image = global::AE.PID.Properties.Resources.compress_16x16;
            this.btnFlatten.Label = "简化";
            this.btnFlatten.Name = "btnFlatten";
            this.btnFlatten.ScreenTip = "删除设备对象携带的属性（不可逆）";
            this.btnFlatten.ShowImage = true;
            this.btnFlatten.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.btnFlatten_Click);
            // 
            // btnSettings
            // 
            this.btnSettings.ControlSize = Microsoft.Office.Core.RibbonControlSize.RibbonControlSizeLarge;
            this.btnSettings.Image = global::AE.PID.Properties.Resources.settings_32x32;
            this.btnSettings.Label = "设置";
            this.btnSettings.Name = "btnSettings";
            this.btnSettings.ScreenTip = "打开配置文件";
            this.btnSettings.ShowImage = true;
            this.btnSettings.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.btnSettings_Click);
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
        internal Microsoft.Office.Tools.Ribbon.RibbonButton btnInitialize;
    }

    partial class ThisRibbonCollection
	{
		internal Ribbon Ribbon
		{
			get { return this.GetRibbon<Ribbon>(); }
		}
	}
}
