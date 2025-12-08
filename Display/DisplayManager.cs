using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TatehamaKTIS.Display.Segment;
using TatehamaKTIS.Font;

namespace TatehamaKTIS.Display
{
    internal class DisplayManager
    {
        DisplayData DisplayData;
        string displayType;
        string displayName;
        Dictionary<string, Dictionary<string, string>> displayConfig;
        DisplayBuilder displayBuilder;
        SegmentReader segmentReader;
        internal Action<Bitmap> displayAction;

        private DateTime lastTouchTime = DateTime.MinValue; // 最後のタッチ時刻
        private TimeSpan minTouchInterval; // 最少間隔

        // 操作履歴キュー
        private readonly ConcurrentQueue<(string ActionType, int X, int Y)> operationQueue = new();
        private readonly SemaphoreSlim operationSemaphore = new(1, 1); // 同時実行を防ぐためのセマフォ

        internal DisplayManager()
        {
            SetDisplayType("Tatehama");
            minTouchInterval = TimeSpan.FromMilliseconds(100);

            // 操作履歴の処理を開始
            _ = ProcessOperationQueueAsync();
        }

        internal async Task SetDisplayType(string type)
        {
            DisplayData = new DisplayData();
            displayBuilder = new DisplayBuilder(DisplayData);
            segmentReader = new SegmentReader(DisplayData);
            displayType = type;
            displayConfig = ParseConfig();
            ConfigureSegmentReaderColors(); // 色設定を行う          
            minTouchInterval = TimeSpan.FromMilliseconds(80);

            await RunStartSequence();
        }

        internal void ModeSW()
        {
            if (!displayConfig.TryGetValue("Common", out var commonConfig) || !commonConfig.TryGetValue("modeswitch", out var startSequence))
            {
                Debug.WriteLine("モード切替未定義");
                return;
            }

            // 最後に default 画面を表示
            if (commonConfig.TryGetValue("modeswitch", out var modeScreen))
            {
                Debug.WriteLine($"モード切替画面に遷移: {modeScreen}");
                var defaultSegments = segmentReader.ReadSegmentsFromFile($"Data/{displayType}/{modeScreen}.txt");
                if (defaultSegments != null && defaultSegments.Count > 0)
                {
                    displayBuilder.displaySegmentDatas = defaultSegments;
                    DisplayUpdate();
                }
            }
        }

        internal void DisplayUpdate()
        {
            var displayImage = displayBuilder.BuildDisplayImage();
            displayAction?.Invoke(displayImage);
        }

        internal void DisplayUpdateDiff()
        {
            var displayImage = displayBuilder.BuildDisplayImageDiff();
            displayAction?.Invoke(displayImage);
        }

        internal void DisplayUpdateDiff(List<DisplaySegmentData> displaySegmentData)
        {
            var displayImage = displayBuilder.BuildDisplayImageDiff(displaySegmentData);
            displayAction?.Invoke(displayImage);
        }

        // タッチダウンイベントを操作履歴に追加
        internal void DisplayTotchDown(int x, int y)
        {
            operationQueue.Enqueue(("Down", x, y));
        }

        // タッチアップイベントを操作履歴に追加
        internal void DisplayTotchUp(int x, int y)
        {
            operationQueue.Enqueue(("Up", x, y));
        }

        // 操作履歴を順次処理する非同期タスク
        private async Task ProcessOperationQueueAsync()
        {
            while (true)
            {
                // キューが空の場合は待機
                if (operationQueue.IsEmpty)
                {
                    await Task.Delay(2);
                    continue;
                }

                // キューから操作を取得
                if (operationQueue.TryDequeue(out var operation))
                {
                    await operationSemaphore.WaitAsync(); // 同時実行を防ぐ
                    try
                    {
                        var (actionType, x, y) = operation;
                        switch (actionType)
                        {
                            case "Down":
                                await HandleTouchDownAsync(x, y);
                                break;
                            case "Up":
                                await HandleTouchUpAsync(x, y);
                                break;
                        }
                    }
                    finally
                    {
                        operationSemaphore.Release();
                        await Task.Delay(50);
                    }
                }
            }
        }

