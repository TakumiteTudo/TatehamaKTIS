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
        internal Dictionary<string, ButtonConfig> buttonConfigs = new Dictionary<string, ButtonConfig>();
        CharService charService;
        internal StringService stringService;

        // 前回の描画結果を保持するフィールド
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
                !segment1.isVisible)
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
