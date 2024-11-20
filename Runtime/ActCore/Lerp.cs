using UnityEngine;

namespace DataKeeper.ActCore
{
    public static class Lerp
    {
        public static float Float(float from, float to, float value)
        {
            return Mathf.Lerp(from, to, value);
        }

        public static int Int(int from, int to, float value)
        {
            return (int)Mathf.Lerp(from, to, value);
        }
        
        public static float Remap(this float value, float fromMin, float fromMax, float toMin,  float toMax)
        {
            return Mathf.Lerp(toMin, toMax, Mathf.InverseLerp(fromMin, fromMax, value));
        }
    
        public static float Map(this float value, float from, float to)
        {
            return Mathf.Lerp(from, to, value);
        }
        
        public static int Remap(this int value, int fromMin, int fromMax, int toMin,  int toMax)
        {
            return (int)Mathf.Lerp(toMin, toMax, Mathf.InverseLerp(fromMin, fromMax, value));
        }
    
        public static int Map(this int value, int from, int to)
        {
            return (int)Mathf.Lerp(from, to, value);
        }
    }
}
