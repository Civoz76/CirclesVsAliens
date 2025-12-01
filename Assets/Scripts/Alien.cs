using System.Collections;
using UnityEngine;

public class Alien : MonoBehaviour
{
    public float speed = 2f;
    public bool emitsWaveOnDeath = false;
    public float extraWaveSpeed = 5f;
    public float extraWaveMaxRadius = 15f;

    public AudioClip hitSound;      // suono da riprodurre quando muore
    public AudioSource audioSource; // sorgente audio optional

    public int scoreValue = 10;

    public bool IsDead { get; private set; }
    void Update()
    {
        transform.Translate(Vector3.down * speed * Time.deltaTime);

        if (transform.position.y < -5f)
        {
            // l'alieno è "passato"
            if (WaveManager.Instance != null)
            {
                WaveManager.Instance.AlienPassedBottom();
            }
            Destroy(gameObject);
        }
    }


    public void Kill(int audioDelay)
    {
        if (IsDead) return;      // 👈 blocca hit doppi
        IsDead = true;

        StartCoroutine(KillRoutine(audioDelay));
    }

    private IEnumerator KillRoutine(int audioDelay)
    {
        yield return new WaitForSeconds(audioDelay*0.1f);
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.AddScore(scoreValue);
        }

        // Suono critico
        float clipLength = 0f;


        if (hitSound != null)
        {
            if (audioSource != null)
            {
                audioSource.PlayOneShot(hitSound);
                clipLength = hitSound.length;
            }
            else
            {
                // se non hai un AudioSource, creane uno temporaneo
                AudioSource.PlayClipAtPoint(hitSound, transform.position, 1f);
                clipLength = hitSound.length;
            }
        }

        // Onda extra (subito, prima della distruzione)
        if (emitsWaveOnDeath && WaveManager.Instance != null)
        {
            var wave = WaveManager.Instance.SpawnWave(transform.position, WaveOrigin.Enemy);
            wave.speed = extraWaveSpeed;
            wave.maxRadius = extraWaveMaxRadius;
        }

        // Disattiva visual e collisioni subito (così non dà fastidio mentre il suono suona)
        var rend = GetComponent<SpriteRenderer>();
        if (rend != null) rend.enabled = false;

        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // Aspetta che il suono finisca
        if (clipLength > 0f)
            yield return new WaitForSeconds(clipLength);
        else
            yield return null;

        Destroy(gameObject);
    }




}