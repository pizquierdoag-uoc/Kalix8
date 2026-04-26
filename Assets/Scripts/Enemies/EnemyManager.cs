using System.Collections;
using UnityEngine;
using TMPro;

public class EnemyManager : MonoBehaviour
{
    [Header("Prefabs de enemigos")]
    public GameObject prefabLinear;
    public GameObject prefabShooter;
    public GameObject prefabSine;
    public GameObject prefabKamikaze;
    public GameObject prefabBoss;

    [Header("Spawn")]
    public float spawnX    =  12f;
    public float spawnMinY = -3.5f;
    public float spawnMaxY =  3.5f;

    [Header("Power-ups (arrastra los prefabs PU_* aquí)")]
    public GameObject[] powerUpPrefabs;

    [Tooltip("Posición fija donde aparecen los power-ups (centro de pantalla recomendado)")]
    public Vector3 powerUpSpawnPos = new Vector3(4f, 0f, 0f);

    [Tooltip("Separación vertical entre power-ups de una misma oleada")]
    public float powerUpSpacing = 1.4f;

    [Header("Texto de aviso de fase (opcional)")]
    public TextMeshProUGUI txtPhaseWarning;

    bool _bossSpawned;

    // Escala un intervalo de tiempo según la dificultad actual
    float S(float t) => t * GameSettings.SpawnIntervalMult;

    void Start()
    {
        if (txtPhaseWarning != null) txtPhaseWarning.text = "";
        StartCoroutine(StageSequence());
    }

    IEnumerator StageSequence()
    {
        // Pausa inicial — el jugador se orienta (3s)
        yield return new WaitForSeconds(3f);

        yield return ShowWarning("FASE 1", 2f);
        yield return Phase1_NormalEnemies();

        yield return SpawnPowerUpWave(3);
        yield return new WaitForSeconds(2f);

        yield return ShowWarning("¡ALERTA! KAMIKAZES", 2f);
        yield return PhaseKamikazes(total: 50, groupSize: 5,
                                    intraDelay: 0.4f,
                                    pauseFirst: S(4.5f), pauseSecond: S(2.5f),
                                    halfAt: 25);

        yield return ShowWarning("FASE 3", 2f);
        yield return Phase3_NormalEnemiesAggressive();

        yield return SpawnPowerUpWave(2);
        yield return new WaitForSeconds(2f);

        yield return ShowWarning("¡SEGUNDA OLEADA!", 2f);
        yield return PhaseKamikazes(total: 30, groupSize: 8,
                                    intraDelay: 0.25f,
                                    pauseFirst: S(2f), pauseSecond: S(2f),
                                    halfAt: 99); // misma pausa siempre

        yield return ShowWarning("⚠ BOSS INCOMING ⚠", 3f);
        yield return Phase5_Boss();
    }

    IEnumerator Phase1_NormalEnemies()
    {
        // Bloque A — Solo Lineales
        yield return SpawnRepeating(prefabLinear, count: 8, interval: S(2.5f));

        // Bloque B — Lineales + Sinusoidales alternados
        yield return SpawnAlternating(prefabLinear, prefabSine, count: 10, interval: S(2.5f));

        // Bloque C — Mix completo: Linear / Sine / Shooter
        yield return SpawnMixedNormal(rounds: 8, interval: S(4.5f), includeShooter: true);
    }

    IEnumerator Phase3_NormalEnemiesAggressive()
    {
        // Bloque A — Grupo rápido de Lineales
        yield return SpawnGroup(prefabLinear, count: 6, intraDelay: 0.5f);
        yield return new WaitForSeconds(S(1.5f));

        // Bloque B — Grupo de Sinusoidales
        yield return SpawnGroup(prefabSine, count: 5, intraDelay: 0.6f);
        yield return new WaitForSeconds(S(1.5f));

        // Bloque C — Grupo de Shooters
        yield return SpawnGroup(prefabShooter, count: 4, intraDelay: 0.8f);
        yield return new WaitForSeconds(S(1.5f));

        // Bloque D — Mix agresivo, intervalos reducidos
        yield return SpawnMixedNormal(rounds: 10, interval: S(4f), includeShooter: true);
    }

    //   total       — cuántos kamikazes en total
    //   groupSize   — tamaño de cada grupo
    //   intraDelay  — delay entre kamikazes dentro del grupo
    //   pauseFirst  — pausa entre grupos en la primera mitad
    //   pauseSecond — pausa entre grupos en la segunda mitad
    //   halfAt      — a partir de cuántos spawneados se considera "segunda mitad"
    IEnumerator PhaseKamikazes(int total, int groupSize, float intraDelay,
                                float pauseFirst, float pauseSecond, int halfAt)
    {
        int spawned = 0;
        while (spawned < total)
        {
            int toSpawn = Mathf.Min(groupSize, total - spawned);
            yield return SpawnGroup(prefabKamikaze, toSpawn, intraDelay);
            spawned += toSpawn;

            float pause = spawned < halfAt ? pauseFirst : pauseSecond;
            yield return new WaitForSeconds(pause);
        }
    }

