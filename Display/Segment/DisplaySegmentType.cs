using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TatehamaKTIS.Display.Segment
{
    public enum DisplaySegmentType
    {
        /// <summary>
        /// 参照
        /// </summary>
        include,
        /// <summary>
        /// 四角形
        /// </summary>
        box,
        /// <summary>
        /// 文字
        /// </summary>
        text,
        /// <summary>
        /// ボタン
        /// </summary>
        button,
        /// <summary>
        /// 画像
        /// </summary>
        image,
        /// <summary>
        /// 編成
        /// </summary>
        formation
    }
}
