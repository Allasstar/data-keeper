using UnityEngine;

namespace DataKeeper.Generic.Data
{
    [CreateAssetMenu(menuName = "DataKeeper/Value Provider/AudioClip Provider", fileName = "AudioClip Provider")]
    public class AudioClipProvider : ScriptableObject, IValueProvider<AudioClip>
    {
        [SerializeField] private AudioClip value = null;
    
        public AudioClip GetValue() => value;
    
        public void SetValue(AudioClip newValue) => value = newValue;
    }
}