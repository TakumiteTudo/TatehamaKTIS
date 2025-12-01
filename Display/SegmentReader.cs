using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using TatehamaKTIS.Display.Segment;

namespace TatehamaKTIS.Display
{
    internal class SegmentReader
    {
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
                var fields = line.Split('\t');
                if (fields.Length < 4)
                {
                    throw new InvalidDataException("データ行の形式が正しくありません。");
                }

                string name = fields[0];
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
                    case DisplaySegmentType.text:
                        string text = fields[6];
                        int scalarX = int.Parse(fields[7]);
                        int scalarY = int.Parse(fields[8]);
                        segments.Add(new TextSegment(name, x, y, color, baseColor, text, scalarX, scalarY));
                        break;

                    // 他のタイプの処理を追加可能
                    default:
                        throw new NotSupportedException($"サポートされていないタイプ: {type}");
                }
            }

            return segments;
        }

        private Color ParseColor(string colorString)
        {
            if (string.IsNullOrEmpty(colorString) || colorString.Equals("none", StringComparison.OrdinalIgnoreCase))
            {
                return Color.Transparent;
            }

            return Color.FromName(colorString);
        }
    }
}
