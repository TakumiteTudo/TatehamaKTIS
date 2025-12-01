using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TatehamaKTIS.Font;

namespace TatehamaKTIS.Display
{
    internal class DisplayManager
    {
        DisplayBuilder displayBuilder;
        SegmentReader segmentReader;
        internal Action<Bitmap> displayAction;

        internal DisplayManager()
        {
            displayBuilder = new DisplayBuilder();
            segmentReader = new SegmentReader();
            DisplayUpdate();
        }

        internal void DisplayUpdate()
        {
            var segmentData = segmentReader.ReadSegmentsFromFile("Data/Tatehama/K00AA.txt");
            var displayImage = displayBuilder.BuildDisplayImage(segmentData);
            displayAction?.Invoke(displayImage);
        }
    }
}
