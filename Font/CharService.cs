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
                        else if (pixelColor.ToArgb() == 0xFFC0C0C0)
                        {
                            fontImage.SetPixel(x, y, Color.Black);
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

            // 半角文字かどうかを判定
            bool isHalfWidth = IsHalfWidthCharacter(str);

            // 使用する幅を決定
            int charWidth = isHalfWidth ? halfcharWidth : fillcharWidth;

            int x = (index % 32) * (fillcharWidth + 1);
            int y = (index / 32) * (charHeight + 1);

            // 元の画像を取得
            Bitmap originalImage = GetLCDFontImageByPos(x, y, charWidth, charHeight);

            // トリミング処理
            int trimWidth = originalImage.Width;
            for (int i = originalImage.Width - 1; i >= 0; i--)
            {
                bool isAllGray = true;
                for (int j = 0; j < originalImage.Height; j++)
                {
                    if (originalImage.GetPixel(i, j).ToArgb() != Color.Black.ToArgb())
                    {
                        isAllGray = false;
                        break;
                    }
                }

                if (!isAllGray)
                {
                    trimWidth = i + 1; // トリミングする幅を更新
                    break;
                }
            }

            // トリミングされた画像を作成
            Bitmap trimmedImage = new Bitmap(trimWidth, originalImage.Height);
            using (Graphics g = Graphics.FromImage(trimmedImage))
            {
                g.DrawImage(originalImage, new Rectangle(0, 0, trimmedImage.Width, trimmedImage.Height),
                    new Rectangle(0, 0, trimmedImage.Width, trimmedImage.Height), GraphicsUnit.Pixel);
            }

            return trimmedImage;
        }

        /// <summary>
        /// 半角文字かどうかを判定する
        /// </summary>
        /// <param name="str">判定対象の文字列</param>
        /// <returns>半角文字の場合は true、それ以外は false</returns>
        private bool IsHalfWidthCharacter(string str)
        {
            if (string.IsNullOrEmpty(str)) return false;

            // Unicode の半角文字範囲をチェック
            char c = str[0];
            return (c >= 0x20 && c <= 0x7E) || (c >= 0xFF61 && c <= 0xFF9F);
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
