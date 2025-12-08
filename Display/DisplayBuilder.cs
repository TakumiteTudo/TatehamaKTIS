using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TatehamaKTIS.Display.Segment;
using TatehamaKTIS.Font;

namespace TatehamaKTIS.Display
{
    internal class DisplayBuilder
    {
        private DisplayData displayData;
        internal List<DisplaySegmentData> displaySegmentDatas { get; set; }
        private Dictionary<string, ButtonConfig> buttonConfigs = new Dictionary<string, ButtonConfig>();
        CharService charService;
        StringService stringService;

        // 前回の描画結果を保持するフィールド
        private Bitmap? previousImage;
        private List<DisplaySegmentData> previousDisplaySegmentDatas { get; set; }

        internal DisplayBuilder(DisplayData displayData)
        {
            this.displayData = displayData;
            charService = new CharService("Image/Char/font.bmp", "Image/Char/char.txt");
            stringService = new StringService(charService, displayData);

            displaySegmentDatas = new List<DisplaySegmentData>();
            previousDisplaySegmentDatas = new List<DisplaySegmentData>();

            // ボタン設定の読み込み
            LoadButtonConfigs();
        }

        internal Bitmap BuildDisplayImage()
        {
            previousDisplaySegmentDatas = displaySegmentDatas.Select(segment => segment.DeepCopy()).ToList(); // DeepCopy を使用
            return getDisplayImage(displaySegmentDatas);
        }

        internal Bitmap BuildDisplayImageDiff()
        {
            var filter = FilterChangedSegments();
            if (filter.Count == 0)
            {
                return previousImage;
            }
            Bitmap canvas = getDisplayImage(filter);

            // 前回の画像と合成
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
            previousDisplaySegmentDatas = displaySegmentDatas.Select(segment => segment.DeepCopy()).ToList(); // DeepCopy を使用   
            return (Bitmap)previousImage.Clone();
        }

        internal Bitmap BuildDisplayImageDiff(List<DisplaySegmentData> displaySegmentData)
        {
            Bitmap canvas = getDisplayImage(displaySegmentData);

            // 前回の画像と合成
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
            previousDisplaySegmentDatas = displaySegmentDatas.Select(segment => segment.DeepCopy()).ToList(); // DeepCopy を使用   
            return (Bitmap)previousImage.Clone();
        }

        internal Bitmap getDisplayImage(List<DisplaySegmentData> segmentDatas)
        {
            // キャンバスの初期化
            Bitmap canvas = new Bitmap(800, 600);
            using (Graphics g = Graphics.FromImage(canvas))
            {
                g.Clear(Color.Transparent); // 背景を透明に設定

                foreach (var segment in displaySegmentDatas)
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

                            // 他のセグメントタイプの処理を追加可能
                            default:
                                throw new NotSupportedException($"サポートされていないセグメントタイプ: {segment.Type}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"行の解析中にエラーが発生しました: {segment}. エラー: {ex.Message}");
                    }
                }
            }
            // 前回の画像を更新
            previousImage = (Bitmap)canvas.Clone();

            return canvas;
        }

        // 差分を抽出するメソッド
        internal List<DisplaySegmentData> FilterChangedSegments()
        {
            // 新しいリストに変更されたセグメントのみを追加
            var changedSegments = new List<DisplaySegmentData>();
            var nowSegments = displaySegmentDatas.Select(segment => segment.DeepCopy()).ToList();

            foreach (var segment in nowSegments)
            {
                var previousSegment = previousDisplaySegmentDatas.FirstOrDefault(prev => prev.name == segment.name);

                // 前回のセグメントが存在しない、または内容が異なる場合に追加
                if (previousSegment == null || !CompareSegmentData(segment, previousSegment))
                {
                    changedSegments.Add(segment);
                }
            }
            // 現在のセグメントを次回の比較用に保存
            previousDisplaySegmentDatas = nowSegments;

            return changedSegments;
        }

