using System.Threading.Tasks;
using Alchemy.Inspector;
using Opoint8182.Audio;
using Opoint8182.Daily;
using Opoint8182.Game;
using Opoint8182.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Opoint8182.Bootstrap
{
    public class BootstrapLoader : MonoBehaviour
    {
        [Title("Scenes")]
        [FoldoutGroup("Scenes")] [SerializeField] private string m_mainMenuSceneName = "MainMenu";
        [FoldoutGroup("Scenes")] [SerializeField] private string m_gameSceneName = "Prototype";

        [Title("Launch flow")]
        [FoldoutGroup("Launch flow")] [SerializeField] private LaunchFlowHost m_launchFlow;
        [Tooltip("How long the loading screen covers a restart or a return to the menu at least, so a fast reload does not flash it by.")]
        [FoldoutGroup("Launch flow")] [SerializeField] [Min(0f)] private float m_minCoverSeconds = 1f;

        private bool m_reloading;

        /// <summary>The loader of this session, or null when the game was not started through Bootstrap (a scene opened on its own).</summary>
        public static BootstrapLoader Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            // Set before either scene load even starts, not after - GameManager.Awake() (which
            // runs as part of Prototype's own load, whenever that completes) reads this
            // synchronously, so there's no async window where it could default to Playing and
            // let the plane/HUD run before this loader gets a chance to catch up. An earlier
            // version called GameManager.EnterMainMenuState() here instead, after awaiting both
            // loads - that left exactly that window open (confirmed: the plane visibly moved and
            // the HUD showed through the menu before the async call landed).
            GameManager.ManagedByBootstrap = true;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private async void Start()
        {
            await LoadInitialScenes();
        }

        /// <summary>
        /// Leaves the current run for the main menu: reloads the game scene (every gameplay system starts fresh, as with
        /// <see cref="RestartRun"/>) but starts it in the menu state, then loads the menu scene back in. The menu scene is
        /// unloaded when a run starts. Ignored while a reload is already in progress.
        /// </summary>
        public void ReturnToMenu() => Reload(true, false);

        /// <summary>
        /// <see cref="ReturnToMenu"/> that lands on the leaderboard screen (Game Over's LEADERBOARD and rank chip); back from
        /// there is the menu. The request is consumed once by <see cref="MainMenuController"/> when the menu builds.
        /// </summary>
        public void ReturnToLeaderboard() => Reload(true, true);

        /// <summary>
        /// Starts a fresh run: reloads the game scene (it starts in the playing state) behind the same loading cover as
        /// <see cref="ReturnToMenu"/>. Called by <see cref="GameManager.RestartRun"/>. Ignored while a reload is already in progress.
        /// </summary>
        public void RestartRun() => Reload(false, false);

        private async void Reload(bool toMenu, bool openLeaderboard)
        {
            if (m_reloading) return;

            m_reloading = true;
            // Set (or cleared) after the re-entry guard so an ignored call cannot leave a stale request for the next menu.
            MainMenuController.OpenLeaderboardOnLoad = toMenu && openLeaderboard;
            try
            {
                await ReloadAsync(toMenu);
            }
            finally
            {
                m_reloading = false;
            }
        }

        private async Task ReloadAsync(bool toMenu)
        {
            // Freeze the scene that is going away: its run, music-less audio and timers must not play on under the cover
            // (a paused run would otherwise be unfrozen for the whole wait). The new GameManager.Awake sets timeScale back
            // to 1 and unpauses the audio; the catch below does it when the load fails.
            Time.timeScale = 0f;
            Sfx.StopAll();

            // The daily counters keep what was used; the run's mode ends here (a restart begins its own again).
            if (toMenu) DailyPlay.EndRun();

            // Covers the reload with the loading screen. It lives on this object's child panel, so it survives the load.
            m_launchFlow?.ShowCover();
            var coverStart = Time.realtimeSinceStartup;

            // Consumed by the new GameManager.Awake, which then starts in the menu state (PlayerManager gates the plane,
            // fuel, spawner and HUD off from there). Set before the load starts, like in Awake above. A restart leaves it
            // false, so the new GameManager starts straight in the playing state.
            GameManager.ManagedByBootstrap = toMenu;
            try
            {
                var gameLoad = SceneManager.LoadSceneAsync(m_gameSceneName, LoadSceneMode.Single);
                while (!gameLoad.isDone)
                {
                    SetCoverProgress(coverStart, gameLoad.progress * (toMenu ? 0.5f : 1f));
                    await Task.Yield();
                }

                // A single-mode load unloads the menu with everything else; check anyway so it can never be loaded twice.
                if (toMenu && !SceneManager.GetSceneByName(m_mainMenuSceneName).isLoaded)
                {
                    var menuLoad = SceneManager.LoadSceneAsync(m_mainMenuSceneName, LoadSceneMode.Additive);
                    while (!menuLoad.isDone)
                    {
                        SetCoverProgress(coverStart, 0.5f + menuLoad.progress * 0.5f);
                        await Task.Yield();
                    }
                }

                // The loads take well under a second on a fast machine, which would flash the cover by. Hold it for the
                // minimum time, the bar filling over it, then two more frames so the new scene's UI has built and drawn.
                while (Time.realtimeSinceStartup - coverStart < m_minCoverSeconds)
                {
                    SetCoverProgress(coverStart, 1f);
                    await Task.Yield();
                }

                m_launchFlow?.SetCoverProgress(1f);
                await Task.Yield();
                await Task.Yield();
            }
            catch (System.Exception exception)
            {
                // A flag left set would start the next GameManager (a RETRY, say) in the menu with no menu to press PLAY on.
                GameManager.ManagedByBootstrap = false;
                MainMenuController.OpenLeaderboardOnLoad = false;
                Time.timeScale = 1f;
                Sfx.SetPaused(false);
                Debug.LogException(exception);
            }
            finally
            {
                m_launchFlow?.HideCover();
            }
        }

        // The bar shows the smaller of the real load progress and the share of the minimum time that has passed, so it
        // fills steadily across the whole cover instead of jumping to full and sitting there.
        private void SetCoverProgress(float coverStart, float loadProgress)
        {
            var timeProgress = m_minCoverSeconds > 0f ? (Time.realtimeSinceStartup - coverStart) / m_minCoverSeconds : 1f;
            m_launchFlow?.SetCoverProgress(Mathf.Min(loadProgress, timeProgress));
        }

        private async Task LoadInitialScenes()
        {
            var operations = new AsyncOperationGroup(2);
            operations.operations.Add(SceneManager.LoadSceneAsync(m_mainMenuSceneName, LoadSceneMode.Additive));
            operations.operations.Add(SceneManager.LoadSceneAsync(m_gameSceneName, LoadSceneMode.Additive));

            while (!operations.IsDone)
            {
                m_launchFlow?.ReportProgress(operations.Progress);
                await Task.Yield();
            }

            SceneManager.SetActiveScene(SceneManager.GetSceneByName(m_gameSceneName));

            // Holds on the loading screen for its minimum time and while the rotate prompt is up; the menu is already
            // loaded and active underneath.
            if (m_launchFlow != null) await m_launchFlow.WaitUntilReadyAsync();
        }
    }
}
