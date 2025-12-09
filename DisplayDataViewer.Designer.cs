namespace TatehamaKTIS.Display
{
    partial class DisplayDataViewer
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TreeView treeViewDisplayData;
        private System.Windows.Forms.Button buttonRefresh;

        /// <summary>
        ///  Clean up any resources being used.
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

        private void InitializeComponent()
        {
            this.treeViewDisplayData = new System.Windows.Forms.TreeView();
            this.buttonRefresh = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // treeViewDisplayData
            // 
            this.treeViewDisplayData.Location = new System.Drawing.Point(12, 12);
            this.treeViewDisplayData.Name = "treeViewDisplayData";
            this.treeViewDisplayData.Size = new System.Drawing.Size(360, 400);
            this.treeViewDisplayData.TabIndex = 0;
            // 
            // buttonRefresh
            // 
            this.buttonRefresh.Location = new System.Drawing.Point(12, 420);
            this.buttonRefresh.Name = "buttonRefresh";
            this.buttonRefresh.Size = new System.Drawing.Size(360, 30);
            this.buttonRefresh.TabIndex = 1;
            this.buttonRefresh.Text = "Refresh";
            this.buttonRefresh.UseVisualStyleBackColor = true;
            this.buttonRefresh.Click += new System.EventHandler(this.buttonRefresh_Click);
            // 
            // DisplayDataViewer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(384, 461);
            this.Controls.Add(this.buttonRefresh);
            this.Controls.Add(this.treeViewDisplayData);
            this.Name = "DisplayDataViewer";
            this.Text = "Display Data Viewer";
            this.ResumeLayout(false);
        }
    }
}