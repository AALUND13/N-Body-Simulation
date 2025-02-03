using UnityEngine;
using UnityEngine.Events;

[ExecuteInEditMode]
public class Star : CelestialBody {
    public Star(Vector3 position, Vector3 velocity, float mass, Material material) : base(position, velocity, mass, material) {
    }

    public override void OnStart() {
        Material.SetColor("_EmissionColor", GetColor());
    }

    //public override void OnCollision(CelestialBody otherBody, float penetration) {
    //    if(otherBody != this) {
    //        float totalMass = _mass + otherBody._mass;
    //        Vector3 combinedMomentum = (_mass * _velocity) + (otherBody._mass * otherBody._velocity);

    //        if(_mass >= otherBody._mass) {
    //            _mass = totalMass;
    //            _velocity = combinedMomentum / _mass; // Conservation of momentum
    //            GetComponent<Renderer>().material.SetColor("_EmissionColor", GetColor());
    //            Destroy(otherBody.gameObject);
    //        } else {
    //            otherBody.OnCollision(this, penetration);
    //        }
    //    }
    //}

    public override void OnCollision(CelestialBody otherBody, float penetration) {
        if(otherBody != this) {
            float totalMass = _mass + otherBody._mass;
            Vector3 combinedMomentum = (_mass * Velocity) + (otherBody._mass * otherBody.Velocity);

            if(_mass >= otherBody._mass) {
                // Absorb the smaller body
                Mass = totalMass;
                _velocity = combinedMomentum / _mass; // Conservation of momentum
                Material.SetColor("_EmissionColor", GetColor());
                CelestialBodyManager.instance.celestialBodies.Remove(otherBody);
            } else {
                otherBody.OnCollision(this, penetration);
            }
        }
    }

    public Color GetColor() {
        float emission = 2000 * Mathf.Pow(_mass, 0.2f);
        return Mathf.CorrelatedColorTemperatureToRGB(emission);
    }
}
