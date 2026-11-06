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

         if (orbitalCamera == null) Debug.Log("Orbital camera missing");

         orbitalTransposer = orbitalCamera.GetComponent<Unity.Cinemachine.CinemachineOrbitalFollow>();
         
         if (orbitalTransposer == null) Debug.Log("Orbital transposer missing");

         CameraController.Instance.onCameraChanged += OnCameraChange;
         
     }

     private void OnCameraChange(Unity.Cinemachine.CinemachineCamera camera)
     {
         if (camera != orbitalCamera) return;

         Unity.Cinemachine.CinemachineOrbitalFollow newTransposer = camera.GetComponent<Unity.Cinemachine.CinemachineOrbitalFollow>();
         if (orbitalTransposer == newTransposer)
         {
             if (rotationCoroutine != null)
             {
                 Debug.Log("Stop rotation");
                 StopCoroutine(rotationCoroutine);
             }

             Debug.Log("Start rotation");
             rotationCoroutine = StartCoroutine(Rotate());
         }
         else
         {
             Debug.Log("Stop rotation");
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
