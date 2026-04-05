using System.Collections.Generic;
using System.Drawing;
using TatehamaKTIS.Display.Rendering;
using TatehamaKTIS.Display.Segment;
using TatehamaKTIS.Font;
using Xunit;

namespace TatehamaKTIS.RendererTests
{
    internal class FakeStringService : IStringService
    {
        public string InterpretString(string str) => str;
        public Bitmap GetLCDFontImageByString(string str, int letterSpacing = 1, bool isVertical = false, Color color = default, Color basecolor = default, int scalarX = 1, int scalarY = 1, int lineSpacing = 3)
        {
            var bmp = new Bitmap(10, 10);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
            }
            return bmp;
        }
    }

    public class BitmapDisplayRendererTests
    {
        [Fact]
        public void RenderFull_ReturnsImage_OfRequestedSize()
        {
            var renderer = new BitmapDisplayRenderer();
            var request = new DisplayRenderRequest
            {
                Segments = new List<DisplaySegmentData>
                {
                    new TextSegment { Type = DisplaySegmentType.text, x = 5, y = 5, Text = "Test", color = Color.Black, baseColor = Color.Transparent }
                },
                Width = 200,
                Height = 100,
                StringService = new FakeStringService(),
                ButtonConfigs = new Dictionary<string, TatehamaKTIS.Display.ButtonConfig>()
            };

            var img = renderer.RenderFull(request);
            Assert.NotNull(img);
            Assert.Equal(200, img.Width);
            Assert.Equal(100, img.Height);
        }

        [Fact]
        public void RenderDelta_WithChangedSegments_ComposesAndReturnsImage()
        {
            var renderer = new BitmapDisplayRenderer();
            var changed = new List<DisplaySegmentData>
            {
                new BoxSegment { Type = DisplaySegmentType.box, x = 10, y = 10, sizeX = 20, sizeY = 20, baseColor = Color.Blue, color = Color.Black }
            };

            var request = new DisplayRenderRequest
            {
                Segments = new List<DisplaySegmentData>(),
                ChangedSegments = changed,
                Width = 300,
                Height = 150,
                StringService = new FakeStringService(),
                ButtonConfigs = new Dictionary<string, TatehamaKTIS.Display.ButtonConfig>()
            };

            var img = renderer.RenderDelta(request);
            Assert.NotNull(img);
            Assert.Equal(300, img.Width);
            Assert.Equal(150, img.Height);
        }
    }
}
