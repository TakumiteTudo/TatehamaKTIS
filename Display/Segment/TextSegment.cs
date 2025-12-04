using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TatehamaKTIS.Display.Segment
{
    internal class TextSegment : DisplaySegmentData
    {
        public string Text { get; set; }
        public int ScalarX { get; set; } = 1;
        public int ScalarY { get; set; } = 1;
        public TextSegment(string name, int x, int y, Color color, Color basecolor, string text, int scalarX, int scalarY) : base(name, DisplaySegmentType.text, x, y, color, basecolor)
        {
            Text = text;
            ScalarX = scalarX;
            ScalarY = scalarY;
        }
        public override DisplaySegmentData DeepCopy()
        {
            return new TextSegment(name, x, y, color, baseColor, Text, ScalarX, ScalarY);
        }
        public override string ToString()
        {
            return $"{base.ToString()} \"{Text}\" at ({x},{y})";
        }
    }
}
