using UnityEngine;

public enum WaveOrigin
{
    LeftAntenna,
    RightAntenna,
    Enemy
}

public class Wave : MonoBehaviour
{
    [Header("Parametri onda")]
    public WaveOrigin origin;
    public float speed = 5f;          // velocità con cui cresce il raggio
    public float maxRadius = 20f;     // per evitare che viva all’infinito
    public float fadeOutTime = 0.5f;  // tempo di dissolvenza dopo l’esplosione

    // raggio attuale in unità mondo
    public float Radius { get; private set; }

    public bool IsConsumed { get; private set; }

    // per scalare lo sprite in funzione del raggio
    [SerializeField] private float spriteBaseRadius = 0.5f; // raggio "nativo" del tuo sprite

    SpriteRenderer sr;
    float alpha = 1f;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void OnEnable()
    {
        Radius = 0f;
        IsConsumed = false;
        alpha = 1f;
        if (sr != null)
        {
            var c = sr.color;
            c.a = 1f;
            sr.color = c;
        }
        UpdateVisual();
    }

    void Update()
    {
        if (!IsConsumed)
        {
            Radius += speed * Time.deltaTime;
            if (Radius >= maxRadius)
            {
                Consume(false); // scompare da solo
            }
            else
            {
                UpdateVisual();
            }
        }
        else
        {
            // fase di dissolvenza dopo che è stata "usata"
            alpha -= Time.deltaTime / fadeOutTime;
            if (sr != null)
            {
                var c = sr.color;
                c.a = Mathf.Clamp01(alpha);
                sr.color = c;
            }

            if (alpha <= 0f)
            {
                Destroy(gameObject);
            }
        }
    }

    void UpdateVisual()
    {
        float scale = (Radius / spriteBaseRadius) * 2f;  // diametro
        transform.localScale = new Vector3(scale, scale, 1f);
    }

    public void Consume(bool triggeredByExplosion)
    {
        if (IsConsumed) return;
        IsConsumed = true;

        // puoi cambiare colore quando viene "consumata"
        if (sr != null && triggeredByExplosion)
        {
            var c = sr.color;
            c = Color.white;
            c.a = alpha;
            sr.color = c;
        }
    }
}
