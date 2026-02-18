using DataKeeper.Attributes;
using UnityEngine;

namespace DataKeeper.Overlay
{
    public class Spinner : MonoBehaviour
    {
        public float spinSpeed = 180;
        
        [StaticPicker]
        public Vector3 spinAxis = Vector3.forward;

        public void Update()
        {
            transform.RotateAround(transform.position, spinAxis, spinSpeed * Time.deltaTime);
        }
    }
}