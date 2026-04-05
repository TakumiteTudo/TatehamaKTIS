using System.Collections.Generic;
using TatehamaKTIS.Display.Segment;
using System.Drawing;

namespace TatehamaKTIS.Display.Rendering
{
    internal interface IDisplayRenderer
    {
        /// <summary>
        /// 全体再描画を行う。戻り値は実装に依存する（具体的なイメージ型を公開しないため object を使用）。
        /// </summary>
        object RenderFull(DisplayRenderRequest request);

        /// <summary>
        /// 差分描画を行う。changedSegments は変更のあったセグメント一覧。
        /// </summary>
        object RenderDelta(DisplayRenderRequest request, IReadOnlyList<DisplaySegmentData> changedSegments);
    }
}
