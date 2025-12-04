using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TatehamaKTIS.Display.Segment
{
    internal abstract class DisplaySegmentData
    {
        public string name { get; set; }
        public DisplaySegmentType Type { get; }
        public int x { get; set; }
        public int y { get; set; }
        public Color color { get; set; }
        public Color baseColor { get; set; }
        public bool isVisible { get; set; } = true;

        protected DisplaySegmentData(string name, DisplaySegmentType type, int x, int y, Color color, Color basecolor)
        {
            this.name = name;
            Type = type;
            this.x = x;
            this.y = y;
            this.color = color;
            this.baseColor = basecolor;
        }

        public override string ToString()
        {
            return $"{Type}";
        }
    }
}
