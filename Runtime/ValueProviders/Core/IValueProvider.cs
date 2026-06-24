namespace DataKeeper.ValueProviders
{
    /// <summary>
    /// The single value-provider contract. Implemented directly by ScriptableObject
    /// asset providers, and (via the per-type marker interfaces) by inline
    /// [Serializable] strategy providers used with SerializeReference.
    /// </summary>
    public interface IValueProvider<T>
    {
        T GetValue();
    }
}