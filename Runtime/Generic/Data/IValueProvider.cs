namespace DataKeeper.Generic.Data
{
    public interface IValueProvider<T>
    {
        T GetValue();
    }
}