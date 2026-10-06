using Alchemy.Inspector;
using Opoint8182.Leaderboard;
using UnityEngine;
using UnityEngine.UIElements;

namespace Opoint8182.UI
{
    /// <summary>
    /// Main-menu Top-10 panel (Live leaderboard, Pass 4). Lives on the MainMenu GameObject next to
    /// MainMenuController, sharing its PanelRenderer. Rows are built in code because the count varies.
    /// </summary>
    [RequireComponent(typeof(PanelRenderer))]
    public class LeaderboardPanelUI : MonoBehaviour
    {
        [Title("References")]
        [FoldoutGroup("References")] [SerializeField] private string m_tileElementName = "leaderboard-tile";
        [FoldoutGroup("References")] [SerializeField] private string m_panelElementName = "leaderboard-panel";
        [FoldoutGroup("References")] [SerializeField] private string m_statusLabelName = "leaderboard-status";
        [FoldoutGroup("References")] [SerializeField] private string m_rowsElementName = "leaderboard-rows";
        [FoldoutGroup("References")] [SerializeField] private string m_retryButtonName = "leaderboard-retry-button";
        [FoldoutGroup("References")] [SerializeField] private string m_closeButtonName = "leaderboard-close-button";

        [Title("Tunables")]
        [FoldoutGroup("Tunables")] [SerializeField] private int m_topLimit = 10;
        [FoldoutGroup("Tunables")] [SerializeField] private float m_tilePressedScale = 0.95f;

        private const string k_LoadingText = "Loading...";
        private const string k_EmptyText = "No scores yet today";
        private const string k_ErrorText = "Couldn't load leaderboard";

        private PanelRenderer m_panelRenderer;
        private VisualElement m_tile;
        private VisualElement m_panel;
        private Label m_statusLabel;
        private VisualElement m_rows;
        private Button m_retryButton;
        private Button m_closeButton;
        private int m_loadToken;

        private void Awake()
        {
            m_panelRenderer = GetComponent<PanelRenderer>();
        }

        private void OnEnable()
        {
            m_panelRenderer.RegisterUIReloadCallback(OnUIReload);
        }

        private void OnDisable()
        {
            m_panelRenderer.UnregisterUIReloadCallback(OnUIReload);
            UnregisterElementCallbacks();
            m_loadToken++;
        }

        private void OnUIReload(PanelRenderer renderer, VisualElement root, int version)
        {
            UnregisterElementCallbacks();

            m_tile = root.Q<VisualElement>(m_tileElementName);
            m_panel = root.Q<VisualElement>(m_panelElementName);
            m_statusLabel = root.Q<Label>(m_statusLabelName);
            m_rows = root.Q<VisualElement>(m_rowsElementName);
            m_retryButton = root.Q<Button>(m_retryButtonName);
            m_closeButton = root.Q<Button>(m_closeButtonName);

            if (m_tile != null)
            {
                m_tile.RegisterCallback<ClickEvent>(OnTileClicked);
                m_tile.RegisterCallback<PointerDownEvent>(OnTilePointerDown);
                m_tile.RegisterCallback<PointerUpEvent>(OnTilePointerUp);
                m_tile.RegisterCallback<PointerLeaveEvent>(OnTilePointerUp);
                m_tile.RegisterCallback<PointerCancelEvent>(OnTilePointerUp);
            }
            if (m_retryButton != null) m_retryButton.clicked += Load;
            if (m_closeButton != null) m_closeButton.clicked += Close;

            SetPanelVisible(false);
        }

        // Guards against double-registration - OnUIReload can fire more than once per enable.
        private void UnregisterElementCallbacks()
        {
            if (m_tile != null)
            {
                m_tile.UnregisterCallback<ClickEvent>(OnTileClicked);
                m_tile.UnregisterCallback<PointerDownEvent>(OnTilePointerDown);
                m_tile.UnregisterCallback<PointerUpEvent>(OnTilePointerUp);
                m_tile.UnregisterCallback<PointerLeaveEvent>(OnTilePointerUp);
                m_tile.UnregisterCallback<PointerCancelEvent>(OnTilePointerUp);
            }
            if (m_retryButton != null) m_retryButton.clicked -= Load;
            if (m_closeButton != null) m_closeButton.clicked -= Close;
        }

        private void OnTileClicked(ClickEvent evt)
        {
            evt.StopPropagation();
            SetPanelVisible(true);
            Load();
        }

        private void OnTilePointerDown(PointerDownEvent evt)
        {
            m_tile.style.scale = new StyleScale(new Scale(new Vector3(m_tilePressedScale, m_tilePressedScale, 1f)));
        }

        private void OnTilePointerUp(IPointerEvent evt)
        {
            m_tile.style.scale = new StyleScale(new Scale(Vector3.one));
        }

        private void Close()
        {
            // Bumping the token drops any fetch still in flight, so a late result can't redraw a closed panel.
            m_loadToken++;
            SetPanelVisible(false);
        }

        private async void Load()
        {
            // Each load bumps the token so a result that lands after a close, a reopen or a
            // retry is dropped instead of overwriting newer state.
            int token = ++m_loadToken;

            ClearRows();
            SetStatus(k_LoadingText);
            SetRetryVisible(false);

            LeaderboardService service = LeaderboardService.TryGetInstance();
            if (service == null)
            {
                ShowError();
                return;
            }

            TopScoresResult result = await service.GetTopScoresAsync(m_topLimit);
            if (this == null || token != m_loadToken) return;

            if (!result.Success)
            {
                ShowError();
                return;
            }

            if (result.Rows.Count == 0 && !result.Self.HasValue)
            {
                SetStatus(k_EmptyText);
                return;
            }

            SetStatus(null);
            foreach (LeaderboardRow row in result.Rows) AddRow(row, false);
            if (result.Self.HasValue) AddRow(result.Self.Value, true);
        }

        private void ShowError()
        {
            SetStatus(k_ErrorText);
            SetRetryVisible(true);
        }

        private void AddRow(LeaderboardRow row, bool separated)
        {
            if (m_rows == null) return;

            var element = new VisualElement();
            element.AddToClassList("leaderboard-row");
            if (row.IsSelf) element.AddToClassList("leaderboard-row--self");
            if (separated) element.AddToClassList("leaderboard-row--gap");

            element.Add(CreateCell($"#{row.Rank}", "leaderboard-cell-rank"));
            element.Add(CreateCell(row.IsSelf ? $"{row.Name} (you)" : row.Name, "leaderboard-cell-name"));
            element.Add(CreateCell(row.Score.ToString(), "leaderboard-cell-score"));
            m_rows.Add(element);
        }

        private static Label CreateCell(string text, string className)
        {
            var label = new Label(text);
            label.AddToClassList("leaderboard-cell");
            label.AddToClassList(className);
            return label;
        }

        private void ClearRows()
        {
            m_rows?.Clear();
        }

        private void SetStatus(string text)
        {
            if (m_statusLabel == null) return;
            m_statusLabel.text = text ?? string.Empty;
            m_statusLabel.style.display = string.IsNullOrEmpty(text) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private void SetRetryVisible(bool visible)
        {
            if (m_retryButton == null) return;
            m_retryButton.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void SetPanelVisible(bool visible)
        {
            if (m_panel == null) return;
            m_panel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
