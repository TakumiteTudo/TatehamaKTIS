using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TatehamaKTIS.Display
{
    internal class ButtonConfig
    {
        /// <summary>
        /// 非点灯・非選択時の文字色
        /// </summary>
        public Color TextCF { get; set; } = Color.Black;
        /// <summary>
        /// 非点灯・選択時の文字色
        /// </summary>
        public Color TextCT { get; set; } = Color.Black;
        /// <summary>
        /// 点灯・非選択時の文字色
        /// </summary>
        public Color TextCFL { get; set; } = Color.Black;
        /// <summary>
        /// 点灯・選択時の文字色
        /// </summary>
        public Color TextCTL { get; set; } = Color.Black;
        /// <summary>
        /// 角の固定領域の幅
        /// </summary>
        public int CornerX { get; set; } = 0;
        /// <summary>
        /// 角の固定領域の高さ
        /// </summary>
        public int CornerY { get; set; } = 0;
    }
}
