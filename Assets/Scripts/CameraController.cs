using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.InputSystem;

public class CameraController : Singleton<CameraController>
{
    [SerializeField]
    private const int DEFAULT_PRIORITY = 10;
    [SerializeField]
    private GameObject cameraTarget;
    [SerializeField]
    private Unity.Cinemachine.CinemachineCamera OverheadCamera;
    [SerializeField]
    private Unity.Cinemachine.CinemachineCamera focusCamera;
   
    
    [SerializeField] private float cameraSpeed = 10f;

    [Header("Vue inclinée (Overhead)")]
    [SerializeField, Range(20f, 89f)] private float pitchNear = 40f;   // inclinaison à distanceMin (plus rasante)
    [SerializeField, Range(20f, 89f)] private float pitchFar = 70f;    // inclinaison à distanceMax (plus plongeante)
    [SerializeField] private float fieldOfView = 30f;
    [SerializeField] private float distanceMin = 40f;
    [SerializeField] private float distanceMax = 250f;
    [SerializeField] private float distanceDefault = 120f;
    [SerializeField] private float distanceZoomSpeed = 2.5f;
    [SerializeField, Range(0.01f, 0.5f)] private float scrollZoomStep = 0.15f;   // part de la distance par cran de molette

    private float distance;
    private Unity.Cinemachine.CinemachineFollow overheadFollow;

    [SerializeField] private CameraMode defaultMode = CameraMode.Overhead;

    [SerializeField] private CameraMode currentMode;
    
    
    private Coroutine panCoroutine;
    private Coroutine zoomCoroutine;

    public event Action<Unity.Cinemachine.CinemachineCamera> onCameraChanged;

    public event Action onSelectAction;
    public event Action onCommandAction;
    public event Action onDeselectAction;
    public event Action onFocusAction;

    [SerializeField] private bool isLocked = false;
    public bool IsLocked
    {
        get => isLocked;
        set => isLocked = value;
    }
    
    public GameObject CameraTarget { get => cameraTarget; }

    void Start()
    {
        overheadFollow = OverheadCamera.GetComponent<Unity.Cinemachine.CinemachineFollow>();
        // Décalage exprimé en coordonnées monde : nord toujours en haut.
        var tracker = overheadFollow.TrackerSettings;
        tracker.BindingMode = Unity.Cinemachine.TargetTracking.BindingMode.WorldSpace;
        // Pas d'amortissement : sinon la caméra traîne derrière la cible et le LookAt la fait pivoter.
        tracker.PositionDamping = Vector3.zero;
        overheadFollow.TrackerSettings = tracker;

        OverheadCamera.Lens.FieldOfView = fieldOfView;
        SetDistance(distanceDefault);
        ChangeCamera(defaultMode);
    }

    // Place la caméra à `distance` de la cible, côté sud ; l'inclinaison varie de pitchNear à pitchFar avec la distance.
    private void SetDistance(float value)
    {
        distance = Mathf.Clamp(value, distanceMin, distanceMax);
        overheadFollow.FollowOffset = CameraMath.TiltedOffset(distance, distanceMin, distanceMax, pitchNear, pitchFar);
    }

    public void ChangeCamera(CameraMode mode)
    {
        currentMode = mode;
 
        Unity.Cinemachine.CinemachineCamera virtualCamera = GetCamera(mode);
        onCameraChanged?.Invoke(virtualCamera);

        virtualCamera.Priority = DEFAULT_PRIORITY;
        virtualCamera.Prioritize();
    }

    public void OnPanChange(InputAction.CallbackContext context)
    {
        if (isLocked) return;
        
        if (context.performed)
        {
            if (panCoroutine != null)
            {
                StopCoroutine(panCoroutine);
            }

            panCoroutine = StartCoroutine(processPan(context));
        } else if (context.canceled)
        {
            if (panCoroutine != null)
            {
                StopCoroutine(panCoroutine);
            }
        }
        
        Debug.Log("PAN CHANGE");
    }


    public void OnTriggerCommand(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            onCommandAction?.Invoke();
        }
    }

    public void OnFocusChanged(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            // Debug.Log("Focus button pressed...");
        } else if (context.performed)
        {
            onSelectAction?.Invoke();
           // Debug.Log("Double tapped - Focus...");
            //onFocusAction?.Invoke();
        } else if (context.canceled)
        {
           // Debug.Log("Double tapped - Select...");
            //onSelectAction?.Invoke();
        }
    }
    
    public void OnZoomChanged(InputAction.CallbackContext context)
    {
        if (isLocked) return;

        // Molette : un cran = un pas immédiat (la boucle par image ne dure qu'une image).
        if (context.control?.device is Mouse)
        {
            if (context.performed)
                SetDistance(distance * (1f + Mathf.Sign(context.ReadValue<float>()) * scrollZoomStep));
            return;
        }

        if (context.performed)
        {
            if (zoomCoroutine != null)
            {
                StopCoroutine(zoomCoroutine);
            }

            zoomCoroutine = StartCoroutine(processZoom(context));
        } else if (context.canceled)
        {
            if (zoomCoroutine != null)
            {
                StopCoroutine(zoomCoroutine);
            }
        }
    }

    private Unity.Cinemachine.CinemachineCamera GetCamera(CameraMode mode)
    {
        switch (mode)
        {
            case CameraMode.Overhead:
                return OverheadCamera;
                break;
            case CameraMode.Focus:
                return focusCamera;
            default:
                return null;
        }
    }

    public IEnumerator processPan(InputAction.CallbackContext context)
    {
        while (true)
        {
            
            // move the camera in the direction of the input vector (2D)
            Vector2 inputVector = context.ReadValue<Vector2>();

            // move the camera in the direction of the input vector (3D)
            Vector3 moveVector = new Vector3(inputVector.x, 0, inputVector.y);
            cameraTarget.transform.position += moveVector * cameraSpeed * Time.deltaTime;
            ClampTarget();

            yield return null;
        }
    }
    
    // Garde la cible de la caméra au-dessus de la carte.
    private void ClampTarget()
    {
        HexGrid grid = GameManager.Instance.HexGrid;
        if (grid == null) return;
        Rect bounds = grid.WorldBounds;
        Vector3 p = cameraTarget.transform.position;
        p.x = Mathf.Clamp(p.x, bounds.xMin, bounds.xMax);
        p.z = Mathf.Clamp(p.z, bounds.yMin, bounds.yMax);
        cameraTarget.transform.position = p;
    }

    public IEnumerator processZoom(InputAction.CallbackContext context)
    {
        //Change the FOV of the camera based on the input. If not keyboard, then adjust the value based on the scrollWheelZoomSpeed
        float zoomInput = context.ReadValue<float>();

        //Debug.Log("Zooming: " + zoomInput);
        while (true)
        {
            //Change the FOV of the camera based on the input. If not keyboard, then adjust the value based on the scrollWheelZoomSpeed
            SetDistance(distance + zoomInput * distanceZoomSpeed * Time.deltaTime);

            yield return null;
        }
    }

    public void OnDeselectAction(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            onDeselectAction?.Invoke();
        }
    }

}


public enum CameraMode
{
    Overhead,
    Focus
}



