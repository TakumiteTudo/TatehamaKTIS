using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TatehamaKTIS.Font
{
    internal class CharService
    {
        int halfcharWidth = 8;
        int fillcharWidth = 17;
        int charHeight = 17;
        private List<string> LCDFontList = new List<string>();
        Bitmap fontImage;

        internal CharService(string Imagepath, string charFilePath)
        {
            // フォント画像の読み込み
            using (Bitmap originalImage = new Bitmap(Imagepath))
            {
                fontImage = new Bitmap(originalImage.Width, originalImage.Height);
                for (int y = 0; y < originalImage.Height; y++)
                {
                    for (int x = 0; x < originalImage.Width; x++)
                    {
                        Color pixelColor = originalImage.GetPixel(x, y);
                        if (pixelColor.ToArgb() == Color.Black.ToArgb())
                        {
                            fontImage.SetPixel(x, y, Color.White); // 黒は白
                        }
                        else if (pixelColor.ToArgb() == 0xC0C0C0)
                        {
                            //灰色は枠について
                        }
                        else
                        {
                            fontImage.SetPixel(x, y, Color.Transparent); // 黒以外は透明
                        }
                    }
                }
            }

            // フォントリストの初期化
            if (File.Exists(charFilePath))
            {
                string[] lines = File.ReadAllLines(charFilePath);
                foreach (string line in lines)
                {
                    string[] chars = line.Split('\t');
                    LCDFontList.AddRange(chars);
                }
            }
            else
            {
                throw new FileNotFoundException($"指定されたファイルが見つかりません: {charFilePath}");
            }
        }

        /// <summary>
        /// 画像内からその文字の画像を取得するメソッド
        /// </summary>
        /// <param name="str">対象文字</param>
        /// <returns>対象文字の画像</returns>
        internal Bitmap GetLCDFontImageByChar(string str)
        {
            if (string.IsNullOrEmpty(str) || !LCDFontList.Contains(str))
            {
                str = "？";
            }
            int index = LCDFontList.IndexOf(str);
            int x = (index % 32) * (fillcharWidth + 1);
            int y = (index / 32) * (charHeight + 1);
            return GetLCDFontImageByPos(x, y, fillcharWidth, charHeight);
        }

        /// <summary>
        /// 指定した座標とサイズに基づいて画像を切り出す
        /// </summary>
        /// <param name="number">切り出す画像の番号</param>
        /// <returns>切り出された画像</returns>
        private Bitmap GetLCDFontImageByPos(int x, int y, int width = 5, int height = 7)
        {
            Bitmap croppedImage = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(croppedImage))
            {
                g.DrawImage(fontImage, new Rectangle(0, 0, width, height), new Rectangle(x, y, width, height), GraphicsUnit.Pixel);
            }
            return croppedImage;
        }
    }
}
