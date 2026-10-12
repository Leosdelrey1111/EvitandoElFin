using UnityEngine;
using System;
using System.Collections.Generic;

public class InventarioEspiritual : MonoBehaviour
{
    [Header("Economía Espiritual")]
    public int cantidadTotems = 0;
    public float energiaEspiritual = 0f;

    [Header("Diario de Memorias")]
    public List<string> recuerdosDesbloqueados = new List<string>();

    public event Action<int> OnCambioTotems;
    public event Action<float> OnCambioEnergia;
    public event Action<string> OnRecuerdoObtenido;

    public static InventarioEspiritual Instancia { get; private set; }

    void Awake()
    {
        if (Instancia != null && Instancia != this)
        {
            Destroy(gameObject);
            return;
        }
        Instancia = this;
    }

    public void AgregarTotems(int cantidad)
    {
        if (cantidad <= 0) return;
        cantidadTotems += cantidad;
        OnCambioTotems?.Invoke(cantidadTotems);
        Debug.Log($"[Inventario] +{cantidad} Tótems obtenidos. Total actual: {cantidadTotems}");
    }

    public bool GastarTotems(int cantidad)
    {
        if (cantidadTotems >= cantidad)
        {
            cantidadTotems -= cantidad;
            OnCambioTotems?.Invoke(cantidadTotems);
            Debug.Log($"[Inventario] -{cantidad} Tótems gastados con el comerciante. Restan: {cantidadTotems}");
            return true;
        }
        Debug.LogWarning("[Inventario] Tótems insuficientes para el canje.");
        return false;
    }

    public void AgregarEnergia(float cantidad)
    {
        energiaEspiritual = Mathf.Max(0, energiaEspiritual + cantidad);
        OnCambioEnergia?.Invoke(energiaEspiritual);
    }

    public void DesbloquearRecuerdo(string nombreRecuerdo)
    {
        if (!recuerdosDesbloqueados.Contains(nombreRecuerdo))
        {
            recuerdosDesbloqueados.Add(nombreRecuerdo);
            OnRecuerdoObtenido?.Invoke(nombreRecuerdo);
            Debug.Log($"[Diario de Memoria] ¡Fragmento recuperado!: {nombreRecuerdo}");
        }
    }
}
