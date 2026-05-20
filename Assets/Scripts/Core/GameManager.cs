using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CyberBrass.Core
{
    /// <summary>
    /// Represents the different states the game can be in.
    /// </summary>
    public enum GameState
    {
        Boot,
        MainMenu,
        Gameplay,
        Paused,
        GameOver,
        Victory
    }

    /// <summary>
    /// The GameManager class coordinates the global game lifecycle, states, and scene management.
    /// Registers itself with the ServiceLocator upon initialization.
    /// </summary>
    [DisallowMultipleComponent]
    public class GameManager : MonoBehaviour
    {
        private static GameManager _instance;

        /// <summary>
        /// Gets the current active state of the game.
        /// </summary>
        public GameState CurrentState { get; private set; } = GameState.Boot;

        /// <summary>
        /// Triggered when the game state changes. Passes the new state and the previous state.
        /// </summary>
        public event Action<GameState, GameState> OnStateChanged;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            // Register this manager with the ServiceLocator
            ServiceLocator.Register<GameManager>(this);
        }

        private void Start()
        {
            // Transition from Boot to the initial state (either MainMenu or auto-load)
            TransitionToState(GameState.MainMenu);
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                ServiceLocator.Unregister<GameManager>();
                _instance = null;
            }
        }

        /// <summary>
        /// Transitions the game state machine to a new state.
        /// Handles cursor locking, time scale adjustments, and triggers state change events.
        /// </summary>
        /// <param name="newState">The target state to transition to.</param>
        public void TransitionToState(GameState newState)
        {
            if (CurrentState == newState) return;

            GameState oldState = CurrentState;
            CurrentState = newState;

            switch (newState)
            {
                case GameState.MainMenu:
                    Time.timeScale = 1f;
                    SetCursorLocked(false);
                    break;
                case GameState.Gameplay:
                    Time.timeScale = 1f;
                    SetCursorLocked(true);
                    break;
                case GameState.Paused:
                    Time.timeScale = 0f;
                    SetCursorLocked(false);
                    break;
                case GameState.GameOver:
                    Time.timeScale = 0f;
                    SetCursorLocked(false);
                    break;
                case GameState.Victory:
                    Time.timeScale = 0f;
                    SetCursorLocked(false);
                    break;
            }

            Debug.Log($"[GameManager] Transitioned from {oldState} to {newState}");
            OnStateChanged?.Invoke(newState, oldState);
        }

        /// <summary>
        /// Helper method to load a scene by name and set the state to Gameplay.
        /// </summary>
        /// <param name="sceneName">Name of the scene to load.</param>
        public void LoadLevel(string sceneName)
        {
            Debug.Log($"[GameManager] Loading Level: {sceneName}");
            SceneManager.LoadScene(sceneName);
            TransitionToState(GameState.Gameplay);
        }

        /// <summary>
        /// Restarts the currently active scene.
        /// </summary>
        public void RestartCurrentLevel()
        {
            string currentSceneName = SceneManager.GetActiveScene().name;
            LoadLevel(currentSceneName);
        }

        /// <summary>
        /// Helper to control mouse cursor locking and visibility.
        /// </summary>
        /// <param name="locked">True to lock and hide cursor, false to unlock and show it.</param>
        private void SetCursorLocked(bool locked)
        {
            if (locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        /// <summary>
        /// Quits the application. Saves progress first.
        /// </summary>
        public void QuitGame()
        {
            Debug.Log("[GameManager] Quitting game...");
            // TODO: Call SaveService.SaveProgressAsync() before quitting
            Application.Quit();
        }
    }
}
