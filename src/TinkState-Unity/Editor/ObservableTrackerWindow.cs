using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TinkState.Internal;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace TinkState.Editor
{
	public class ObservableTrackerWindow : EditorWindow
	{
		readonly List<ObservableTracker.TrackingData> data = new();

		ListView listView;

		[MenuItem("Window/Observable Tracker")]
		public static void Open()
		{
			var window = GetWindow<ObservableTrackerWindow>();
			window.titleContent = new GUIContent("Observable Tracker");
		}

		void OnEnable()
		{
			ObservableTracker.EnableTracking = true;
		}

		void OnDisable()
		{
			ObservableTracker.EnableTracking = false;
		}

		public void CreateGUI()
		{
			const string uxmlGUID = "bc0d80c10c9ac4d5ab4a26571a0e49e3";
			var treeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath(uxmlGUID));
			var tree = treeAsset.Instantiate();
			tree.style.flexGrow = 1;
			rootVisualElement.Add(tree);

			var stackTraceView = tree.Q<Label>("stack-trace");

			listView = tree.Q<ListView>("observable-list");
			listView.itemsSource = data;
			listView.selectionType = SelectionType.Single;
			listView.makeItem = () => new Label();
			listView.bindItem = (element, index) =>
			{
				var label = (Label)element;
				label.text = data[index].Type;
			};
			listView.onSelectedIndicesChange += indices =>
			{
				foreach (var index in indices)
				{
					var trackingData = data[index];
					Debug.Log("Choosing " + index);
					stackTraceView.text = trackingData.Stack;
					return;
				}

				stackTraceView.text = "";
			};
		}

		private void Update()
		{
			if (ObservableTracker.CheckDirty())
			{
				Debug.Log("Updating");
				data.Clear();
				ObservableTracker.IterateEntries((observer, trackingData) =>
				{
					Debug.Log("Adding: " + trackingData.Type);
					data.Add(trackingData);
				});
				listView.Rebuild();
			}
		}
	}
}
