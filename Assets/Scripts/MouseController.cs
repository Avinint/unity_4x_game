using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class MouseController : Singleton<MouseController>
{
    public Action<RaycastHit> OnLeftMouseClick;
    public Action<RaycastHit> OnRightMouseClick;
    public Action<RaycastHit> OnMiddleMouseClick;
    public Action<RaycastHit> OnMouseMoving;
    // Déclenché une fois quand la souris quitte la grille (autre objet, vide ou UI).
    public Action onMouseExit;

    private Camera _mainCamera;
    private bool _isOverGrid;
    
    // Start is called before the first frame update
    void Start()
    {
        _mainCamera = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
        
        if (Input.GetAxis("Mouse X") != 0 || Input.GetAxis("Mouse Y") != 0)
        {
            CheckMouseMove(ray);
        }
        
        if (Input.GetMouseButtonDown(0))
        {
            CheckMouseClick(ray, 0);
        }
        if (Input.GetMouseButtonDown(1))
        {
            CheckMouseClick(ray, 1);
        }
        if (Input.GetMouseButtonDown(2))
        {
            CheckMouseClick(ray, 2);
        }
    }

    void CheckMouseMove(Ray ray)
    {
        RaycastHit hitInfo;
        // Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        // RaycastHit hit;

        bool overUI = UnityEngine.EventSystems.EventSystem.current != null
                      && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();

        if (!overUI && Physics.Raycast(ray, out hitInfo, Mathf.Infinity) && hitInfo.collider.CompareTag("Grid"))
        {
            _isOverGrid = true;
            OnMouseMoving?.Invoke(hitInfo);
        }
        else if (_isOverGrid)
        {
            _isOverGrid = false;
            onMouseExit?.Invoke();
        }
    }

    void CheckMouseClick(Ray ray, int mouseButton)
    {
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity))
        {
            if (mouseButton == 0)
                OnLeftMouseClick?.Invoke(hit);
            else if (mouseButton == 1)
                OnRightMouseClick?.Invoke(hit);
            else if (mouseButton == 2)
                OnMiddleMouseClick?.Invoke(hit);
        }
    }
}
