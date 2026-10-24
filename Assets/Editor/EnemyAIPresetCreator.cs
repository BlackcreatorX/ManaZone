using UnityEditor;
using UnityEngine;
using System.IO;

public static class EnemyAIPresetCreator
{
    [MenuItem("Mana Zone/Enemy AI/Create Example Personalities")]
    private static void CreateExamples()
    {
        const string folder = "Assets/EnemyAI/Personalities";
        if (!AssetDatabase.IsValidFolder("Assets/EnemyAI")) AssetDatabase.CreateFolder("Assets", "EnemyAI");
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/EnemyAI", "Personalities");

        Create(folder, "Prodigy", 1.35f, 1.35f, 0.55f, 0.4f, 3.5f, 0.9f, 0.5f, 0.2f, 0.35f);
        Create(folder, "Strategist", 0.75f, 0.55f, 1.1f, 0.8f, 2.8f, 0.2f, 0.25f, 0.95f, 0.35f);
        Create(folder, "Artillery", 0.85f, 0.9f, 1.1f, 0.7f, 2.5f, 0.2f, 0.95f, 0.2f, 0.35f);
        Create(folder, "Illusionist", 0.95f, 0.85f, 0.9f, 0.9f, 3f, 0.45f, 0.45f, 0.4f, 0.95f);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Example enemy AI personalities created in " + folder);
    }

    private static void Create(string folder, string name, float aggression, float manaSpending,
        float reaction, float interval, float speed, float harassment, float charged, float counter, float control)
    {
        string path = folder + "/" + name + ".asset";
        if (File.Exists(path)) return;
        EnemyAIPersonality asset = ScriptableObject.CreateInstance<EnemyAIPersonality>();
        SerializedObject data = new SerializedObject(asset);
        Set(data, "aggression", aggression);
        Set(data, "manaSpending", manaSpending);
        Set(data, "reactionTime", reaction);
        Set(data, "selectionInterval", interval);
        Set(data, "movementSpeed", speed);
        Set(data, "harassmentPreference", harassment);
        Set(data, "chargedPreference", charged);
        Set(data, "counterPreference", counter);
        Set(data, "controlPreference", control);
        data.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.CreateAsset(asset, path);
    }

    private static void Set(SerializedObject data, string property, float value)
    {
        SerializedProperty field = data.FindProperty(property);
        if (field != null) field.floatValue = value;
    }
}