    IEnumerator Phase5_Boss()
    {
        if (prefabBoss == null || _bossSpawned) yield break;
        _bossSpawned = true;
        Instantiate(prefabBoss, new Vector3(spawnX, 0f, 0f), Quaternion.identity);
        Debug.Log("★ BOSS APARECE ★");
    }


    // N enemigos del mismo tipo con delay entre ellos
    IEnumerator SpawnRepeating(GameObject prefab, int count, float interval)
    {
        for (int i = 0; i < count; i++)
        {
            SpawnEnemy(prefab);
            yield return new WaitForSeconds(interval);
        }
    }

    // Alternando entre dos tipos
    IEnumerator SpawnAlternating(GameObject prefabA, GameObject prefabB,
                                  int count, float interval)
    {
        for (int i = 0; i < count; i++)
        {
            SpawnEnemy(i % 2 == 0 ? prefabA : prefabB);
            yield return new WaitForSeconds(interval);
        }
    }

    // Grupo compacto de N enemigos del mismo tipo
    IEnumerator SpawnGroup(GameObject prefab, int count, float intraDelay)
    {
        for (int i = 0; i < count; i++)
        {
            SpawnEnemy(prefab);
            yield return new WaitForSeconds(intraDelay);
        }
    }

    // Oleada cíclica de los 3 tipos normales (Linear → Sine → Shooter)
    // Cada 3 rondas añade un Linear extra de refuerzo
    IEnumerator SpawnMixedNormal(int rounds, float interval, bool includeShooter)
    {
        int typeCount = includeShooter ? 3 : 2;
        for (int r = 0; r < rounds; r++)
        {
            switch (r % typeCount)
            {
                case 0: SpawnEnemy(prefabLinear);  break;
                case 1: SpawnEnemy(prefabSine);    break;
                case 2: SpawnEnemy(prefabShooter); break;
            }

            // Cada 3 rondas, refuerzo inmediato
            if (r > 0 && r % 3 == 0)
            {
                yield return new WaitForSeconds(S(0.5f));
                SpawnEnemy(prefabLinear);
            }

            yield return new WaitForSeconds(interval);
        }
    }

    // Instancia un enemigo en posición X fija, Y aleatoria
    void SpawnEnemy(GameObject prefab)
    {
        if (prefab == null) return;
        float y = Random.Range(spawnMinY, spawnMaxY);
        Instantiate(prefab, new Vector3(spawnX, y, 0f), Quaternion.identity);
    }

    // Spawna 'count' power-ups aleatorios separados verticalmente.
    IEnumerator SpawnPowerUpWave(int count)
    {
        if (powerUpPrefabs == null || powerUpPrefabs.Length == 0)
        {
            Debug.LogWarning("[EnemyManager] No hay power-up prefabs asignados en el Inspector.");
            yield break;
        }

        // Lista de índices mezclada para no repetir el mismo ítem seguido
        int[] indices = new int[count];
        for (int i = 0; i < count; i++)
            indices[i] = Random.Range(0, powerUpPrefabs.Length);

        float totalHeight = (count - 1) * powerUpSpacing;
        float startY      = powerUpSpawnPos.y + totalHeight * 0.5f;

        for (int i = 0; i < count; i++)
        {
            GameObject prefab = powerUpPrefabs[indices[i]];
            if (prefab == null) continue;

            Vector3 pos = new Vector3(
                powerUpSpawnPos.x,
                startY - i * powerUpSpacing,
                0f);

            Instantiate(prefab, pos, Quaternion.identity);
            Debug.Log($"[EnemyManager] PowerUp spawneado: {prefab.name} en {pos}");

            yield return new WaitForSeconds(0.3f);   // pequeño retraso entre ítems
        }
    }

    IEnumerator ShowWarning(string message, float duration)
    {
        if (txtPhaseWarning != null)
        {
            txtPhaseWarning.text  = message;
            txtPhaseWarning.alpha = 1f;
        }

        Debug.Log($"── {message} ──");
        yield return new WaitForSeconds(duration);

        if (txtPhaseWarning != null)
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * 2f;
                txtPhaseWarning.alpha = 1f - t;
                yield return null;
            }
            txtPhaseWarning.text  = "";
            txtPhaseWarning.alpha = 1f;
        }
    }
}
