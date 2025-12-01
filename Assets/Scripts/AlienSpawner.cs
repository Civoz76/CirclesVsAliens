using UnityEngine;

public class AlienSpawner : MonoBehaviour
{
    public Alien alienPrefab;
    public float spawnInterval = 1.5f;
    public float minX = -7f;
    public float maxX = 7f;
    public float spawnY = 6f;

    [Range(0f, 1f)]
    public float probabilityEmitterAlien = 0.2f;

    float timer;
    int alienCounter = 0;

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            timer = 0f;
            SpawnAlien();
        }
    }

    void SpawnAlien()
    {
        float x = Random.Range(minX, maxX);
        Vector3 pos = new Vector3(x, spawnY, 0f);

        Alien a = Instantiate(alienPrefab, pos, Quaternion.identity);
        a.speed += Random.Range(0.0f,  0.01f * alienCounter);

        bool isEmitter = Random.value < probabilityEmitterAlien;
        a.emitsWaveOnDeath = isEmitter;
        var sr = a.GetComponent<SpriteRenderer>();
        if (isEmitter && sr != null)
        {
            sr.color = Color.cyan;
        }

        alienCounter++;

    }
}
