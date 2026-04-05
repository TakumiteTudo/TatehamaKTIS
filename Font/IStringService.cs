using System.Drawing;

namespace TatehamaKTIS.Font
{
    internal interface IStringService
    {
        string InterpretString(string str);

        Bitmap GetLCDFontImageByString(string str, int letterSpacing = 1, bool isVertical = false, Color color = default, Color basecolor = default, int scalarX = 1, int scalarY = 1, int lineSpacing = 3);
    }
}
