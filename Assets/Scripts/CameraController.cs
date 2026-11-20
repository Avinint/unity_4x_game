using System;
using System.Collections;
using UnityEngine;

using UnityEngine.InputSystem;

public class CameraController : Singleton<CameraController>
{
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
    [SerializeField] private float distanceMax = 250f;          // plancher du zoom maximal
    [SerializeField] private bool fitMapOnMaxZoom = true;       // le zoom maximal montre toute la carte
    [SerializeField, Range(0f, 1f)] private float recenterFrom = 0.6f;   // à partir de cette part du zoom max, la vue se recentre
    [SerializeField] private float distanceDefault = 120f;
    [SerializeField] private float distanceZoomSpeed = 2.5f;
    [SerializeField, Range(0.01f, 0.5f)] private float scrollZoomStep = 0.15f;   // part de la distance par cran de molette

    [Header("Défilement par les bords")]
    [SerializeField] private bool edgeScroll = true;
    [SerializeField] private float edgeScrollMargin = 15f;   // pixels

    private float distance;
    private Unity.Cinemachine.CinemachineFollow overheadFollow;

    // Vitesse de défilement proportionnelle au zoom : cameraSpeed à la distance par défaut.
    private float PanSpeed => cameraSpeed * distance / distanceDefault;

    [SerializeField] private float dragThreshold = 6f;   // pixels : en deçà, c'est un clic (sélection)
    private bool leftPressed, dragging;
    private Vector2 pressScreen;
    private Vector3 grabPoint;

    private void Update()
    {
        if (isLocked || Mouse.current == null) return;
        if (leftPressed) DragMap();
        else EdgeScroll();
    }

    // Clic gauche glissé : la carte suit la souris, le point saisi reste sous le curseur.
    private void DragMap()
    {
        Vector2 mouse = Mouse.current.position.ReadValue();
        if (!dragging && (mouse - pressScreen).magnitude < dragThreshold) return;
        if (!dragging)
        {
            dragging = true;
            if (centerCoroutine != null) { StopCoroutine(centerCoroutine); centerCoroutine = null; }
        }
        if (!TryGroundPoint(mouse, out Vector3 current)) return;
        Vector3 delta = grabPoint - current;
        cameraTarget.transform.position += new Vector3(delta.x, 0f, delta.z);
        ClampTarget();
    }

    // Point du sol (plan de la grille) sous une position de l'écran.
    private static bool TryGroundPoint(Vector2 screen, out Vector3 point)
    {
        point = default;
        HexGrid grid = GameManager.Instance != null ? GameManager.Instance.HexGrid : null;
        if (Camera.main == null || grid == null) return false;
        Ray ray = Camera.main.ScreenPointToRay(screen);
        if (!new Plane(Vector3.up, grid.transform.position).Raycast(ray, out float enter)) return false;
        point = ray.GetPoint(enter);
        return true;
    }

    // Souris près d'un bord de la fenêtre : la vue glisse dans cette direction.
    private void EdgeScroll()
    {
        if (!edgeScroll || !Application.isFocused) return;
        Vector2 direction = CameraMath.EdgeScrollDirection(
            Mouse.current.position.ReadValue(), new Vector2(Screen.width, Screen.height), edgeScrollMargin);
        if (direction == Vector2.zero) return;
        cameraTarget.transform.position += new Vector3(direction.x, 0f, direction.y) * PanSpeed * Time.deltaTime;
        ClampTarget();
    }

    [SerializeField] private CameraMode defaultMode = CameraMode.Overhead;

    [SerializeField] private CameraMode currentMode;
    
    
    private Coroutine panCoroutine;
    private Coroutine zoomCoroutine;

    public event Action<Unity.Cinemachine.CinemachineCamera> onCameraChanged;

    public event Action onSelectAction;
    public event Action onCommandAction;
    public event Action onDeselectAction;

    [SerializeField] private bool isLocked = false;
    public bool IsLocked
    {
        get => isLocked;
        set => isLocked = value;
    }
    
    public GameObject CameraTarget { get => cameraTarget; }

    [SerializeField] private float centerDuration = 0.3f;   // durée du glissement vers l'unité sélectionnée
    private Coroutine centerCoroutine;

    // Fait glisser la vue jusqu'à `position`, sans changer le zoom.
    public void CenterOn(Vector3 position)
    {
        if (isLocked) return;
        if (centerCoroutine != null) StopCoroutine(centerCoroutine);
        Vector3 end = new Vector3(position.x, cameraTarget.transform.position.y, position.z);
        centerCoroutine = StartCoroutine(GlideTo(end));
    }

