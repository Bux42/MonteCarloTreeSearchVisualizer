using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Camera")]
    public Camera mainCamera;

    [Header("Zoom")]
    public float zoomSpeed = 500000f;
    public float minDistance = .01f;
    public float maxDistance = 500000000000f;

    [Header("Rotation")]
    public float rotationSpeed = 20f;

    [Header("Focus Transition")]
    public float focusLerpSpeed = 5f;

    Transform target;          // current focus target
    bool focusMode = false;    // are we locked on a node?
    bool orbiting = false; // are we orbiting a target?
    float distance = 10f;      // current camera distance
    Vector3 focusPoint;        // what we look at

    void Start()
    {
        focusPoint = Vector3.zero;
        distance = Vector3.Distance(mainCamera.transform.position, focusPoint);
    }

    void Update()
    {
        HandleInput();
        UpdateCamera();
    }

    void HandleInput()
    {
        // --- Focus / Unfocus ---
        if (Input.GetKeyDown(KeyCode.F) && !Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl))
        {
            Debug.Log("F pressed, checking for node under mouse...");
            var hitNode = RaycastNodeUnderMouse();
            if (hitNode != null)
            {
                target = hitNode.transform;
                focusMode = true;
                Debug.Log($"Focusing on node: {target.name}");
            }
        }
        if (Input.GetKeyDown(KeyCode.F) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
        {
            focusMode = false;
            target = null;
        }

        // --- Zoom ---
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.001f)
        {
            distance = Mathf.Clamp(distance - scroll * zoomSpeed, minDistance, maxDistance);
        }

        // --- Rotation ---
        if (Input.GetMouseButton(2)) // middle mouse
        {
            float dx = Input.GetAxis("Mouse X");
            float dy = -Input.GetAxis("Mouse Y");

            Quaternion rot = Quaternion.Euler(dy * rotationSpeed, dx * rotationSpeed, 0);
            Vector3 dir = mainCamera.transform.position - focusPoint;
            dir = rot * dir;
            focusPoint = mainCamera.transform.position - dir;

            orbiting = true;
        }
        else
        {
            orbiting = false;
        }
    }

    void UpdateCamera()
    {
        // Update focus point
        if (focusMode && target != null)
        {
            focusPoint = Vector3.Lerp(focusPoint, target.position, Time.deltaTime * focusLerpSpeed);
        }

        // Camera position is focusPoint + back along current forward
        Vector3 dir = (mainCamera.transform.position - focusPoint).normalized;
        Vector3 desiredPos = focusPoint + dir * distance;

        mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, desiredPos, Time.deltaTime * focusLerpSpeed);
        mainCamera.transform.LookAt(focusPoint);
    }

    MctsNode RaycastNodeUnderMouse()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
        {
            return hit.collider.GetComponentInParent<MctsNode>();
        }
        return null;
    }
}
