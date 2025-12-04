namespace TatehamaKTIS
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

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

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            pictureBox1 = new PictureBox();
            contextMenuStrip1 = new ContextMenuStrip(components);
            強制初期選択ToolStripMenuItem = new ToolStripMenuItem();
            強制再起動ToolStripMenuItem = new ToolStripMenuItem();
            モード切替SWToolStripMenuItem = new ToolStripMenuItem();
            timer1 = new System.Windows.Forms.Timer(components);
            現在画像をToolStripMenuItem = new ToolStripMenuItem();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            contextMenuStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // pictureBox1
            // 
            pictureBox1.BackColor = Color.Black;
            pictureBox1.ContextMenuStrip = contextMenuStrip1;
            pictureBox1.Location = new Point(0, 0);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(800, 600);
            pictureBox1.TabIndex = 0;
            pictureBox1.TabStop = false;
            pictureBox1.MouseDown += pictureBox1_MouseDown;
            pictureBox1.MouseUp += pictureBox1_MouseUp;
            // 
            // contextMenuStrip1
            // 
            contextMenuStrip1.Items.AddRange(new ToolStripItem[] { 現在画像をToolStripMenuItem, 強制初期選択ToolStripMenuItem, 強制再起動ToolStripMenuItem, モード切替SWToolStripMenuItem });
            contextMenuStrip1.Name = "contextMenuStrip1";
            contextMenuStrip1.Size = new Size(181, 114);
            // 
            // 強制初期選択ToolStripMenuItem
            // 
            強制初期選択ToolStripMenuItem.Name = "強制初期選択ToolStripMenuItem";
            強制初期選択ToolStripMenuItem.Size = new Size(180, 22);
            強制初期選択ToolStripMenuItem.Text = "強制初期選択";
            強制初期選択ToolStripMenuItem.Click += 強制初期選択ToolStripMenuItem_Click;
            // 
            // 強制再起動ToolStripMenuItem
            // 
            強制再起動ToolStripMenuItem.Name = "強制再起動ToolStripMenuItem";
            強制再起動ToolStripMenuItem.Size = new Size(180, 22);
            強制再起動ToolStripMenuItem.Text = "強制再起動";
            強制再起動ToolStripMenuItem.Click += 強制再起動ToolStripMenuItem_Click;
            // 
            // モード切替SWToolStripMenuItem
            // 
            モード切替SWToolStripMenuItem.Name = "モード切替SWToolStripMenuItem";
            モード切替SWToolStripMenuItem.Size = new Size(180, 22);
            モード切替SWToolStripMenuItem.Text = "モード切替SW";
            モード切替SWToolStripMenuItem.Click += モード切替SWToolStripMenuItem_Click;
            // 
            // timer1
            // 
            timer1.Enabled = true;
            timer1.Interval = 500;
            timer1.Tick += timer1_Tick;
            // 
            // 現在画像をToolStripMenuItem
            // 
            現在画像をToolStripMenuItem.Name = "現在画像をToolStripMenuItem";
            現在画像をToolStripMenuItem.Size = new Size(180, 22);
            現在画像をToolStripMenuItem.Text = "現在画像コピー";
            現在画像をToolStripMenuItem.Click += 現在画像コピーToolStripMenuItem_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(42, 52, 58);
            BackgroundImageLayout = ImageLayout.None;
            ClientSize = new Size(800, 600);
            Controls.Add(pictureBox1);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Name = "Form1";
            Text = "KTIS";
            TopMost = true;
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            contextMenuStrip1.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private PictureBox pictureBox1;
        private System.Windows.Forms.Timer timer1;
        private ContextMenuStrip contextMenuStrip1;
        private ToolStripMenuItem 強制初期選択ToolStripMenuItem;
        private ToolStripMenuItem 強制再起動ToolStripMenuItem;
        private ToolStripMenuItem モード切替SWToolStripMenuItem;
        private ToolStripMenuItem 現在画像をToolStripMenuItem;
    }
}
