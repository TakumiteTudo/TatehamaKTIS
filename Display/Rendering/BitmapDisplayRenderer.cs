using System.Collections.Generic;
using TatehamaKTIS.Display.Segment;
using System.Drawing;

namespace TatehamaKTIS.Display.Rendering
{
    /// <summary>
    /// 既存の Bitmap ベースの描画処理を暫定的にラップする実装（互換性用）。
    /// 将来はここに従来の描画ロジックを移植してください。
    /// </summary>
    internal class BitmapDisplayRenderer : IDisplayRenderer
    {
        public object RenderFull(DisplayRenderRequest request)
        {
            Bitmap bmp = new Bitmap(request.ScreenSize.Width, request.ScreenSize.Height);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
            }
            return bmp;
        }

        public object RenderDelta(DisplayRenderRequest request, IReadOnlyList<DisplaySegmentData> changedSegments)
        {
            // 簡易実装: 全体再描画にフォールバック
            return RenderFull(request);
        }
    }
}
