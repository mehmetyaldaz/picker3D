using System;
using Picker3D.Player;
using UnityEngine;

namespace Picker3D.Core
{
    public sealed class GameFlowController : MonoBehaviour
    {
        [SerializeField] private PlayerMovement playerMovement;

        public event Action<GameState, GameState> StateChanged;

        public GameState CurrentState { get; private set; } = GameState.Initializing;

        private void Awake()
        {
            if (playerMovement == null)
            {
                Debug.LogError(
                    $"{nameof(GameFlowController)} on '{name}' requires a {nameof(PlayerMovement)} reference.",
                    this);
            }
        }

        private void Start()
        {
            ApplyPlayerMovementState();
            PrepareToPlay();
        }

        public void PrepareToPlay()
        {
            ChangeState(GameState.Ready);
        }

        public void StartPlaying()
        {
            if (CurrentState != GameState.Ready &&
                CurrentState != GameState.Initializing)
            {
                return;
            }

            ChangeState(GameState.PlayingPart);
        }

        public void StopForDrop()
        {
            if (CurrentState != GameState.PlayingPart)
            {
                return;
            }

            ChangeState(GameState.WaitingForDrop);
        }

        public void BeginPartResolution()
        {
            if (CurrentState != GameState.WaitingForDrop)
            {
                return;
            }

            ChangeState(GameState.ResolvingPart);
        }

        public void BeginTransition()
        {
            if (CurrentState != GameState.ResolvingPart)
            {
                return;
            }

            ChangeState(GameState.Transitioning);
        }

        public void ResumePlaying()
        {
            if (CurrentState != GameState.Transitioning)
            {
                return;
            }

            ChangeState(GameState.PlayingPart);
        }

        public void EnterFinalRamp()
        {
            ChangeState(GameState.FinalRamp);
        }

        public void CompleteLevel()
        {
            ChangeState(GameState.LevelCompleted);
        }

        public void WaitForLevelContinue()
        {
            ChangeState(GameState.LevelFinished);
        }

        public void FailLevel()
        {
            ChangeState(GameState.Failed);
        }

        private void ChangeState(GameState nextState)
        {
            if (CurrentState == nextState)
            {
                return;
            }

            GameState previousState = CurrentState;
            CurrentState = nextState;
            ApplyPlayerMovementState();
            Debug.Log(
                $"[MovementDebug][GameFlow] State changed | {previousState} -> {nextState}",
                this);
            StateChanged?.Invoke(previousState, nextState);
        }

        private void ApplyPlayerMovementState()
        {
            if (playerMovement == null)
            {
                return;
            }

            bool enableForwardMovement =
                CurrentState == GameState.PlayingPart;
            bool enableHorizontalMovement =
                CurrentState == GameState.PlayingPart ||
                CurrentState == GameState.WaitingForDrop ||
                CurrentState == GameState.ResolvingPart ||
                CurrentState == GameState.Transitioning;

            playerMovement.SetMovementEnabled(
                enableForwardMovement,
                enableHorizontalMovement);
        }

        private void OnValidate()
        {
            if (playerMovement == null)
            {
                Debug.LogWarning(
                    $"{nameof(GameFlowController)} on '{name}' has no {nameof(PlayerMovement)} assigned.",
                    this);
            }
        }
    }
}
