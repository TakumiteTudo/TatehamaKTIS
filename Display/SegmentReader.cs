using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using TatehamaKTIS.Display.Segment;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace TatehamaKTIS.Display
{
    internal class SegmentReader
    {
        private DisplayData displayData;
        private readonly Dictionary<string, Color> colorMap = new Dictionary<string, Color>();

        public SegmentReader(DisplayData displayData)
        {
            this.displayData = displayData;
        }

        public List<DisplaySegmentData> ReadSegmentsFromFile(string inputFilePath)
        {
            var segments = new List<DisplaySegmentData>();

            if (!File.Exists(inputFilePath))
            {
                throw new FileNotFoundException($"指定されたファイルが見つかりません: {inputFilePath}");
            }

            var fileLines = File.ReadAllLines(inputFilePath); // 変数名を変更

            if (fileLines.Length < 2)
            {
                throw new InvalidDataException("ファイルにデータ行が含まれていません。");
            }

            var lineStack = new Stack<string>(); // 入れ子の if を処理するためのスタック

            for (int i = 1; i < fileLines.Length; i++) // ヘッダ行をスキップ
            {
                var line = fileLines[i];
                try
                {
                    var fields = line.Split('\t');
                    if (fields.Length < 4)
                    {
                        throw new InvalidDataException("データ行の形式が正しくありません。");
                    }

                    string name = fields[0];

                    if (string.IsNullOrWhiteSpace(name))
                    {
                        continue;
                    }

                    // transition セグメントの処理
                    if (fields[3] == "transition")
                    {
                        string targetScreen = fields[6];
                        Debug.WriteLine($"画面遷移: {targetScreen}");

                        string targetFilePath = Path.Combine(Path.GetDirectoryName(inputFilePath) ?? string.Empty, targetScreen + ".txt");
                        if (File.Exists(targetFilePath))
                        {
                            // 再帰的に新しい画面を読み込む
                            return ReadSegmentsFromFile(targetFilePath);
                        }
                        else
                        {
                            throw new FileNotFoundException($"遷移先の画面ファイルが見つかりません: {targetFilePath}");
                        }
                    }

                    // if セグメントの処理
                    if (fields[3] == "if")
                    {
                        string condition = fields[6];
                        Debug.WriteLine($"　　評価：{condition}");
                        bool conditionResult = EvaluateCondition(condition);

                        Debug.WriteLine($"　　結果：{conditionResult}");
                        if (conditionResult)
                        {
                            lineStack.Push("if"); // 条件が適合する場合、処理を続行
                        }
                        else
                        {
                            // 条件が適合しない場合、対応する終了セグメントまでスキップ
                            int skipCount = 1;
                            while (skipCount > 0 && ++i < fileLines.Length)
                            {
                                var skipFields = fileLines[i].Split('\t');
                                if (skipFields[3] == "if")
                                {
                                    skipCount++;
                                }
                                else if (skipFields[3] == "end")
                                {
                                    skipCount--;
                                }
                            }
                        }
                        continue;
                    }

                    // 終了セグメントの処理
                    if (fields[3] == "end")
                    {
                        if (lineStack.Count > 0 && lineStack.Peek() == "if")
                        {
                            lineStack.Pop(); // 対応する if をスタックから削除
                        }
                        else
                        {
                            Debug.WriteLine($"end セグメントに対応する if が見つかりません: {line}");
                        }
                        continue;
                    }

                    // ボタン設定セグメントの処理
                    if (fields[3] == "buttonConfig")
                    {
                        string targetButtonName = fields[6];

                        // 対象のボタンを検索
                        var targetButton = segments.OfType<ButtonSegment>().FirstOrDefault(b => b.name == targetButtonName);
                        if (targetButton == null)
                        {
                            Debug.WriteLine($"ボタン設定セグメント: 対象のボタンが見つかりません: {targetButtonName}");
                            continue;
                        }

                        // isChecked の設定
                        if (int.TryParse(fields[1], out int isCheckedValue) && (isCheckedValue == 0 || isCheckedValue == 1))
                        {
                            targetButton.isChecked = isCheckedValue == 1;
                        }
                        else
                        {
                            Debug.WriteLine($"ボタン設定セグメント: isChecked の値が無効です: {fields[1]}");
                        }

                        // isLighting の設定
                        if (int.TryParse(fields[2], out int isLightingValue))
                        {
                            targetButton.isLighting = isLightingValue;
                        }
                        else
                        {
                            Debug.WriteLine($"ボタン設定セグメント: isLighting の値が無効です: {fields[2]}");
                        }

                        continue;
                    }

                    // 変数セグメントの処理
                    if (fields[3] == "variable")
                    {
                        string variableName = fields[0]; // 変数名
                        string variableValue = fields[6]; // 変数の値  
                        Debug.WriteLine($"　代入：{variableName} = {variableValue}");

                        // [var:<変数名>] の形式の場合、displayData から値を取得
                        if (variableValue.StartsWith("{var:") && variableValue.EndsWith("}"))
                        {
                            string referencedVariable = variableValue.Substring(5, variableValue.Length - 6);
                            if (displayData.GetAllData().TryGetValue(referencedVariable, out object referencedValueObj))
                            {
                                variableValue = referencedValueObj as string ?? string.Empty; // object を string にキャスト
                            }
                            else
                            {
                                Debug.WriteLine($"指定された変数が見つかりません: {referencedVariable}");
                                variableValue = string.Empty; // 見つからない場合は空文字列を代入
                            }
                        }
                        // [datafile:<filepath>] の形式の場合、TSV ファイルを読み込む
                        else if (variableValue.StartsWith("{datafile:") && variableValue.EndsWith("}"))
                        {
                            var data = variableValue.Substring(10, variableValue.Length - 11).Split(","); // 変数名変更
                            string dataFilePath = data[0];
                            bool isDataHeader = data.Length >= 2;
                            int DataHeader = 0;
                            if (isDataHeader)
                            {
                                int.TryParse(data[1], out DataHeader);
                            }

                            string fullPath = Path.Combine(Path.GetDirectoryName(inputFilePath) ?? string.Empty, dataFilePath);

                            if (!File.Exists(fullPath))
                            {
                                Debug.WriteLine($"指定されたデータファイルが見つかりません: {fullPath}");
                                continue;
                            }

                            try
                            {
                                // TSV ファイルを読み込み、先頭 2 行をスキップ
                                var dataFileLines = File.ReadAllLines(fullPath).Skip(2).ToArray();

                                // 2 次元配列を作成
                                var nestedData = new Dictionary<string, object>();

                                for (int rowIndex = 0; rowIndex < dataFileLines.Length; rowIndex++)
                                {
                                    var row = dataFileLines[rowIndex].Split('\t');

                                    // 空行をスキップ
                                    if (row.All(string.IsNullOrWhiteSpace))
                                    {
                                        continue;
                                    }

                                    // キーを生成
                                    string rowKey;
                                    if (isDataHeader && DataHeader > 0)
                                    {
                                        // 指定された数の先頭列を連結してキーを生成
                                        rowKey = string.Join("^", row.Take(DataHeader));
                                    }
                                    else
                                    {
                                        // デフォルトのキー（行インデックス）
                                        rowKey = rowIndex.ToString();
                                    }

                                    var rowDict = new Dictionary<string, object>();
                                    int rowMaxColumnIndex = 0; // 各行の最大データ列インデックス

                                    for (int colIndex = DataHeader; colIndex < row.Length; colIndex++) // DataHeader 列以降を処理
                                    {
                                        // 空文字列も保持
                                        rowDict[(colIndex - DataHeader).ToString()] = row[colIndex];

                                        // 全角スペースを保持するため、Trim を削除
                                        if (!string.IsNullOrEmpty(row[colIndex]))
                                        {
                                            rowMaxColumnIndex = colIndex; // 各行の最大データ列インデックスを更新
                                        }
                                    }

                                    // 末尾の空列を削除（各行ごとに処理）
                                    var keysToRemove = rowDict.Keys
                                        .Where(key => int.Parse(key) > (rowMaxColumnIndex - DataHeader)) // 各行の最大データ列インデックスを超える列を削除
                                        .ToList();

                                    foreach (var key in keysToRemove)
                                    {
                                        rowDict.Remove(key);
                                    }

                                    nestedData[rowKey] = rowDict;
                                }

                                // DisplayData に登録
                                displayData.RegisterNestedData(variableName, nestedData);
                                Debug.WriteLine($"データファイルを変数に登録しました: {variableName}");
                                continue;
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"データファイルの読み込み中にエラーが発生しました: {ex.Message}");
                            }
                        }
                        // displayData に値を設定                  
                        Debug.WriteLine($"　　代入結果: {variableName} = {variableValue}");
                        displayData[variableName] = variableValue;
                        continue;
                    }

                    // 他のセグメントの処理（既存のロジック）
                    int x = int.Parse(fields[1]);
                    int y = int.Parse(fields[2]);
                    if (!Enum.TryParse(fields[3], true, out DisplaySegmentType type))
                    {
                        throw new InvalidDataException($"無効なタイプ: {fields[3]}");
                    }

                    Color color = ParseColor(fields[4]);
                    Color baseColor = ParseColor(fields[5]);

                    switch (type)
                    {
                        case DisplaySegmentType.include:
                            string includeFilePath = Path.Combine(Path.GetDirectoryName(inputFilePath) ?? string.Empty, fields[6] + ".txt");
                            if (File.Exists(includeFilePath))
                            {
                                var includedSegments = ReadSegmentsFromFile(includeFilePath);
                                segments.AddRange(includedSegments);
                            }
                            else
                            {
                                throw new FileNotFoundException($"インクルードファイルが見つかりません: {includeFilePath}");
                            }
                            break;

                        case DisplaySegmentType.text:
                            string text = fields[6];
                            int scalarX = int.Parse(fields[7]);
                            int scalarY = int.Parse(fields[8]);
                            segments.Add(new TextSegment(name, x, y, color, baseColor, text, scalarX, scalarY));
                            break;

                        case DisplaySegmentType.box:
                            int sizeX = int.Parse(fields[9]);
                            int sizeY = int.Parse(fields[10]);
                            segments.Add(new BoxSegment(name, x, y, color, baseColor, sizeX, sizeY));
                            break;

                        case DisplaySegmentType.button:
                            string buttonText = fields[6];
                            int buttonScalarX = int.Parse(fields[7]);
                            int buttonScalarY = int.Parse(fields[8]);
                            int buttonSizeX = int.Parse(fields[9]);
                            int buttonSizeY = int.Parse(fields[10]);
                            string groupname = fields[11];
                            if (!Enum.TryParse(fields[12], true, out ButtonType buttonType))
                            {
                                throw new InvalidDataException($"無効なボタンタイプ: {fields[12]}");
                            }

                            // 関数リストを作成
                            var functionList = new List<Tuple<string, string>>();
                            for (int j = 13; j < fields.Length; j += 2)
                            {
                                if (j + 1 < fields.Length && !string.IsNullOrWhiteSpace(fields[j]) && !string.IsNullOrWhiteSpace(fields[j + 1]))
                                {
                                    functionList.Add(new Tuple<string, string>(fields[j], fields[j + 1]));
                                }
                            }

                            segments.Add(new ButtonSegment(name, x, y, color, baseColor, buttonText, buttonScalarX, buttonScalarY, buttonSizeX, buttonSizeY, fields[4], groupname, buttonType, functionList));
                            break;

                        case DisplaySegmentType.image:
                            string filename = fields[6];
                            segments.Add(new ImageSegment(name, x, y, color, baseColor, filename));
                            break;

                        case DisplaySegmentType.formation:
                            // 他のタイプの処理を追加可能
                            break;

                        default:
                            throw new NotSupportedException($"サポートされていないタイプ: {type}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"行の解析中にエラーが発生しました: {line}. エラー: {ex.Message}\n{ex.StackTrace}");
                }
            }

            return segments;
        }

        private bool EvaluateCondition(string condition)
        {
            try
            {
                var tokens = TokenizeCondition(condition);
                var postfix = ConvertToPostfix(tokens);
                return EvaluatePostfix(postfix);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"条件式の評価中にエラーが発生しました: {condition}. エラー: {ex.Message}");
                throw;
            }
        }

        private List<string> TokenizeCondition(string condition)
        {
            var tokens = new List<string>();
            var sb = new StringBuilder();

            for (int i = 0; i < condition.Length; i++)
            {
                char c = condition[i];

                if (char.IsWhiteSpace(c))
                {
                    if (sb.Length > 0)
                    {
                        tokens.Add(sb.ToString());
                        sb.Clear();
                    }
                }
                else if ("()=!><&|+*-/".Contains(c))
                {
                    if (sb.Length > 0)
                    {
                        tokens.Add(sb.ToString());
                        sb.Clear();
                    }

                    // 特殊ケース: "==" や "!=" などの2文字演算子を処理
                    if (i + 1 < condition.Length && "=!><".Contains(condition[i + 1]))
                    {
                        tokens.Add($"{c}{condition[i + 1]}");
                        i++; // 2文字目をスキップ
                    }
                    else if (c == '!' && i + 1 < condition.Length && condition[i + 1] == '(')
                    {
                        // "!(" の形式を1つのトークンとして扱う
                        tokens.Add("!(");
                        i++; // '(' をスキップ
                    }
                    else
                    {
                        tokens.Add(c.ToString());
                    }
                }
                else
                {
                    sb.Append(c);
                }
            }

            if (sb.Length > 0)
            {
                tokens.Add(sb.ToString());
            }

            //Debug.WriteLine(string.Join(",", tokens));
            return tokens;
        }

        private List<string> ConvertToPostfix(List<string> tokens)
        {
            var output = new List<string>();
            var operators = new Stack<string>();

            // 演算子の優先順位
            var precedence = new Dictionary<string, int>
            {
                { "||", 1 },
                { "&&", 2 },
                { "==", 3 }, { "!=", 3 }, { ">", 3 }, { "<", 3 }, { ">=", 3 }, { "<=", 3 },
                { "+", 4 }, { "-", 4 }, { "*", 5 }, { "/", 5 },
                { "!(" , 0 }, // "!(" の優先順位を高く設定
                { "(", 0 }, { ")", 0 }
            };

            foreach (var token in tokens)
            {
                if (IsOperand(token))
                {
                    output.Add(token); // オペランドはそのまま出力
                }
                else if (token == "(" || token == "!(")
                {
                    operators.Push(token);
                }
                else if (token == ")")
                {
                    while (operators.Count > 0 && operators.Peek() != "(" && operators.Peek() != "!(")
                    {
                        output.Add(operators.Pop());
                    }

                    // "!(" の場合は特別処理
                    if (operators.Count > 0 && operators.Peek() == "!(")
                    {
                        output.Add(operators.Pop());
                    }
                    else if (operators.Count > 0 && operators.Peek() == "(")
                    {
                        operators.Pop(); // "(" を削除
                    }
                }
                else
                {
                    while (operators.Count > 0 && precedence[operators.Peek()] >= precedence[token])
                    {
                        output.Add(operators.Pop());
                    }
                    operators.Push(token);
                }
            }

            while (operators.Count > 0)
            {
                output.Add(operators.Pop());
            }

            return output;
        }

        private bool EvaluatePostfix(List<string> postfix)
        {
            var stack = new Stack<object>();

            try
            {
                foreach (var token in postfix)
                {
                    if (IsOperand(token))
                    {
                        stack.Push(GetOperandValue(token));
                    }
                    else
                    {
                        if (token == "!(")
                        {
                            if (stack.Count < 1)
                            {
                                throw new InvalidOperationException($"条件式の評価中にスタックが不足しました。トークン: {token}");
                            }

                            var operand = stack.Pop();
                            stack.Push(!Convert.ToBoolean(operand));
                        }
                        else if ("+-*/".Contains(token))
                        {
                            if (stack.Count < 2)
                            {
                                throw new InvalidOperationException($"算術演算中にスタックが不足しました。トークン: {token}");
                            }

                            var right = stack.Pop();
                            var left = stack.Pop();
                            stack.Push(EvaluateArithmetic(left, right, token));
                        }
                        else
                        {
                            if (stack.Count < 2)
                            {
                                throw new InvalidOperationException($"条件式の評価中にスタックが不足しました。トークン: {token}");
                            }

                            var right = stack.Pop();
                            var left = stack.Pop();
                            stack.Push(EvaluateOperator(left, right, token));
                        }
                    }
                }

                if (stack.Count != 1)
                {
                    throw new InvalidOperationException("条件式の評価結果が不正です。スタックに残っている要素数が不正です。");
                }

                return Convert.ToBoolean(stack.Pop());
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ポストフィックス評価中にエラーが発生しました: {string.Join(" ", postfix)}. スタック状態: {string.Join(", ", stack)}. エラー: {ex.Message}");
                throw;
            }
        }

        private object GetOperandValue(string operand)
        {
            // シングルクォートで囲まれている場合は固定文字列として扱う
            if (operand.StartsWith("'") && operand.EndsWith("'"))
            {
                return ParseDynamicValue(operand.Substring(1, operand.Length - 2)); // クォートを除去
            }

            // 配列アクセス記法（例: 案内始発[設定運行路線名].Count）を処理
            if (operand.Contains("[") && operand.Contains("]"))
            {
                var baseKey = operand.Substring(0, operand.IndexOf("["));
                var indexKey = operand.Substring(operand.IndexOf("[") + 1, operand.IndexOf("]") - operand.IndexOf("[") - 1);

                // indexKey がシングルクォートで囲まれていない場合、変数として解釈
                if (!indexKey.StartsWith("'") || !indexKey.EndsWith("'"))
                {
                    var indexKeys = indexKey.Split("^");
                    if (indexKeys.Length >= 1)
                    {
                        var keys = new List<string>();
                        foreach (var key in indexKeys)
                        {
                            if (displayData.GetAllData().TryGetValue(key, out var keyValue))
                            {
                                keys.Add(keyValue as string ?? string.Empty);
                            }
                        }
                        indexKey = string.Join("^", keys);
                    }
                    else
                    {
                        Debug.WriteLine($"指定されたインデックス変数が見つかりません: {indexKey}");
                        return 0; // 見つからない場合は 0 を返す
                    }
                }
                else
                {
                    // シングルクォートを除去
                    indexKey = indexKey.Substring(1, indexKey.Length - 2);
                }
                Debug.WriteLine($"　　　　キー名変換：{indexKey}");

                if (operand.EndsWith(".Count"))
                {
                    if (displayData.GetAllData().TryGetValue(baseKey, out var baseValue) && baseValue is Dictionary<string, object> nestedDict)
                    {
                        if (nestedDict.TryGetValue(indexKey, out var targetValue) && targetValue is Dictionary<string, object> targetDict)
                        {
                            return targetDict.Count; // 要素数を返す
                        }
                    }
                    Debug.WriteLine($"指定された辞書が見つかりません: {operand}");
                    return 0; // 見つからない場合は 0 を返す
                }
            }

            // 変数の場合は displayData から取得
            if (displayData.GetAllData().TryGetValue(operand, out var value))
            {
                Debug.WriteLine($"　　　オペランド取得: {operand} = {value}");
                return ParseDynamicValue((string)value);
            }

            Debug.WriteLine($"オペランドが見つかりません: {operand}. displayData に存在しない可能性があります。");
            return null; // 存在しない場合は null を返す
        }

        private object ParseDynamicValue(string value)
        {
            // 全角数値を半角数値に変換
            value = ConvertFullWidthToHalfWidth(value);

            // 数値として解釈可能かチェック
            if (int.TryParse(value, out var intValue))
            {
                return intValue;
            }
            if (double.TryParse(value, out var doubleValue))
            {
                return doubleValue;
            }

            // それ以外は文字列として扱う
            return value;
        }

        private string ConvertFullWidthToHalfWidth(string input)
        {
            var sb = new StringBuilder();
            foreach (var c in input)
            {
                // 全角数値（U+FF10～U+FF19）を半角数値（U+0030～U+0039）に変換
                if (c >= '０' && c <= '９')
                {
                    sb.Append((char)(c - '０' + '0'));
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        private bool EvaluateOperator(object left, object right, string op)
        {
            Debug.WriteLine($"　　　型変換: 左辺 = {left}, 右辺 = {right}, 演算子 = {op}");

            // 動的型変換
            if (left is string leftStr && right is string rightStr)
            {
                left = ParseDynamicValue(leftStr);
                right = ParseDynamicValue(rightStr);
            }
            else if (left is string leftStrOnly)
            {
                left = ParseDynamicValue(leftStrOnly);
            }
            else if (right is string rightStrOnly)
            {
                right = ParseDynamicValue(rightStrOnly);
            }

            Debug.WriteLine($"　　　型変換後 - 左辺 = {left} ({left?.GetType()}), 右辺 = {right} ({right?.GetType()})");

            // null の処理
            if (left == null || right == null)
            {
                switch (op)
                {
                    case "==": return left == right; // 両方 null の場合のみ true
                    case "!=": return left != right; // どちらかが null の場合 true
                    default: return false; // 他の演算子では false
                }
            }

            // 型が不正に変換されていないかチェック
            if (left is bool || right is bool)
            {
                throw new InvalidOperationException($"オペランドの型が不正です: 左辺 = {left}, 右辺 = {right}");
            }

            // 演算子の処理
            switch (op)
            {
                case "==": return Equals(left, right);
                case "!=": return !Equals(left, right);
                case ">": return Convert.ToDouble(left) > Convert.ToDouble(right);
                case "<": return Convert.ToDouble(left) < Convert.ToDouble(right);
                case ">=": return Convert.ToDouble(left) >= Convert.ToDouble(right);
                case "<=": return Convert.ToDouble(left) <= Convert.ToDouble(right);
                case "&&": return Convert.ToBoolean(left) && Convert.ToBoolean(right);
                case "||": return Convert.ToBoolean(left) || Convert.ToBoolean(right);

                default: throw new InvalidOperationException($"不明な演算子: {op}");
            }
        }
        private object EvaluateArithmetic(object left, object right, string op)
        {
            // 左辺と右辺を数値に変換
            double leftValue = Convert.ToDouble(left);
            double rightValue = Convert.ToDouble(right);

            // 算術演算を実行
            return op switch
            {
                "+" => leftValue + rightValue,
                "-" => leftValue - rightValue,
                "*" => leftValue * rightValue,
                "/" => rightValue == 0 ? throw new DivideByZeroException("ゼロ除算が発生しました。") : leftValue / rightValue,
                _ => throw new InvalidOperationException($"不明な算術演算子: {op}")
            };
        }

        private bool IsOperand(string token)
        {
            // オペランド（変数名またはリテラル値）かどうかを判定
            return !new[] { "||", "&&", "==", "!=", ">", "<", ">=", "<=", "!(", "(", ")", "+", "-", "*", "/" }.Contains(token);
        }

        public void LoadColorConfigFromDictionary(Dictionary<string, string> colorConfig)
        {
            colorMap.Clear();
            foreach (var kvp in colorConfig)
            {
                string colorName = kvp.Key;
                string colorValue = kvp.Value;

                // "0x" プレフィックスを削除
                if (colorValue.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                {
                    colorValue = colorValue.Substring(2);
                }

                if (int.TryParse(colorValue, System.Globalization.NumberStyles.HexNumber, null, out int argb))
                {
                    colorMap[colorName] = Color.FromArgb(unchecked((int)argb));
                }
                else
                {
                    Debug.WriteLine($"無効な色コード: {colorValue}");
                }
            }
        }

        private Color ParseColor(string colorString)
        {
            if (string.IsNullOrEmpty(colorString) || colorString.Equals("none", StringComparison.OrdinalIgnoreCase))
            {
                return Color.Transparent;
            }

            if (colorMap.TryGetValue(colorString, out Color color))
            {
                return color;
            }

            throw new KeyNotFoundException($"指定された色名が見つかりません: {colorString}");
        }
    }
}
