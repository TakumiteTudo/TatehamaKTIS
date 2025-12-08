using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TatehamaKTIS.Display
{
    internal class DisplayData
    {
        // 内部データ構造
        private readonly Dictionary<string, string> displayDatas = new();

        // インデクサーの実装
        public string this[string key]
        {
            get
            {
                // 存在しないキーにアクセスした場合、空文字列を返す
                if (!displayDatas.ContainsKey(key))
                {
                    displayDatas[key] = string.Empty; // 自動的にキーを追加
                }
                return displayDatas[key];
            }
            set
            {
                // 存在しないキーの場合、自動的に追加
                displayDatas[key] = value;
            }
        }

        // データをすべて取得するメソッド（デバッグや確認用）
        public Dictionary<string, string> GetAllData()
        {
            return new Dictionary<string, string>(displayDatas);
        }
    }
}
