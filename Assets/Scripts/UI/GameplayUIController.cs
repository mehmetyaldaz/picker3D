using Picker3D.Cosmetics;
using Picker3D.Core;
using Picker3D.Level;
using Picker3D.Missions;
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
        [SerializeField] private LevelManager levelManager;
        [SerializeField] private PlayerCosmeticController
            cosmeticController;
        [SerializeField] private GemWallet gemWallet;
        [SerializeField] private MissionManager missionManager;

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
                tapToPlayScreen.ResetRequested +=
                    HandleResetRequested;
                tapToPlayScreen.AddLevelRequested +=
                    HandleAddLevelRequested;
                tapToPlayScreen.AddGemRequested +=
                    HandleAddGemRequested;
                tapToPlayScreen.AddOneLevelRequested +=
                    HandleAddOneLevelRequested;
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
                tapToPlayScreen.ResetRequested -=
                    HandleResetRequested;
                tapToPlayScreen.AddLevelRequested -=
                    HandleAddLevelRequested;
                tapToPlayScreen.AddGemRequested -=
                    HandleAddGemRequested;
                tapToPlayScreen.AddOneLevelRequested -=
                    HandleAddOneLevelRequested;
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

        private void HandleResetRequested()
        {
            if (gameFlow.CurrentState != GameState.Ready ||
                restartService.IsRestarting)
            {
                return;
            }

            cosmeticController.ResetAllCosmetics();
            missionManager.ResetAllMissions();
            levelManager.ResetLevelProgress();
            restartService.RestartCurrentLevel(0f);
        }

        private void HandleAddLevelRequested()
        {
            if (gameFlow.CurrentState != GameState.Ready ||
                restartService.IsRestarting)
            {
                return;
            }

            levelManager.AddLevelProgress(10);
            restartService.RestartCurrentLevel(0f);
        }

        private void HandleAddGemRequested()
        {
            if (gameFlow.CurrentState == GameState.Ready)
            {
                gemWallet.AddGems(5000);
            }
        }

        private void HandleAddOneLevelRequested()
        {
            if (gameFlow.CurrentState != GameState.Ready ||
                restartService.IsRestarting)
            {
                return;
            }

            levelManager.AddLevelProgress(1);
            restartService.RestartCurrentLevel(0f);
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

            if (levelManager == null)
            {
                levelManager =
                    FindFirstObjectByType<LevelManager>();
            }

            if (cosmeticController == null)
            {
                cosmeticController =
                    FindFirstObjectByType<
                        PlayerCosmeticController>();
            }

            if (gemWallet == null)
            {
                gemWallet =
                    FindFirstObjectByType<GemWallet>();
            }

            if (missionManager == null)
            {
                missionManager =
                    FindFirstObjectByType<MissionManager>();
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
                restartService != null &&
                levelManager != null &&
                cosmeticController != null &&
                gemWallet != null &&
                missionManager != null;

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
