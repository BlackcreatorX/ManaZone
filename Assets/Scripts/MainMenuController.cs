using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Connects the main menu controls and animates its panels.</summary>
public sealed class MainMenuController : MonoBehaviour
{
    [SerializeField] private float transitionDuration = 0.22f;

    private RectTransform mainPanel;
    private RectTransform optionsPanel;
    private RectTransform creditsPanel;
    private Coroutine transition;

    private void Awake()
    {
        Transform root = transform;
        mainPanel = root.Find("P_MainMenu") as RectTransform;
        optionsPanel = root.Find("P_OptionsPanle") as RectTransform;
        creditsPanel = root.Find("P_Credits") as RectTransform;

        BindButton(mainPanel, "UIB_Play", PlayGame);
        BindButton(mainPanel, "UIB_Options", OpenOptions);
        BindButton(mainPanel, "UIB_Credits", OpenCredits);
        BindButton(mainPanel, "UIB_Exit", QuitGame);
        BindButton(optionsPanel, "UIB_BackButton", BackToMain);
        BindButton(creditsPanel, "UIB_BackButton", BackToMain);

        BindSlider(optionsPanel, "Music Slider", "ManaZone.MusicVolume", AudioManager.Instance.SetMusicVolume);
        BindSlider(optionsPanel, "SFX Slider", "ManaZone.SfxVolume", AudioManager.Instance.SetSfxVolume);

        SetInitialPanel(mainPanel, true);
        SetInitialPanel(optionsPanel, false);
        SetInitialPanel(creditsPanel, false);
        if (mainPanel != null) StartCoroutine(AnimatePanel(mainPanel, true));
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void BindButton(RectTransform panel, string buttonName, UnityEngine.Events.UnityAction action)
    {
        if (panel == null) return;
        Transform buttonTransform = panel.Find(buttonName);
        Button button = buttonTransform != null ? buttonTransform.GetComponent<Button>() : null;
        if (button != null)
        {
            button.onClick.AddListener(() => AudioManager.Instance.PlaySfx("UiSound"));
            button.onClick.AddListener(action);
        }
        else Debug.LogWarning($"No se encontró el botón '{buttonName}' en '{panel.name}'.", this);
    }

    private void BindSlider(RectTransform panel, string sliderName, string preferenceKey, UnityEngine.Events.UnityAction<float> apply)
    {
        if (panel == null) return;
        Transform sliderTransform = panel.Find(sliderName);
        Slider slider = sliderTransform != null ? sliderTransform.GetComponent<Slider>() : null;
        if (slider == null)
        {
            Debug.LogWarning($"No se encontró el slider '{sliderName}' en '{panel.name}'.", this);
            return;
        }

        float savedValue = PlayerPrefs.GetFloat(preferenceKey, slider.value);
        slider.SetValueWithoutNotify(savedValue);
        apply(savedValue);
        slider.onValueChanged.AddListener(value =>
        {
            apply(value);
            PlayerPrefs.SetFloat(preferenceKey, value);
        });
    }

    private static void SetInitialPanel(RectTransform panel, bool visible)
    {
        if (panel == null) return;
        panel.gameObject.SetActive(visible);
        panel.localScale = Vector3.one;
        CanvasGroup group = GetOrAddCanvasGroup(panel);
        group.alpha = 1f;
        group.interactable = visible;
        group.blocksRaycasts = visible;
    }

    private void OpenOptions() => ShowPanel(optionsPanel);
    private void OpenCredits() => ShowPanel(creditsPanel);
    private void BackToMain() => ShowPanel(mainPanel);

    private void ShowPanel(RectTransform destination)
    {
        if (destination == null || (transition == null && destination.gameObject.activeSelf)) return;
        if (transition != null) StopCoroutine(transition);
        transition = StartCoroutine(TransitionTo(destination));
    }

    private IEnumerator TransitionTo(RectTransform destination)
    {
        RectTransform current = mainPanel.gameObject.activeSelf ? mainPanel
            : optionsPanel.gameObject.activeSelf ? optionsPanel
            : creditsPanel.gameObject.activeSelf ? creditsPanel : null;

        if (current != null && current != destination)
            yield return AnimatePanel(current, false);

        if (current != destination)
            yield return AnimatePanel(destination, true);
        transition = null;
    }

    private IEnumerator AnimatePanel(RectTransform panel, bool entering)
    {
        CanvasGroup group = GetOrAddCanvasGroup(panel);
        if (entering)
        {
            panel.gameObject.SetActive(true);
            group.interactable = false;
            group.blocksRaycasts = false;
            group.alpha = 0f;
            panel.localScale = Vector3.one * 0.94f;
        }
        else
        {
            group.interactable = false;
            group.blocksRaycasts = false;
        }

        float startAlpha = group.alpha;
        Vector3 startScale = panel.localScale;
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, transitionDuration);
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = t * t * (3f - 2f * t);
            group.alpha = Mathf.Lerp(startAlpha, entering ? 1f : 0f, eased);
            panel.localScale = Vector3.Lerp(startScale, entering ? Vector3.one : Vector3.one * 0.96f, eased);
            yield return null;
        }

        group.alpha = entering ? 1f : 0f;
        panel.localScale = entering ? Vector3.one : Vector3.one * 0.96f;
        if (entering)
        {
            group.interactable = true;
            group.blocksRaycasts = true;
        }
        else panel.gameObject.SetActive(false);
    }

    private static CanvasGroup GetOrAddCanvasGroup(RectTransform panel)
    {
        if (panel == null) return null;

        if (!panel.TryGetComponent(out CanvasGroup group) || group == null)
            group = panel.gameObject.AddComponent<CanvasGroup>();

        return group;
    }

//LOAD INDEX +1
    private void PlayGame() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);

    private static void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
