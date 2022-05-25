using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Saro.MoonAsset
{
    internal class MoonAssetReferenceTab : TabWindow
    {
        private TreeViewState m_MoonAssetReferenceTreeState;
        private MultiColumnHeaderState m_MoonAssetReferenceTreeMCHState;
        private MoonAssetReferenceTree m_MoonAssetReferenceTree;

        public override string TabName => "Reference";

        public override void OnEnable()
        {
            if (m_MoonAssetReferenceTreeState == null)
                m_MoonAssetReferenceTreeState = new TreeViewState();

            var headerState = MoonAssetReferenceTree.CreateDefaultMultiColumnHeaderState();// multiColumnTreeViewRect.width);
            if (MultiColumnHeaderState.CanOverwriteSerializedFields(m_MoonAssetReferenceTreeMCHState, headerState))
                MultiColumnHeaderState.OverwriteSerializedFields(m_MoonAssetReferenceTreeMCHState, headerState);
            m_MoonAssetReferenceTreeMCHState = headerState;

            m_MoonAssetReferenceTree = new MoonAssetReferenceTree(m_MoonAssetReferenceTreeState, m_MoonAssetReferenceTreeMCHState);
        }

        public override void OnGUI(Rect rect)
        {
            m_MoonAssetReferenceTree.Reload();
            m_MoonAssetReferenceTree.OnGUI(rect);
        }
    }
}
