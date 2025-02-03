using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Experimental.GlobalIllumination;


[ExecuteInEditMode]
public abstract class CelestialBody : MonoBehaviour {
    // Dont change theses values directly, use the properties instead
    // Unless you don't want the changes run the OnPropertyChange method
    [SerializeField] public float _radius;
    [SerializeField] public float _radiusMultiplier = 1.0f;
    [SerializeField] public float _mass;
    [SerializeField] public Vector3 _velocity;
    [SerializeField] public bool UseUnityPhysics = false;

    [SerializeField] public Transform ObjTransform;
    [SerializeField] public Rigidbody ObjRigidbody;

    public float Radius {
        get => _radius;
        set {
            _radius = value;
            OnPropertyChange();
        }
    }

    public float RadiusMultiplier {
        get => _radiusMultiplier;
        set {
            _radiusMultiplier = value;
            OnPropertyChange();
        }
    }

    public float Mass {
        get {
            if(UseUnityPhysics) return ObjRigidbody.mass;
            return _mass;
        }
        set {
            if(UseUnityPhysics) ObjRigidbody.mass = value;
            _mass = value;
            OnPropertyChange();
        }
    }

    public Vector3 Velocity {
        get {
            if(UseUnityPhysics) return ObjRigidbody.velocity;
            return _velocity;
        }
        set {
            if(UseUnityPhysics) ObjRigidbody.velocity = value;
            _velocity = value;
            OnPropertyChange();
        }
    }

    internal Vector3 _position;

    public UnityEvent OnEnabledEvent = new UnityEvent();
    public UnityEvent OnDisabledEvent = new UnityEvent();
    public UnityEvent OnUpdateEvent = new UnityEvent();
    public UnityEvent OnChangeEvent = new UnityEvent();

    private void Awake() {
        ObjTransform = transform;
        ObjRigidbody = GetComponent<Rigidbody>();
    }

    private void Start() {
        _position = transform.position;
        _radius = 4 / 3.0f * Mathf.PI * Mathf.Pow(_mass, 1.0f / 3.0f) / 3.0f * _radiusMultiplier;
        ObjTransform.localScale = new Vector3(_radius * 2, _radius * 2, _radius * 2);


        OnStart();
    }

    private void OnEnable() {
        if(CelestialBodyManager.instance != null && !CelestialBodyManager.instance.celestialBodies.Contains(this)) CelestialBodyManager.instance.celestialBodies.Add(this);
        OnEnabled();
        OnEnabledEvent.Invoke();
    }

    private void OnValidate() {
        OnPropertyChange();
    }

    private void OnDisable() {
        if(CelestialBodyManager.instance != null && CelestialBodyManager.instance.celestialBodies.Contains(this)) CelestialBodyManager.instance.celestialBodies.Remove(this);
        OnDisabled();
        OnDisabledEvent.Invoke();
    }

    void Update() {
        if(ObjTransform.position != _position) {
            _position = transform.position;
            OnPropertyChange();
        }
    }

    public void OnPropertyChange() {
        _radius = 4 / 3.0f * Mathf.PI * Mathf.Pow(_mass, 1.0f / 3.0f) / 3.0f * _radiusMultiplier;
        ObjTransform.localScale = new Vector3(_radius * 2, _radius * 2, _radius * 2);

        OnChange();
        OnChangeEvent.Invoke();
    }

    public virtual void OnCollision(CelestialBody otherBody, float penetration) {
        if(otherBody != this) {
            float totalMass = _mass + otherBody._mass;
            Vector3 combinedMomentum = (_mass * Velocity) + (otherBody._mass * otherBody.Velocity);

            if(_mass >= otherBody._mass) {
                // Absorb the smaller body
                _mass = totalMass;
                _velocity = combinedMomentum / _mass; // Conservation of momentum
                Destroy(otherBody.gameObject);
            } else {
                // The other body absorbs this one
                otherBody._mass = totalMass;
                otherBody._velocity = combinedMomentum / otherBody._mass; // Conservation of momentum
                Destroy(gameObject);
            }
        }
    }

    public virtual void OnStart() { }
    public virtual void OnEnabled() { }
    public virtual void OnDisabled() { }
    public virtual void OnUpdate() { }
    public virtual void OnChange() { }
}
