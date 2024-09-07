using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [SerializeField]
    private GameObject cameraTarget;
    [SerializeField]
    private CinemachineVirtualCamera _virtualCamera;
    [SerializeField]
    private float cameraSpeed = 10f;

    private Coroutine panCoroutine;

    public void OnPanChange(InputAction.CallbackContext context)
    {
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

            yield return null;
        }
    }
}
