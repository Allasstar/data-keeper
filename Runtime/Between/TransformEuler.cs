using UnityEngine;

namespace DataKeeper.Between
{
    public class TransformEuler : TweenBase<Transform, Vector3>
    {
        public TransformEuler(Transform target) : base(target)
        {
            this.startValue = target.eulerAngles;
            this.endValue = target.eulerAngles + Vector3.up * 90f;
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
            target.eulerAngles = value;
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