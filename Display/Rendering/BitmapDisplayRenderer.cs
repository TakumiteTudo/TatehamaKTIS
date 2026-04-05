using System.Collections.Generic;
using TatehamaKTIS.Display.Segment;
using System.Drawing;
using TatehamaKTIS.Display;

namespace TatehamaKTIS.Display.Rendering
{
    /// <summary>
    /// 既存の Bitmap ベースの描画処理を暫定的にラップする実装（互換性用）。
    /// DisplayBuilder の既存ロジックを使って Bitmap を返す。
    /// </summary>
    internal class BitmapDisplayRenderer : IDisplayRenderer
    {
        private readonly DisplayBuilder builder;

        internal BitmapDisplayRenderer(DisplayBuilder builder)
        {
            this.builder = builder;
        }

        public object RenderFull(DisplayRenderRequest request)
        {
            // DisplayBuilder 側の既存実装を利用
            var bmp = builder.BuildDisplayImage();
            return bmp;
        }

        public object RenderDelta(DisplayRenderRequest request, IReadOnlyList<DisplaySegmentData> changedSegments)
        {
            if (changedSegments == null || changedSegments.Count == 0)
            {
                return builder.BuildDisplayImageDiff();
            }
            return builder.BuildDisplayImageDiff(new List<DisplaySegmentData>(changedSegments));
        }
    }
}
