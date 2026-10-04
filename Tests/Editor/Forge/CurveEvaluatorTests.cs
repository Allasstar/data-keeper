using DataKeeper.Forge.Dsp;
using NUnit.Framework;
using Unity.Collections;

namespace DataKeeper.Forge.Tests
{
    public class CurveEvaluatorTests
    {
        [Test]
        public void Evaluate_HitsBreakpointsAndHoldsOutsideRange()
        {
            using var points = new NativeArray<Breakpoint>(new[]
            {
                new Breakpoint(0.2f, 0f),
                new Breakpoint(0.5f, 1f, 0.8f),
                new Breakpoint(1f, 0.25f),
            }, Allocator.Temp);

            Assert.AreEqual(0f, CurveEvaluator.Evaluate(points, 0, 3, 0f));
            Assert.AreEqual(0f, CurveEvaluator.Evaluate(points, 0, 3, 0.2f));
            Assert.AreEqual(1f, CurveEvaluator.Evaluate(points, 0, 3, 0.5f), 1e-6f);
            Assert.AreEqual(0.25f, CurveEvaluator.Evaluate(points, 0, 3, 1f));
            Assert.AreEqual(0.25f, CurveEvaluator.Evaluate(points, 0, 3, 2f));
        }

        [Test]
        public void Bend_TensionShapesMidpoint()
        {
            Assert.AreEqual(0.5f, CurveEvaluator.Bend(0.5f, 0f), 1e-6f);
            Assert.Less(CurveEvaluator.Bend(0.5f, 1f), 0.5f);
            Assert.Greater(CurveEvaluator.Bend(0.5f, -1f), 0.5f);
        }

        [Test]
        public void Evaluate_EmptyCurveIsZero()
        {
            using var points = new NativeArray<Breakpoint>(0, Allocator.Temp);
            Assert.AreEqual(0f, CurveEvaluator.Evaluate(points, 0, 0, 0.5f));
        }
    }
}
