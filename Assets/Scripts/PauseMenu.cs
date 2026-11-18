using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Builds a pause menu at runtime and keeps it available across scenes.</summary>
public sealed class PauseMenu : MonoBehaviour
{
    [SerializeField] private string mainMenuScene = "MenuScene";

    private static PauseMenu instance;

    private GameObject menuRoot;
    private bool isPaused;
    private CursorLockMode previousLockMode;
    private bool previousCursorVisibility;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateAtStartup()
    {
        if (instance == null)
        {
            instance = FindFirstObjectByType<PauseMenu>();
            if (instance == null)
                instance = new GameObject("PauseMenu").AddComponent<PauseMenu>();
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureEventSystem();
        BuildMenu();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame)
            return;

        if (SceneManager.GetActiveScene().name == mainMenuScene)
            return;

        if (isPaused)
            Resume();
        else
            Pause();
    }

    private void OnDestroy()
    {
        if (instance != this)
            return;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        Time.timeScale = 1f;
        instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        bool wasPaused = isPaused;
        isPaused = false;
        Time.timeScale = 1f;
        EnsureEventSystem();
        if (menuRoot != null)
            menuRoot.SetActive(false);

        if (wasPaused || scene.name == mainMenuScene)
        {
            Cursor.lockState = previousLockMode;
            Cursor.visible = previousCursorVisibility;
        }
    }

    private void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f;
        previousLockMode = Cursor.lockState;
        previousCursorVisibility = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        menuRoot.SetActive(true);
    }

    private void Resume()
    {
        isPaused = false;
        Time.timeScale = 1f;
        menuRoot.SetActive(false);
        Cursor.lockState = previousLockMode;
        Cursor.visible = previousCursorVisibility;
    }

    private void Restart()
    {
        Time.timeScale = 1f;
        isPaused = false;
        Cursor.lockState = previousLockMode;
        Cursor.visible = previousCursorVisibility;
        Scene activeScene = SceneManager.GetActiveScene();
#if UNITY_EDITOR
        if (activeScene.buildIndex < 0)
        {
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
                activeScene.path,
                new LoadSceneParameters(LoadSceneMode.Single));
            return;
        }
#endif
        SceneManager.LoadScene(activeScene.buildIndex);
    }

    private void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        isPaused = false;
        SceneManager.LoadScene(mainMenuScene);
    }

    private static void EnsureEventSystem()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystem = eventSystemObject.AddComponent<EventSystem>();
        }

        InputSystemUIInputModule inputModule = eventSystem.GetComponent<InputSystemUIInputModule>();
        if (inputModule == null)
        {
            BaseInputModule[] existingModules = eventSystem.GetComponents<BaseInputModule>();
            foreach (BaseInputModule module in existingModules)
                module.enabled = false;

            inputModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            inputModule.AssignDefaultActions();
        }
    }

    private void BuildMenu()
    {
        GameObject canvasObject = new GameObject("Pause Menu Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        menuRoot = CreateRect("Pause Menu", canvasObject.transform);
        Stretch(menuRoot.GetComponent<RectTransform>());

        Image backdrop = menuRoot.AddComponent<Image>();
        backdrop.color = new Color(0f, 0f, 0f, 0.72f);

        GameObject panel = CreateRect("Panel", menuRoot.transform);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(560f, 580f);

        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.075f, 0.085f, 0.13f, 0.98f);

        CreateLabel("PAUSA", panel.transform, new Vector2(0f, 205f), 38f, FontStyles.Bold);
        CreateButton("Reanudar", panel.transform, new Vector2(0f, 100f), Resume);
        CreateButton("Reiniciar", panel.transform, new Vector2(0f, 10f), Restart);
        CreateButton("Menú principal", panel.transform, new Vector2(0f, -80f), ReturnToMainMenu);
        CreateButton("Salir", panel.transform, new Vector2(0f, -170f), Application.Quit);

        menuRoot.SetActive(false);
    }

    private static GameObject CreateRect(string objectName, Transform parent)
    {
        GameObject child = new GameObject(objectName, typeof(RectTransform));
        child.transform.SetParent(parent, false);
        return child;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void CreateLabel(string text, Transform parent, Vector2 position, float fontSize, FontStyles fontStyle)
    {
        GameObject label = CreateRect(text, parent);
        RectTransform rect = label.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(440f, 72f);

        TextMeshProUGUI tmp = label.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = fontStyle;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        if (TMP_Settings.defaultFontAsset != null)
            tmp.font = TMP_Settings.defaultFontAsset;
    }

    private static void CreateButton(string label, Transform parent, Vector2 position, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = CreateRect(label + " Button", parent);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(340f, 64f);

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.2f, 0.25f, 0.38f, 1f);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);

        GameObject textObject = CreateRect("Label", buttonObject.transform);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        Stretch(textRect);

        TextMeshProUGUI tmp = textObject.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 27f;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        if (TMP_Settings.defaultFontAsset != null)
            tmp.font = TMP_Settings.defaultFontAsset;
    }
}
