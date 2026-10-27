using Dories.Event.Runtime;
using UnityEditor;
using UnityEngine;

namespace Dories.Event.Editor
{
    [CustomEditor(typeof(EventEntity))]
    public class EventEntityInspector : UnityEditor.Editor
    {
        private Vector2 _scrollPosition;
        private bool _monitorFoldout = true;

        public override bool RequiresConstantRepaint()
        {
            return Application.isPlaying;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            DrawMonitor();
        }

        private void DrawMonitor()
        {
            _monitorFoldout = EditorGUILayout.Foldout(_monitorFoldout, "Events", true);
            if (!_monitorFoldout)
                return;

            EditorGUI.indentLevel++;

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("进入 Play Mode 后显示各事件的订阅与发布情况。", MessageType.Info);
                EditorGUI.indentLevel--;
                return;
            }

            var infos = ((EventEntity)target).Infos;
            EditorGUILayout.LabelField("事件类型数量", infos.Count.ToString());

            if (infos.Count == 0)
            {
                EditorGUILayout.LabelField("(无事件)", EditorStyles.miniLabel);
                EditorGUI.indentLevel--;
                return;
            }

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField("Event", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("Subs", EditorStyles.miniLabel, GUILayout.Width(40f));
            EditorGUILayout.LabelField("Published", EditorStyles.miniLabel, GUILayout.Width(64f));
            EditorGUILayout.LabelField("Last", EditorStyles.miniLabel, GUILayout.Width(56f));
            EditorGUILayout.EndHorizontal();

            float now = Time.unscaledTime;
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.MaxHeight(280f));
            foreach (var pair in infos)
            {
                var info = pair.Value;
                EditorGUILayout.BeginHorizontal();
                // 有人发但没人收时标黄，便于发现漏订阅
                var color = GUI.color;
                if (info.Subscribers == 0 && info.PublishCount > 0)
                    GUI.color = Color.yellow;
                GUILayout.Label(new GUIContent(pair.Key.Name, pair.Key.FullName));
                GUILayout.Label(info.Subscribers.ToString(), GUILayout.Width(40f));
                GUILayout.Label(info.PublishCount.ToString(), GUILayout.Width(64f));
                GUILayout.Label(info.LastPublishTime < 0f ? "-" : $"{now - info.LastPublishTime:F1}s", GUILayout.Width(56f));
                GUI.color = color;
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();

            EditorGUI.indentLevel--;
        }
    }
}