        // 2つの DisplaySegmentData を比較するメソッド
        private bool CompareSegmentData(DisplaySegmentData segment1, DisplaySegmentData segment2)
        {
            // 基本プロパティを比較
            if (segment1.Type != segment2.Type ||
                segment1.x != segment2.x ||
                segment1.y != segment2.y ||
                segment1.color != segment2.color ||
                segment1.baseColor != segment2.baseColor ||
                segment1.isVisible != segment2.isVisible)
            {
                return false;
            }

            // 型ごとの追加プロパティを比較
            switch (segment1)
            {
                case TextSegment text1 when segment2 is TextSegment text2:
                    return text1.Text == text2.Text &&
                           text1.ScalarX == text2.ScalarX &&
                           text1.ScalarY == text2.ScalarY;

                case BoxSegment box1 when segment2 is BoxSegment box2:
                    return box1.sizeX == box2.sizeX &&
                           box1.sizeY == box2.sizeY;

                case ButtonSegment button1 when segment2 is ButtonSegment button2:
                    return button1.Text == button2.Text &&
                           button1.scalarX == button2.scalarX &&
                           button1.scalarY == button2.scalarY &&
                           button1.sizeX == button2.sizeX &&
                           button1.sizeY == button2.sizeY &&
                           button1.buttonColor == button2.buttonColor &&
                           button1.isChecked == button2.isChecked &&
                           button1.isLighting == button2.isLighting && button1.isLighting <= 0;

                case ImageSegment image1 when segment2 is ImageSegment image2:
                    return image1.filename == image2.filename;

                default:
                    // 未対応の型の場合は常に異なるとみなす
                    return false;
            }
        }

        private void DrawTextSegment(Graphics g, TextSegment textSegment)
        {
            if (textSegment == null) return;

            // テキストセグメントの描画
            Bitmap textImage = stringService.GetLCDFontImageByString(
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
                sg.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor; // アンチエイリアスを無効化
                sg.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half; // ピクセルのオフセットを調整
                sg.DrawImage(textImage, new Rectangle(0, 0, textImage.Width, textImage.Height));
            }

            g.DrawImage(textImage, textSegment.x, textSegment.y);
        }

        private void DrawBoxSegment(Graphics g, BoxSegment boxSegment)
        {
            if (boxSegment == null) return;

            // ボックスセグメントの描画
            using (Brush fillBrush = new SolidBrush(boxSegment.baseColor))
            using (Pen borderPen = new Pen(boxSegment.color, 1))
            {
                // 塗りつぶし
                g.FillRectangle(fillBrush, boxSegment.x, boxSegment.y, boxSegment.sizeX, boxSegment.sizeY);

                // 外枠
                g.DrawRectangle(borderPen, boxSegment.x, boxSegment.y, boxSegment.sizeX - 1, boxSegment.sizeY - 1);
            }
        }

