using UnityEngine;
using UnityEngine.Events;


[ExecuteInEditMode]
public class CelestialBody {
    // Dont change theses values directly, use the properties instead
    // Unless you don't want the changes run the OnPropertyChange method
    public float _radius;
    public float _mass;
    public Vector3 _velocity;
    //[SerializeField] public bool UseUnityPhysics = false;

    private Vector3 _position = Vector3.zero;
    public Vector3 Position {
        get {
            return _position;
        }
        set {
            _position = value;
            matrix.SetTRS(Position, Quaternion.identity, Vector3.one * _radius * 1.5f);
        }
    }

    public Material Material;

    internal Matrix4x4 matrix = Matrix4x4.identity;

    public CelestialBody(Vector3 position, Vector3 velocity, float mass, Material material) {
        Position = position;
        Material = new Material(material);
        _mass = mass;
        _velocity = velocity;
        _radius = 4 / 3.0f * Mathf.PI * Mathf.Pow(_mass, 1.0f / 3.0f) / 3.0f;
        matrix.SetTRS(Position, Quaternion.identity, Vector3.one * _radius * 1.5f);

        CelestialBodyManager.instance.celestialBodies.Add(this);

        OnStart();
    }
    public float Radius {
        get => _radius;
        set {
            _radius = value;
            OnPropertyChange();
        }
    }

    //public float RadiusMultiplier {
    //    get => _radiusMultiplier;
    //    set {
    //        _radiusMultiplier = value;
    //        matrix = Matrix4x4.TRS(Position, Quaternion.identity, Vector3.one * _radius);
    //        //OnPropertyChange();
    //    }
    //}

    public float Mass {
        get {
            return _mass;
        }
        set {
            _mass = value;
            OnPropertyChange();
        }
    }

    public Vector3 Velocity {
        get {
            return _velocity;
        }
        set {
            _velocity = value;
            OnPropertyChange();
        }
    }

    public UnityEvent OnEnabledEvent = new UnityEvent();
    public UnityEvent OnDisabledEvent = new UnityEvent();
    public UnityEvent OnUpdateEvent = new UnityEvent();
    public UnityEvent OnChangeEvent = new UnityEvent();

    //private void OnEnable() {
    //    if(CelestialBodyManager.instance != null && !CelestialBodyManager.instance.celestialBodies.Contains(this)) CelestialBodyManager.instance.celestialBodies.Add(this);
    //    OnEnabled();
    //    OnEnabledEvent.Invoke();
    //}

    //private void OnValidate() {
    //    OnPropertyChange();
    //}

    //private void OnDisable() {
    //    if(CelestialBodyManager.instance != null && CelestialBodyManager.instance.celestialBodies.Contains(this)) CelestialBodyManager.instance.celestialBodies.Remove(this);
    //    OnDisabled();
    //    OnDisabledEvent.Invoke();
    //}

    //void Update() {
    //    if(ObjTransform.position != _position) {
    //        _position = transform.position;
    //        OnPropertyChange();
    //    }
    //}

    public void OnPropertyChange() {
        _radius = 4 / 3.0f * Mathf.PI * Mathf.Pow(_mass, 1.0f / 3.0f) / 3.0f;
        matrix.SetTRS(Position, Quaternion.identity, Vector3.one * _radius * 1.5f);

        OnChange();
        OnChangeEvent.Invoke();
    }

    public virtual void OnCollision(CelestialBody otherBody, float penetration) {
        if(otherBody != this) {
            float totalMass = _mass + otherBody._mass;
            Vector3 combinedMomentum = (_mass * Velocity) + (otherBody._mass * otherBody.Velocity);

            if(_mass >= otherBody._mass) {
                // Absorb the smaller body
                Mass = totalMass;
                _velocity = combinedMomentum / _mass; // Conservation of momentum
                CelestialBodyManager.instance.celestialBodies.Remove(otherBody);
            } else {
                otherBody.OnCollision(this, penetration);
            }
        }
    }

    public virtual void OnStart() { }
    public virtual void OnEnabled() { }
    public virtual void OnDisabled() { }
    public virtual void OnUpdate() { }
    public virtual void OnChange() { }
}
