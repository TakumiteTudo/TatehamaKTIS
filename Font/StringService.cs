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

        public Bitmap GetLCDFontImageByString(string str, int letterSpacing = 1, bool isVertical = false, Color color = default, Color basecolor = default, int scalarX = 1, int scalarY = 1, int lineSpacing = 3)
        {
            // デフォルトの色を設定
            if (color == default) color = Color.Black;
            if (basecolor == default) basecolor = Color.Transparent;

            // 各文字の画像を取得
            int currentScalarX = scalarX;
            int currentScalarY = scalarY;
            bool scalingActive = false;

            // 初期の結合画像サイズ
            int totalWidth = 0;
            int totalHeight = 0;

            Bitmap combinedImage = new Bitmap(1, 1); // 初期サイズは最小
            Graphics g = Graphics.FromImage(combinedImage); // Graphics オブジェクトを明示的に作成
            g.Clear(basecolor); // 背景色を設定

            int offsetX = 0; // 横方向の描画位置
            int offsetY = 0; // 縦方向の描画位置
            int maxLineHeight = 0; // 現在の行の最大高さ

            foreach (string token in ParseStringWithControlCharacters(str))
            {
                if (token.StartsWith("[sc:") && token.EndsWith("]"))
                {
                    // スケーリング開始制御文字の処理
                    string[] scalars = token.Substring(4, token.Length - 5).Split(',');
                    if (scalars.Length == 2 && int.TryParse(scalars[0], out int sx) && int.TryParse(scalars[1], out int sy))
                    {
                        currentScalarX = sx;
                        currentScalarY = sy;
                        scalingActive = true;
                    }
                }
                else if (token == "[sc]")
                {
                    // スケーリング終了制御文字の処理
                    currentScalarX = scalarX;
                    currentScalarY = scalarY;
                    scalingActive = false;
                }
                else
                {
                    // 通常の文字処理（1文字ずつ処理）
                    string[] lines = token.Split(new[] { "\\n" }, StringSplitOptions.None); // "\\n" で分割
                    for (int i = 0; i < lines.Length; i++)
                    {
                        string line = lines[i];
                        foreach (char c in line)
                        {
                            Bitmap charBitmap = charService.GetLCDFontImageByChar(c.ToString());
                            if (scalingActive || scalarX != 1 || scalarY != 1)
                            {
                                charBitmap = ScaleBitmap(charBitmap, currentScalarX, currentScalarY);
                            }

                            // 必要に応じて結合画像を拡張
                            int newWidth = Math.Max(totalWidth, offsetX + charBitmap.Width);
                            int newHeight = Math.Max(totalHeight, offsetY + charBitmap.Height);

                            if (newWidth > combinedImage.Width || newHeight > combinedImage.Height)
                            {
                                Bitmap newCombinedImage = new Bitmap(newWidth, newHeight);
                                using (Graphics newGraphics = Graphics.FromImage(newCombinedImage))
                                {
                                    newGraphics.Clear(basecolor);
                                    newGraphics.DrawImage(combinedImage, 0, 0); // 既存の画像をコピー
                                }
                                combinedImage.Dispose();
                                combinedImage = newCombinedImage;
                                g.Dispose();
                                g = Graphics.FromImage(combinedImage); // 新しい Graphics オブジェクトを作成
                            }

                            // 文字色を適用
                            Bitmap coloredBmp = ApplyColorToBitmap(charBitmap, color);

                            // 文字を描画
                            g.DrawImage(coloredBmp, new Point(offsetX, offsetY));
                            offsetX += charBitmap.Width + (letterSpacing * currentScalarX);
                            maxLineHeight = Math.Max(maxLineHeight, charBitmap.Height);
                            totalWidth = combinedImage.Width;
                            totalHeight = combinedImage.Height;
                        }

                        // 改行処理（最後の行は改行しない）
                        if (i < lines.Length - 1)
                        {
                            offsetX = 0;
                            offsetY += maxLineHeight + (lineSpacing * currentScalarY);
                            maxLineHeight = 0;
                        }
                    }
                }
            }

            // 最後に Graphics オブジェクトを破棄
            g.Dispose();

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

        private IEnumerable<string> ParseStringWithControlCharacters(string str)
        {
            List<string> tokens = new List<string>();
            int startIndex = 0;

            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] == '[')
                {
                    int endIndex = str.IndexOf(']', i);
                    if (endIndex > i)
                    {
                        if (startIndex < i)
                        {
                            tokens.Add(str.Substring(startIndex, i - startIndex));
                        }
                        tokens.Add(str.Substring(i, endIndex - i + 1));
                        i = endIndex;
                        startIndex = i + 1;
                    }
                }
            }

            if (startIndex < str.Length)
            {
                tokens.Add(str.Substring(startIndex));
            }

            return tokens;
        }

        private Bitmap ScaleBitmap(Bitmap bitmap, int scalarX, int scalarY)
        {
            int newWidth = bitmap.Width * scalarX;
            int newHeight = bitmap.Height * scalarY;
            Bitmap scaledBitmap = new Bitmap(newWidth, newHeight);

            using (Graphics g = Graphics.FromImage(scaledBitmap))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor; // アンチエイリアスを無効化   
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half; // ピクセルのオフセットを調整
                g.DrawImage(bitmap, new Rectangle(0, 0, newWidth, newHeight));
            }

            return scaledBitmap;
        }
    }
}
