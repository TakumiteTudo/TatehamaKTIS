using System;
using System.Collections.Generic;
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

        public Bitmap GetLCDFontImageByString(string str, int letterSpacing = 1, bool isVertical = false)
        {
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
                g.Clear(Color.Transparent); // 背景を透明に設定

                int offset = 0;
                foreach (Bitmap bmp in bitmapList)
                {
                    if (isVertical)
                    {
                        g.DrawImage(bmp, new Point(0, offset));
                        offset += bmp.Height + letterSpacing;
                    }
                    else
                    {
                        g.DrawImage(bmp, new Point(offset, 0));
                        offset += bmp.Width + letterSpacing;
                    }
                }
            }

            return combinedImage;
        }
    }
}
