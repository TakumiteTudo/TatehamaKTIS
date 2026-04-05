using System.Collections.Generic;
using TatehamaKTIS.Display.Segment;
using System.Drawing;
using TatehamaKTIS.Display;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Collections.Generic;

namespace TatehamaKTIS.Display.Rendering
{
    /// <summary>
    /// 既存の Bitmap ベースの描画処理を暫定的にラップする実装（互換性用）。
    /// DisplayBuilder の既存ロジックを使って Bitmap を返す。
    /// </summary>
    internal class BitmapDisplayRenderer : IDisplayRenderer
    {
        private Bitmap? previousImage;
        private Font.IStringService? currentStringService;
        private IReadOnlyDictionary<string, ButtonConfig>? currentButtonConfigs;
        private readonly System.Action<System.Drawing.Image>? presentAction;

        internal BitmapDisplayRenderer(System.Action<System.Drawing.Image>? presentAction = null)
        {
            this.presentAction = presentAction;
        }

        public void RenderFull(DisplayRenderRequest request)
        {
            var segments = request.Segments ?? new List<DisplaySegmentData>();
            // setup services from request
            currentStringService = request.StringService;
            currentButtonConfigs = request.ButtonConfigs;

            int w = request.Width;
            int h = request.Height;
            Bitmap canvas = new Bitmap(w, h);
            using (Graphics g = Graphics.FromImage(canvas))
            {
                g.Clear(Color.Transparent);
                foreach (var segment in segments)
                {
                    try
                    {
                        switch (segment.Type)
                        {
                            case DisplaySegmentType.text:
                                DrawTextSegment(g, segment as TextSegment);
                                break;
                            case DisplaySegmentType.box:
                                DrawBoxSegment(g, segment as BoxSegment);
                                break;
                            case DisplaySegmentType.image:
                                DrawImageSegment(g, segment as ImageSegment);
                                break;
                            case DisplaySegmentType.button:
                                DrawButtonSegment(g, segment as ButtonSegment);
                                break;
                            default:
                                throw new NotSupportedException($"サポートされていないセグメントタイプ: {segment.Type}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"描画中にエラー: {ex.Message}");
                    }
                }
            }
            previousImage = (Bitmap)canvas.Clone();
            var toPresent = (System.Drawing.Image)canvas.Clone();
            if (presentAction != null)
            {
                presentAction(toPresent);
            }
            else
            {
                // no-op: renderer produced image but no present action provided
                toPresent.Dispose();
            }
        }

        public void RenderDelta(DisplayRenderRequest request)
        {
            // setup services from request
            currentStringService = request.StringService;
            currentButtonConfigs = request.ButtonConfigs;

            var changed = request.ChangedSegments ?? new List<DisplaySegmentData>();
            if (changed.Count == 0)
            {
                if (previousImage != null)
                {
                    var clone = (System.Drawing.Image)previousImage.Clone();
                    if (presentAction != null) presentAction(clone); else clone.Dispose();
                }
                return;
            }

            int w2 = request.Width;
            int h2 = request.Height;
            Bitmap canvas = new Bitmap(w2, h2);
            using (Graphics g = Graphics.FromImage(canvas))
            {
                g.Clear(Color.Transparent);
                foreach (var segment in changed)
                {
                    try
                    {
                        switch (segment.Type)
                        {
                            case DisplaySegmentType.text:
                                DrawTextSegment(g, segment as TextSegment);
                                break;
                            case DisplaySegmentType.box:
                                DrawBoxSegment(g, segment as BoxSegment);
                                break;
                            case DisplaySegmentType.image:
                                DrawImageSegment(g, segment as ImageSegment);
                                break;
                            case DisplaySegmentType.button:
                                DrawButtonSegment(g, segment as ButtonSegment);
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"差分描画エラー: {ex.Message}");
                    }
                }
            }

            if (previousImage != null)
            {
                using (Graphics g = Graphics.FromImage(previousImage))
                {
                    g.DrawImage(canvas, 0, 0);
                }
            }
            else
            {
                previousImage = (Bitmap)canvas.Clone();
            }
            var toPresent = (System.Drawing.Image)previousImage.Clone();
            if (presentAction != null)
            {
                presentAction(toPresent);
            }
            else
            {
                toPresent.Dispose();
            }
        }

        private void DrawTextSegment(Graphics g, TextSegment textSegment)
        {
            if (textSegment == null) return;
            Bitmap textImage = (currentStringService ?? throw new InvalidOperationException("StringService not provided in request")).GetLCDFontImageByString(
                textSegment.Text,
                letterSpacing: 1,
                isVertical: false,
                color: textSegment.color,
                basecolor: textSegment.baseColor,
                scalarX: textSegment.ScalarX,
                scalarY: textSegment.ScalarY
            );

            using (Graphics sg = Graphics.FromImage(textImage))
            {
                sg.InterpolationMode = InterpolationMode.NearestNeighbor;
                sg.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
                sg.DrawImage(textImage, new Rectangle(0, 0, textImage.Width, textImage.Height));
            }

            g.DrawImage(textImage, textSegment.x, textSegment.y);
        }

        private void DrawBoxSegment(Graphics g, BoxSegment boxSegment)
        {
            if (boxSegment == null) return;
            using (Brush fillBrush = new SolidBrush(boxSegment.baseColor))
            using (Pen borderPen = new Pen(boxSegment.color, 1))
            {
                g.FillRectangle(fillBrush, boxSegment.x, boxSegment.y, boxSegment.sizeX, boxSegment.sizeY);
                g.DrawRectangle(borderPen, boxSegment.x, boxSegment.y, boxSegment.sizeX - 1, boxSegment.sizeY - 1);
            }
        }

        private void DrawImageSegment(Graphics g, ImageSegment imageSegment)
        {
            if (imageSegment == null) return;
            string imagePath = Path.Combine("Image", "Image", imageSegment.filename + ".png");
            if (File.Exists(imagePath))
            {
                using (Bitmap image = new Bitmap(imagePath))
                {
                    g.DrawImage(image, imageSegment.x, imageSegment.y);
                }
            }
            else
            {
                throw new FileNotFoundException($"画像ファイルが見つかりません: {imagePath}");
            }
        }

        private void DrawButtonSegment(Graphics g, ButtonSegment buttonSegment)
        {
            if (buttonSegment == null) return;
            if (buttonSegment.Text == "")
            {
                buttonSegment.isVisible = false;
            }
            if (!buttonSegment.isVisible) return;

            bool isNowLighting = GetButtonisNowLighting(buttonSegment);
            string buttonImagePath = GetButtonImageFileName(buttonSegment, isNowLighting);
            if (!File.Exists(buttonImagePath)) throw new FileNotFoundException($"ボタン画像が見つかりません: {buttonImagePath}");

            if (currentButtonConfigs == null || !currentButtonConfigs.TryGetValue(buttonSegment.buttonColor, out ButtonConfig config))
            {
                throw new KeyNotFoundException($"ボタン設定が見つかりません: {buttonSegment.buttonColor}");
            }

            int buttonWidth = buttonSegment.sizeX;
            int buttonHeight = buttonSegment.sizeY;
            int cornerWidth = config.CornerX;
            int cornerHeight = config.CornerY;

            int contentWidth = 0;
            int contentHeight = 0;
            Bitmap contentImage = new Bitmap(1, 1);

            string content = buttonSegment.Text;
            if (content.StartsWith("{image:") && content.EndsWith("}"))
            {
                string imageName = content.Substring(7, content.Length - 8);
                string imagePath = Path.Combine("Image", "Button", "Image", imageName + ".png");
                if (File.Exists(imagePath))
                {
                    using (contentImage = new Bitmap(imagePath))
                    {
                        contentWidth = contentImage.Width;
                        contentHeight = contentImage.Height;
                    }
                }
            }
            else
            {
                contentImage = (currentStringService ?? throw new InvalidOperationException("StringService not provided in request")).GetLCDFontImageByString(
                    content,
                    letterSpacing: 1,
                    isVertical: false,
                    color: buttonSegment.isChecked
                        ? (isNowLighting ? config.TextCTL : config.TextCT)
                        : (isNowLighting ? config.TextCFL : config.TextCF),
                    basecolor: Color.Transparent,
                    scalarX: buttonSegment.scalarX,
                    scalarY: buttonSegment.scalarY,
                    lineSpacing: 3
                );

                contentWidth = contentImage.Width;
                contentHeight = contentImage.Height;
            }

            if (contentWidth <= 1 || contentHeight <= 1)
            {
                buttonSegment.isVisible = false;
                return;
            }

            using (Bitmap buttonImage = new Bitmap(buttonImagePath))
            {
                for (int x = 0; x < buttonImage.Width; x++)
                {
                    for (int y = 0; y < buttonImage.Height; y++)
                    {
                        if (buttonImage.GetPixel(x, y).ToArgb() == unchecked((int)0xFFFF00FF))
                        {
                            buttonImage.SetPixel(x, y, buttonSegment.baseColor);
                        }
                    }
                }

                using (Bitmap scaledButton = new Bitmap(buttonWidth, buttonHeight))
                using (Graphics sg = Graphics.FromImage(scaledButton))
                {
                    sg.InterpolationMode = InterpolationMode.NearestNeighbor;
                    sg.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
                    sg.DrawImage(buttonImage, new Rectangle(0, 0, cornerWidth, cornerHeight), new Rectangle(0, 0, cornerWidth, cornerHeight), GraphicsUnit.Pixel);
                    sg.DrawImage(buttonImage, new Rectangle(buttonWidth - cornerWidth, 0, cornerWidth, cornerHeight), new Rectangle(buttonImage.Width - cornerWidth, 0, cornerWidth, cornerHeight), GraphicsUnit.Pixel);
                    sg.DrawImage(buttonImage, new Rectangle(0, buttonHeight - cornerHeight, cornerWidth, cornerHeight), new Rectangle(0, buttonImage.Height - cornerHeight, cornerWidth, cornerHeight), GraphicsUnit.Pixel);
                    sg.DrawImage(buttonImage, new Rectangle(buttonWidth - cornerWidth, buttonHeight - cornerHeight, cornerWidth, cornerHeight), new Rectangle(buttonImage.Width - cornerWidth, buttonImage.Height - cornerHeight, cornerWidth, cornerHeight), GraphicsUnit.Pixel);

                    sg.DrawImage(buttonImage, new Rectangle(cornerWidth, 0, buttonWidth - 2 * cornerWidth, cornerHeight), new Rectangle(cornerWidth, 0, buttonImage.Width - 2 * cornerWidth, cornerHeight), GraphicsUnit.Pixel);
                    sg.DrawImage(buttonImage, new Rectangle(cornerWidth, buttonHeight - cornerHeight, buttonWidth - 2 * cornerWidth, cornerHeight), new Rectangle(cornerWidth, buttonImage.Height - cornerHeight, buttonImage.Width - 2 * cornerWidth, cornerHeight), GraphicsUnit.Pixel);
                    sg.DrawImage(buttonImage, new Rectangle(0, cornerHeight, cornerWidth, buttonHeight - 2 * cornerHeight), new Rectangle(0, cornerHeight, cornerWidth, buttonImage.Height - 2 * cornerHeight), GraphicsUnit.Pixel);
                    sg.DrawImage(buttonImage, new Rectangle(buttonWidth - cornerWidth, cornerHeight, cornerWidth, buttonHeight - 2 * cornerHeight), new Rectangle(buttonImage.Width - cornerWidth, cornerHeight, cornerWidth, buttonImage.Height - 2 * cornerHeight), GraphicsUnit.Pixel);

                    sg.DrawImage(buttonImage, new Rectangle(cornerWidth, cornerHeight, buttonWidth - 2 * cornerWidth, buttonHeight - 2 * cornerHeight), new Rectangle(cornerWidth, cornerHeight, buttonImage.Width - 2 * cornerWidth, buttonImage.Height - 2 * cornerHeight), GraphicsUnit.Pixel);

                    g.DrawImage(scaledButton, buttonSegment.x, buttonSegment.y);
                }
            }

            if (content.StartsWith("{image:") && content.EndsWith("}"))
            {
                using (contentImage)
                {
                    contentImage.MakeTransparent(Color.FromArgb(unchecked((int)0xFFFF00FF)));
                    int imageX = buttonSegment.x + (buttonWidth - contentImage.Width) / 2;
                    int imageY = buttonSegment.y + (buttonHeight - contentImage.Height) / 2;
                    g.DrawImage(contentImage, imageX, imageY);
                }
            }
            else
            {
                int textX = buttonSegment.x + (buttonWidth - contentImage.Width) / 2;
                int textY = buttonSegment.y + (buttonHeight - contentImage.Height) / 2 - 1;
                g.DrawImage(contentImage, textX, textY);
            }
        }

        private bool GetButtonisNowLighting(ButtonSegment buttonSegment)
        {
            if (buttonSegment.isLighting == -1) return true;
            else if (buttonSegment.isLighting > 0)
            {
                TimeSpan elapsedTime = DateTime.Now - buttonSegment.originTime;
                bool isCurrentlyLit = (elapsedTime.TotalMilliseconds % (buttonSegment.isLighting * 2)) < buttonSegment.isLighting;
                if (isCurrentlyLit) return true;
            }
            return false;
        }

        private string GetButtonImageFileName(ButtonSegment buttonSegment, bool isNowLighting)
        {
            string baseName = buttonSegment.buttonColor;
            string stateSuffix = "";
            if (buttonSegment.isChecked) stateSuffix += "_t"; else stateSuffix += "_f";
            if (isNowLighting) stateSuffix += "l";
            string fileName = $"{baseName}{stateSuffix}.png";
            return Path.Combine("Image", "Button", fileName);
        }
    }
}
