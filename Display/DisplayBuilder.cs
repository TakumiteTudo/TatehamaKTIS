using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TatehamaKTIS.Display.Segment;
using TatehamaKTIS.Font;

namespace TatehamaKTIS.Display
{
    internal class DisplayBuilder
    {
        CharService charService;
        StringService stringService;

        internal DisplayBuilder()
        {
            charService = new CharService("Image/Char/font.bmp", "Image/Char/char.txt");
            stringService = new StringService(charService);
        }

        internal Bitmap BuildDisplayImage(List<DisplaySegmentData> segments)
        {
            // キャンバスの初期化
            Bitmap canvas = new Bitmap(800, 600);
            using (Graphics g = Graphics.FromImage(canvas))
            {
                g.Clear(Color.Transparent); // 背景を透明に設定

                foreach (var segment in segments)
                {
                    switch (segment.Type)
                    {
                        case DisplaySegmentType.text:
                            if (segment is TextSegment textSegment)
                            {
                                // テキストセグメントの描画
                                Bitmap textImage = stringService.GetLCDFontImageByString(
                                    textSegment.Text,
                                    letterSpacing: 1,
                                    isVertical: false
                                );

                                // スケーリング
                                Bitmap scaledTextImage = new Bitmap(
                                    textImage,
                                    textImage.Width * textSegment.ScalarX,
                                    textImage.Height * textSegment.ScalarY
                                );

                                g.DrawImage(scaledTextImage, textSegment.x, textSegment.y);
                            }
                            break;

                        // 他のセグメントタイプの処理を追加可能
                        default:
                            throw new NotSupportedException($"サポートされていないセグメントタイプ: {segment.Type}");
                    }
                }
            }

            return canvas;
        }
    }
}
