using System.Collections;
using UnityEngine;

public class Explosion : MonoBehaviour
{
    public float radius = 1.5f;
    public float gfxradius = 1.5f;
    public float lifeTime = 0.3f;
   
    void Start()
    {
        transform.localScale = Vector3.one * radius * 2f;
        transform.localScale = Vector3.one * gfxradius * 2f;
        Destroy(gameObject, lifeTime);
        DealDamage();
    }

    void DealDamage()
    {
        int killed = 0;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius);
        foreach (var hit in hits)
        {
            Alien a = hit.GetComponent<Alien>();
            if (a != null)
            {
                a.Kill(killed);
                killed++;
            }
        }

        // camera shake
        if (killed > 0 && CameraShake.Instance != null)
        {
            // leggero, aumenta solo se tanti
            float mult = (killed >= 5) ? 1.5f : 1f;
            CameraShake.Instance.Shake(mult);

            if (killed >= 3)
            {
                WaveManager.Instance.Flash();
            }
        }
    }

    //mostra raggio esplosione
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
