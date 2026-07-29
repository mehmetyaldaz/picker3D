using Picker3D.Core;
using Picker3D.Level;
using UnityEngine;

namespace Picker3D.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIScreenRouter))]
    public sealed class GameplayUIController : MonoBehaviour
    {
        [Header("Screens")]
        [SerializeField] private UIScreenRouter screenRouter;
        [SerializeField] private TapToPlayScreen tapToPlayScreen;
        [SerializeField] private FailedScreen failedScreen;
        [SerializeField] private LevelFinishedScreen levelFinishedScreen;
        [SerializeField] private StoreScreen storeScreen;
        [SerializeField] private MissionScreen missionScreen;
        [SerializeField] private LevelProgressHUD levelProgressHud;

        [Header("Game Systems")]
        [SerializeField] private GameFlowController gameFlow;
        [SerializeField] private LevelRestartService restartService;

        private void Reset()
        {
            FindLocalReferences();
        }

        private void Awake()
        {
            FindLocalReferences();

            if (!ValidateReferences())
            {
                enabled = false;
                return;
            }

            gameFlow.EnableManualStart();
        }

        private void OnEnable()
        {
            if (gameFlow != null)
            {
                gameFlow.StateChanged += HandleGameStateChanged;
            }

            if (tapToPlayScreen != null)
            {
                tapToPlayScreen.Tapped += HandleTapToPlay;
                tapToPlayScreen.StoreRequested +=
                    HandleStoreRequested;
                tapToPlayScreen.MissionRequested +=
                    HandleMissionRequested;
            }

            if (failedScreen != null)
            {
                failedScreen.ContinueRequested +=
                    HandleContinueRequested;
            }

            if (levelFinishedScreen != null)
            {
                levelFinishedScreen.ContinueRequested +=
                    HandleLevelFinishedContinueRequested;
            }

            if (storeScreen != null)
            {
                storeScreen.CloseRequested +=
                    HandleStoreCloseRequested;
            }

            if (missionScreen != null)
            {
                missionScreen.CloseRequested +=
                    HandleMissionCloseRequested;
            }
        }

        private void Start()
        {
            ShowScreenForState(gameFlow.CurrentState);
        }

        private void OnDisable()
        {
            if (gameFlow != null)
            {
                gameFlow.StateChanged -= HandleGameStateChanged;
            }

            if (tapToPlayScreen != null)
            {
                tapToPlayScreen.Tapped -= HandleTapToPlay;
                tapToPlayScreen.StoreRequested -=
                    HandleStoreRequested;
                tapToPlayScreen.MissionRequested -=
                    HandleMissionRequested;
            }

            if (failedScreen != null)
            {
                failedScreen.ContinueRequested -=
                    HandleContinueRequested;
            }

            if (levelFinishedScreen != null)
            {
                levelFinishedScreen.ContinueRequested -=
                    HandleLevelFinishedContinueRequested;
            }

            if (storeScreen != null)
            {
                storeScreen.CloseRequested -=
                    HandleStoreCloseRequested;
            }

            if (missionScreen != null)
            {
                missionScreen.CloseRequested -=
                    HandleMissionCloseRequested;
            }
        }

        private void HandleTapToPlay()
        {
            if (gameFlow.CurrentState != GameState.Ready)
            {
                return;
            }

            screenRouter.HideAll();
            gameFlow.StartPlaying();
        }

        private void HandleStoreRequested()
        {
            if (gameFlow.CurrentState != GameState.Ready)
            {
                return;
            }

            SetLevelProgressVisible(false);
            screenRouter.Show(UIScreenId.Store);
        }

        private void HandleStoreCloseRequested()
        {
            if (gameFlow.CurrentState != GameState.Ready ||
                screenRouter.CurrentScreenId != UIScreenId.Store)
            {
                return;
            }

            SetLevelProgressVisible(true);
            screenRouter.Show(UIScreenId.TapToPlay);
        }

        private void HandleMissionRequested()
        {
            if (gameFlow.CurrentState != GameState.Ready)
            {
                return;
            }

            SetLevelProgressVisible(false);
            screenRouter.Show(UIScreenId.Mission);
        }

        private void HandleMissionCloseRequested()
        {
            if (gameFlow.CurrentState != GameState.Ready ||
                screenRouter.CurrentScreenId !=
                UIScreenId.Mission)
            {
                return;
            }

            SetLevelProgressVisible(true);
            screenRouter.Show(UIScreenId.TapToPlay);
        }

        private void HandleContinueRequested()
        {
            if (gameFlow.CurrentState != GameState.Failed ||
                restartService.IsRestarting)
            {
                return;
            }

            screenRouter.HideAll();
            restartService.RestartCurrentLevel(0f);
        }

        private void HandleLevelFinishedContinueRequested()
        {
            if (gameFlow.CurrentState !=
                GameState.LevelFinished)
            {
                return;
            }

            gameFlow.PrepareToPlay();
        }

        private void HandleGameStateChanged(
            GameState previousState,
            GameState nextState)
        {
            ShowScreenForState(nextState);
        }

        private void ShowScreenForState(GameState state)
        {
            SetLevelProgressVisible(true);

            switch (state)
            {
                case GameState.Ready:
                    screenRouter.Show(UIScreenId.TapToPlay);
                    break;

                case GameState.Failed:
                    screenRouter.Show(UIScreenId.Failed);
                    break;

                case GameState.LevelFinished:
                    screenRouter.Show(
                        UIScreenId.LevelFinished);
                    break;

                default:
                    screenRouter.HideAll();
                    break;
            }
        }

        private void FindLocalReferences()
        {
            if (screenRouter == null)
            {
                screenRouter = GetComponent<UIScreenRouter>();
            }

            if (tapToPlayScreen == null)
            {
                tapToPlayScreen =
                    GetComponentInChildren<TapToPlayScreen>(true);
            }

            if (failedScreen == null)
            {
                failedScreen =
                    GetComponentInChildren<FailedScreen>(true);
            }

            if (levelFinishedScreen == null)
            {
                levelFinishedScreen =
                    GetComponentInChildren<LevelFinishedScreen>(
                        true);
            }

            if (storeScreen == null)
            {
                storeScreen =
                    GetComponentInChildren<StoreScreen>(true);
            }

            if (missionScreen == null)
            {
                missionScreen =
                    GetComponentInChildren<MissionScreen>(true);
            }

            if (levelProgressHud == null)
            {
                levelProgressHud =
                    GetComponentInChildren<LevelProgressHUD>(true);
            }
        }

        private bool ValidateReferences()
        {
            bool isValid =
                screenRouter != null &&
                tapToPlayScreen != null &&
                failedScreen != null &&
                levelFinishedScreen != null &&
                storeScreen != null &&
                missionScreen != null &&
                levelProgressHud != null &&
                gameFlow != null &&
                restartService != null;

            if (!isValid)
            {
                Debug.LogError(
                    $"{nameof(GameplayUIController)} on '{name}' has missing Inspector references.",
                    this);
            }

            return isValid;
        }

        private void SetLevelProgressVisible(bool isVisible)
        {
            if (levelProgressHud != null)
            {
                levelProgressHud.gameObject.SetActive(isVisible);
            }
        }

        private void OnValidate()
        {
            FindLocalReferences();
        }
    }
}
