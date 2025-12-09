using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace TatehamaKTIS.Display
{
    internal partial class DisplayDataViewer : Form
    {
        private readonly DisplayData displayData;

        public DisplayDataViewer(DisplayData displayData)
        {
            InitializeComponent();
            this.displayData = displayData;

            LoadDisplayData();
        }

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
        }

        private void LoadDisplayData()
        {
            // DisplayData の内容をツリービューに表示
            treeViewDisplayData.Nodes.Clear();
            var allData = displayData.GetAllData();

            foreach (var kvp in allData)
            {
                var rootNode = new TreeNode(kvp.Key);
                AddChildNodes(rootNode, kvp.Value);
                treeViewDisplayData.Nodes.Add(rootNode);
            }
        }

        private void AddChildNodes(TreeNode parentNode, object value)
        {
            if (value is Dictionary<string, object> dict)
            {
                foreach (var kvp in dict)
                {
                    var childNode = new TreeNode(kvp.Key);
                    AddChildNodes(childNode, kvp.Value);
                    parentNode.Nodes.Add(childNode);
                }
            }
            else
            {
                parentNode.Nodes.Add(new TreeNode(value?.ToString() ?? "null"));
            }
        }

        private List<string> GetExpandedNodes(TreeNodeCollection nodes)
        {
            var expandedNodes = new List<string>();
            foreach (TreeNode node in nodes)
            {
                if (node.IsExpanded)
                {
                    expandedNodes.Add(node.FullPath);
                }

                expandedNodes.AddRange(GetExpandedNodes(node.Nodes));
            }
            return expandedNodes;
        }

        private void RestoreExpandedNodes(TreeNodeCollection nodes, List<string> expandedNodes)
        {
            foreach (TreeNode node in nodes)
            {
                if (expandedNodes.Contains(node.FullPath))
                {
                    node.Expand();
                }

                RestoreExpandedNodes(node.Nodes, expandedNodes);
            }
        }

        private void buttonRefresh_Click(object sender, EventArgs e)
        {
            // ツリービューの展開状態を記録
            var expandedNodes = GetExpandedNodes(treeViewDisplayData.Nodes);

            // データを更新
            LoadDisplayData();

            // 展開状態を復元
            RestoreExpandedNodes(treeViewDisplayData.Nodes, expandedNodes);
        }
    }
}
