namespace DataKeeper.ValueProviders
{
    // Enums used by the Vector3 derived providers. Serialized as ints, so the namespace
    // move is data-safe without [MovedFrom].
    public enum Vector3Type { Position = 0, EulerRotation = 1, Scale = 2 }
    public enum SpaceType   { Global = 0, Local = 1 }
    public enum LookAtType  { ToTarget = 0, ToOther = 1 }
}