        // タッチダウン処理
        private async Task HandleTouchDownAsync(int x, int y)
        {
            Debug.WriteLine($"タッチ：{DateTime.Now:O}");
            var timeSinceLastTouch = DateTime.Now - lastTouchTime;
            if (timeSinceLastTouch < minTouchInterval)
            {
                var waitTime = minTouchInterval - timeSinceLastTouch;
                await Task.Delay(waitTime);
            }
            lastTouchTime = DateTime.Now; // 最後のタッチ時刻を更新

            Debug.WriteLine($"タッチ：{x}, {y}");
            var button = DetectButtonTouch(x, y);
            if (button != null)
            {
                Debug.WriteLine($"ボタン押：{button.name}");
                button.isChecked = !button.isChecked;
            }
            Debug.WriteLine($"DisplayUpdateDiff：{DateTime.Now:O}");
            DisplayUpdateDiff([button]);
            Debug.WriteLine($"タッチ終：{DateTime.Now:O}");
        }

        // タッチアップ処理
        private async Task HandleTouchUpAsync(int x, int y)
        {
            var timeSinceLastTouch = DateTime.Now - lastTouchTime;
            if (timeSinceLastTouch < minTouchInterval)
            {
                var waitTime = minTouchInterval - timeSinceLastTouch;
                await Task.Delay(waitTime);
            }
            lastTouchTime = DateTime.Now; // 最後のタッチ時刻を更新

            var button = DetectButtonTouch(x, y);
            if (button != null)
            {
                if (button.buttonType == ButtonType.function)
                {
                    button.isChecked = false;
                    foreach (var func in button.functionList)
                    {
                        Debug.WriteLine($"関数実行: {func.Item1} パラメータ: {func.Item2}");
                        switch (func.Item1)
                        {
                            case "transition":
                                var newDisplayName = func.Item2;
                                Debug.WriteLine($"画面遷移: {newDisplayName}");
                                try
                                {
                                    var newSegments = segmentReader.ReadSegmentsFromFile($"Data/Tatehama/{newDisplayName}.txt");
                                    if (newSegments != null && newSegments.Count > 0)
                                    {
                                        displayBuilder.displaySegmentDatas = newSegments;
                                    }
                                }
                                catch (FileNotFoundException ex)
                                {
                                    Debug.WriteLine($"画面未定義：{newDisplayName}");
                                }
                                break;
                        }
                    }
                }
            }
            DisplayUpdateDiff([button]);
        }

        public ButtonSegment? DetectButtonTouch(int touchX, int touchY)
        {
            foreach (var segment in displayBuilder.displaySegmentDatas)
            {
                if (segment is ButtonSegment button)
                {
                    if (touchX >= button.x && touchX <= button.x + button.sizeX &&
                        touchY >= button.y && touchY <= button.y + button.sizeY)
                    {
                        return button;
                    }
                }
            }
            return null;
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

        private async Task RunStartSequence()
        {
            if (!displayConfig.TryGetValue("Common", out var commonConfig) || !commonConfig.TryGetValue("start", out var startSequence))
            {
                Debug.WriteLine("スタートシーケンスが定義されていません。");
                return;
            }

            // スタートシーケンスを解析
            var sequences = startSequence.Split('/');
            foreach (var sequence in sequences)
            {
                var parts = sequence.Split(',');
                if (parts.Length != 2)
                {
                    Debug.WriteLine($"無効なシーケンス形式: {sequence}");
                    continue;
                }

                var screenName = parts[0].Trim();
                if (!int.TryParse(parts[1].Trim(), out int durationMs))
                {
                    Debug.WriteLine($"無効な表示秒数: {parts[1]}");
                    continue;
                }

                // 画面を切り替え
                Debug.WriteLine($"画面遷移: {screenName} ({durationMs}ms)");
                var newSegments = segmentReader.ReadSegmentsFromFile($"Data/{displayType}/{screenName}.txt");
                if (newSegments != null && newSegments.Count > 0)
                {
                    displayBuilder.displaySegmentDatas = newSegments;
                    DisplayUpdate();
                }

                // 指定された時間待機
                await Task.Delay(durationMs);
            }

            // 最後に default 画面を表示
            if (commonConfig.TryGetValue("default", out var defaultScreen))
            {
                Debug.WriteLine($"デフォルト画面に遷移：{defaultScreen}");
                try
                {
                    var defaultSegments = segmentReader.ReadSegmentsFromFile($"Data/{displayType}/{defaultScreen}.txt");
                    if (defaultSegments != null && defaultSegments.Count > 0)
                    {
                        displayBuilder.displaySegmentDatas = defaultSegments;
                        DisplayUpdate();
                    }
                }
                catch (FileNotFoundException ex)
                {
                    Debug.WriteLine($"画面未定義：{defaultScreen}");
                }
            }
        }
    }
}