    private IEnumerator GlideTo(Vector3 end)
    {
        Vector3 start = cameraTarget.transform.position;
        for (float t = 0f; t < centerDuration; t += Time.deltaTime)
        {
            cameraTarget.transform.position = Vector3.Lerp(start, end, Mathf.SmoothStep(0f, 1f, t / centerDuration));
            ClampTarget();
            yield return null;
        }
        cameraTarget.transform.position = end;
        ClampTarget();
        centerCoroutine = null;
    }

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
        if (GameManager.Instance != null) GameManager.Instance.PlayerTurnStart += OnPlayerTurnStart;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.PlayerTurnStart -= OnPlayerTurnStart;
    }

    // Monde qui fait le tour : en défilant toujours dans le même sens, x grandit et la précision des float baisse.
    // Au début du tour du joueur local, tout est à l'arrêt (la fin de tour attend les mouvements) : la cible est
    // ramenée près du centre d'un nombre entier de largeurs ; les colonnes se replacent, la vue reste identique.
    private void OnPlayerTurnStart(Player player)
    {
        HexGrid grid = GameManager.Instance.HexGrid;
        if (player != GameManager.Instance.LocalPlayer || grid == null || !grid.Wraps) return;
        Vector3 p = cameraTarget.transform.position;
        float shift = HexMetrics.WrapShift(grid.WorldBounds.center.x, p.x, grid.WorldWidth);
        if (shift == 0f) return;
        if (centerCoroutine != null) { StopCoroutine(centerCoroutine); centerCoroutine = null; }
        cameraTarget.transform.position = p - Vector3.right * shift;
        grid.RefreshWrapShifts();
    }

    // Place la caméra à `distance` de la cible, côté sud ; l'inclinaison varie de pitchNear à pitchFar avec la distance.
    private void SetDistance(float value)
    {
        float max = MaxDistance;
        distance = Mathf.Clamp(value, distanceMin, max);
        overheadFollow.FollowOffset = CameraMath.TiltedOffset(distance, distanceMin, max, pitchNear, pitchFar);
        ClampTarget();   // en dézoomant, la zone autorisée se resserre vers le centre
    }

    // Distance maximale : au moins de quoi voir toute la carte (recalculée, l'écran peut changer de taille).
    private float MaxDistance
    {
        get
        {
            HexGrid grid = GameManager.Instance.HexGrid;
            // Monde qui fait le tour : chaque colonne n'a qu'une copie, la vue ne doit pas dépasser 90 % du monde.
            if (grid != null && grid.Wraps && Camera.main != null)
                return Mathf.Max(distanceMin,
                    CameraMath.FitDistance(new Vector2(grid.WorldWidth, 0f), fieldOfView, Camera.main.aspect, pitchFar, 0.9f));
            if (!fitMapOnMaxZoom || grid == null || Camera.main == null) return distanceMax;
            Vector2 size = grid.WorldBounds.size + Vector2.one * 2f * grid.HexSize;   // cases entières, pas seulement leurs centres
            return Mathf.Max(distanceMax, CameraMath.FitDistance(size, fieldOfView, Camera.main.aspect, pitchFar));
        }
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
            if (centerCoroutine != null) StopCoroutine(centerCoroutine);
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


    public void OnTriggerCommand(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            onCommandAction?.Invoke();
        }
    }

    // Clic gauche : bref = sélection au relâchement ; glissé = déplacement de la carte (DragMap), sans sélection.
    public void OnFocusChanged(InputAction.CallbackContext context)
    {
        if (context.started && Mouse.current != null)
        {
            pressScreen = Mouse.current.position.ReadValue();
            leftPressed = TryGroundPoint(pressScreen, out grabPoint);
            dragging = false;
        }
        else if (context.canceled)
        {
            bool wasDrag = dragging;
            leftPressed = dragging = false;
            if (!wasDrag) onSelectAction?.Invoke();
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
            cameraTarget.transform.position += moveVector * PanSpeed * Time.deltaTime;
            ClampTarget();

            yield return null;
        }
    }
    
    // Garde la cible de la caméra au-dessus de la carte.
    private void ClampTarget()
    {
        HexGrid grid = GameManager.Instance.HexGrid;
        if (grid == null) return;
        Rect bounds = CameraMath.ShrinkRect(grid.WorldBounds, CameraMath.PanFreedom(distance, MaxDistance, recenterFrom));
        Vector3 p = cameraTarget.transform.position;
        if (!grid.Wraps) p.x = Mathf.Clamp(p.x, bounds.xMin, bounds.xMax);   // monde qui fait le tour : est-ouest libre
        p.z = Mathf.Clamp(p.z, bounds.yMin, bounds.yMax);
        cameraTarget.transform.position = p;
    }

    public IEnumerator processZoom(InputAction.CallbackContext context)
    {
        // Touches A / E : zoom continu tant qu'on appuie.
        float zoomInput = context.ReadValue<float>();

        while (true)
        {
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



