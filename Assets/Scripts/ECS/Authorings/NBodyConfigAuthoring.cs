using Unity.Entities;
using UnityEngine;

public class NBodyConfigAuthoring : MonoBehaviour {
    public float G = 1f;

    public float Theta = 0.5f;
    public float Epsilon = 0.1f;

    private class Baker : Baker<NBodyConfigAuthoring> {
        public override void Bake(NBodyConfigAuthoring authoring) {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new NBodyConfig {
                G = authoring.G,
                Theta = authoring.Theta,
                Epsilon = authoring.Epsilon
            });
        }
    }
}
