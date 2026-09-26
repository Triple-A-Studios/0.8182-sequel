using System.Collections;
using System.Collections.Generic;
using Alchemy.Inspector;
using Opoint8182.Game;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Opoint8182.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class MainMenuController : MonoBehaviour
    {
        [Title("References")]
        [FoldoutGroup("References")] [SerializeField] private string m_playButtonElementName = "play-button";
        [FoldoutGroup("References")] [SerializeField] private string m_safeAreaElementName = "safe-area";
        [FoldoutGroup("References")] [SerializeField] private float m_baseSafeAreaPadding = 24f;

        [Title("Tunables")]
        // Travels down by exactly the lip's rest offset (14px, see play-button-lip in
        // MainMenu.uss) so the button fully covers it on press instead of scaling down, which
        // shrinks the button from its center and exposes the lip on all sides, not just the
        // bottom - confirmed visually, direct developer correction.
        [FoldoutGroup("Tunables")] [SerializeField] private float m_playButtonPressTravelY = 14f;
        [FoldoutGroup("Tunables")] [SerializeField] private float m_stubButtonPressedScale = 0.95f;

        [Title("Stub buttons")]
        [FoldoutGroup("Stub buttons")] [SerializeField] private string m_stubClickableClassName = "stub-clickable";
        [FoldoutGroup("Stub buttons")] [SerializeField] private string m_stubToastElementName = "stub-toast";
        [FoldoutGroup("Stub buttons")] [SerializeField] private string m_stubToastLabelElementName = "stub-toast-label";
        [FoldoutGroup("Stub buttons")] [SerializeField] private string m_stubToastMessage = "IMPLEMENTED IN ALPHA";
        [FoldoutGroup("Stub buttons")] [SerializeField] private float m_stubToastDuration = 3f;
        [FoldoutGroup("Stub buttons")] [SerializeField] private float m_rapidClickWindowSeconds = 1.5f;
        [FoldoutGroup("Stub buttons")] [SerializeField] private int m_rapidClickThreshold = 3;
        [FoldoutGroup("Stub buttons")]
        [SerializeField]
        private string[] m_rapidClickJokeMessages =
        {
            "OK OK, IT'S STILL ALPHA",
            "THE DEV IS ONE PERSON, BE NICE",
            "CLICKING HARDER WON'T HELP",
            "THAT TICKLES",
            "PATIENCE. ALPHA. SOON(ish).",
        };

        private UIDocument m_uiDocument;
        private VisualElement m_playButton;
        private VisualElement m_safeArea;
        private List<VisualElement> m_stubButtons;
        private VisualElement m_stubToast;
        private Label m_stubToastLabel;
        private Coroutine m_hideStubToastCoroutine;
        private float m_lastStubClickTime = -999f;
        private int m_rapidClickStreak;
        private int m_jokeMessageIndex;

        private void Awake()
        {
            m_uiDocument = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            var root = m_uiDocument.rootVisualElement;
            m_playButton = root.Q<VisualElement>(m_playButtonElementName);
            m_safeArea = root.Q<VisualElement>(m_safeAreaElementName);
            m_stubToast = root.Q<VisualElement>(m_stubToastElementName);
            m_stubToastLabel = root.Q<Label>(m_stubToastLabelElementName);
            m_stubButtons = root.Query<VisualElement>(className: m_stubClickableClassName).ToList();

            m_playButton.RegisterCallback<ClickEvent>(OnPlayClicked);
            m_playButton.RegisterCallback<PointerDownEvent>(OnPlayPointerDown);
            m_playButton.RegisterCallback<PointerUpEvent>(OnPlayPointerUp);
            m_playButton.RegisterCallback<PointerLeaveEvent>(OnPlayPointerUp);
            m_playButton.RegisterCallback<PointerCancelEvent>(OnPlayPointerUp);
            m_safeArea.RegisterCallback<GeometryChangedEvent>(ApplySafeAreaOnce);

            foreach (var stubButton in m_stubButtons)
            {
                stubButton.RegisterCallback<ClickEvent>(OnStubButtonClicked);
                stubButton.RegisterCallback<PointerDownEvent>(OnStubButtonPointerDown);
                stubButton.RegisterCallback<PointerUpEvent>(OnStubButtonPointerUp);
                stubButton.RegisterCallback<PointerLeaveEvent>(OnStubButtonPointerUp);
                stubButton.RegisterCallback<PointerCancelEvent>(OnStubButtonPointerUp);
            }
        }

        private void OnDisable()
        {
            m_playButton.UnregisterCallback<ClickEvent>(OnPlayClicked);
            m_playButton.UnregisterCallback<PointerDownEvent>(OnPlayPointerDown);
            m_playButton.UnregisterCallback<PointerUpEvent>(OnPlayPointerUp);
            m_playButton.UnregisterCallback<PointerLeaveEvent>(OnPlayPointerUp);
            m_playButton.UnregisterCallback<PointerCancelEvent>(OnPlayPointerUp);
            m_safeArea.UnregisterCallback<GeometryChangedEvent>(ApplySafeAreaOnce);

            foreach (var stubButton in m_stubButtons)
            {
                stubButton.UnregisterCallback<ClickEvent>(OnStubButtonClicked);
                stubButton.UnregisterCallback<PointerDownEvent>(OnStubButtonPointerDown);
                stubButton.UnregisterCallback<PointerUpEvent>(OnStubButtonPointerUp);
                stubButton.UnregisterCallback<PointerLeaveEvent>(OnStubButtonPointerUp);
                stubButton.UnregisterCallback<PointerCancelEvent>(OnStubButtonPointerUp);
            }
        }

        private void OnPlayClicked(ClickEvent evt)
        {
            Debug.Log("[MainMenu] PLAY pressed");

            // Hide immediately so there's no chance of a leftover-menu flash once gameplay
            // resumes - GameManager.EnterPlayingState only takes effect next frame (see
            // GameManager's own comment on why), unloading MainMenu is fire-and-forget.
            m_uiDocument.rootVisualElement.style.display = DisplayStyle.None;
            GameManager.TryGetInstance()?.EnterPlayingState();
            SceneManager.UnloadSceneAsync(gameObject.scene);
        }

        private void OnPlayPointerDown(PointerDownEvent evt)
        {
            m_playButton.style.translate = new StyleTranslate(new Translate(0, m_playButtonPressTravelY, 0));
        }

        private void OnPlayPointerUp(IPointerEvent evt)
        {
            m_playButton.style.translate = new StyleTranslate(new Translate(0, 0, 0));
        }

        private void OnStubButtonPointerDown(PointerDownEvent evt)
        {
            var button = (VisualElement)evt.currentTarget;
            button.style.scale = new StyleScale(new Scale(new Vector3(m_stubButtonPressedScale, m_stubButtonPressedScale, 1f)));
        }

        private void OnStubButtonPointerUp(IPointerEvent evt)
        {
            var button = (VisualElement)((EventBase)evt).currentTarget;
            button.style.scale = new StyleScale(new Scale(Vector3.one));
        }

        // Stops here so a click on a nested stub-clickable (currency-add-button inside
        // currency-chip) doesn't also trigger its parent chip's handler via event bubbling.
        private void OnStubButtonClicked(ClickEvent evt)
        {
            evt.StopPropagation();

            var now = Time.unscaledTime;
            m_rapidClickStreak = now - m_lastStubClickTime <= m_rapidClickWindowSeconds ? m_rapidClickStreak + 1 : 1;
            m_lastStubClickTime = now;

            var message = m_rapidClickStreak >= m_rapidClickThreshold ? NextJokeMessage() : m_stubToastMessage;
            ShowStubToast(message);
        }

        private string NextJokeMessage()
        {
            if (m_rapidClickJokeMessages.Length == 0) return m_stubToastMessage;

            var message = m_rapidClickJokeMessages[m_jokeMessageIndex % m_rapidClickJokeMessages.Length];
            m_jokeMessageIndex++;
            return message;
        }

        private void ShowStubToast(string message)
        {
            m_stubToastLabel.text = message;
            m_stubToast.style.display = DisplayStyle.Flex;

            if (m_hideStubToastCoroutine != null) StopCoroutine(m_hideStubToastCoroutine);
            m_hideStubToastCoroutine = StartCoroutine(HideStubToastAfterDelay());
        }

        private IEnumerator HideStubToastAfterDelay()
        {
            yield return new WaitForSecondsRealtime(m_stubToastDuration);
            m_stubToast.style.display = DisplayStyle.None;
            m_hideStubToastCoroutine = null;
        }

        // One-shot: the panel's resolved size (needed to convert Screen.safeArea into panel-space
        // padding) isn't known until the first layout pass, so this fires once off GeometryChangedEvent
        // and immediately unregisters rather than recomputing every layout change.
        private void ApplySafeAreaOnce(GeometryChangedEvent evt)
        {
            m_safeArea.UnregisterCallback<GeometryChangedEvent>(ApplySafeAreaOnce);

            var safeArea = Screen.safeArea;
            var insetLeftNorm = safeArea.xMin / Screen.width;
            var insetRightNorm = (Screen.width - safeArea.xMax) / Screen.width;
            var insetTopNorm = (Screen.height - safeArea.yMax) / Screen.height;
            var insetBottomNorm = safeArea.yMin / Screen.height;

            var panelWidth = evt.newRect.width;
            var panelHeight = evt.newRect.height;

            m_safeArea.style.paddingLeft = m_baseSafeAreaPadding + insetLeftNorm * panelWidth;
            m_safeArea.style.paddingRight = m_baseSafeAreaPadding + insetRightNorm * panelWidth;
            m_safeArea.style.paddingTop = m_baseSafeAreaPadding + insetTopNorm * panelHeight;
            m_safeArea.style.paddingBottom = m_baseSafeAreaPadding + insetBottomNorm * panelHeight;
        }
    }
}
