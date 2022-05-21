using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Saro.XAsset
{
    internal class XAssetReferenceTab : TabWindow
    {
        private TreeViewState m_XAssetReferenceTreeState;
        private MultiColumnHeaderState m_XAssetReferenceTreeMCHState;
        private XAssetReferenceTree m_XAssetReferenceTree;

        public override string TabName => "Reference";

        public override void OnEnable()
        {
            if (m_XAssetReferenceTreeState == null)
                m_XAssetReferenceTreeState = new TreeViewState();

            var headerState = XAssetReferenceTree.CreateDefaultMultiColumnHeaderState();// multiColumnTreeViewRect.width);
            if (MultiColumnHeaderState.CanOverwriteSerializedFields(m_XAssetReferenceTreeMCHState, headerState))
                MultiColumnHeaderState.OverwriteSerializedFields(m_XAssetReferenceTreeMCHState, headerState);
            m_XAssetReferenceTreeMCHState = headerState;

            m_XAssetReferenceTree = new XAssetReferenceTree(m_XAssetReferenceTreeState, m_XAssetReferenceTreeMCHState);
        }

        public override void OnGUI(Rect rect)
        {
            m_XAssetReferenceTree.Reload();
            m_XAssetReferenceTree.OnGUI(rect);
        }
    }
}
