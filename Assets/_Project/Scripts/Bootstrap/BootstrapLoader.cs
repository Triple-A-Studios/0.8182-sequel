using System.Threading.Tasks;
using Alchemy.Inspector;
using Opoint8182.Game;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Opoint8182.Bootstrap
{
    public class BootstrapLoader : MonoBehaviour
    {
        [Title("Scenes")]
        [FoldoutGroup("Scenes")] [SerializeField] private string m_mainMenuSceneName = "MainMenu";
        [FoldoutGroup("Scenes")] [SerializeField] private string m_gameSceneName = "Prototype";

        private void Awake()
        {
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

        private async void Start()
        {
            await LoadInitialScenes();
        }

        private async Task LoadInitialScenes()
        {
            var operations = new AsyncOperationGroup(2);
            operations.operations.Add(SceneManager.LoadSceneAsync(m_mainMenuSceneName, LoadSceneMode.Additive));
            operations.operations.Add(SceneManager.LoadSceneAsync(m_gameSceneName, LoadSceneMode.Additive));

            while (!operations.IsDone)
            {
                await Task.Yield();
            }

            SceneManager.SetActiveScene(SceneManager.GetSceneByName(m_gameSceneName));
        }
    }
}
