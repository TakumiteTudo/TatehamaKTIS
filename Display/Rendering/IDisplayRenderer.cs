using System.Collections.Generic;
using TatehamaKTIS.Display.Segment;
using System.Drawing;

namespace TatehamaKTIS.Display.Rendering
{
    internal interface IDisplayRenderer
    {
        /// <summary>
        /// 全体再描画を行う。戻り値は System.Drawing.Image を返す。
        /// </summary>
        System.Drawing.Image RenderFull(DisplayRenderRequest request);

        /// <summary>
        /// 差分描画を行う。
        /// </summary>
        System.Drawing.Image RenderDelta(DisplayRenderRequest request);
    }
}
