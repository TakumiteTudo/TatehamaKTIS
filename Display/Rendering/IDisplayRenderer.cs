using System.Collections.Generic;
using TatehamaKTIS.Display.Segment;
using System.Drawing;

namespace TatehamaKTIS.Display.Rendering
{
    internal interface IDisplayRenderer
    {
        /// <summary>
        /// 全体再描画を行う。戻り値はなく、レンダラ実装側で表示先を更新する責務を持ちます。
        /// </summary>
        void RenderFull(DisplayRenderRequest request);

        /// <summary>
        /// 差分描画を行う。レンダラ実装側で表示先を更新します。
        /// </summary>
        void RenderDelta(DisplayRenderRequest request);
    }
}
