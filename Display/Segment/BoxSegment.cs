using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace TatehamaKTIS.Display.Segment
{
    internal class BoxSegment : DisplaySegmentData
    {
        public int sizeX { get; set; }
        public int sizeY { get; set; }

        public BoxSegment(string name, int x, int y, Color color, Color basecolor, int sizeX, int sizeY)
            : base(name, DisplaySegmentType.box, x, y, color, basecolor)
        {
            this.sizeX = sizeX;
            this.sizeY = sizeY;
        }

        public override DisplaySegmentData DeepCopy()
        {
            return new BoxSegment(name, x, y, color, baseColor, sizeX, sizeY);
        }

        public override string ToString()
        {
            return $"{base.ToString()} Box at ({x},{y}) with size ({sizeX}x{sizeY})";
        }
    }
}
