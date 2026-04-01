namespace DataKeeper.BeeTween
{
    public interface EaseProvider
    {
        float Evaluate(IBeeTweenContext context, float t);
    }
}