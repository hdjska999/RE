using System;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Helpers para crear y modificar Light2D (URP) por reflexión, sin depender del ensamblado de URP al compilar.
/// Los usan AuraPickup, LuzParpadeante y las herramientas de Editor (farolas de Raccoon City).
/// </summary>
public static class Luz2DUtil
{
    private static Type tipoLuz;

    public static Type TipoLight2D
    {
        get
        {
            if (tipoLuz == null)
                tipoLuz = Type.GetType("UnityEngine.Rendering.Universal.Light2D, Unity.RenderPipelines.Universal.Runtime");
            return tipoLuz;
        }
    }

    /// <summary>Añade (o reutiliza) una luz puntual 2D en el objeto.</summary>
    public static Component CrearPuntual(GameObject go, Color color, float intensidad, float radioExterior, float radioInterior = -1f)
    {
        if (TipoLight2D == null || go == null) return null;
        Component luz = go.GetComponent(TipoLight2D);
        if (luz == null) luz = go.AddComponent(TipoLight2D);
        Establecer(luz, "lightType", "Point");
        Establecer(luz, "color", color);
        Establecer(luz, "intensity", intensidad);
        Establecer(luz, "pointLightOuterRadius", radioExterior);
        Establecer(luz, "pointLightInnerRadius", radioInterior >= 0f ? radioInterior : radioExterior * 0.2f);
        return luz;
    }

    public static void Establecer(Component luz, string propiedad, object valor)
    {
        if (luz == null) return;
        try
        {
            PropertyInfo p = luz.GetType().GetProperty(propiedad, BindingFlags.Public | BindingFlags.Instance);
            if (p == null || !p.CanWrite) return;
            if (p.PropertyType.IsEnum && valor is string s) valor = Enum.Parse(p.PropertyType, s);
            p.SetValue(luz, valor);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[Luz2DUtil] No se pudo asignar " + propiedad + ": " + e.Message);
        }
    }
}
