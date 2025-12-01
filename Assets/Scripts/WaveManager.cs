using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class WaveManager : MonoBehaviour
{
    [Header("Game")]
    public int startLives = 3;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI finalScoreText;
    public TextMeshProUGUI highScoreText;
    public TextMeshProUGUI livesText;
    public GameObject gameOverPanel;
    public GameObject gameStartPanel;
    public GameObject newRecord;

    int score = 0;
    int hiScore = 0;
    int lives = 0;
    bool isGameOver = false;
    public static WaveManager Instance { get; private set; }

    [Header("Riferimenti antenna")]
    public Transform leftAntenna;
    public Transform rightAntenna;

    [Header("Prefabs")]
    public Wave wavePrefab;
    public Explosion explosionPrefab;

    [Header("Audio")]
    public AudioSource sfxOneShotSource;        // per suoni brevi (emit, explosion)
    public AudioSource intersectionLoopSource;  // loop basso quando ci sono intersezioni
    public AudioClip waveEmitClip;
    public AudioClip explosionClip;
    public AudioClip lifeLostClip;
    public AudioClip gameOverClip;

    [Header("Explosions")]
    public float explosionRadius = 1.5f;

    private List<Wave> activeWaves = new List<Wave>();

    public GameObject debugDotPrefab, alienSpawner;

    public Image flashCanvas;

    void Awake()
    {
        hiScore = PlayerPrefs.GetInt("HighScore", 0);
        if (highScoreText != null)
            highScoreText.text = $"Record: {hiScore}";


        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;

        lives = startLives;
        UpdateUI();
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    void Update()
    {
        if (isGameOver)
        {
            HandleIntersectionLoopSound();  // opzionale, puoi anche spegnerlo
            return;
        }

        HandleInput();
        HandleIntersectionLoopSound();
    }

    public void AddScore(int amount)
    {
        score += amount;
        UpdateUI();
    }

    public void AlienPassedBottom()
    {
        if (isGameOver) return;

        lives--;
        UpdateUI();

        PlayOneShot(lifeLostClip);
        WaveManager.Instance.RedFlash();


        if (lives <= 0)
        {
            TriggerGameOver();
        }
    }

    void TriggerGameOver()
    {
        isGameOver = true;

        // ferma il loop di intersezione se vuoi
        if (intersectionLoopSource != null && intersectionLoopSource.isPlaying)
            intersectionLoopSource.Stop();

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        PlayOneShot(gameOverClip);

        if(score>hiScore)
        {
            hiScore = score;
            PlayerPrefs.SetInt("HighScore", hiScore);
            PlayerPrefs.Save();

            if (highScoreText != null)
                highScoreText.text = $"Record: {hiScore}";

            newRecord.SetActive(true);
        }

    }

    void UpdateUI()
    {
        if (scoreText != null)
            scoreText.text = $"Score: {score}";
        if (finalScoreText != null)
            finalScoreText.text = $"Score: {score}";

        if (livesText != null)
            livesText.text = $"Lives: {lives}";
    }


    void HandleInput()
    {

        // sinistra
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if(gameStartPanel.activeSelf==true)
                StartGame();
            if (gameOverPanel.activeSelf == true)
                Restart();
        }

        // sinistra
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            SpawnWave(leftAntenna.position, WaveOrigin.LeftAntenna);
        }

        // destra
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            SpawnWave(rightAntenna.position, WaveOrigin.RightAntenna);
        }

        // giù → esplodi tutte le intersezioni
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            TriggerExplosionsOnIntersections();
        }
    }

    public Wave SpawnWave(Vector3 position, WaveOrigin origin)
    {
        // Limite: max 2 onde attive per antenna (Left/Right)
        if (origin == WaveOrigin.LeftAntenna || origin == WaveOrigin.RightAntenna)
        {
            int count = 0;
            for (int i = 0; i < activeWaves.Count; i++)
            {
                var w = activeWaves[i];
                if (w == null || w.IsConsumed) continue;

                if (w.origin == origin)
                    count++;
            }

            if (count >= 2)
            {
                // abbiamo già 2 onde per questa antenna → non ne creiamo altre
                return null;
            }
        }

        // per le onde dei nemici (WaveOrigin.Enemy) nessun limite
        Wave wave = Instantiate(wavePrefab, position, Quaternion.identity);
        wave.origin = origin;
        wave.GetComponent<SpriteRenderer>().color = Color.green;
        activeWaves.Add(wave);

        PlayOneShot(waveEmitClip);
        return wave;
    }

    void TriggerExplosionsOnIntersections()
    {
        List<Vector2> explosionPoints = new List<Vector2>();
        HashSet<Wave> consumed = new HashSet<Wave>();

        for (int i = 0; i < activeWaves.Count; i++)
        {
            var wi = activeWaves[i];
            if (wi == null || wi.IsConsumed) continue;

            for (int j = i + 1; j < activeWaves.Count; j++)
            {
                var wj = activeWaves[j];
                if (wj == null || wj.IsConsumed) continue;

                if (TryGetIntersections(wi, wj, out Vector2 p1, out Vector2 p2, out int count))
                {
                    if (count >= 1)
                        explosionPoints.Add(p1);
                    if (count == 2)
                        explosionPoints.Add(p2);

                    consumed.Add(wi);
                    consumed.Add(wj);
                }
            }
        }

        // genera esplosioni in tutti i punti
        foreach (var p in explosionPoints)
        {
            SpawnExplosion(p);
        }

        // consuma le onde usate
        foreach (var w in consumed)
        {
            w.Consume(true);
        }

        activeWaves.RemoveAll(w => w == null || w.IsConsumed);

        if (explosionPoints.Count > 0)
        {
            PlayOneShot(explosionClip);
        }
    }

    void SpawnExplosion(Vector2 position)
    {
        Explosion e = Instantiate(explosionPrefab, position, Quaternion.identity);
        e.radius = explosionRadius;
    }

    bool TryGetIntersections(Wave w1, Wave w2, out Vector2 i1, out Vector2 i2, out int count)
    {
        Vector2 c1 = w1.transform.position;
        Vector2 c2 = w2.transform.position;
        float r1 = w1.Radius;
        float r2 = w2.Radius;

        i1 = Vector2.zero;
        i2 = Vector2.zero;
        count = 0;

        float d = Vector2.Distance(c1, c2);
        if (d <= Mathf.Epsilon)
            return false;

        // nessuna intersezione
        if (d > r1 + r2) return false;
        if (d < Mathf.Abs(r1 - r2)) return false;

        // formula standard
        float a = (r1 * r1 - r2 * r2 + d * d) / (2f * d);
        float hSq = r1 * r1 - a * a;
        if (hSq < 0f) hSq = 0f;
        float h = Mathf.Sqrt(hSq);

        Vector2 P2 = c1 + a * (c2 - c1) / d;

        Vector2 offset = new Vector2(
            -(c2.y - c1.y) * (h / d),
             (c2.x - c1.x) * (h / d)
        );

        Vector2 p1 = P2 + offset;
        Vector2 p2 = P2 - offset;

        // tangenti? (un solo punto)
        if (Mathf.Approximately(h, 0f))
        {
            i1 = p1;
            count = 1;
        }
        else
        {
            i1 = p1;
            i2 = p2;
            count = 2;
        }

        return true;
    }


    void HandleIntersectionLoopSound()
    {
        bool hasIntersection = false;

        // cerchiamo almeno UNA coppia di onde che abbia uno o due punti di intersezione
        for (int i = 0; i < activeWaves.Count && !hasIntersection; i++)
        {
            var wi = activeWaves[i];
            if (wi == null || wi.IsConsumed) continue;

            for (int j = i + 1; j < activeWaves.Count; j++)
            {
                var wj = activeWaves[j];
                if (wj == null || wj.IsConsumed) continue;

                if (TryGetIntersections(wi, wj, out _, out _, out int count) && count > 0)
                {
                    hasIntersection = true;
                    break; // usciamo dal secondo for
                }
            }
        }

        // gestione del suono di "hum" di intersezione
        if (hasIntersection && intersectionLoopSource != null && !intersectionLoopSource.isPlaying)
        {
            intersectionLoopSource.Play();
        }
        else if (!hasIntersection && intersectionLoopSource != null && intersectionLoopSource.isPlaying)
        {
            intersectionLoopSource.Stop();
        }
    }

    public void StartGame()
    {
        gameStartPanel.SetActive(false);
        alienSpawner.SetActive(true);
    }
    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    void PlayOneShot(AudioClip clip)
    {
        if (clip == null || sfxOneShotSource == null) return;
        sfxOneShotSource.PlayOneShot(clip);
    }

    public void Flash()
    {
        StartCoroutine(FlashScreen(Color.white));
    }
    public void RedFlash()
    {
        StartCoroutine(FlashScreen(Color.red));
    }
    IEnumerator FlashScreen(Color c)
    {
        // flash veloce
        flashCanvas.color = new Color(c.r,c.g,c.b,0.6f); // picco
        yield return new WaitForSeconds(0.05f);

        // fade out
        float t = 0.15f;
        while (t > 0)
        {
            t -= Time.deltaTime;
            flashCanvas.color = new Color(c.r, c.g, c.b, Mathf.Lerp(0, 0.6f, t / 0.15f)); // picco

            yield return null;
        }

        flashCanvas.color = new Color(c.r, c.g, c.b, 0);
    }
}
