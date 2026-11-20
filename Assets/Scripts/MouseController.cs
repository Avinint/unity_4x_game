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
    private Vector3 _lastCameraPosition;
    private Quaternion _lastCameraRotation;
    
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

        // La case sous la souris change aussi quand la vue bouge (défilement, zoom), souris immobile.
        Transform cam = _mainCamera.transform;
        bool cameraMoved = cam.position != _lastCameraPosition || cam.rotation != _lastCameraRotation;
        _lastCameraPosition = cam.position;
        _lastCameraRotation = cam.rotation;
        if (mouse.delta.ReadValue() != Vector2.zero || cameraMoved) CheckMouseMove(ray);
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

    // Case visée : si le rayon touche d'abord le modèle d'une unité visible, sa case (la caméra est inclinée :
    // le sol derrière le modèle est la case voisine) ; sinon le collider « Grid » le plus proche ;
    // sinon (case inexplorée d'une copie décalée, hors du maillage plat de Grid) : point du rayon sur le plan du sol.
    private static bool TryRaycastGrid(Ray ray, out RaycastHit gridHit)
    {
        gridHit = default;
        bool found = false;
        float best = float.MaxValue;
        float unitDistance = float.MaxValue;
        Unit unit = null;

        foreach (RaycastHit hit in Physics.RaycastAll(ray, Mathf.Infinity))
        {
            var u = hit.collider.GetComponentInParent<Unit>();
            if (u != null && u.IsVisible && u.Tile != null && hit.distance < unitDistance)
            {
                unit = u;
                unitDistance = hit.distance;
            }
            if (hit.collider.CompareTag("Grid") && hit.distance < best)
            {
                best = hit.distance;
                gridHit = hit;
                found = true;
            }
        }
        if (unit != null && unitDistance < best)
        {
            gridHit.point = unit.Tile.CentrePosition;   // HexGrid retrouve la case à partir du point
            return true;
        }
        if (found) return true;
        var grid = GameManager.Instance != null ? GameManager.Instance.HexGrid : null;
        if (grid == null) return false;
        var ground = new Plane(Vector3.up, grid.transform.position);
        if (!ground.Raycast(ray, out float enter)) return false;
        gridHit.point = ray.GetPoint(enter);
        return true;
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
