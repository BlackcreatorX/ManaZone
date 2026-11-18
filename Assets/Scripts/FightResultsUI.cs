using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Creates and drives the shared win/lose screen in every fight scene.</summary>
public sealed class FightResultsUI : MonoBehaviour
{
    private const string MainMenuScene = "MenuScene";
    private static FightResultsUI instance;
    private static bool sceneHooked;
    public static bool IsShowingResult => instance != null && instance.resultShown;

    private readonly HashSet<int> livingEnemies = new HashSet<int>();
    private bool resultShown;
    private GameObject winRoot;
    private GameObject loseRoot;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
        sceneHooked = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void HookSceneChanges()
    {
        if (sceneHooked) return;
        sceneHooked = true;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (instance != null)
            Destroy(instance.gameObject);
        instance = null;

        if (!scene.name.StartsWith("Fight", StringComparison.OrdinalIgnoreCase))
            return;

        GameObject owner = new GameObject("FightResultsUI");
        instance = owner.AddComponent<FightResultsUI>();
        instance.Initialize();
    }

    private void Initialize()
    {
        // Inactive enemy placeholders in the fight scenes are not opponents and
        // must not keep the living-enemy counter above zero.
        foreach (EnemyCombatAI enemy in FindObjectsByType<EnemyCombatAI>(FindObjectsSortMode.None))
            if (enemy != null) livingEnemies.Add(enemy.gameObject.GetInstanceID());
        foreach (SpellableTarget enemy in FindObjectsByType<SpellableTarget>(FindObjectsSortMode.None))
            if (enemy != null) livingEnemies.Add(enemy.gameObject.GetInstanceID());

        // Fight3 contains early, unfinished versions of these screens. The shared
        // runtime screens below replace them and are present in every Fight scene.
        GameObject[] sceneObjects = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (GameObject sceneObject in sceneObjects)
        {
            if (sceneObject != gameObject &&
                (string.Equals(sceneObject.name, "Win", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(sceneObject.name, "Lose", StringComparison.OrdinalIgnoreCase)))
                sceneObject.SetActive(false);
        }

        EnsureEventSystem();
        Canvas canvas = CreateCanvas();
        winRoot = CreateResultPanel(canvas.transform, true);
        loseRoot = CreateResultPanel(canvas.transform, false);
        winRoot.SetActive(false);
        loseRoot.SetActive(false);
    }

    // HPSystem calls this synchronously before destroying the defeated character.
    public static void ReportDeath(bool playerDied, bool enemyDied, int enemyInstanceId)
    {
        EnsureInstanceForActiveFight();
        if (instance == null || instance.resultShown) return;
        if (playerDied)
        {
            instance.ShowResult(false);
            return;
        }
        if (enemyDied) ReportEnemyDefeated(enemyInstanceId);
    }

    public static void ReportEnemyDefeated(int enemyInstanceId)
    {
        EnsureInstanceForActiveFight();
        if (instance == null || instance.resultShown) return;
        instance.livingEnemies.Remove(enemyInstanceId);
        if (instance.livingEnemies.Count == 0)
            instance.ShowResult(true);
    }

    private static void EnsureInstanceForActiveFight()
    {
        if (instance != null || !SceneManager.GetActiveScene().name.StartsWith("Fight", StringComparison.OrdinalIgnoreCase))
            return;

        GameObject owner = new GameObject("FightResultsUI");
        instance = owner.AddComponent<FightResultsUI>();
        instance.Initialize();
    }

    private void ShowResult(bool won)
    {
        if (resultShown) return;
        resultShown = true;
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        winRoot.SetActive(won);
        loseRoot.SetActive(!won);
    }

    private GameObject CreateResultPanel(Transform parent, bool won)
    {
        GameObject root = CreateRect(won ? "Win" : "Lose", parent, Vector2.zero, Vector2.one, Vector2.zero);
        Image shade = root.AddComponent<Image>();
        shade.color = new Color(0.025f, 0.035f, 0.06f, 0.84f);

        GameObject card = CreateRect("Card", root.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(720f, 480f));
        Image cardImage = card.AddComponent<Image>();
        cardImage.color = new Color(0.08f, 0.11f, 0.16f, 0.98f);

        CreateText(card.transform, won ? "YOU WIN" : "YOU LOSE", 48,
            won ? new Color(0.39f, 0.95f, 0.58f) : new Color(1f, 0.35f, 0.3f),
            new Vector2(0f, 145f), new Vector2(640f, 82f));
        CreateText(card.transform, won ? "¡Victoria!" : "El jugador ha sido derrotado.", 23,
            Color.white, new Vector2(0f, 88f), new Vector2(640f, 48f));

        bool hasNextFight = won && HasNextFight();
        float firstButtonY = hasNextFight ? 5f : -8f;
        CreateButton(card.transform, "Reintentar", new Vector2(0f, firstButtonY), RestartFight, new Color(0.16f, 0.5f, 0.42f));
        CreateButton(card.transform, "Menú principal", new Vector2(0f, firstButtonY - 70f), ReturnToMenu, new Color(0.3f, 0.36f, 0.47f));
        if (hasNextFight)
            CreateButton(card.transform, "Siguiente pelea", new Vector2(0f, firstButtonY - 140f), LoadNextFight, new Color(0.45f, 0.3f, 0.65f));

        return root;
    }

    private static Canvas CreateCanvas()
    {
        GameObject canvasObject = new GameObject("FightResultsCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        return canvas;
    }

    private static GameObject CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 size)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;
        return obj;
    }

    private static void CreateText(Transform parent, string value, float size, Color color, Vector2 anchoredPosition, Vector2 dimensions)
    {
        GameObject obj = CreateRect(value, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), dimensions);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchoredPosition = anchoredPosition;
        TextMeshProUGUI text = obj.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.fontStyle = FontStyles.Bold;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
    }

    private static void CreateButton(Transform parent, string label, Vector2 anchoredPosition, UnityEngine.Events.UnityAction action, Color color)
    {
        GameObject obj = CreateRect(label, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(380f, 58f));
        obj.GetComponent<RectTransform>().anchoredPosition = anchoredPosition;
        Image image = obj.AddComponent<Image>();
        image.color = color;
        Button button = obj.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);

        GameObject labelObject = CreateRect("Label", obj.transform, Vector2.zero, Vector2.one, Vector2.zero);
        TextMeshProUGUI text = labelObject.AddComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 22f;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
    }

    private static bool HasNextFight() => GetNextFight() != null;

    private static string GetNextFight()
    {
        string current = SceneManager.GetActiveScene().name;
        if (current.Equals("Fight1", StringComparison.OrdinalIgnoreCase)) return "Fight2";
        if (current.Equals("Fight2", StringComparison.OrdinalIgnoreCase)) return "Fight3";
        return null;
    }

    private static void RestartFight()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private static void ReturnToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(MainMenuScene);
    }

    private static void LoadNextFight()
    {
        string next = GetNextFight();
        if (string.IsNullOrEmpty(next)) return;
        Time.timeScale = 1f;
        SceneManager.LoadScene(next);
    }

    private static void EnsureEventSystem()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            GameObject obj = new GameObject("EventSystem", typeof(EventSystem));
            eventSystem = obj.GetComponent<EventSystem>();
        }

        if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
        {
            foreach (BaseInputModule module in eventSystem.GetComponents<BaseInputModule>())
                module.enabled = false;
            eventSystem.gameObject.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }
    }
}
