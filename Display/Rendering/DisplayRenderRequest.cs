using System.Collections.Generic;
using TatehamaKTIS.Display.Segment;
using System.Drawing;

namespace TatehamaKTIS.Display.Rendering
{
    internal class DisplayRenderRequest
    {
        public IReadOnlyList<DisplaySegmentData> Segments { get; set; } = new List<DisplaySegmentData>();
        public IReadOnlyList<DisplaySegmentData>? ChangedSegments { get; set; }
        public Size ScreenSize { get; set; } = new Size(800, 600);
    }
}
