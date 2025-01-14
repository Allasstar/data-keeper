using UnityEngine;

namespace DataKeeper.Between
{
    public class TransformLocalPosition : TweenBase<Transform, Vector3>
    {
        public TransformLocalPosition(Transform target) : base(target)
        {
            this.startValue = target.localPosition;
            this.endValue = target.localPosition + Vector3.forward;
        }

        public override TweenBase<Transform, Vector3> Speed(float speed)
        {
            this.speed = speed;
            if (speed > 0)
            {
                this.duration = Vector3.Distance(startValue, endValue) / speed;
            }
            return this;
        }

        protected override void SetTargetValue(Vector3 value)
        {
            target.localPosition = value;
        }

        protected override void LerpValueAndSetTargetValue(float value)
        {
            SetTargetValue(Vector3.Lerp(startValue, endValue, value));
        }

        protected override void HandleIncrementLoop(Vector3 start, Vector3 end)
        {
            Vector3 increment = endValue - startValue;
            startValue = endValue;
            endValue += increment;
            SetTargetValue(startValue);
        }
    }
}