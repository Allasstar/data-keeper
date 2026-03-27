namespace DataKeeper.BeeTween
{
    /// <summary>
    /// Base interface for all tween contexts
    /// </summary>
    public interface IBeeTweenContext
    {
        object Target { get; }
        IBeeTweenNode RootNode { get; }
        bool IsValid();
    }
    
    /// <summary>
    /// Generic context interface for type-safe operations
    /// </summary>
    public interface IBeeTweenContext<T> : IBeeTweenContext where T : class
    {
        new T Target { get; }
    }
}