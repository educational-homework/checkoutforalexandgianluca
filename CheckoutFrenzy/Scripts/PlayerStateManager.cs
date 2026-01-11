using System.Collections.Generic;
using UnityEngine;

namespace CryingSnow.CheckoutFrenzy
{
    public enum PlayerState { Free, Holding, Moving, Working, Busy, Paused }

    public class PlayerStateManager
    {
        public PlayerState CurrentState { get; private set; }

        private readonly Stack<PlayerState> stateStack = new();
        private readonly PlayerController player;

        public PlayerStateManager(PlayerController player)
        {
            this.player = player;
            PushState(CurrentState);
        }

        public void PushState(PlayerState newState)
        {
            if (CurrentState != newState)
            {
                stateStack.Push(CurrentState);
                CurrentState = newState;
                HandleTimeScale(newState);
                UpdateCrosshair(newState);
            }
        }

        public void PopState()
        {
            if (stateStack.Count > 0)
            {
                PlayerState restored = stateStack.Pop();
                CurrentState = restored;
                HandleTimeScale(restored);
                UpdateCrosshair(restored);
            }
        }

        public void ClearAll()
        {
            stateStack.Clear();
        }

        private void UpdateCrosshair(PlayerState state)
        {
            bool show = state is PlayerState.Free
                or PlayerState.Holding
                or PlayerState.Moving;

            UIManager.Instance.ToggleCrosshair(show);
        }

        private void HandleTimeScale(PlayerState state)
        {
            Time.timeScale = (state == PlayerState.Paused) ? 0f : 1f;
        }
    }
}
