using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Picker3D.Core
{
    [DisallowMultipleComponent]
    public sealed class LevelRestartService : MonoBehaviour
    {
        private bool isRestarting;
        private Func<IEnumerator> runtimeRestartRoutine;

        public bool IsRestarting => isRestarting;

        public void SetRuntimeRestartRoutine(
            Func<IEnumerator> restartRoutine)
        {
            runtimeRestartRoutine = restartRoutine;
        }

        public void ClearRuntimeRestartRoutine(
            Func<IEnumerator> restartRoutine)
        {
            if (runtimeRestartRoutine == restartRoutine)
            {
                runtimeRestartRoutine = null;
            }
        }

        public void RestartCurrentLevel(float delay)
        {
            if (isRestarting)
            {
                return;
            }

            StartCoroutine(RestartRoutine(Mathf.Max(0f, delay)));
        }

        private IEnumerator RestartRoutine(float delay)
        {
            isRestarting = true;

            if (delay > 0f)
            {
                yield return new WaitForSecondsRealtime(delay);
            }

            if (runtimeRestartRoutine != null)
            {
                yield return runtimeRestartRoutine();
                isRestarting = false;
                yield break;
            }

            Scene activeScene = SceneManager.GetActiveScene();

            if (activeScene.buildIndex < 0)
            {
                Debug.LogError(
                    $"Cannot restart scene '{activeScene.path}'. Add the scene to the active Build Profile scene list.",
                    this);
                isRestarting = false;
                yield break;
            }

            Time.timeScale = 1f;
            AsyncOperation restartOperation = SceneManager.LoadSceneAsync(
                activeScene.buildIndex,
                LoadSceneMode.Single);

            if (restartOperation == null)
            {
                Debug.LogError(
                    $"Unity could not start reloading scene '{activeScene.path}'.",
                    this);
                isRestarting = false;
            }
        }
    }
}
