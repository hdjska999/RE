using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class IntroManager : MonoBehaviour
{
    [Header("Referencias")]
    public TextMeshProUGUI introText;
    
    [Header("Configuración")]
    public string nombreEscenaJuego = "NombreDeTuEscena"; 
    public float typeSpeed = 0.4f; // Velocidad a la que aparece cada letra (menor = más rápido)
    public float readTime = 3.5f;   // Tiempo que se queda la frase al terminar de escribirse
    public float fadeSpeed = 3.5f;    // Velocidad del desvanecimiento al final de la frase

    private string[] lineasHistoria = {
        "Nunca me han gustado los turnos de noche. La ciudad se siente muy diferente a estas horas.",
        "PAUSA2",
        "Demasiado silenciosa.",
        "PAUSA2",
        "El trabajo era sencillo. Esta noche solo tenía una entrega.",
        "PAUSA2",
        "Un contenedor médico.",
        "Me dijeron que no lo abriera.",
        "No sabía qué llevaba dentro.",
        "Tampoco me importaba demasiado.",
        "PAUSA2",
        "Y pensar que esta sería mi última entrega.",
        "Supongo que debí haber hecho más preguntas."
    };

    void Start()
    {
        // Nos aseguramos de que el texto sea completamente opaco, pero ocultamos todas las letras
        introText.color = new Color(1f, 1f, 1f, 1f);
        introText.maxVisibleCharacters = 0;
        StartCoroutine(ReproducirIntro());
    }

    IEnumerator ReproducirIntro()
    {
        yield return new WaitForSeconds(1f);

        foreach (string linea in lineasHistoria)
        {
            if (linea == "PAUSA2")
            {
                yield return new WaitForSeconds(1f);
                continue; 
            }

            // 1. Preparamos el texto restaurando la opacidad y ocultando las letras
            introText.color = new Color(1f, 1f, 1f, 1f);
            introText.text = linea;
            introText.maxVisibleCharacters = 0;

            // 2. Efecto de Máquina de Escribir (revela letra por letra)
            for (int i = 0; i <= linea.Length; i++)
            {
                introText.maxVisibleCharacters = i;
                yield return new WaitForSeconds(typeSpeed);
            }

            // 3. Pausa para que el jugador lea la frase completa
            yield return new WaitForSeconds(readTime);

            // 4. Fade Out (La frase desaparece suavemente como antes)
            yield return StartCoroutine(FadeTexto(0f));
            yield return new WaitForSeconds(0.5f);
        }

        yield return new WaitForSeconds(1f);
        
        // Cambio de escena
        SceneManager.LoadScene(nombreEscenaJuego); 
    }

    IEnumerator FadeTexto(float alphaObjetivo)
    {
        Color c = introText.color;
        while (Mathf.Abs(c.a - alphaObjetivo) > 0.01f)
        {
            c.a = Mathf.Lerp(c.a, alphaObjetivo, Time.deltaTime * fadeSpeed);
            introText.color = c;
            yield return null;
        }
        c.a = alphaObjetivo;
        introText.color = c;
    }
}