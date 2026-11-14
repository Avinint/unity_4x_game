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
        Mouse mouse = Mouse.current;
        if (mouse == null) return;   // pas de souris branchée
        Ray ray = _mainCamera.ScreenPointToRay(mouse.position.ReadValue());

        if (mouse.delta.ReadValue() != Vector2.zero) CheckMouseMove(ray);
        if (mouse.leftButton.wasPressedThisFrame) CheckMouseClick(ray, 0);
        if (mouse.rightButton.wasPressedThisFrame) CheckMouseClick(ray, 1);
        if (mouse.middleButton.wasPressedThisFrame) CheckMouseClick(ray, 2);
    }

    void CheckMouseMove(Ray ray)
    {
        RaycastHit hitInfo;

        bool overUI = UnityEngine.EventSystems.EventSystem.current != null
                      && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();

        if (!overUI && TryRaycastGrid(ray, out hitInfo))
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

    // Premier collider portant le tag "Grid" sur le rayon : le terrain sous une unité reste atteignable.
    private static bool TryRaycastGrid(Ray ray, out RaycastHit gridHit)
    {
        gridHit = default;
        bool found = false;
        float best = float.MaxValue;

        foreach (RaycastHit hit in Physics.RaycastAll(ray, Mathf.Infinity))
        {
            if (hit.collider.CompareTag("Grid") && hit.distance < best)
            {
                best = hit.distance;
                gridHit = hit;
                found = true;
            }
        }
        return found;
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
