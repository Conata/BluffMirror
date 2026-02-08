using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using FPSTrump.Manager;

/// <summary>
/// シーンのセットアップを検証するエディタツール
/// </summary>
public class SceneSetupValidator : EditorWindow
{
    [MenuItem("Tools/Baba/Validate Scene Setup")]
    public static void ShowWindow()
    {
        var window = GetWindow<SceneSetupValidator>("Scene Setup Validator");
        window.minSize = new Vector2(400, 300);
        window.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Scene Setup Validator", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox(
                "This tool must be run in Play Mode to validate runtime instances.",
                MessageType.Warning);
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Validate Current Scene", GUILayout.Height(40)))
        {
            ValidateScene();
        }

        if (GUILayout.Button("Fix Missing Managers", GUILayout.Height(40)))
        {
            FixMissingManagers();
        }
    }

    private void ValidateScene()
    {
        Debug.Log("=== Scene Setup Validation ===");
        Debug.Log($"Current Scene: {SceneManager.GetActiveScene().name}");
        Debug.Log($"Play Mode: {Application.isPlaying}");

        if (Application.isPlaying)
        {
            // Runtime validation
            Debug.Log($"GameSettings.Instance: {(GameSettings.Instance != null ? "✓ OK" : "✗ NULL")}");
            Debug.Log($"LocalizationManager.Instance: {(LocalizationManager.Instance != null ? "✓ OK" : "✗ NULL")}");
            Debug.Log($"PlayerBirthdayManager.Instance: {(PlayerBirthdayManager.Instance != null ? "✓ OK" : "✗ NULL")}");
            Debug.Log($"PlayerNameManager.Instance: {(PlayerNameManager.Instance != null ? "✓ OK" : "✗ NULL")}");

            if (GameSettings.Instance != null)
            {
                var lang = GameSettings.Instance.GetLanguage();
                Debug.Log($"Current Language: {lang}");
            }

            if (LocalizationManager.Instance != null)
            {
                Debug.Log($"LocalizationManager.IsJapanese: {LocalizationManager.Instance.IsJapanese}");
            }
        }
        else
        {
            // Edit mode validation
            var gameSettings = FindFirstObjectByType<GameSettings>();
            var locManager = FindFirstObjectByType<LocalizationManager>();
            var birthdaySetupUI = FindFirstObjectByType<FPSTrump.UI.BirthdaySetupUI>();
            var titleUI = FindFirstObjectByType<TitleUI>();
            var birthdayManager = FindFirstObjectByType<PlayerBirthdayManager>();
            var nameManager = FindFirstObjectByType<PlayerNameManager>();

            Debug.Log($"GameSettings (Edit): {(gameSettings != null ? "✓ Found" : "✗ Not Found")}");
            Debug.Log($"LocalizationManager (Edit): {(locManager != null ? "✓ Found" : "✗ Not Found")}");
            Debug.Log($"BirthdaySetupUI (Edit): {(birthdaySetupUI != null ? "✓ Found" : "✗ Not Found")}");
            Debug.Log($"TitleUI (Edit): {(titleUI != null ? "✓ Found" : "✗ Not Found")}");
            Debug.Log($"PlayerBirthdayManager (Edit): {(birthdayManager != null ? "✓ Found" : "✗ Not Found")}");
            Debug.Log($"PlayerNameManager (Edit): {(nameManager != null ? "✓ Found" : "✗ Not Found")}");
        }

        Debug.Log("=== Validation Complete ===");
    }

    private void FixMissingManagers()
    {
        if (Application.isPlaying)
        {
            Debug.LogWarning("Cannot fix managers in Play Mode. Stop Play Mode first.");
            return;
        }

        Debug.Log("=== Fixing Missing Managers ===");

        // GameSettings
        var gameSettings = FindFirstObjectByType<GameSettings>();
        if (gameSettings == null)
        {
            var go = new GameObject("GameSettings");
            go.AddComponent<GameSettings>();
            Debug.Log("✓ Created GameSettings");
        }
        else
        {
            Debug.Log("✓ GameSettings already exists");
        }

        // LocalizationManager
        var locManager = FindFirstObjectByType<LocalizationManager>();
        if (locManager == null)
        {
            var go = new GameObject("LocalizationManager");
            go.AddComponent<LocalizationManager>();
            Debug.Log("✓ Created LocalizationManager");
        }
        else
        {
            Debug.Log("✓ LocalizationManager already exists");
        }

        Debug.Log("=== Fix Complete ===");
        EditorUtility.DisplayDialog("Success",
            "Manager objects have been created/validated.\n" +
            "Enter Play Mode to test.",
            "OK");
    }
}
