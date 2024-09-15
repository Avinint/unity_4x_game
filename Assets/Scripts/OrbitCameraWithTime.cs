using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class OrbitCameraWithTime : MonoBehaviour
{
    [SerializeField]
     CinemachineVirtualCamera orbitalCamera;
     CinemachineOrbitalTransposer orbitalTransposer;
     private float rotationSpeed;

     private Coroutine rotationCoroutine;

     private void Awake()
     {
         if (orbitalCamera == null) orbitalCamera = GetComponent<CinemachineVirtualCamera>();

         if (orbitalCamera == null) Debug.Log("Orbital camera missing");

         orbitalTransposer = orbitalCamera.GetCinemachineComponent<CinemachineOrbitalTransposer>();
         
         if (orbitalTransposer == null) Debug.Log("Orbital transposer missing");

         rotationSpeed = orbitalTransposer.m_XAxis.m_MaxSpeed;

         CameraController.Instance.onCameraChanged += OnCameraChange;
         
     }

     private void OnCameraChange(CinemachineVirtualCamera camera)
     {
         if (camera != orbitalCamera) return;

         CinemachineOrbitalTransposer newTransposer = camera.GetCinemachineComponent<CinemachineOrbitalTransposer>();
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
             orbitalTransposer.m_XAxis.Value += rotationSpeed * Time.deltaTime;
             yield return null;
         }
     }
     
#if UNITY_EDITOR
     [CustomEditor(typeof(OrbitCameraWithTime))]
     public class OrbitCameraEditor : Editor
     {
         private float prevMaxSpeed;

         public override void OnInspectorGUI()
         {
             base.OnInspectorGUI();

             OrbitCameraWithTime orbitCameraScript = (OrbitCameraWithTime)target;

             if (Application.isPlaying)
             {
                 // Check if the maxSpeed has changed
                 if (orbitCameraScript.orbitalTransposer != null &&
                     orbitCameraScript.orbitalTransposer.m_XAxis.m_MaxSpeed != prevMaxSpeed)
                 {
                     orbitCameraScript.rotationSpeed = orbitCameraScript.orbitalTransposer.m_XAxis.m_MaxSpeed;
                     prevMaxSpeed = orbitCameraScript.orbitalTransposer.m_XAxis.m_MaxSpeed;
                 }
             }
         }
     }
#endif
}
