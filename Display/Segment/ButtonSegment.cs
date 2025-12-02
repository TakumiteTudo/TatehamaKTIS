using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TatehamaKTIS.Display.Segment
{
    internal class ButtonSegment : DisplaySegmentData
    {
        public string Text { get; set; }
        public int scalarX { get; set; }
        public int scalarY { get; set; }
        public int sizeX { get; set; }
        public int sizeY { get; set; }
        public string buttonColor { get; set; }
        public string groupname { get; set; }
        public ButtonType buttonType { get; set; }
        public List<Tuple<string, string>> functionList { get; set; }

        /// <summary>
        /// 選択状態
        /// </summary>
        public bool isChecked { get; set; }
        /// <summary>
        /// 点灯状態
        /// </summary>
        public int isLighting { get; set; }

        public DateTime originTime { get; set; }

        public ButtonSegment(string name, int x, int y, System.Drawing.Color color, System.Drawing.Color basecolor, string text, int scalarX, int scalarY, int sizeX, int sizeY, string buttonColor, string groupname, ButtonType buttonType, List<Tuple<string, string>> functionList) : base(name, DisplaySegmentType.button, x, y, color, basecolor)
        {
            Text = text;
            this.scalarX = scalarX;
            this.scalarY = scalarY;
            this.sizeX = sizeX;
            this.sizeY = sizeY;
            this.buttonColor = buttonColor;
            this.groupname = groupname;
            this.buttonType = buttonType;
            this.functionList = functionList;
            isChecked = false;
            isLighting = 0;
            originTime = DateTime.Now;
        }
    }

    enum ButtonType
    {
        /// <summary>
        /// チェックボックス
        /// </summary>
        checkbox,
        /// <summary>
        /// 処理実行
        /// </summary>
        function
    }
}
