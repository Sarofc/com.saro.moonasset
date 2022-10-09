using Saro.SEditor;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Saro.MoonAsset
{
    internal class MoonAssetReferenceTab : TabWindow
    {
        private TreeViewState m_MoonAssetReferenceTreeState;
        private MultiColumnHeaderState m_MoonAssetReferenceTreeMCHState;
        private MoonAssetReferenceTree m_MoonAssetReferenceTree;
        private SearchField m_SearchField;
        private string m_SearchText;

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

            m_SearchField = new SearchField();
        }

        public override void OnGUI(Rect rect)
        {
            var searchRect = EditorGUILayout.GetControlRect();
            var treeRect = new Rect(rect.x, rect.y, rect.width, rect.height);
            treeRect.y += searchRect.height;
            treeRect.height -= searchRect.height;

            var searchText = m_SearchField.OnToolbarGUI(searchRect, m_SearchText);
            if (searchText != m_SearchText)
            {
                m_MoonAssetReferenceTree.searchString = m_SearchText = searchText;
            }

            m_MoonAssetReferenceTree.Reload();
            m_MoonAssetReferenceTree.OnGUI(treeRect);
        }
    }
}
