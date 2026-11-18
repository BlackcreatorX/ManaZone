using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Controls the pause menu authored in the scene hierarchy.</summary>
public sealed class PauseMenu : MonoBehaviour
{
    [Header("Menu references")]
    [SerializeField] private GameObject menuRoot;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private string mainMenuScene = "MenuScene";

    private static PauseMenu instance;
    private bool isPaused;
    private CursorLockMode previousLockMode;
    private bool previousCursorVisibility;

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

        if (menuRoot == null)
        {
            Debug.LogError("PauseMenu necesita la referencia Menu Root asignada desde la jerarquía.", this);
            enabled = false;
            return;
        }

        menuRoot.SetActive(false);
        if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
        if (restartButton != null) restartButton.onClick.AddListener(Restart);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        if (quitButton != null) quitButton.onClick.AddListener(Application.Quit);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Update()
    {
        if (FightResultsUI.IsShowingResult)
            return;

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
        if (menuRoot == null)
            return;

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
        if (menuRoot != null)
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
}
