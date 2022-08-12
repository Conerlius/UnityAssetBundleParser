using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public class AssetBundleParserWindow : EditorWindow
    {
        /// <summary>
        /// 单例
        /// </summary>
        private static AssetBundleParserWindow _instance = null;
        /// <summary>
        /// 窗口名称
        /// </summary>
        private const string WindowTitle = "AssetBundle解释";
        /// <summary>
        /// 简单的默认ab路径
        /// </summary>
        private string _defauleAssetBundlePath = "charactersystem.spriteatlas.bundle";
        /// <summary>
        /// 内容窗口的滚动位置
        /// </summary>
        private Vector2 _contentPosition = Vector2.zero;

        enum InfoType : uint
        {
            SimpleInfo = 0,
            FullInfo = 1,
            All
        }
        /// <summary>
        /// 信息展开便签
        /// </summary>
        private bool[] _infosFoldout = new bool[(uint)InfoType.All];

        private SimpleInfo _simpleInfo = null;
        private FullInfo _fullInfo = null;

        #region 窗口开关
        /// <summary>
        /// 打开窗口
        /// </summary>
        [MenuItem("Tools/AssetBundleParser &1")]
        static void OpenWindow()
        {
            if (_instance == null)
            {
                _instance = GetWindow<AssetBundleParserWindow>(WindowTitle);
            }
            _instance.Show(true);
        }
        /// <summary>
        /// 关闭窗口
        /// </summary>
        [MenuItem("Tools/CloseAssetBundleParser &2")]
        static void CloseWindow()
        {
            if (_instance)
                _instance.Close();
            _instance = null;
        }
        #endregion
        /// <summary>
        /// 界面绘制
        /// </summary>
        private void OnGUI()
        {
            EditorGUILayout.BeginVertical();
            // ab选择
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("AssetBundle路径:", _defauleAssetBundlePath);
            if (EditorGUILayout.DropdownButton(new GUIContent("选择"), FocusType.Keyboard))
            {
                var filePath = EditorUtility.OpenFilePanel("请选择指定的AssetBundle", String.Empty, ".bundle");
                if (!string.IsNullOrEmpty(filePath))
                {
                    _defauleAssetBundlePath = filePath;
                }
            }
            EditorGUILayout.EndHorizontal();
            // 信息
            EditorGUILayout.BeginScrollView(_contentPosition);
            EditorGUILayout.BeginVertical();
            // ab中简单的信息
            _infosFoldout[(uint)InfoType.SimpleInfo] = EditorGUILayout.Foldout(_infosFoldout[(uint)InfoType.SimpleInfo], "简单的信息", true);
            if (_infosFoldout[(uint)InfoType.SimpleInfo])
            {
                EditorGUI.indentLevel++;
                if (_simpleInfo == null)
                {
                    if (EditorGUILayout.DropdownButton(new GUIContent("获取"), FocusType.Keyboard))
                    {
                        _simpleInfo = AssetBundleUtility.GetSimpleInfo(_defauleAssetBundlePath);
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("AssetBundle Name:", _simpleInfo.AssetBundleName);
                    EditorGUILayout.LabelField("AssetBundle InstanceId:", _simpleInfo.InstanceId.ToString());
                    EditorGUILayout.LabelField("AssetBundle MemoryBudgetKB:", _simpleInfo.MemoryBudgetKB.ToString());
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();
            // 解释完整
            EditorGUILayout.BeginVertical();
            _infosFoldout[(uint)InfoType.FullInfo] = EditorGUILayout.Foldout(_infosFoldout[(uint)InfoType.FullInfo], "详细的信息", true);
            if (_infosFoldout[(uint)InfoType.FullInfo])
            {
                EditorGUI.indentLevel++;
                if (_fullInfo == null)
                {
                    if (EditorGUILayout.DropdownButton(new GUIContent("获取"), FocusType.Keyboard))
                    {
                        _fullInfo = AssetBundleUtility.GetFullInfo(_defauleAssetBundlePath);
                    }
                }
                else
                {
                    // EditorGUILayout.LabelField("AssetBundle Name:", _simpleInfo.AssetBundleName);
                    // EditorGUILayout.LabelField("AssetBundle InstanceId:", _simpleInfo.InstanceId.ToString());
                    // EditorGUILayout.LabelField("AssetBundle MemoryBudgetKB:", _simpleInfo.MemoryBudgetKB.ToString());
                }
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        #region 信息获取

        

        #endregion
    }
}
