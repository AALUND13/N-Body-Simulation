using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class FreeCamera : MonoBehaviour {
    public float MovementSpeed = 10.0f;
    public float LookSpeed = 2.0f;
    public float AccelerationRate = 1.0f;
    public float MaxAcceleration = 5.0f; // Prevent infinite acceleration

    private float _yaw = 0.0f;
    private float _pitch = 0.0f;

    private float _accelerationForward = 1.0f;
    private float _accelerationRight = 1.0f;

    private GameObject _followTarget;
    public GameObject FollowTarget;
    public Vector3 Offset;
    public LayerMask selectableLayer; // LayerMask for objects that can be followed

    private bool isFollowing = false; // Whether we are currently following an object

    public void SetOffset() {
        if(FollowTarget) {
            Offset = transform.position - FollowTarget.transform.position;
            _followTarget = FollowTarget;
            isFollowing = true;
        } else {
            _followTarget = null;
            isFollowing = false;
        }
    }

    void Update() {
        HandleObjectSelection(); // Check for middle-click object selection

        // Camera rotation
        if(Input.GetMouseButton(1)) {
            Cursor.lockState = CursorLockMode.Locked;
            _yaw += LookSpeed * Input.GetAxis("Mouse X");
            _pitch -= LookSpeed * Input.GetAxis("Mouse Y");

            // Clamp pitch to prevent flipping
            _pitch = Mathf.Clamp(_pitch, -80f, 80f);

            transform.eulerAngles = new Vector3(_pitch, _yaw, 0.0f);
        } else {
            Cursor.lockState = CursorLockMode.None;
        }

        // Camera movement
        float moveForward = Input.GetAxis("Vertical");
        float moveRight = Input.GetAxis("Horizontal");

        // Acceleration logic
        if(moveForward != 0) {
            _accelerationForward = Mathf.Min(_accelerationForward + Time.unscaledDeltaTime * AccelerationRate, MaxAcceleration);
        } else {
            _accelerationForward = 1.0f;
        }

        if(moveRight != 0) {
            _accelerationRight = Mathf.Min(_accelerationRight + Time.unscaledDeltaTime * AccelerationRate, MaxAcceleration);
        } else {
            _accelerationRight = 1.0f;
        }

        float moveForwardSpeed = moveForward * MovementSpeed * _accelerationForward * Time.unscaledDeltaTime;
        float moveRightSpeed = moveRight * MovementSpeed * _accelerationRight * Time.unscaledDeltaTime;
        float moveUp = 0.0f;

        if(Input.GetKey(KeyCode.E)) {
            moveUp = MovementSpeed * Time.unscaledDeltaTime;
        } else if(Input.GetKey(KeyCode.Q)) {
            moveUp = -MovementSpeed * Time.unscaledDeltaTime;
        }

        Vector3 movement = new Vector3(moveRightSpeed, moveUp, moveForwardSpeed);

        if(isFollowing && _followTarget) {
            // Move the camera relative to the followed object
            Offset += transform.right * moveRightSpeed; // Move horizontally
            Offset += transform.up * moveUp; // Move vertically
            Offset += transform.forward * moveForwardSpeed; // Move forward/backward

            transform.position = _followTarget.transform.position + Offset;
        } else {
            // Normal movement
            transform.Translate(movement, Space.Self);
        }
    }

    /// <summary>
    /// Handles object selection when middle-clicking.
    /// If clicked on empty space, stop following.
    /// </summary>
    void HandleObjectSelection() {
        if(Input.GetMouseButtonDown(2)) { // Middle mouse click
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if(Physics.Raycast(ray, out hit, Mathf.Infinity, selectableLayer)) {
                FollowTarget = hit.collider.gameObject;
                SetOffset(); // Set offset to maintain position
                Debug.Log("Following: " + FollowTarget.name);
            } else {
                // If clicking on empty space, stop following
                FollowTarget = null;
                _followTarget = null;
                isFollowing = false;
                Debug.Log("Stopped following.");
            }
        }
    }
}
