using TatehamaKTIS.Display;

namespace TatehamaKTIS
{
    public partial class Form1 : Form
    {
        DisplayManager displayManager;
        public Form1()
        {
            InitializeComponent();
            displayManager = new DisplayManager();
            displayManager.displayAction = (bmp) =>
            {
                if (InvokeRequired)
                {
                    Invoke(new Action(() =>
                    {
                        pictureBox1.Image = bmp;
                    }));
                }
                else
                {
                    pictureBox1.Image = bmp;
                }
            };
            displayManager.DisplayUpdate();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            this.Text = "TIMS";
            displayManager.DisplayUpdate();
        }

        private void pictureBox1_MouseDown(object sender, EventArgs e)
        {
            if (sender is PictureBox pictureBox && e is MouseEventArgs mouseEventArgs)
            {
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
    }
}
