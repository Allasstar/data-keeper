using UnityEngine;
using UnityEngine.UI;

namespace DataKeeper.ValueProviders
{
    // Per-type marker interfaces over IValueProvider<T>. These exist so SerializeReference
    // fields stay typed as a non-generic interface — TypeCache / SerializeReferenceSelector
    // enumerate implementors of these reliably (constructed-generic interface fields are not).
    //
    // Only inline [Serializable] strategy providers implement these markers, so the selector
    // dropdown never offers a ScriptableObject asset provider (those implement IValueProvider<T>
    // directly instead).

    public interface IBoolProvider          : IValueProvider<bool> { }
    public interface IFloatProvider         : IValueProvider<float> { }
    public interface IIntProvider           : IValueProvider<int> { }
    public interface IStringProvider        : IValueProvider<string> { }
    public interface IVector2Provider       : IValueProvider<Vector2> { }
    public interface IVector3Provider       : IValueProvider<Vector3> { }
    public interface IColorProvider         : IValueProvider<Color> { }
    public interface ITransformProvider     : IValueProvider<Transform> { }
    public interface IRectTransformProvider : IValueProvider<RectTransform> { }
    public interface IImageProvider         : IValueProvider<Image> { }
}
