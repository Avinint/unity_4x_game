using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using UnityEngine.InputSystem;

public class CameraController : Singleton<CameraController>
{
    [SerializeField]
    private const int DEFAULT_PRIORITY = 10;
    [SerializeField]
    private GameObject cameraTarget;
    [SerializeField]
    private CinemachineVirtualCamera topDownCamera;
    [SerializeField]
    private CinemachineVirtualCamera focusCamera;
   
    
    [SerializeField] private float cameraSpeed = 10f;
    [SerializeField] private float cameraZoomSpeed = 1f;
    [SerializeField] private float cameraZoomMin = 15f;
    [SerializeField] private float cameraZoomMax = 100f;
    [SerializeField] private float cameraZoomDefault = 50f;

    [SerializeField] private CameraMode defaultMode = CameraMode.TopDown;

    [SerializeField] private CameraMode currentMode;
    
    
    private Coroutine panCoroutine;
    private Coroutine zoomCoroutine;

    public event Action<CinemachineVirtualCamera> onCameraChanged;

    public event Action onSelectAction;
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
        topDownCamera.m_Lens.FieldOfView = cameraZoomDefault;
        ChangeCamera(defaultMode);
    }

    public void ChangeCamera(CameraMode mode)
    {
        currentMode = mode;
 
        CinemachineVirtualCamera virtualCamera = GetCamera(mode);
        onCameraChanged?.Invoke(virtualCamera);

        virtualCamera.Priority = DEFAULT_PRIORITY;
        virtualCamera.MoveToTopOfPrioritySubqueue();
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

    public void OnFocusChanged(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            // Debug.Log("Focus button pressed...");
        } else if (context.performed)
        {
            Debug.Log("Double tapped - Focus...");
            onFocusAction?.Invoke();
        } else if (context.canceled)
        {
            Debug.Log("Double tapped - Select...");
            onSelectAction?.Invoke();
        }
    }
    
    public void OnZoomChanged(InputAction.CallbackContext context)
    {
    
        if (isLocked) return;
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

    private CinemachineVirtualCamera GetCamera(CameraMode mode)
    {
        switch (mode)
        {
            case CameraMode.TopDown:
                return topDownCamera;
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
            Debug.Log("Moving: " + inputVector);
            
            // move the camera in the direction of the input vector (3D)
            Vector3 moveVector = new Vector3(inputVector.x, 0, inputVector.y);
            cameraTarget.transform.position += moveVector * cameraSpeed * Time.deltaTime;

            Debug.Log("Position: " + cameraTarget.transform.position);
            
            yield return null;
        }
    }
    
    public IEnumerator processZoom(InputAction.CallbackContext context)
    {
        //Change the FOV of the camera based on the input. If not keyboard, then adjust the value based on the scrollWheelZoomSpeed
        float zoomInput = context.ReadValue<float>();

        //Debug.Log("Zooming: " + zoomInput);
        while (true)
        {
            //Change the FOV of the camera based on the input. If not keyboard, then adjust the value based on the scrollWheelZoomSpeed
            float zoomAmount = topDownCamera.m_Lens.FieldOfView + zoomInput * cameraZoomSpeed * Time.deltaTime;
            topDownCamera.m_Lens.FieldOfView = Mathf.Clamp(zoomAmount, cameraZoomMin, cameraZoomMax);

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
    TopDown,
    Focus
}



