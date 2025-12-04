using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace TatehamaKTIS.Display.Segment
{
    internal class ImageSegment : DisplaySegmentData
    {
        public string filename { get; set; }

        public ImageSegment(string name, int x, int y, Color color, Color basecolor, string filename)
            : base(name, DisplaySegmentType.image, x, y, color, basecolor)
        {
            this.filename = filename;
        }

        public override DisplaySegmentData DeepCopy()
        {
            return new ImageSegment(name, x, y, color, baseColor, filename);
        }

        public override string ToString()
        {
            return $"{base.ToString()} Image at ({x},{y}) with file \"{filename}\"";
        }
    }
}
