using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TatehamaKTIS.Font
{
    internal class StringService
    {
        CharService charService;
        public StringService(CharService charService)
        {
            this.charService = charService;
        }

        public Bitmap GetLCDFontImageByString(string str, int letterSpacing = 1, bool isVertical = false, Color color = default, Color basecolor = default)
        {
            // デフォルトの色を設定
            if (color == default) color = Color.Black;
            if (basecolor == default) basecolor = Color.Transparent;

            // 各文字の画像を取得
            List<Bitmap> bitmapList = new List<Bitmap>();
            foreach (char c in str)
            {
                bitmapList.Add(charService.GetLCDFontImageByChar(c.ToString()));
            }

            // 画像の幅と高さを計算
            int totalWidth = isVertical ? bitmapList.Max(b => b.Width) : bitmapList.Sum(b => b.Width) + (bitmapList.Count - 1) * letterSpacing;
            int totalHeight = isVertical ? bitmapList.Sum(b => b.Height) + (bitmapList.Count - 1) * letterSpacing : bitmapList.Max(b => b.Height);

            // 結合画像を作成
            Bitmap combinedImage = new Bitmap(totalWidth, totalHeight);
            using (Graphics g = Graphics.FromImage(combinedImage))
            {
                g.Clear(basecolor); // 背景色を設定

                int offset = 0;
                foreach (Bitmap bmp in bitmapList)
                {
                    // 文字色を適用
                    Bitmap coloredBmp = ApplyColorToBitmap(bmp, color);

                    if (isVertical)
                    {
                        g.DrawImage(coloredBmp, new Point(0, offset));
                        offset += bmp.Height + letterSpacing;
                    }
                    else
                    {
                        g.DrawImage(coloredBmp, new Point(offset, 0));
                        offset += bmp.Width + letterSpacing; // 各文字の幅を考慮
                    }
                }
            }

            return combinedImage;
        }

        private Bitmap ApplyColorToBitmap(Bitmap bitmap, Color color)
        {
            Bitmap coloredBitmap = new Bitmap(bitmap.Width, bitmap.Height);
            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    Color pixelColor = bitmap.GetPixel(x, y);
                    if (pixelColor.A > 0) // 透明でないピクセルにのみ色を適用
                    {
                        coloredBitmap.SetPixel(x, y, color);
                    }
                    else
                    {
                        coloredBitmap.SetPixel(x, y, Color.Transparent);
                    }
                }
            }
            return coloredBitmap;
        }
    }
}
