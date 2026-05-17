//using System.Collections;
//using UnityEngine;
//using static StepOutlineManager;

//[RequireComponent(typeof(VisualProxy))]
//public class ProxyOutline : MonoBehaviour
//{
//    [SerializeField] private Color outlineColor = Color.yellow;
//    [SerializeField] private Outline.Mode outlineMode = Outline.Mode.OutlineAll;

//    private VisualProxy _proxy;
//    private Outline _currentOutline;
//    private GameObject _currentProxyGO; // traccia su QUALE proxy è montato l'outline

//    private float _minWidth;
//    private float _maxWidth;
//    private float _halfCycleDuration;
//    private PulseEasing _easingType;
//    private bool _isActive;

//    private void Awake()
//    {
//        _proxy = GetComponent<VisualProxy>();
//        enabled = false;
//    }

//    private void OnEnable()
//    {
//        _proxy.OnProxyChanged += OnProxyChanged;
//    }

//    private void OnDisable()
//    {
//        _proxy.OnProxyChanged -= OnProxyChanged;
//        RemoveOutline();
//    }

//    // ── API StepOutlineManager ────────────────────────────────────────────────

//    public void Activate(float minWidth, float maxWidth, float halfCycleDuration, PulseEasing easingType)
//    {
//        _minWidth = minWidth;
//        _maxWidth = maxWidth;
//        _halfCycleDuration = halfCycleDuration;
//        _easingType = easingType;
//        _isActive = true;
//        enabled = true;

//        if (_proxy.activeproxy != null)
//            ApplyOutline(_proxy.activeproxy);
//    }

//    public void Deactivate()
//    {
//        _isActive = false;
//        RemoveOutline();
//        enabled = false;
//    }

//    public void SetOutlineWidth(float width)
//    {
//        if (_currentOutline != null)
//            _currentOutline.OutlineWidth = width;
//    }

//    // ── Cambio proxy ──────────────────────────────────────────────────────────

//    private void OnProxyChanged()
//    {
//        // Rimuove sempre l'outline dal vecchio proxy, indipendentemente dal nuovo
//        RemoveOutline();

//        if (_isActive && _proxy.activeproxy != null)
//            ApplyOutline(_proxy.activeproxy);
//    }

//    // ── Outline ───────────────────────────────────────────────────────────────

//    private void ApplyOutline(GameObject target)
//    {
//        // Sicurezza: se per qualche motivo c'è ancora un outline residuo, lo puliamo
//        if (_currentOutline != null)
//            RemoveOutline();

//        _currentProxyGO = target;

//        // Outline ha [DisallowMultipleComponent]: se esiste già non ne aggiungiamo un altro
//        _currentOutline = target.GetComponent<Outline>();
//        bool wasPreexisting = _currentOutline != null;

//        if (!wasPreexisting)
//            _currentOutline = target.AddComponent<Outline>();

//        _currentOutline.OutlineMode = outlineMode;
//        _currentOutline.OutlineColor = outlineColor;
//        _currentOutline.OutlineWidth = _minWidth;
//        _currentOutline.enabled = true;
//    }

//    private void RemoveOutline()
//    {
//        if (_currentOutline != null)
//        {
//            // Usa DestroyImmediate per garantire rimozione istantanea nello stesso frame
//            // così il prossimo ApplyOutline non trova residui
//            if (Application.isPlaying)
//                DestroyImmediate(_currentOutline);
//            else
//                DestroyImmediate(_currentOutline);

//            _currentOutline = null;
//        }

//        _currentProxyGO = null;
//    }
//}