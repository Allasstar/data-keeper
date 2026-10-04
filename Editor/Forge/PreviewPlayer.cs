using System;
using Unity.Collections;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DataKeeper.Editor.Forge
{
    public sealed class PreviewPlayer : IDisposable
    {
        private GameObject _host;
        private AudioSource _source;
        private AudioClip _clip;
        private float[] _data;
        private float _volume = 1f;

        public bool IsPlaying => _source != null && _source.isPlaying;
        public int TimeSamples => _source != null ? _source.timeSamples : 0;

        public float Volume
        {
            get => _volume;
            set
            {
                _volume = Mathf.Clamp01(value);
                if (_source != null) _source.volume = _volume;
            }
        }

        public void Load(NativeArray<float> interleaved, int channels, int sampleRate)
        {
            EnsureSource();

            var frames = interleaved.Length / channels;
            if (_clip == null || _clip.samples != frames || _clip.channels != channels || _clip.frequency != sampleRate)
            {
                if (_clip != null) Object.DestroyImmediate(_clip);
                _clip = AudioClip.Create("Forge Preview", frames, channels, sampleRate, false);
                _clip.hideFlags = HideFlags.HideAndDontSave;
            }

            if (_data == null || _data.Length != interleaved.Length) _data = new float[interleaved.Length];
            interleaved.CopyTo(_data);
            _clip.SetData(_data, 0);

            // Reassigning the clip stops playback; skipping it lets live edits be heard mid-play.
            if (_source.clip != _clip) _source.clip = _clip;
        }

        public void Play()
        {
            if (_source == null || _clip == null) return;
            _source.Stop();
            _source.Play();
        }

        public void Stop()
        {
            if (_source != null) _source.Stop();
        }

        public void Dispose()
        {
            if (_clip != null) Object.DestroyImmediate(_clip);
            if (_host != null) Object.DestroyImmediate(_host);
            _clip = null;
            _host = null;
            _source = null;
        }

        private void EnsureSource()
        {
            if (_source != null) return;

            _host = new GameObject("Forge Preview") { hideFlags = HideFlags.HideAndDontSave };
            _source = _host.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;
            _source.volume = _volume;
        }
    }
}
