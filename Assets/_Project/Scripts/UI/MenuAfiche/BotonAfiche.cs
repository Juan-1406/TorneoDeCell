using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class BotonAfiche : Button, ICancelHandler
{
    [SerializeField] private RecorteAfiche imagen;
    [SerializeField] private LuzAfiche resplandor;
    [SerializeField] private MenuAfiche menu;
    private Material instancia;
    private bool seleccionado, sostenido;
    private float mezcla, pulsadoHasta, entrada = 1;
    private Vector2 posicion;
    public bool Seleccionado => seleccionado;
    public float IntensidadSeleccion => mezcla;

    public void Configurar(RecorteAfiche grafica, LuzAfiche luz, Material material, MenuAfiche propietario)
    {
        imagen = grafica; resplandor = luz; menu = propietario;
        imagen.material = material; targetGraphic = imagen;
        transition = Transition.None;
    }

    protected override void Awake()
    {
        base.Awake();
        if (!Application.isPlaying) return;
        if (imagen != null)
        {
            instancia = new Material(imagen.material);
            imagen.material = instancia;
        }
        posicion = ((RectTransform)transform).anchoredPosition;
    }

    private void Update()
    {
        if (!Application.isPlaying) return;
        float dt = Time.unscaledDeltaTime;
        mezcla = Mathf.MoveTowards(mezcla, seleccionado ? 1 : 0, dt * 8f);
        bool pulsado = sostenido || Time.unscaledTime < pulsadoHasta;
        float escala = Mathf.Lerp(.97f, 1f, entrada) * (1 + mezcla * .018f) * (pulsado ? .975f : 1);
        transform.localScale = Vector3.one * escala;
        ((RectTransform)transform).anchoredPosition = posicion + new Vector2(0, (1 - entrada) * -10 + (pulsado ? -3 : 0));
        if (instancia != null)
        {
            instancia.SetFloat("_Seleccion", mezcla);
            instancia.SetFloat("_Pulsado", pulsado ? 1 : 0);
            instancia.SetFloat("_Brillo", .5f + .5f * Mathf.Sin(Time.unscaledTime * 2));
        }
        if (resplandor != null)
            resplandor.color = new Color(1, .63f, .16f, mezcla * (.18f + .025f * Mathf.Sin(Time.unscaledTime * 2)));
    }

    public void Entrada(float valor) => entrada = valor;
    public void ConfirmarVisual() => pulsadoHasta = Time.unscaledTime + .16f;
    public override void OnSelect(BaseEventData eventData)
    {
        base.OnSelect(eventData); seleccionado = true;
        if (menu != null) menu.SonarSeleccion();
    }
    public override void OnDeselect(BaseEventData eventData)
    {
        base.OnDeselect(eventData); seleccionado = false; sostenido = false;
    }
    public override void OnPointerEnter(PointerEventData eventData)
    {
        base.OnPointerEnter(eventData);
        if (IsActive() && IsInteractable() && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(gameObject, eventData);
    }
    public override void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);
        if (eventData.button == PointerEventData.InputButton.Left && IsInteractable()) sostenido = true;
    }
    public override void OnPointerUp(PointerEventData eventData)
    {
        base.OnPointerUp(eventData); sostenido = false;
        if (eventData.button == PointerEventData.InputButton.Left) ConfirmarVisual();
    }
    public override void OnSubmit(BaseEventData eventData)
    {
        if (!IsActive() || !IsInteractable()) return;
        ConfirmarVisual(); onClick.Invoke();
    }
    public void OnCancel(BaseEventData eventData)
    {
        if (menu != null) menu.SeleccionarSalir();
    }
    protected override void OnDisable() { sostenido = false; base.OnDisable(); }
    protected override void OnDestroy()
    {
        if (instancia != null)
        {
            if (Application.isPlaying) Destroy(instancia);
            else DestroyImmediate(instancia);
        }
        base.OnDestroy();
    }
}
