using System;
using System.Threading;
using DataKeeper.Attributes;
using DataKeeper.Generic;
using UnityEngine;

namespace DataKeeper.BeeTween
{
    [AddComponentMenu("DataKeeper/BeeTween/Bee Tween Player")]
    public class BeeTweenPlayer : MonoBehaviour
    {
        public bool runOnEnable = false;
        public bool stopBeforeRun = false;

        [field: SerializeField] public Optional<float> RestartOnEnd  { get; private set; } = new Optional<float>(1, false);
        [field: SerializeField] public Optional<float> RestartOnFail { get; private set; } = new Optional<float>(1, false);

        [SerializeReference, SerializeReferenceSelector]
        public IBeeTweenNode RootNode;

        private CancellationTokenSource _cts;

        private void OnEnable()
        {
            if (!runOnEnable) return;
            Run();
        }

        private void OnDisable()
        {
            Stop();
        }

        [ContextMenu(nameof(Run))]
        public void Run()
        {
            _ = RunAsync();
        }

        [ContextMenu(nameof(Stop))]
        public void Stop()
        {
            _cts?.Cancel();
        }

        public async Awaitable RunAsync()
        {
            if (stopBeforeRun && _cts != null)
            {
                _cts.Cancel();
                _cts = null;
            }

            _cts = new CancellationTokenSource();

            if (RootNode == null) return;

            try
            {
                await RootNode.ExecuteAsync(_cts);

                if (RestartOnEnd.Enabled)
                {
                    await Awaitable.WaitForSecondsAsync(RestartOnEnd.Value, _cts.Token);
                    Run();
                }
            }
            catch (OperationCanceledException)
            {
                /* expected on disable */
            }
            catch (Exception e)
            {
                Debug.LogError(e, this);

                if (RestartOnFail.Enabled)
                {
                    await Awaitable.WaitForSecondsAsync(RestartOnFail.Value, _cts.Token);
                    Run();
                }
            }
        }
    }
}
