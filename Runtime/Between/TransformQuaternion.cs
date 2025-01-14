using UnityEngine;

namespace DataKeeper.Between
{
    public class TransformQuaternion : TweenBase<Transform, Quaternion>
    {
        public TransformQuaternion(Transform target) : base(target)
        {
            this.startValue = target.rotation;
            this.endValue = Quaternion.FromToRotation(target.forward, target.right);
        }

        public override TweenBase<Transform, Quaternion> Speed(float speed)
        {
            this.speed = speed;
            if (speed > 0)
            {
                this.duration = Quaternion.Angle(startValue, endValue) / speed;
            }

            return this;
        }

        protected override void SetTargetValue(Quaternion value)
        {
            target.rotation = value;
        }

        protected override void LerpValueAndSetTargetValue(float value)
        {
            SetTargetValue(Quaternion.Lerp(startValue, endValue, value));
        }

        protected override void HandleIncrementLoop(Quaternion start, Quaternion end)
        {
            Quaternion increment = end * Quaternion.Inverse(start);
            startValue = endValue;
            endValue = increment * endValue;
            SetTargetValue(startValue);
        }
    }
}