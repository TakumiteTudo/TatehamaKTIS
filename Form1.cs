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
            displayManager.DisplayUpdate();
        }
    }
}
