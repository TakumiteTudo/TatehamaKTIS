using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using TatehamaKTIS.Display.Segment;

namespace TatehamaKTIS.Display
{
    internal class SegmentReader
    {
        private readonly Dictionary<string, Color> colorMap = new Dictionary<string, Color>();

        public SegmentReader()
        {
        }

        public List<DisplaySegmentData> ReadSegmentsFromFile(string filePath)
        {
            var segments = new List<DisplaySegmentData>();

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"指定されたファイルが見つかりません: {filePath}");
            }

            var lines = File.ReadAllLines(filePath);

            if (lines.Length < 2)
            {
                throw new InvalidDataException("ファイルにデータ行が含まれていません。");
            }

            // ヘッダ行をスキップしてデータ行を処理
            foreach (var line in lines.Skip(1))
            {
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
                            string includeFilePath = Path.Combine(Path.GetDirectoryName(filePath) ?? string.Empty, fields[6] + ".txt");
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
                            for (int i = 13; i < fields.Length; i += 2)
                            {
                                if (i + 1 < fields.Length && !string.IsNullOrWhiteSpace(fields[i]) && !string.IsNullOrWhiteSpace(fields[i + 1]))
                                {
                                    functionList.Add(new Tuple<string, string>(fields[i], fields[i + 1]));
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
                    Debug.WriteLine($"行の解析中にエラーが発生しました: {line}. エラー: {ex.Message}");
                }
            }

            return segments;
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

        private void LoadColorConfig(string colorConfigPath)
        {
            if (!File.Exists(colorConfigPath))
            {
                throw new FileNotFoundException($"色設定ファイルが見つかりません: {colorConfigPath}");
            }

            var lines = File.ReadAllLines(colorConfigPath);

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("["))
                {
                    continue; // 空行やセクションヘッダをスキップ
                }

                var parts = line.Split('=');
                if (parts.Length != 2)
                {
                    throw new InvalidDataException($"色設定ファイルの形式が正しくありません: {line}");
                }

                string colorName = parts[0].Trim();
                string colorValue = parts[1].Trim();

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
                    throw new InvalidDataException($"無効な色コード: {colorValue}");
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
