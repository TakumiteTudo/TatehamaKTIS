using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace TatehamaKTIS.Display
{
    internal class DisplayData : IDisposable
    {
        // 内部データ構造
        private readonly Dictionary<string, object> displayDatas = new();

        DisplayDataViewer displayDataViewer;

        internal DisplayData()
        {
            displayDataViewer = new DisplayDataViewer(this);
            displayDataViewer.Show();
        }

        // Dispose メソッドの実装
        public void Dispose()
        {
            // displayDataViewer を閉じる
            if (displayDataViewer != null && !displayDataViewer.IsDisposed)
            {
                displayDataViewer.Close();
                displayDataViewer.Dispose();
                displayDataViewer = null;
            }
        }

        // インデクサーの実装
        public string this[string key]
        {
            get => GetValueByKey(key);
            set => SetValueByKey(key, value);
        }

        // データをすべて取得するメソッド（デバッグや確認用）
        public Dictionary<string, object> GetAllData()
        {
            return new Dictionary<string, object>(displayDatas);
        }

        // キーを解釈して値を取得
        private string GetValueByKey(string key)
        {
            //Debug.WriteLine($"　　　キー検索：{key}");
            var (baseKey, indices) = ParseKey(key);

            if (!displayDatas.ContainsKey(baseKey))
            {
                return string.Empty;
            }

            object current = displayDatas[baseKey];

            foreach (var index in indices)
            {
                //Debug.WriteLine($"　　　　キーindex：{index}");
                if (current is Dictionary<string, object> dict && dict.ContainsKey(index))
                {
                    current = dict[index];
                }
                else
                {
                    //Debug.WriteLine($"　　　キー結果：{key} => {string.Empty}");
                    return string.Empty;
                }
            }

            // 値が辞書型でなく、単純な文字列の場合はそのまま返す
            if (current is string strValue)
            {
                //Debug.WriteLine($"　　　キー結果：{key} => {strValue}");
                return strValue;
            }

            // 辞書型の場合は JSON 形式の文字列に変換して返す
            if (current is Dictionary<string, object> nestedDict)
            {
                //Debug.WriteLine($"　　　キー結果：{key} => {System.Text.Json.JsonSerializer.Serialize(nestedDict)}");
                return System.Text.Json.JsonSerializer.Serialize(nestedDict);
            }

            //Debug.WriteLine(current?.ToString() ?? string.Empty);
            return current?.ToString() ?? string.Empty;
        }

        // キーを解釈して値を設定
        private void SetValueByKey(string key, object value)
        {
            var (baseKey, indices) = ParseKey(key);

            if (!displayDatas.ContainsKey(baseKey))
            {
                // 新しいキーの場合、文字列ならそのまま登録
                if (value is string)
                {
                    displayDatas[baseKey] = value;
                    return;
                }

                // それ以外の場合は辞書型として初期化
                displayDatas[baseKey] = new Dictionary<string, object>();
            }

            if (indices.Count == 0)
            {
                displayDatas[baseKey] = value;
                return;
            }

            object current = displayDatas[baseKey];
            for (int i = 0; i < indices.Count - 1; i++)
            {
                var index = indices[i];
                if (current is Dictionary<string, object> dict)
                {
                    if (!dict.ContainsKey(index))
                    {
                        dict[index] = new Dictionary<string, object>();
                    }
                    current = dict[index];
                }
                else
                {
                    throw new InvalidOperationException($"キーの形式が不正です: {key}");
                }
            }

            if (current is Dictionary<string, object> finalDict)
            {
                // 最後のインデックスに値を設定
                finalDict[indices[^1]] = value;
            }
            else
            {
                throw new InvalidOperationException($"キーの形式が不正です: {key}");
            }
        }

        // キーを解析してベースキーとインデックスのリストを取得
        private (string baseKey, List<string> indices) ParseKey(string key)
        {
            // キーが単純な文字列の場合（例: "画面数"）
            if (!key.Contains("["))
            {
                return (key, new List<string>());
            }

            var match = Regex.Match(key, @"^(?<baseKey>[^\[]+)(?<indices>(\[[^\]]+\])*)$");
            if (!match.Success)
            {
                throw new ArgumentException($"キーの形式が不正です: {key}");
            }

            var baseKey = match.Groups["baseKey"].Value;
            var indices = new List<string>();

            var indexMatches = Regex.Matches(match.Groups["indices"].Value, @"\[(?<index>[^\]]+)\]");
            foreach (Match indexMatch in indexMatches)
            {
                var index = indexMatch.Groups["index"].Value;

                if (index.StartsWith("'") && index.EndsWith("'"))
                {
                    indices.Add(EvaluateExpression(index.Trim('\'')));
                }
                else if (index.Contains("^"))
                {
                    indices.Add(EvaluateCompositeKey(index));
                }
                else if (displayDatas.ContainsKey(index))
                {
                    indices.Add(this[index]);
                }
                else
                {
                    indices.Add(index);
                }
            }

            return (baseKey, indices);
        }

        private string EvaluateCompositeKey(string key)
        {
            var parts = key.Split('^');
            var resolvedParts = new List<string>();

            foreach (var part in parts)
            {
                if (displayDatas.ContainsKey(part))
                {
                    resolvedParts.Add(this[part]);
                }
                else
                {
                    resolvedParts.Add(part);
                }
            }

            return string.Join("^", resolvedParts);
        }

        private string EvaluateExpression(string expression)
        {
            try
            {
                // 式内のシングルクォートを削除
                expression = expression.Replace("'", "");

                var dataTable = new DataTable();
                foreach (var key in displayDatas.Keys)
                {
                    if (expression.Contains(key))
                    {
                        expression = expression.Replace(key, this[key]);
                    }
                }

                var result = dataTable.Compute(expression, null);
                return result?.ToString() ?? string.Empty;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"数式の評価に失敗しました: {expression}. エラー: {ex.Message}");
            }
        }

        // n次元配列をまとめて登録するメソッド
        public void RegisterNestedData(string baseKey, Dictionary<string, object> nestedData)
        {
            if (!displayDatas.ContainsKey(baseKey))
            {
                displayDatas[baseKey] = new Dictionary<string, object>();
            }

            if (displayDatas[baseKey] is not Dictionary<string, object> baseDict)
            {
                throw new InvalidOperationException($"ベースキー {baseKey} は辞書型ではありません。");
            }

            MergeDictionaries(baseDict, nestedData);
        }

        // 辞書を再帰的にマージするヘルパーメソッド
        private void MergeDictionaries(Dictionary<string, object> target, Dictionary<string, object> source)
        {
            foreach (var kvp in source)
            {
                if (kvp.Value is Dictionary<string, object> sourceDict)
                {
                    if (!target.ContainsKey(kvp.Key))
                    {
                        target[kvp.Key] = new Dictionary<string, object>();
                    }

                    if (target[kvp.Key] is Dictionary<string, object> targetDict)
                    {
                        MergeDictionaries(targetDict, sourceDict);
                    }
                    else
                    {
                        throw new InvalidOperationException($"キー {kvp.Key} の型が一致しません。");
                    }
                }
                else
                {
                    target[kvp.Key] = kvp.Value;
                }
            }
        }
    }
}
