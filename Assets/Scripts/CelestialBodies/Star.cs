using UnityEngine;
using UnityEngine.Events;

[ExecuteInEditMode]
public class Star : CelestialBody {
    public override void OnStart() {
        float emission = 400 * (4 / 3.0f * Mathf.PI * Mathf.Pow(_mass, 1.0f / 3.0f) / 3.0f);
        if (Application.isPlaying) {
            GetComponent<Renderer>().material.SetColor("_EmissionColor", GetColor());
        }
    }

    public override void OnCollision(CelestialBody otherBody, float penetration) {
        if(otherBody != this) {
            float totalMass = _mass + otherBody._mass;
            Vector3 combinedMomentum = (_mass * _velocity) + (otherBody._mass * otherBody._velocity);

            if(_mass >= otherBody._mass) {
                _mass = totalMass;
                _velocity = combinedMomentum / _mass; // Conservation of momentum
                GetComponent<Renderer>().material.SetColor("_EmissionColor", GetColor());
                Destroy(otherBody.gameObject);
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
