using System.Diagnostics;
using TatehamaKTIS.Display;
using TatehamaKTIS.Display.Segment;

namespace TatehamaKTIS
{
    public partial class Form1 : Form
    {
        DisplayManager displayManager;
        bool nowtouch;
        public Form1()
        {
            InitializeComponent();
            displayManager = new DisplayManager(new Display.Rendering.BitmapDisplayRenderer());

            displayManager.displayAction = (img) =>
            {
                if (InvokeRequired)
                {
                    Invoke(new Action(() =>
                    {
                        pictureBox1.Image = img;
                    }));
                }
                else
                {
                    pictureBox1.Image = img;
                }
            };
            displayManager.DisplayUpdate();
            nowtouch = false;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            this.Text = "KTIS";
            displayManager.DisplayUpdateDiff();
        }

        private void pictureBox1_MouseDown(object sender, EventArgs e)
        {
            if (sender is PictureBox pictureBox && e is MouseEventArgs mouseEventArgs)
            {
                if (MouseButtons != MouseButtons.Left)
                {
                    nowtouch = false;
                    return;
                }
                nowtouch = true;
                // クリック位置を取得
                int clickX = mouseEventArgs.X;
                int clickY = mouseEventArgs.Y;

                // 画像のスケーリングを考慮して座標を変換
                if (pictureBox.Image != null)
                {
                    float scaleX = (float)pictureBox.Image.Width / pictureBox.Width;
                    float scaleY = (float)pictureBox.Image.Height / pictureBox.Height;

                    clickX = (int)(mouseEventArgs.X * scaleX);
                    clickY = (int)(mouseEventArgs.Y * scaleY);
                }

                // DisplayManager にクリック位置を渡す
                displayManager.DisplayTotchDown(clickX, clickY);
            }
        }

        private void pictureBox1_MouseUp(object sender, EventArgs e)
        {
            if (sender is PictureBox pictureBox && e is MouseEventArgs mouseEventArgs)
            {
                if (!nowtouch)
                {
                    return;
                }
                nowtouch = false;
                // クリック位置を取得
                int clickX = mouseEventArgs.X;
                int clickY = mouseEventArgs.Y;

                // 画像のスケーリングを考慮して座標を変換
                if (pictureBox.Image != null)
                {
                    float scaleX = (float)pictureBox.Image.Width / pictureBox.Width;
                    float scaleY = (float)pictureBox.Image.Height / pictureBox.Height;

                    clickX = (int)(mouseEventArgs.X * scaleX);
                    clickY = (int)(mouseEventArgs.Y * scaleY);
                }

                // DisplayManager にクリック位置を渡す
                displayManager.DisplayTotchUp(clickX, clickY);
            }
        }

        private void 強制初期選択ToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void 強制再起動ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            displayManager.SetDisplayType("Tatehama");
        }

        private void モード切替SWToolStripMenuItem_Click(object sender, EventArgs e)
        {
            displayManager.ModeSW();
        }

        private void 現在画像コピーToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (pictureBox1.Image != null)
            {
                // クリップボードに画像をコピー
                Clipboard.SetImage(pictureBox1.Image);
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }
    }
}
