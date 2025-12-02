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
    internal class DisplayManager
    {
        string displayType;
        string displayName;
        Dictionary<string, Dictionary<string, string>> displayConfig;
        DisplayBuilder displayBuilder;
        SegmentReader segmentReader;
        internal Action<Bitmap> displayAction;

        private DateTime lastTouchTime = DateTime.MinValue; // 最後のタッチ時刻
        private readonly TimeSpan minTouchInterval = TimeSpan.FromMilliseconds(100); // 最少間隔（例: 200ms）

        internal DisplayManager()
        {
            displayBuilder = new DisplayBuilder();
            segmentReader = new SegmentReader();
            DisplayUpdate();
            SetDisplayType("Tatehama");
        }

        internal void SetDisplayType(string type)
        {
            displayType = type;
            displayConfig = ParseConfig();
            ConfigureSegmentReaderColors(); // 色設定を行う

            if (displayConfig.ContainsKey("Common"))
            {
                var commonConfig = displayConfig["Common"];
                if (commonConfig.ContainsKey("default"))
                {
                    displayName = commonConfig["default"];
                }
            }
            displayBuilder.displaySegmentDatas = segmentReader.ReadSegmentsFromFile($"Data/{displayType}/{displayName}.txt");
            DisplayUpdate();
        }

        internal void DisplayUpdate()
        {
            var displayImage = displayBuilder.BuildDisplayImage();
            displayAction?.Invoke(displayImage);
        }

        internal async Task DisplayTotchDown(int x, int y)
        {
            var timeSinceLastTouch = DateTime.Now - lastTouchTime;
            if (timeSinceLastTouch < minTouchInterval)
            {
                var waitTime = minTouchInterval - timeSinceLastTouch;
                await Task.Delay(waitTime);
            }

            lastTouchTime = DateTime.Now; // 最後のタッチ時刻を更新
            Debug.WriteLine($"タッチ：{x}, {y}");
            // タッチ処理の実装（必要に応じて）
            var button = DetectButtonTouch(x, y);
            if (button != null)
            {
                Debug.WriteLine($"ボタン押：{button.name}");
                button.isChecked = !button.isChecked;
            }
            DisplayUpdate();
        }

        internal async Task DisplayTotchUp(int x, int y)
        {
            var timeSinceLastTouch = DateTime.Now - lastTouchTime;
            if (timeSinceLastTouch < minTouchInterval)
            {
                var waitTime = minTouchInterval - timeSinceLastTouch;
                await Task.Delay(waitTime);
            }

            lastTouchTime = DateTime.Now; // 最後のタッチ時刻を更新
            Debug.WriteLine($"タッチ：{x}, {y}");
            // タッチ処理の実装（必要に応じて）
            var button = DetectButtonTouch(x, y);
            if (button != null)
            {
                Debug.WriteLine($"ボタン離：{button.name}");
                if (button.buttonType == ButtonType.function)
                {
                    button.isChecked = false;
                    // 関数タイプのボタン処理
                    foreach (var func in button.functionList)
                    {
                        Debug.WriteLine($"関数実行: {func.Item1} パラメータ: {func.Item2}");
                        switch (func.Item1)
                        {
                            case "transition":
                                // 表示切替処理
                                var newDisplayName = func.Item2;
                                Debug.WriteLine($"画面遷移: {newDisplayName}");
                                var newSegments = segmentReader.ReadSegmentsFromFile($"Data/Tatehama/{newDisplayName}.txt");
                                if (newSegments != null && newSegments.Count > 0)
                                {
                                    displayBuilder.displaySegmentDatas = newSegments;
                                }
                                break;
                        }
                    }
                }
            }
            DisplayUpdate();
        }

        public ButtonSegment? DetectButtonTouch(int touchX, int touchY)
        {
            foreach (var segment in displayBuilder.displaySegmentDatas)
            {
                if (segment is ButtonSegment button)
                {
                    // ボタン領域内かどうかを判定
                    if (touchX >= button.x && touchX <= button.x + button.sizeX &&
                        touchY >= button.y && touchY <= button.y + button.sizeY)
                    {
                        return button; // タッチされたボタンを返す
                    }
                }
            }
            return null; // タッチされたボタンがない場合
        }

        private Dictionary<string, Dictionary<string, string>> ParseConfig()
        {
            var configData = new Dictionary<string, Dictionary<string, string>>();
            string filePath = Path.Combine("Data", displayType, "_DATA.txt");

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"指定された表示形式が存在しません。: {displayType}");
            }

            string? currentTag = null;

            foreach (var line in File.ReadLines(filePath))
            {
                string trimmedLine = line.Trim();

                // 空行やコメント行をスキップ
                if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#"))
                {
                    continue;
                }

                // タグ行（例: [Common]）を検出
                if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                {
                    currentTag = trimmedLine.Trim('[', ']');
                    if (!configData.ContainsKey(currentTag))
                    {
                        configData[currentTag] = new Dictionary<string, string>();
                    }
                }
                else if (currentTag != null)
                {
                    // 項目と値を解析（例: key=value）
                    var keyValue = trimmedLine.Split('=', 2);
                    if (keyValue.Length == 2)
                    {
                        string key = keyValue[0].Trim();
                        string value = keyValue[1].Trim();
                        configData[currentTag][key] = value;
                    }
                }
            }

            return configData;
        }

        private void ConfigureSegmentReaderColors()
        {
            if (displayConfig.TryGetValue("Color", out var colorSection))
            {
                segmentReader.LoadColorConfigFromDictionary(colorSection);
            }
            else
            {
                Debug.WriteLine("Color セクションが見つかりませんでした。");
            }
        }
    }
}
