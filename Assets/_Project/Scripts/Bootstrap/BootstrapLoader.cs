using System.Threading.Tasks;
using Alchemy.Inspector;
using Opoint8182.Audio;
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

        private bool m_returningToMenu;

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
        /// <see cref="GameManager.RestartRun"/>) but starts it in the menu state, then loads the menu scene back in. The menu
        /// scene is unloaded when a run starts. Ignored while a return is already in progress.
        /// </summary>
        public async void ReturnToMenu()
        {
            if (m_returningToMenu) return;

            m_returningToMenu = true;
            try
            {
                await ReturnToMenuAsync();
            }
            finally
            {
                m_returningToMenu = false;
            }
        }

        private async Task ReturnToMenuAsync()
        {
            // The same resets RestartRun does: a pause must not leak its frozen clock and muted audio into the next scene.
            Time.timeScale = 1f;
            Sfx.SetPaused(false);
            Sfx.StopAll();

            // Consumed by the new GameManager.Awake, which then starts in the menu state (PlayerManager gates the plane,
            // fuel, spawner and HUD off from there). Set before the load starts, like in Awake above.
            GameManager.ManagedByBootstrap = true;
            try
            {
                var gameLoad = SceneManager.LoadSceneAsync(m_gameSceneName, LoadSceneMode.Single);
                while (!gameLoad.isDone) await Task.Yield();

                // A single-mode load unloads the menu with everything else; check anyway so it can never be loaded twice.
                if (!SceneManager.GetSceneByName(m_mainMenuSceneName).isLoaded)
                {
                    var menuLoad = SceneManager.LoadSceneAsync(m_mainMenuSceneName, LoadSceneMode.Additive);
                    while (!menuLoad.isDone) await Task.Yield();
                }
            }
            catch (System.Exception exception)
            {
                // A flag left set would start the next GameManager (a RETRY, say) in the menu with no menu to press PLAY on.
                GameManager.ManagedByBootstrap = false;
                Debug.LogException(exception);
            }
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
