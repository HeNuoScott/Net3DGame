using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class MyAllPostprocessor : AssetPostprocessor
{
    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        if (File.Exists(Application.dataPath + "PluginInit.init")) {
            return;
        }
        File.Create(Application.dataPath + "PluginInit.init");

        AddLayer("Player");
        AddLayer("Enemy");

        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty it = tagManager.GetIterator();
        int layerID1 = 0, layerID2 = 0;
        while (it.NextVisible(true))
        {
            if (it.name == "layers")
            {
                for (int i = 0; i < it.arraySize; i++)
                {
                    if (i == 3 || i == 6 || i == 7)
                    {
                        continue;
                    }
                    SerializedProperty dataPoint = it.GetArrayElementAtIndex(i);
                    if (dataPoint.stringValue == "Player")
                    {
                        layerID1 = i;
                    }
                    if (dataPoint.stringValue == "Enemy")
                    {
                        layerID2 = i;
                    }
                }
            }
        }
        Physics.IgnoreLayerCollision(layerID1, layerID1);
        Physics.IgnoreLayerCollision(layerID1, layerID2);
        
        DirectoryInfo dir = new DirectoryInfo(Application.dataPath);
        FileInfo[] files = dir.GetFiles("*.unity", SearchOption.AllDirectories);
        List<EditorBuildSettingsScene> editorBuildSettingsScenes = new List<EditorBuildSettingsScene>();
        foreach (var sceneFile in files)
        {
            string[] str = sceneFile.FullName.Split(new string[] { "Assets\\" }, StringSplitOptions.RemoveEmptyEntries);
            string scenePath = "Assets/" + str[1];
            if (!string.IsNullOrEmpty(scenePath))
                editorBuildSettingsScenes.Add(new EditorBuildSettingsScene(scenePath, true));
        }
        EditorBuildSettings.scenes = editorBuildSettingsScenes.ToArray();
    }

    private static void AddLayer(string layer)
    {
        for (int i = 0; i < UnityEditorInternal.InternalEditorUtility.layers.Length; i++)
        {
            if (UnityEditorInternal.InternalEditorUtility.layers[i].Contains(layer))
                return;
        }

        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty it = tagManager.GetIterator();
        while (it.NextVisible(true))
        {
            if (it.name == "layers")
            {
                for (int i = 0; i < it.arraySize; i++)
                {
                    if (i == 3 || i == 6 || i == 7)
                    {
                        continue;
                    }
                    SerializedProperty dataPoint = it.GetArrayElementAtIndex(i);
                    if (string.IsNullOrEmpty(dataPoint.stringValue))
                    {
                        dataPoint.stringValue = layer;
                        tagManager.ApplyModifiedProperties();
                        return;
                    }
                }
            }
        }
    }
}
