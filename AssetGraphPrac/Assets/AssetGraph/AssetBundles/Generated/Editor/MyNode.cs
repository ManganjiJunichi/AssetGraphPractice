using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using UnityEngine.AssetGraph;
using Model=UnityEngine.AssetGraph.DataModel.Version2;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;

[CustomNode("Custom/MyNode", 1000)]
public class MyNode : Node {

	[SerializeField] private SerializableMultiTargetString m_myValue;

	public override string ActiveStyle {
		get {
			return "node 8 on";
		}
	}

	public override string InactiveStyle {
		get {
			return "node 8";
		}
	}

	public override string Category {
		get {
			return "Custom";
		}
	}

	public override void Initialize(Model.NodeData data) {
		m_myValue = new SerializableMultiTargetString();
		data.AddDefaultInputPoint();
		data.AddDefaultOutputPoint();
	}

	public override Node Clone(Model.NodeData newData) {
		var newNode = new MyNode();
		newNode.m_myValue = new SerializableMultiTargetString(m_myValue);
		newData.AddDefaultInputPoint();
		newData.AddDefaultOutputPoint();
		return newNode;
	}

	public override void OnInspectorGUI(NodeGUI node, AssetReferenceStreamManager streamManager, NodeGUIEditor editor, Action onValueChanged) {

		EditorGUILayout.HelpBox("My Custom Node: Implement your own Inspector.", MessageType.Info);
		editor.UpdateNodeName(node);

		GUILayout.Space(10f);

		//Show target configuration tab
		editor.DrawPlatformSelector(node);
		using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
			// Draw Platform selector tab. 
			var disabledScope = editor.DrawOverrideTargetToggle(node, m_myValue.ContainsValueOf(editor.CurrentEditingGroup), (bool b) => {
				using(new RecordUndoScope("Remove Target Platform Settings", node, true)) {
					if(b) {
						m_myValue[editor.CurrentEditingGroup] = m_myValue.DefaultValue;
					} else {
						m_myValue.Remove(editor.CurrentEditingGroup);
					}
					onValueChanged();
				}
			});

			// Draw tab contents
			using (disabledScope) {
				var val = m_myValue[editor.CurrentEditingGroup];

				var newValue = EditorGUILayout.TextField("My Value:", val);
				if (newValue != val) {
					using(new RecordUndoScope("My Value Changed", node, true)){
						m_myValue[editor.CurrentEditingGroup] = newValue;
						onValueChanged();
					}
				}
			}
		}
	}

	/**
	 * ノードが繋がれた際に走る処理
	 */ 
	public override void Prepare (BuildTarget target, 
		Model.NodeData node, 
		IEnumerable<PerformGraph.AssetGroups> incoming, 
		IEnumerable<Model.ConnectionData> connectionsToOutput, 
		PerformGraph.Output Output) 
	{
		// Do nothing
	}

	/**
	 * ノードが実行された際に走る処理
	 */
	public override void Build (BuildTarget target, 
		Model.NodeData nodeData, 
		IEnumerable<PerformGraph.AssetGroups> incoming, 
		IEnumerable<Model.ConnectionData> connectionsToOutput, 
		PerformGraph.Output outputFunc,
		Action<Model.NodeData, string, float> progressFunc)
	{
		// Do nothing
		if (incoming == null)
			return;

		var datas = incoming.SelectMany(x => x.assetGroups.Values).Distinct().ToList();
		if (datas == null)
			return;

        var addressSetting = AddressableAssetSettingsDefaultObject.Settings;
		string groupName = "CustomGroup";
        foreach (var data in datas)
		{
			foreach(var reference in data)
			{
				foreach(var referenceData in reference.allData)
				{
                    var group = addressSetting.FindGroup(groupName);
                    if (group == null)
                    {
						var groupTemplate = addressSetting.GetGroupTemplateObject(0) as AddressableAssetGroupTemplate;
						var newGroup = addressSetting.CreateGroup(groupName, false, false, true, null, groupTemplate.GetTypes());
						groupTemplate.ApplyToAddressableAssetGroup(newGroup);
						AssetDatabase.SaveAssets();

						group = addressSetting.FindGroup(groupName);
						if (group == null)
						{
							Debug.LogError($"グループの作成に失敗{groupName}");
							return;
						}
					}

					string assetName = referenceData.name;
					string lavel = Path.GetDirectoryName(assetName);
					var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(referenceData));
					addressSetting.CreateOrMoveEntry(guid, group);
					AssetDatabase.SaveAssets();

					var entry = group.GetAssetEntry(guid);
					entry.SetAddress(Path.GetFileNameWithoutExtension(assetName));
					if (string.IsNullOrEmpty(lavel) == false)
					{
						entry.SetLabel(lavel, true, true);
					}
					AssetDatabase.SaveAssets();

				}
            }
		}

    }
}
