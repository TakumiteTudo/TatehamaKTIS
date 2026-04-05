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
        DisplayData displayData;
        string displayType;
        Dictionary<string, Dictionary<string, string>> displayConfig;
        DisplayBuilder displayBuilder;
        SegmentReader segmentReader;
        internal Action<object> displayAction;
        private Rendering.IDisplayRenderer? renderer;

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
            displayData = new DisplayData();
            displayBuilder = new DisplayBuilder(displayData);
            // レンダラをDisplayBuilderベースの既存実装で初期化
            renderer = new Rendering.BitmapDisplayRenderer(displayBuilder);
            segmentReader = new SegmentReader(displayData);
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
            if (renderer != null)
            {
                var request = new Rendering.DisplayRenderRequest
                {
                    Segments = displayBuilder.displaySegmentDatas,
                    ScreenSize = new System.Drawing.Size(800, 600)
                };
                var img = renderer.RenderFull(request);
                displayAction?.Invoke(img);
            }
            else
            {
                var displayImage = displayBuilder.BuildDisplayImage();
                displayAction?.Invoke(displayImage);
            }
        }

        internal void DisplayUpdateDiff()
        {
            if (renderer != null)
            {
                var changed = displayBuilder.FilterChangedSegments();
                var request = new Rendering.DisplayRenderRequest
                {
                    Segments = displayBuilder.displaySegmentDatas,
                    ChangedSegments = changed,
                    ScreenSize = new System.Drawing.Size(800, 600)
                };
                var img = renderer.RenderDelta(request, changed);
                displayAction?.Invoke(img);
            }
            else
            {
                var displayImage = displayBuilder.BuildDisplayImageDiff();
                displayAction?.Invoke(displayImage);
            }
        }

        internal void DisplayUpdateDiff(List<DisplaySegmentData> displaySegmentData)
        {
            if (renderer != null)
            {
                var request = new Rendering.DisplayRenderRequest
                {
                    Segments = displayBuilder.displaySegmentDatas,
                    ChangedSegments = displaySegmentData,
                    ScreenSize = new System.Drawing.Size(800, 600)
                };
                var img = renderer.RenderDelta(request, displaySegmentData);
                displayAction?.Invoke(img);
            }
            else
            {
                var displayImage = displayBuilder.BuildDisplayImageDiff(displaySegmentData);
                displayAction?.Invoke(displayImage);
            }
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
            var timeSinceLastTouch = DateTime.Now - lastTouchTime;
            if (timeSinceLastTouch < minTouchInterval)
            {
                var waitTime = minTouchInterval - timeSinceLastTouch;
                await Task.Delay(waitTime);
            }
            lastTouchTime = DateTime.Now; // 最後のタッチ時刻を更新

            Debug.WriteLine($"タッチ：{x}, {y}");
            var button = DetectButtonTouch(x, y);
            if (button != null && button.isVisible)
            {
                Debug.WriteLine($"ボタン押：{button.name}");
                if (button.buttonType != ButtonType.checkbox)
                {
                    button.isChecked = !button.isChecked;
                    DisplayUpdateDiff([button]);
                }
            }
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
            var doDisplayUpdate = false;

            var button = DetectButtonTouch(x, y);
            if (button != null && button.isVisible)
            {
                if (button.buttonType == ButtonType.checkbox)
                {
                    button.isChecked = !button.isChecked;
                }
                else
                {
                    button.isChecked = false;
                }
                foreach (var func in button.functionList)
                {
                    Debug.WriteLine($"関数実行: {func.Item1} パラメータ: {func.Item2}");
                    try
                    {
                        switch (func.Item1)
                        {
                            case "assignment":
                                HandleAssignment(func.Item2);
                                break;

                            case "exclusive":
                                HandleExclusive(button.groupname, button);
                                break;

                            case "buttonConfig":
                                HandleButtonConfig(func.Item2);
                                break;

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
                                    doDisplayUpdate = true;
                                    DisplayUpdate();
                                }
                                catch (FileNotFoundException ex)
                                {
                                    Debug.WriteLine($"画面未定義：{newDisplayName}");
                                }
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"関数実行エラー：{ex.Message}\n{ex.StackTrace}");
                    }
                }
                if (!doDisplayUpdate)
                {
                    DisplayUpdateDiff([button]);
                }
            }
        }

        public ButtonSegment? DetectButtonTouch(int touchX, int touchY)
        {
            foreach (var segment in displayBuilder.displaySegmentDatas)
            {
                if (segment is ButtonSegment button)
                {
                    // ボタンが非表示の場合はスキップ
                    if (!button.isVisible)
                    {
                        continue;
                    }

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
        private void HandleAssignment(string parameter)
        {
            // '+=' または '-=' を含むか確認
            if (parameter.Contains("+=") || parameter.Contains("-="))
            {
                // '+=' または '-=' を中心に分割
                var parts = parameter.Split(new[] { "+=", "-=" }, 2, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2)
                {
                    Debug.WriteLine($"無効な assignment パラメータ: {parameter}");
                    return;
                }

                string left = parts[0].Trim(); // 左辺の変数名
                string right = parts[1].Trim(); // 右辺の値または変数名

                // 現在の値を取得（存在しない場合は 0 をデフォルト値とする）
                if (!displayData.GetAllData().TryGetValue(left, out var currentValueStr) || !int.TryParse((string)currentValueStr, out int currentValue))
                {
                    currentValue = 0;
                }

                // 右辺の値を取得
                int rightValue;
                if (right.StartsWith("'") && right.EndsWith("'"))
                {
                    // リテラル値の場合
                    rightValue = int.Parse(right.Trim('\''));
                }
                else if (displayData.GetAllData().TryGetValue(right, out var rightValueStr) && int.TryParse((string)rightValueStr, out int parsedRightValue))
                {
                    // 変数の場合
                    rightValue = parsedRightValue;
                }
                else
                {
                    Debug.WriteLine($"無効な右辺値: {right}");
                    return;
                }

                // 加算または減算を実行
                int result = parameter.Contains("+=") ? currentValue + rightValue : currentValue - rightValue;

                // 結果を保存
                displayData[left] = result.ToString();
                Debug.WriteLine($"　　代入結果: {left} = {result}");
            }
            else
            {
                // '=' を中心に分割し、前後の空白を削除
                var parts = parameter.Split(new[] { '=' }, 2, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2)
                {
                    Debug.WriteLine($"無効な assignment パラメータ: {parameter}");
                    return;
                }

                string left = parts[0].Trim(); // 左辺の変数名
                string right = parts[1].Trim(); // 右辺の値または変数名
                string data;

                if (right.StartsWith("'") && right.EndsWith("'"))
                {
                    // <変数> = '<値>'
                    string value = right.Trim('\'');
                    data = value;
                }
                else if (right.StartsWith("group."))
                {
                    // <変数> = group.<ボタングループ名>
                    string groupName = right.Substring(6);
                    var checkedButtons = displayBuilder.displaySegmentDatas
                        .OfType<ButtonSegment>()
                        .Where(b => b.groupname == groupName && b.isChecked)
                        .Select(b => b.name);

                    data = string.Join(",", checkedButtons);
                }
                else if (right.StartsWith("grouptext."))
                {
                    // <変数> = grouptext.<ボタングループ名>
                    string groupName = right.Substring(10);
                    var checkedButtons = displayBuilder.displaySegmentDatas
                        .OfType<ButtonSegment>()
                        .Where(b => b.groupname == groupName && b.isChecked)
                        .Select(b => displayBuilder.stringService.InterpretString(b.Text));

                    data = string.Join(",", checkedButtons);
                }
                else
                {
                    // <変数> = <変数>
                    if (displayData.GetAllData().TryGetValue(right, out object referencedValueObj))
                    {
                        data = referencedValueObj as string ?? string.Empty; // object を string にキャスト
                    }
                    else
                    {
                        Debug.WriteLine($"指定された変数が見つかりません: {right}");
                        data = string.Empty; // 見つからない場合は空文字列を代入
                    }
                }
                displayData[left] = data;
                Debug.WriteLine($"　　代入結果: {left} = {data}");
            }
        }

        private void HandleExclusive(string groupName, ButtonSegment currentButton)
        {
            var buttonsInGroup = displayBuilder.displaySegmentDatas
                .OfType<ButtonSegment>()
                .Where(b => b.groupname == groupName);

            foreach (var button in buttonsInGroup)
            {
                if (button == currentButton)
                {
                    button.isChecked = true;
                }
                else
                {
                    if (button.isChecked == true)
                    {
                        // 他のボタンは isChecked を false に設定
                        button.isChecked = false;
                        DisplayUpdateDiff([button]);
                    }
                }
            }
        }

        private void HandleButtonConfig(string parameter)
        {
            var parts = parameter.Split(':');
            if (parts.Length != 2)
            {
                Debug.WriteLine($"無効な buttonConfig パラメータ: {parameter}");
                return;
            }

            string buttonName = parts[0].Trim();
            var config = parts[1].Trim().Split(',');

            var targetButton = displayBuilder.displaySegmentDatas
                .OfType<ButtonSegment>()
                .FirstOrDefault(b => b.name == buttonName);

            if (targetButton == null)
            {
                Debug.WriteLine($"ボタンが見つかりません: {buttonName}");
                return;
            }

            if (config.Length >= 2 &&
                int.TryParse(config[0].ToString(), out int isCheckedValue) &&
                int.TryParse(config[1].ToString(), out int isLightingValue))
            {
                targetButton.isChecked = isCheckedValue == 1;
                targetButton.isLighting = isLightingValue;
            }
            else
            {
                Debug.WriteLine($"無効なボタン設定: {config}");
            }
            DisplayUpdateDiff([targetButton]);
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