        private void DrawImageSegment(Graphics g, ImageSegment imageSegment)
        {
            if (imageSegment == null) return;

            // 画像セグメントの描画
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
            // ボタン画像のファイル名を取得
            bool isNowLighting = GetButtonisNowLighting(buttonSegment);
            string buttonImagePath = GetButtonImageFileName(buttonSegment, isNowLighting);

            if (!File.Exists(buttonImagePath))
            {
                throw new FileNotFoundException($"ボタン画像が見つかりません: {buttonImagePath}");
            }

            // ボタン設定を取得
            if (!buttonConfigs.TryGetValue(buttonSegment.buttonColor, out ButtonConfig config))
            {
                throw new KeyNotFoundException($"ボタン設定が見つかりません: {buttonSegment.buttonColor}");
            }

            // ボタンのサイズと角のサイズを取得
            int buttonWidth = buttonSegment.sizeX;
            int buttonHeight = buttonSegment.sizeY;
            int cornerWidth = config.CornerX;
            int cornerHeight = config.CornerY;

            using (Bitmap buttonImage = new Bitmap(buttonImagePath))
            {
                // 透明色を設定
                buttonImage.MakeTransparent(Color.FromArgb(unchecked((int)0xFFFF00FF)));

                // 描画用のボタン画像を作成
                using (Bitmap scaledButton = new Bitmap(buttonWidth, buttonHeight))
                using (Graphics sg = Graphics.FromImage(scaledButton))
                {
                    sg.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor; // アンチエイリアスを無効化
                    sg.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half; // ピクセルのオフセットを調整
                    // 四隅を描画（原寸のまま）
                    sg.DrawImage(buttonImage, new Rectangle(0, 0, cornerWidth, cornerHeight), new Rectangle(0, 0, cornerWidth, cornerHeight), GraphicsUnit.Pixel); // 左上
                    sg.DrawImage(buttonImage, new Rectangle(buttonWidth - cornerWidth, 0, cornerWidth, cornerHeight), new Rectangle(buttonImage.Width - cornerWidth, 0, cornerWidth, cornerHeight), GraphicsUnit.Pixel); // 右上
                    sg.DrawImage(buttonImage, new Rectangle(0, buttonHeight - cornerHeight, cornerWidth, cornerHeight), new Rectangle(0, buttonImage.Height - cornerHeight, cornerWidth, cornerHeight), GraphicsUnit.Pixel); // 左下
                    sg.DrawImage(buttonImage, new Rectangle(buttonWidth - cornerWidth, buttonHeight - cornerHeight, cornerWidth, cornerHeight), new Rectangle(buttonImage.Width - cornerWidth, buttonImage.Height - cornerHeight, cornerWidth, cornerHeight), GraphicsUnit.Pixel); // 右下

                    // 辺を描画（引き延ばし）
                    sg.DrawImage(buttonImage, new Rectangle(cornerWidth, 0, buttonWidth - 2 * cornerWidth, cornerHeight), new Rectangle(cornerWidth, 0, buttonImage.Width - 2 * cornerWidth, cornerHeight), GraphicsUnit.Pixel); // 上辺
                    sg.DrawImage(buttonImage, new Rectangle(cornerWidth, buttonHeight - cornerHeight, buttonWidth - 2 * cornerWidth, cornerHeight), new Rectangle(cornerWidth, buttonImage.Height - cornerHeight, buttonImage.Width - 2 * cornerWidth, cornerHeight), GraphicsUnit.Pixel); // 下辺
                    sg.DrawImage(buttonImage, new Rectangle(0, cornerHeight, cornerWidth, buttonHeight - 2 * cornerHeight), new Rectangle(0, cornerHeight, cornerWidth, buttonImage.Height - 2 * cornerHeight), GraphicsUnit.Pixel); // 左辺
                    sg.DrawImage(buttonImage, new Rectangle(buttonWidth - cornerWidth, cornerHeight, cornerWidth, buttonHeight - 2 * cornerHeight), new Rectangle(buttonImage.Width - cornerWidth, cornerHeight, cornerWidth, buttonImage.Height - 2 * cornerHeight), GraphicsUnit.Pixel); // 右辺

                    // 中央を描画（引き延ばし）
                    sg.DrawImage(buttonImage, new Rectangle(cornerWidth, cornerHeight, buttonWidth - 2 * cornerWidth, buttonHeight - 2 * cornerHeight), new Rectangle(cornerWidth, cornerHeight, buttonImage.Width - 2 * cornerWidth, buttonImage.Height - 2 * cornerHeight), GraphicsUnit.Pixel);

                    // 描画結果をキャンバスに描画
                    g.DrawImage(scaledButton, buttonSegment.x, buttonSegment.y);
                }
            }

            // テキストまたは画像を描画
            string content = buttonSegment.Text;

            if (content.StartsWith("[image:") && content.EndsWith("]"))
            {
                // 画像を描画するパターン
                string imageName = content.Substring(7, content.Length - 8); // "[image:<ファイル名>]" からファイル名を抽出
                string imagePath = Path.Combine("Image", "Button", "Image", imageName + ".png");

                if (File.Exists(imagePath))
                {
                    using (Bitmap contentImage = new Bitmap(imagePath))
                    {
                        // 透明色を設定
                        contentImage.MakeTransparent(Color.FromArgb(unchecked((int)0xFFFF00FF)));
                        // 画像をボタン中央に配置
                        int imageX = buttonSegment.x + (buttonWidth - contentImage.Width) / 2;
                        int imageY = buttonSegment.y + (buttonHeight - contentImage.Height) / 2;
                        g.DrawImage(contentImage, imageX, imageY);
                    }
                }
                else
                {
                    Debug.WriteLine($"指定された画像ファイルが見つかりません: {imagePath}");
                }
            }
            else
            {
                // テキストを描画するパターン
                Bitmap textImage = stringService.GetLCDFontImageByString(
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

                // テキストをボタン中央に配置
                int textX = buttonSegment.x + (buttonWidth - textImage.Width) / 2;
                int textY = buttonSegment.y + (buttonHeight - textImage.Height) / 2 - 1;
                g.DrawImage(textImage, textX, textY);
            }
        }

        private bool GetButtonisNowLighting(ButtonSegment buttonSegment)
        {
            // 点灯状態を確認
            if (buttonSegment.isLighting == -1)
            {
                return true;
            }
            else if (buttonSegment.isLighting > 0)
            {
                // 点滅状態を計算
                TimeSpan elapsedTime = DateTime.Now - buttonSegment.originTime;
                bool isCurrentlyLit = (elapsedTime.TotalMilliseconds % (buttonSegment.isLighting * 2)) < buttonSegment.isLighting;

                if (isCurrentlyLit)
                {
                    return true;
                }
            }
            return false;
        }

        private string GetButtonImageFileName(ButtonSegment buttonSegment, bool isNowLighting)
        {
            // ボタンの状態に応じたファイル名を生成
            string baseName = buttonSegment.buttonColor; // ボタンの色名を基にする
            string stateSuffix = "";

            // 選択状態を確認
            if (buttonSegment.isChecked)
            {
                stateSuffix += "_t";
            }
            else
            {
                stateSuffix += "_f";
            }

            // 点灯状態を確認
            if (isNowLighting)
            {
                stateSuffix += "l";
            }

            // ファイル名を組み立てる
            string fileName = $"{baseName}{stateSuffix}.png";
            return Path.Combine("Image", "Button", fileName);
        }


        private void LoadButtonConfigs()
        {
            string buttonConfigDirectory = Path.Combine("Image", "Button");
            if (!Directory.Exists(buttonConfigDirectory))
            {
                throw new DirectoryNotFoundException($"ボタン設定ディレクトリが見つかりません: {buttonConfigDirectory}");
            }

            foreach (var filePath in Directory.GetFiles(buttonConfigDirectory, "*.txt"))
            {
                string colorName = Path.GetFileNameWithoutExtension(filePath); // ファイル名を色名として解釈
                var config = ParseButtonConfig(filePath);
                buttonConfigs[colorName] = config;
            }
        }

        private ButtonConfig ParseButtonConfig(string filePath)
        {
            var config = new ButtonConfig();
            var lines = File.ReadAllLines(filePath);

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var parts = line.Split(':');
                if (parts.Length != 2) continue;

                string key = parts[0].Trim();
                string value = parts[1].Trim();

                switch (key)
                {
                    case "TextCF":
                        config.TextCF = ParseColor(value);
                        break;
                    case "TextCT":
                        config.TextCT = ParseColor(value);
                        break;
                    case "TextCFL":
                        config.TextCFL = ParseColor(value);
                        break;
                    case "TextCTL":
                        config.TextCTL = ParseColor(value);
                        break;
                    case "Corner":
                        var cornerParts = value.Split(',');
                        if (cornerParts.Length == 2 &&
                            int.TryParse(cornerParts[0], out int cornerX) &&
                            int.TryParse(cornerParts[1], out int cornerY))
                        {
                            config.CornerX = cornerX;
                            config.CornerY = cornerY;
                        }
                        break;
                }
            }

            return config;
        }

        private Color ParseColor(string colorString)
        {
            // Remove "0x" prefix if present
            if (colorString.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                colorString = colorString.Substring(2);
            }

            if (int.TryParse(colorString, System.Globalization.NumberStyles.HexNumber, null, out int argb))
            {
                return Color.FromArgb(argb);
            }
            throw new FormatException($"無効な色コード: {colorString}");
        }
    }
}
