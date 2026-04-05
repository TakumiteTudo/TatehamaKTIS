using System.Collections.Generic;
using TatehamaKTIS.Display.Segment;
using System.Collections.Generic;
using TatehamaKTIS.Font;

namespace TatehamaKTIS.Display.Rendering
{
    internal class DisplayRenderRequest
    {
        // セグメント一覧
        public IReadOnlyList<DisplaySegmentData> Segments { get; set; } = new List<DisplaySegmentData>();

        // 変更のあったセグメント一覧（差分描画時に使用）
        public IReadOnlyList<DisplaySegmentData>? ChangedSegments { get; set; }

        // 描画領域の幅・高さ（GDI 型に依存しないプリミティブ）
        public int Width { get; set; } = 800;
        public int Height { get; set; } = 600;

        // 文字列サービスやボタン設定など、レンダラが必要とする情報はリクエスト経由で渡す
        public IStringService? StringService { get; set; }
        public IReadOnlyDictionary<string, TatehamaKTIS.Display.ButtonConfig>? ButtonConfigs { get; set; }
    }
}
