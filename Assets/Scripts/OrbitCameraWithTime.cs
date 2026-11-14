using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

public class OrbitCameraWithTime : MonoBehaviour
{
    [SerializeField]
     Unity.Cinemachine.CinemachineCamera orbitalCamera;
     Unity.Cinemachine.CinemachineOrbitalFollow orbitalTransposer;
     [SerializeField] private float rotationSpeed = 10f; // degrés/s

     private Coroutine rotationCoroutine;

     private void Awake()
     {
         if (orbitalCamera == null) orbitalCamera = GetComponent<Unity.Cinemachine.CinemachineCamera>();

         if (orbitalCamera == null)
         {
             Debug.LogError("Orbital camera missing : l'objet n'a pas de CinemachineCamera (ancienne VirtualCamera non convertie ?)", this);
             enabled = false;
             return;
         }

         orbitalTransposer = orbitalCamera.GetComponent<Unity.Cinemachine.CinemachineOrbitalFollow>();
         
         if (orbitalTransposer == null)
         {
             Debug.LogError("CinemachineOrbitalFollow manquant sur la camera", this);
             enabled = false;
             return;
         }

         CameraController.Instance.onCameraChanged += OnCameraChange;

     }

     private void OnDestroy()
     {
         // L'abonnement se fait dans Awake : on se désabonne à la destruction.
         if (CameraController.Instance != null)
             CameraController.Instance.onCameraChanged -= OnCameraChange;
     }

     private void OnCameraChange(Unity.Cinemachine.CinemachineCamera camera)
     {
         if (camera != orbitalCamera) return;

         Unity.Cinemachine.CinemachineOrbitalFollow newTransposer = camera.GetComponent<Unity.Cinemachine.CinemachineOrbitalFollow>();
         if (orbitalTransposer == newTransposer)
         {
             if (rotationCoroutine != null)
             {
                 StopCoroutine(rotationCoroutine);
             }

             rotationCoroutine = StartCoroutine(Rotate());
         }
         else
         {
             StopCoroutine(rotationCoroutine);
         }
     }

     public IEnumerator Rotate()
     {
         while (true)
         {
             orbitalTransposer.HorizontalAxis.Value += rotationSpeed * Time.deltaTime;
             yield return null;
         }
     }
}
