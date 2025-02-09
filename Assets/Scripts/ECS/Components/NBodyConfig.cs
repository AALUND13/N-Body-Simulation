using Unity.Entities;

public struct NBodyConfig : IComponentData {
    public float G;

    public float Theta;
    public float Epsilon;
}
