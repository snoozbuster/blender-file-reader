namespace BlenderFileBrowser
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if(disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.TreeNode treeNode1 = new System.Windows.Forms.TreeNode("Open a file to view it here.");
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.fileTree = new System.Windows.Forms.TreeView();
            this.blendFileOpenDialog = new System.Windows.Forms.OpenFileDialog();
            this.fieldNameTextBox = new System.Windows.Forms.TextBox();
            this.iFieldBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.fieldNameLabel = new System.Windows.Forms.Label();
            this.fullyQualifiedNameLabel = new System.Windows.Forms.Label();
            this.fullyQualifiedNameTextBox = new System.Windows.Forms.TextBox();
            this.fieldTypeLabel = new System.Windows.Forms.Label();
            this.fieldTypeNameTextBox = new System.Windows.Forms.TextBox();
            this.typeSizeLabel = new System.Windows.Forms.Label();
            this.typeSizeTextBox = new System.Windows.Forms.TextBox();
            this.lengthLabel = new System.Windows.Forms.Label();
            this.lengthTextBox = new System.Windows.Forms.TextBox();
            this.totalLabel = new System.Windows.Forms.Label();
            this.totalTextBox = new System.Windows.Forms.TextBox();
            this.flagGroupBox = new System.Windows.Forms.GroupBox();
            this.is2DArrayCheckBox = new System.Windows.Forms.CheckBox();
            this.isArrayCheckBox = new System.Windows.Forms.CheckBox();
            this.pointerToPointerCheckBox = new System.Windows.Forms.CheckBox();
            this.isPointerCheckBox = new System.Windows.Forms.CheckBox();
            this.isPrimitiveCheckBox = new System.Windows.Forms.CheckBox();
            this.valueTextBox = new System.Windows.Forms.TextBox();
            this.valueLinkLabel = new System.Windows.Forms.LinkLabel();
            this.pointedToValueTreeView = new System.Windows.Forms.TreeView();
            this.pointedValueLabel = new System.Windows.Forms.Label();
            this.commentsLabel = new System.Windows.Forms.Label();
            this.commentsBox = new System.Windows.Forms.TextBox();
            this.menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.iFieldBindingSource)).BeginInit();
            this.flagGroupBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(763, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openToolStripMenuItem,
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // openToolStripMenuItem
            // 
            this.openToolStripMenuItem.Name = "openToolStripMenuItem";
            this.openToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
            this.openToolStripMenuItem.Size = new System.Drawing.Size(146, 22);
            this.openToolStripMenuItem.Text = "&Open";
            this.openToolStripMenuItem.Click += new System.EventHandler(this.openToolStripMenuItem_Click);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(146, 22);
            this.exitToolStripMenuItem.Text = "E&xit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // fileTree
            // 
            this.fileTree.Location = new System.Drawing.Point(12, 27);
            this.fileTree.Name = "fileTree";
            treeNode1.Name = "defaultNode";
            treeNode1.Text = "Open a file to view it here.";
            this.fileTree.Nodes.AddRange(new System.Windows.Forms.TreeNode[] {
            treeNode1});
            this.fileTree.ShowRootLines = false;
            this.fileTree.Size = new System.Drawing.Size(302, 338);
            this.fileTree.TabIndex = 1;
            // 
            // blendFileOpenDialog
            // 
            this.blendFileOpenDialog.DefaultExt = "blend";
            this.blendFileOpenDialog.Filter = "Blender files|*.blend;*.blend1;*.blend2";
            // 
            // fieldNameTextBox
            // 
            this.fieldNameTextBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.iFieldBindingSource, "Name", true));
            this.fieldNameTextBox.Location = new System.Drawing.Point(321, 47);
            this.fieldNameTextBox.Name = "fieldNameTextBox";
            this.fieldNameTextBox.ReadOnly = true;
            this.fieldNameTextBox.Size = new System.Drawing.Size(124, 20);
            this.fieldNameTextBox.TabIndex = 2;
            // 
            // iFieldBindingSource
            // 
            this.iFieldBindingSource.DataSource = typeof(BlenderFileReader.IField);
            // 
            // fieldNameLabel
            // 
            this.fieldNameLabel.AutoSize = true;
            this.fieldNameLabel.Location = new System.Drawing.Point(321, 28);
            this.fieldNameLabel.Name = "fieldNameLabel";
            this.fieldNameLabel.Size = new System.Drawing.Size(38, 13);
            this.fieldNameLabel.TabIndex = 3;
            this.fieldNameLabel.Text = "Name:";
            // 
            // fullyQualifiedNameLabel
            // 
            this.fullyQualifiedNameLabel.AutoSize = true;
            this.fullyQualifiedNameLabel.Location = new System.Drawing.Point(321, 73);
            this.fullyQualifiedNameLabel.Name = "fullyQualifiedNameLabel";
            this.fullyQualifiedNameLabel.Size = new System.Drawing.Size(106, 13);
            this.fullyQualifiedNameLabel.TabIndex = 5;
            this.fullyQualifiedNameLabel.Text = "Fully Qualified Name:";
            // 
            // fullyQualifiedNameTextBox
            // 
            this.fullyQualifiedNameTextBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.iFieldBindingSource, "FullyQualifiedName", true));
            this.fullyQualifiedNameTextBox.Location = new System.Drawing.Point(321, 92);
            this.fullyQualifiedNameTextBox.Name = "fullyQualifiedNameTextBox";
            this.fullyQualifiedNameTextBox.ReadOnly = true;
            this.fullyQualifiedNameTextBox.Size = new System.Drawing.Size(124, 20);
            this.fullyQualifiedNameTextBox.TabIndex = 4;
            // 
            // fieldTypeLabel
            // 
            this.fieldTypeLabel.AutoSize = true;
            this.fieldTypeLabel.Location = new System.Drawing.Point(455, 28);
            this.fieldTypeLabel.Name = "fieldTypeLabel";
            this.fieldTypeLabel.Size = new System.Drawing.Size(65, 13);
            this.fieldTypeLabel.TabIndex = 7;
            this.fieldTypeLabel.Text = "Type Name:";
            // 
            // fieldTypeNameTextBox
            // 
            this.fieldTypeNameTextBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.iFieldBindingSource, "TypeName", true));
            this.fieldTypeNameTextBox.Location = new System.Drawing.Point(455, 47);
            this.fieldTypeNameTextBox.Name = "fieldTypeNameTextBox";
            this.fieldTypeNameTextBox.ReadOnly = true;
            this.fieldTypeNameTextBox.Size = new System.Drawing.Size(100, 20);
            this.fieldTypeNameTextBox.TabIndex = 3;
            // 
            // typeSizeLabel
            // 
            this.typeSizeLabel.AutoSize = true;
            this.typeSizeLabel.Location = new System.Drawing.Point(455, 73);
            this.typeSizeLabel.Name = "typeSizeLabel";
            this.typeSizeLabel.Size = new System.Drawing.Size(30, 13);
            this.typeSizeLabel.TabIndex = 9;
            this.typeSizeLabel.Text = "Size:";
            // 
            // typeSizeTextBox
            // 
            this.typeSizeTextBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.iFieldBindingSource, "Size", true));
            this.typeSizeTextBox.Location = new System.Drawing.Point(455, 92);
            this.typeSizeTextBox.Name = "typeSizeTextBox";
            this.typeSizeTextBox.ReadOnly = true;
            this.typeSizeTextBox.Size = new System.Drawing.Size(30, 20);
            this.typeSizeTextBox.TabIndex = 5;
            // 
            // lengthLabel
            // 
            this.lengthLabel.AutoSize = true;
            this.lengthLabel.Location = new System.Drawing.Point(485, 73);
            this.lengthLabel.Name = "lengthLabel";
            this.lengthLabel.Size = new System.Drawing.Size(43, 13);
            this.lengthLabel.TabIndex = 11;
            this.lengthLabel.Text = "Length:";
            // 
            // lengthTextBox
            // 
            this.lengthTextBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.iFieldBindingSource, "Length", true));
            this.lengthTextBox.Location = new System.Drawing.Point(490, 92);
            this.lengthTextBox.Name = "lengthTextBox";
            this.lengthTextBox.ReadOnly = true;
            this.lengthTextBox.Size = new System.Drawing.Size(30, 20);
            this.lengthTextBox.TabIndex = 6;
            // 
            // totalLabel
            // 
            this.totalLabel.AutoSize = true;
            this.totalLabel.Location = new System.Drawing.Point(525, 73);
            this.totalLabel.Name = "totalLabel";
            this.totalLabel.Size = new System.Drawing.Size(34, 13);
            this.totalLabel.TabIndex = 13;
            this.totalLabel.Text = "Total:";
            // 
            // totalTextBox
            // 
            this.totalTextBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.iFieldBindingSource, "Length", true));
            this.totalTextBox.Location = new System.Drawing.Point(526, 92);
            this.totalTextBox.Name = "totalTextBox";
            this.totalTextBox.ReadOnly = true;
            this.totalTextBox.Size = new System.Drawing.Size(30, 20);
            this.totalTextBox.TabIndex = 7;
            // 
            // flagGroupBox
            // 
            this.flagGroupBox.Controls.Add(this.is2DArrayCheckBox);
            this.flagGroupBox.Controls.Add(this.isArrayCheckBox);
            this.flagGroupBox.Controls.Add(this.pointerToPointerCheckBox);
            this.flagGroupBox.Controls.Add(this.isPointerCheckBox);
            this.flagGroupBox.Controls.Add(this.isPrimitiveCheckBox);
            this.flagGroupBox.Location = new System.Drawing.Point(565, 28);
            this.flagGroupBox.Name = "flagGroupBox";
            this.flagGroupBox.Size = new System.Drawing.Size(186, 84);
            this.flagGroupBox.TabIndex = 19;
            this.flagGroupBox.TabStop = false;
            this.flagGroupBox.Text = "Flags";
            // 
            // is2DArrayCheckBox
            // 
            this.is2DArrayCheckBox.AutoSize = true;
            this.is2DArrayCheckBox.DataBindings.Add(new System.Windows.Forms.Binding("Checked", this.iFieldBindingSource, "Is2DArray", true));
            this.is2DArrayCheckBox.Enabled = false;
            this.is2DArrayCheckBox.Location = new System.Drawing.Point(113, 38);
            this.is2DArrayCheckBox.Name = "is2DArrayCheckBox";
            this.is2DArrayCheckBox.Size = new System.Drawing.Size(67, 17);
            this.is2DArrayCheckBox.TabIndex = 23;
            this.is2DArrayCheckBox.Text = "2D Array";
            this.is2DArrayCheckBox.UseVisualStyleBackColor = true;
            // 
            // isArrayCheckBox
            // 
            this.isArrayCheckBox.AutoSize = true;
            this.isArrayCheckBox.DataBindings.Add(new System.Windows.Forms.Binding("Checked", this.iFieldBindingSource, "IsArray", true));
            this.isArrayCheckBox.Enabled = false;
            this.isArrayCheckBox.Location = new System.Drawing.Point(113, 17);
            this.isArrayCheckBox.Name = "isArrayCheckBox";
            this.isArrayCheckBox.Size = new System.Drawing.Size(50, 17);
            this.isArrayCheckBox.TabIndex = 22;
            this.isArrayCheckBox.Text = "Array";
            this.isArrayCheckBox.UseVisualStyleBackColor = true;
            // 
            // pointerToPointerCheckBox
            // 
            this.pointerToPointerCheckBox.AutoSize = true;
            this.pointerToPointerCheckBox.DataBindings.Add(new System.Windows.Forms.Binding("Checked", this.iFieldBindingSource, "IsPointerToPointer", true));
            this.pointerToPointerCheckBox.Enabled = false;
            this.pointerToPointerCheckBox.Location = new System.Drawing.Point(10, 59);
            this.pointerToPointerCheckBox.Name = "pointerToPointerCheckBox";
            this.pointerToPointerCheckBox.Size = new System.Drawing.Size(111, 17);
            this.pointerToPointerCheckBox.TabIndex = 21;
            this.pointerToPointerCheckBox.Text = "Pointer To Pointer";
            this.pointerToPointerCheckBox.UseVisualStyleBackColor = true;
            // 
            // isPointerCheckBox
            // 
            this.isPointerCheckBox.AutoSize = true;
            this.isPointerCheckBox.DataBindings.Add(new System.Windows.Forms.Binding("Checked", this.iFieldBindingSource, "IsPointer", true));
            this.isPointerCheckBox.Enabled = false;
            this.isPointerCheckBox.Location = new System.Drawing.Point(10, 38);
            this.isPointerCheckBox.Name = "isPointerCheckBox";
            this.isPointerCheckBox.Size = new System.Drawing.Size(59, 17);
            this.isPointerCheckBox.TabIndex = 20;
            this.isPointerCheckBox.Text = "Pointer";
            this.isPointerCheckBox.UseVisualStyleBackColor = true;
            // 
            // isPrimitiveCheckBox
            // 
            this.isPrimitiveCheckBox.AutoSize = true;
            this.isPrimitiveCheckBox.DataBindings.Add(new System.Windows.Forms.Binding("Checked", this.iFieldBindingSource, "IsPrimitive", true));
            this.isPrimitiveCheckBox.Enabled = false;
            this.isPrimitiveCheckBox.Location = new System.Drawing.Point(10, 17);
            this.isPrimitiveCheckBox.Name = "isPrimitiveCheckBox";
            this.isPrimitiveCheckBox.Size = new System.Drawing.Size(65, 17);
            this.isPrimitiveCheckBox.TabIndex = 19;
            this.isPrimitiveCheckBox.Text = "Primitive";
            this.isPrimitiveCheckBox.UseVisualStyleBackColor = true;
            // 
            // valueTextBox
            // 
            this.valueTextBox.Location = new System.Drawing.Point(321, 136);
            this.valueTextBox.Name = "valueTextBox";
            this.valueTextBox.ReadOnly = true;
            this.valueTextBox.Size = new System.Drawing.Size(430, 20);
            this.valueTextBox.TabIndex = 9;
            // 
            // valueLinkLabel
            // 
            this.valueLinkLabel.AutoSize = true;
            this.valueLinkLabel.DisabledLinkColor = System.Drawing.SystemColors.ControlText;
            this.valueLinkLabel.LinkBehavior = System.Windows.Forms.LinkBehavior.NeverUnderline;
            this.valueLinkLabel.Location = new System.Drawing.Point(321, 119);
            this.valueLinkLabel.Name = "valueLinkLabel";
            this.valueLinkLabel.Size = new System.Drawing.Size(37, 13);
            this.valueLinkLabel.TabIndex = 8;
            this.valueLinkLabel.TabStop = true;
            this.valueLinkLabel.Text = "Value:";
            this.valueLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.valueLinkLabel_LinkClicked);
            // 
            // pointedToValueTreeView
            // 
            this.pointedToValueTreeView.Enabled = false;
            this.pointedToValueTreeView.Location = new System.Drawing.Point(320, 180);
            this.pointedToValueTreeView.Name = "pointedToValueTreeView";
            this.pointedToValueTreeView.Size = new System.Drawing.Size(288, 185);
            this.pointedToValueTreeView.TabIndex = 10;
            // 
            // pointedValueLabel
            // 
            this.pointedValueLabel.AutoSize = true;
            this.pointedValueLabel.Location = new System.Drawing.Point(321, 162);
            this.pointedValueLabel.Name = "pointedValueLabel";
            this.pointedValueLabel.Size = new System.Drawing.Size(88, 13);
            this.pointedValueLabel.TabIndex = 22;
            this.pointedValueLabel.Text = "Pointed to Value:";
            // 
            // commentsLabel
            // 
            this.commentsLabel.AutoSize = true;
            this.commentsLabel.Location = new System.Drawing.Point(614, 162);
            this.commentsLabel.Name = "commentsLabel";
            this.commentsLabel.Size = new System.Drawing.Size(59, 13);
            this.commentsLabel.TabIndex = 23;
            this.commentsLabel.Text = "Comments:";
            // 
            // commentsBox
            // 
            this.commentsBox.Location = new System.Drawing.Point(614, 178);
            this.commentsBox.Multiline = true;
            this.commentsBox.Name = "commentsBox";
            this.commentsBox.ReadOnly = true;
            this.commentsBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.commentsBox.Size = new System.Drawing.Size(137, 187);
            this.commentsBox.TabIndex = 11;
            this.commentsBox.Text = resources.GetString("commentsBox.Text");
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(763, 377);
            this.Controls.Add(this.commentsBox);
            this.Controls.Add(this.commentsLabel);
            this.Controls.Add(this.pointedValueLabel);
            this.Controls.Add(this.pointedToValueTreeView);
            this.Controls.Add(this.valueLinkLabel);
            this.Controls.Add(this.valueTextBox);
            this.Controls.Add(this.flagGroupBox);
            this.Controls.Add(this.totalLabel);
            this.Controls.Add(this.totalTextBox);
            this.Controls.Add(this.lengthLabel);
            this.Controls.Add(this.lengthTextBox);
            this.Controls.Add(this.typeSizeLabel);
            this.Controls.Add(this.typeSizeTextBox);
            this.Controls.Add(this.fieldTypeLabel);
            this.Controls.Add(this.fieldTypeNameTextBox);
            this.Controls.Add(this.fullyQualifiedNameLabel);
            this.Controls.Add(this.fullyQualifiedNameTextBox);
            this.Controls.Add(this.fieldNameLabel);
            this.Controls.Add(this.fieldNameTextBox);
            this.Controls.Add(this.fileTree);
            this.Controls.Add(this.menuStrip1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MainMenuStrip = this.menuStrip1;
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Blender File Browser";
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.iFieldBindingSource)).EndInit();
            this.flagGroupBox.ResumeLayout(false);
            this.flagGroupBox.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.TreeView fileTree;
        private System.Windows.Forms.OpenFileDialog blendFileOpenDialog;
        private System.Windows.Forms.TextBox fieldNameTextBox;
        private System.Windows.Forms.Label fieldNameLabel;
        private System.Windows.Forms.Label fullyQualifiedNameLabel;
        private System.Windows.Forms.TextBox fullyQualifiedNameTextBox;
        private System.Windows.Forms.BindingSource iFieldBindingSource;
        private System.Windows.Forms.Label fieldTypeLabel;
        private System.Windows.Forms.TextBox fieldTypeNameTextBox;
        private System.Windows.Forms.Label typeSizeLabel;
        private System.Windows.Forms.TextBox typeSizeTextBox;
        private System.Windows.Forms.Label lengthLabel;
        private System.Windows.Forms.TextBox lengthTextBox;
        private System.Windows.Forms.Label totalLabel;
        private System.Windows.Forms.TextBox totalTextBox;
        private System.Windows.Forms.GroupBox flagGroupBox;
        private System.Windows.Forms.CheckBox is2DArrayCheckBox;
        private System.Windows.Forms.CheckBox isArrayCheckBox;
        private System.Windows.Forms.CheckBox pointerToPointerCheckBox;
        private System.Windows.Forms.CheckBox isPointerCheckBox;
        private System.Windows.Forms.CheckBox isPrimitiveCheckBox;
        private System.Windows.Forms.TextBox valueTextBox;
        private System.Windows.Forms.LinkLabel valueLinkLabel;
        private System.Windows.Forms.TreeView pointedToValueTreeView;
        private System.Windows.Forms.Label pointedValueLabel;
        private System.Windows.Forms.Label commentsLabel;
        private System.Windows.Forms.TextBox commentsBox;
    }
}

