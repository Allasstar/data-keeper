using DataKeeper.MathFunc;
using UnityEngine;

namespace DataKeeper.Between
{
    public class TransformScale : TweenBase<Transform, Vector3>
    {
        public TransformScale(Transform target) : base(target)
        {
            this.startValue = target.localScale;
            this.endValue = target.localScale + Vector3.forward;
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
            target.localScale = value;
        }

        protected override void LerpValueAndSetTargetValue(float value)
        {
            SetTargetValue(Lerp.LerpVector3Unclamped(startValue, endValue, value));
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