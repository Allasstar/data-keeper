using System;
using System.Threading;
using DataKeeper.Attributes;
using DataKeeper.Generic;
using UnityEngine;

namespace DataKeeper.BeeTween
{
    /// <summary>
    /// Main player component that executes tween sequences
    /// </summary>
    [AddComponentMenu("DataKeeper/BeeTween/Bee Tween Player")]
    public class BeeTweenPlayer : MonoBehaviour
    {
        public bool runOnEnable = false;
        public bool stopBeforeRun = false;
        public bool waitContext = false;
        public bool restartOnEnd = false;
        
        [field: SerializeField] public Optional<float> RestartOnFail { get; private set; } = new Optional<float>(1, false);
        
        [SerializeReference, SerializeReferenceSelector]
        public IBeeTweenContext Context;

        private CancellationTokenSource _cts;

        private void OnEnable()
        {
            if (!runOnEnable) return;
            Run();
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

            while (waitContext && (Context == null || !Context.IsValid()))
            {
                await Awaitable.EndOfFrameAsync(_cts.Token);
            }
            
            if (Context?.RootNode == null) return;

            try
            {
                Debug.Log($"Start > FC: {Time.frameCount}", this);
                await Context.RootNode.ExecuteAsync(Context, _cts);
                Debug.Log($"End > FC: {Time.frameCount}", this);

                if (restartOnEnd)
                {
                    await Awaitable.EndOfFrameAsync(_cts.Token);
                    Run();
                }
            }
            catch (OperationCanceledException)
            {
                /* expected on disable */
            }
            catch (Exception e)
            {
                Debug.Log($"Fail > FC: {Time.frameCount}", this);
                Debug.LogError(e, this);

                if (RestartOnFail.Enabled)
                {
                    await Awaitable.WaitForSecondsAsync(RestartOnFail.Value, _cts.Token);
                    Run();
                }
            }
        }

        private void OnDisable()
        {
            Stop();
        }
    }
}
