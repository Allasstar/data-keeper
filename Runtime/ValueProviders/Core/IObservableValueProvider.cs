using System;

namespace DataKeeper.ValueProviders
{
    // Optional companion to IValueProvider<T> for sources that change on their own
    // (locale switch, blackboard write, remote data): consumers re-pull GetValue() when it fires.
    // Implementations attach to their source when the first handler is added and detach with the
    // last, so an unobserved provider costs nothing.
    public interface IObservableValueProvider
    {
        event Action ValueChanged;
    }

    public static class ValueProviderExtensions
    {
        public static void Bind<T>(this IValueProvider<T> provider, Action onValueChanged)
        {
            if (provider is IObservableValueProvider observable)
                observable.ValueChanged += onValueChanged;
        }

        public static void Unbind<T>(this IValueProvider<T> provider, Action onValueChanged)
        {
            if (provider is IObservableValueProvider observable)
                observable.ValueChanged -= onValueChanged;
        }
    }
}
