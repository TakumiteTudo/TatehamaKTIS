using System;
using System.Collections.Generic;
using System.Drawing;
using TatehamaKTIS.Display.Rendering;
using TatehamaKTIS.Display.Segment;
using TatehamaKTIS.Font;
using TatehamaKTIS.Display;

class Program
{
    public static int RunTests()
    {
        try
        {
            Console.WriteLine("Starting basic renderer tests...");

            System.Drawing.Image? lastPresented = null;
            var renderer = new BitmapDisplayRenderer((img) =>
            {
                // capture presented image for test assertions
                lastPresented = img;
            });

            var requestFull = new DisplayRenderRequest
            {
                Segments = new List<DisplaySegmentData>
                {
                    new TextSegment("t1", 5, 5, Color.Black, Color.Transparent, "T", 1, 1)
                },
                Width = 200,
                Height = 100,
                StringService = new FakeStringService(),
                ButtonConfigs = new Dictionary<string, ButtonConfig>()
            };

            renderer.RenderFull(requestFull);
            if (lastPresented == null || lastPresented.Width != 200 || lastPresented.Height != 100)
            {
                Console.WriteLine("RenderFull failed: unexpected image size");
                return 2;
            }
            lastPresented.Dispose();
            lastPresented = null;
            Console.WriteLine("RenderFull OK");

            var requestDelta = new DisplayRenderRequest
            {
                Segments = new List<DisplaySegmentData>(),
                ChangedSegments = new List<DisplaySegmentData>
                {
                    new BoxSegment("b1", 10, 10, Color.Black, Color.Blue, 20, 20)
                },
                Width = 300,
                Height = 150,
                StringService = new FakeStringService(),
                ButtonConfigs = new Dictionary<string, ButtonConfig>()
            };

            renderer.RenderDelta(requestDelta);
            if (lastPresented == null || lastPresented.Width != 300 || lastPresented.Height != 150)
            {
                Console.WriteLine("RenderDelta failed: unexpected image size");
                return 3;
            }
            lastPresented.Dispose();
            lastPresented = null;
            Console.WriteLine("RenderDelta OK");

            Console.WriteLine("All basic renderer tests passed");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Test execution failed: {ex}");
            return 1;
        }
    }
}

// Minimal fake string service for tests
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

