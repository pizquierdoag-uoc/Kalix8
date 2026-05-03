using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
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

    [Header("Power-ups")]
    public GameObject[] powerUpPrefabs;

    [Tooltip("Posición fija donde aparecen los power-ups")]
    public Vector3 powerUpSpawnPos = new Vector3(4f, 0f, 0f);

    [Tooltip("Separación vertical entre power-ups de una misma oleada")]
    public float powerUpSpacing = 1.4f;

    [Header("Texto de aviso de fase")]
    public TextMeshProUGUI txtPhaseWarning;

    bool _bossSpawned;
    Coroutine _stageCoroutine;

    // Escala un intervalo de tiempo según la dificultad actual
    float S(float t) => t * GameSettings.SpawnIntervalMult;

    void Start()
    {
        if (txtPhaseWarning != null) txtPhaseWarning.text = "";
        _stageCoroutine = StartCoroutine(StageSequence());
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.f2Key.wasPressedThisFrame) SkipToBoss();
    }

    public void SkipToBoss()
    {
        if (_bossSpawned) return;

        // Para la secuencia actual
        if (_stageCoroutine != null) { StopCoroutine(_stageCoroutine); _stageCoroutine = null; }

        // Limpia todos los enemigos activos en escena
        foreach (var eb in FindObjectsByType<EnemyBase>(FindObjectsSortMode.None))
            if (eb != null) Destroy(eb.gameObject);

        // Oculta avisos de HUD que pudieran estar activos
        HUDController.Instance?.HideKamikazeWarning();
        if (txtPhaseWarning != null) { txtPhaseWarning.text = ""; txtPhaseWarning.alpha = 1f; }

        _stageCoroutine = StartCoroutine(BossRun());
    }

    IEnumerator BossRun()
    {
        // Para ir directamente al boss
        yield return BossWarningSequence();
        yield return Phase5_Boss();
    }

    IEnumerator StageSequence()
    {
        // Pausa inicial para que el jugador se oriente ponemos 3 segundos
        yield return new WaitForSeconds(3f);

        yield return ShowWarning("FASE 1", 2f);

        // Fase 1
        yield return Phase1_NormalEnemies();

        // Soltamos powerups
        yield return SpawnPowerUpWave(3);
        yield return new WaitForSeconds(2f);

        // Fase 2 Kamikazes
        yield return KamikazeWarningSequence();
        yield return PhaseKamikazes(total: 60, groupSize: 9,
                                    intraDelay: 0.35f,
                                    pauseFirst: S(3.5f), pauseSecond: S(2f),
                                    halfAt: 30, speedMult: 3f);

        // Fase 3
        yield return ShowWarning("FASE 3", 2f);
        yield return Phase3_NormalEnemiesAggressive();

        // Soltamos powerups
        yield return SpawnPowerUpWave(3);
        yield return new WaitForSeconds(2f);

        // Fase 4 Kamikazes
        yield return KamikazeWarningSequence();
        yield return PhaseKamikazes(total: 45, groupSize: 9,
                                    intraDelay: 0.25f,
                                    pauseFirst: S(1.5f), pauseSecond: S(1.5f),
                                    halfAt: 999, speedMult: 3f);

        // Fase 5 Boss
        yield return BossWarningSequence();
        yield return Phase5_Boss();
    }

    IEnumerator Phase1_NormalEnemies()
    {
        // Fase 1

        // Ola 1: Linear solitario en el centro para primer contacto
        SpawnAt(prefabLinear, 0f);
        yield return new WaitForSeconds(S(2.5f));

        // Ola 2: Par simétrico de Linears arriba y abajo
        SpawnAt(prefabLinear,  2.5f);
        SpawnAt(prefabLinear, -2.5f);
        yield return new WaitForSeconds(S(3f));

        // Ola 3: Fila de 3 Linears equidistantes 
        SpawnAt(prefabLinear,  2.3f);
        yield return new WaitForSeconds(0.4f);
        SpawnAt(prefabLinear,  0f);
        yield return new WaitForSeconds(0.4f);
        SpawnAt(prefabLinear, -2.3f);
        yield return new WaitForSeconds(S(3f));

        // Ola 4: Par simétrico de Sines perimera vez tipo Sine
        SpawnAt(prefabSine,  2f);
        SpawnAt(prefabSine, -2f);
        yield return new WaitForSeconds(S(3.5f));

        // Ola 5: Formación en V — Linears exteriores + Sine al centro
        SpawnAt(prefabLinear,  3f);
        SpawnAt(prefabLinear, -3f);
        yield return new WaitForSeconds(0.5f);
        SpawnAt(prefabSine, 0f);
        yield return new WaitForSeconds(S(3.5f));

        // Ola 6: Cascada diagonal de 4 Sines (arriba → abajo)
        SpawnAt(prefabSine,  3f);
        yield return new WaitForSeconds(0.5f);
        SpawnAt(prefabSine,  1f);
        yield return new WaitForSeconds(0.5f);
        SpawnAt(prefabSine, -1f);
        yield return new WaitForSeconds(0.5f);
        SpawnAt(prefabSine, -3f);
        yield return new WaitForSeconds(S(3.5f));

        // Ola 7: 2 Linears flanqueando + 1 Shooter central — presentación del Shooter
        SpawnAt(prefabLinear,  2f);
        SpawnAt(prefabLinear, -2f);
        yield return new WaitForSeconds(0.6f);
        SpawnAt(prefabShooter, 0f);
        yield return new WaitForSeconds(S(4f));

        // Ola 8: Doble par de Linears (exterior → interior en rápida sucesión)
        SpawnAt(prefabLinear,  3f);
        SpawnAt(prefabLinear, -3f);
        yield return new WaitForSeconds(0.5f);
        SpawnAt(prefabLinear,  1.2f);
        SpawnAt(prefabLinear, -1.2f);
        yield return new WaitForSeconds(S(3.5f));

        // Ola 9: 2 Shooters simétricos + Sine central
        SpawnAt(prefabShooter,  2.2f);
        SpawnAt(prefabShooter, -2.2f);
        yield return new WaitForSeconds(0.6f);
        SpawnAt(prefabSine, 0f);
        yield return new WaitForSeconds(S(4f));

        // Ola 10 (cierre fase 1): Abanico de 5 Linears — centro primero, expandiéndose
        SpawnAt(prefabLinear, 0f);
        yield return new WaitForSeconds(0.3f);
        SpawnAt(prefabLinear,  1.8f);
        SpawnAt(prefabLinear, -1.8f);
        yield return new WaitForSeconds(0.3f);
        SpawnAt(prefabLinear,  3.2f);
        SpawnAt(prefabLinear, -3.2f);
        yield return new WaitForSeconds(S(3f));
    }

    IEnumerator Phase3_NormalEnemiesAggressive()
    {
        // Fase 3
        
        // Ola 1: Triple diagonal rápida de Linears (top → centro → bottom)
        SpawnAt(prefabLinear,  3f);
        yield return new WaitForSeconds(0.3f);
        SpawnAt(prefabLinear,  0f);
        yield return new WaitForSeconds(0.3f);
        SpawnAt(prefabLinear, -3f);
        yield return new WaitForSeconds(S(2f));

        // Ola 2: Cuatro Linears en 2 pares rápidos (exterior luego interior)
        SpawnAt(prefabLinear,  3f);
        SpawnAt(prefabLinear, -3f);
        yield return new WaitForSeconds(0.35f);
        SpawnAt(prefabLinear,  1.2f);
        SpawnAt(prefabLinear, -1.2f);
        yield return new WaitForSeconds(S(2.5f));

        // Ola 3: Triple Sine simétrico (exteriores + centro)
        SpawnAt(prefabSine,  3f);
        SpawnAt(prefabSine, -3f);
        yield return new WaitForSeconds(0.5f);
        SpawnAt(prefabSine, 0f);
        yield return new WaitForSeconds(S(3f));

        // Ola 4: Cuatro Shooters en cuadrado — máxima presión de fuego
        SpawnAt(prefabShooter,  2.5f);
        SpawnAt(prefabShooter, -2.5f);
        yield return new WaitForSeconds(0.4f);
        SpawnAt(prefabShooter,  0.8f);
        SpawnAt(prefabShooter, -0.8f);
        yield return new WaitForSeconds(S(3.5f));

        // Ola 5: Flanqueo cruzado — Linears y Sines intercalados en lados opuestos
        SpawnAt(prefabLinear,  2.5f);
        SpawnAt(prefabSine,   -2.5f);
        yield return new WaitForSeconds(0.5f);
        SpawnAt(prefabSine,    2.5f);
        SpawnAt(prefabLinear, -2.5f);
        yield return new WaitForSeconds(S(2.5f));

        // Ola 6: Diagonal de Sines + Shooters cubriendo huecos
        SpawnAt(prefabSine,     3f);
        yield return new WaitForSeconds(0.35f);
        SpawnAt(prefabSine,     0f);
        SpawnAt(prefabShooter, -1.5f);
        yield return new WaitForSeconds(0.35f);
        SpawnAt(prefabSine,    -3f);
        SpawnAt(prefabShooter,  1.5f);
        yield return new WaitForSeconds(S(3f));

        // Ola 7: Muro de 5 Sines en abanico completo
        SpawnAt(prefabSine,  3.2f);
        yield return new WaitForSeconds(0.25f);
        SpawnAt(prefabSine,  1.6f);
        yield return new WaitForSeconds(0.25f);
        SpawnAt(prefabSine,  0f);
        yield return new WaitForSeconds(0.25f);
        SpawnAt(prefabSine, -1.6f);
        yield return new WaitForSeconds(0.25f);
        SpawnAt(prefabSine, -3.2f);
        yield return new WaitForSeconds(S(3f));

        // Ola 8 (cierre fase 3): Embestida total con los 3 tipos en flancos alternos
        SpawnAt(prefabLinear,   3f);
        SpawnAt(prefabShooter, -3f);
        yield return new WaitForSeconds(0.3f);
        SpawnAt(prefabSine,    1.5f);
        SpawnAt(prefabLinear, -1.5f);
        yield return new WaitForSeconds(0.3f);
        SpawnAt(prefabShooter,  0f);
        yield return new WaitForSeconds(0.3f);
        SpawnAt(prefabLinear,   3f);
        SpawnAt(prefabLinear,  -3f);
        yield return new WaitForSeconds(S(3f));
    }

    //   total       — cuántos kamikazes en total
    //   groupSize   — tamaño de cada grupo
    //   intraDelay  — delay entre kamikazes dentro del grupo
    //   pauseFirst  — pausa entre grupos en la primera mitad
    //   pauseSecond — pausa entre grupos en la segunda mitad
    //   halfAt      — a partir de cuántos spawneados se considera "segunda mitad"
    IEnumerator PhaseKamikazes(int total, int groupSize, float intraDelay,
                                float pauseFirst, float pauseSecond, int halfAt,
                                float speedMult = 1f)
    {
        int spawned = 0;
        while (spawned < total)
        {
            int toSpawn = Mathf.Min(groupSize, total - spawned);
            yield return SpawnKamikazeGroup(toSpawn, intraDelay, speedMult);
            spawned += toSpawn;

            float pause = spawned < halfAt ? pauseFirst : pauseSecond;
            yield return new WaitForSeconds(pause);
        }
    }

    IEnumerator SpawnKamikazeGroup(int count, float intraDelay, float speedMult)
    {
        for (int i = 0; i < count; i++)
        {
            GameObject go = SpawnEnemy(prefabKamikaze);
            if (go != null && speedMult != 1f)
            {
                EnemyBase eb = go.GetComponent<EnemyBase>();
                if (eb != null) eb.moveSpeed *= speedMult;
            }
            yield return new WaitForSeconds(intraDelay);
        }
    }

    IEnumerator KamikazeWarningSequence()
    {
        float sfxLen = AudioManager.Instance != null ? AudioManager.Instance.WarningSFXLength : 2f;
        HUDController.Instance?.ShowKamikazeWarning();   // muestra panel + 1er sonido
        yield return new WaitForSeconds(sfxLen);
        AudioManager.Instance?.PlayWarningSFXOneShot();  // 2o sonido
        float remaining = 4f - sfxLen;
        if (remaining > 0f) yield return new WaitForSeconds(remaining);
        HUDController.Instance?.HideKamikazeWarning();
    }

    IEnumerator BossWarningSequence()
    {
        AudioManager.Instance?.PlaySFX("warning");
        HUDController.Instance?.StartBossWarning();
        yield return new WaitForSeconds(6f);
        HUDController.Instance?.StopBossWarning();
    }

    IEnumerator Phase5_Boss()
    {
        if (prefabBoss == null || _bossSpawned) yield break;
        _bossSpawned = true;
        Instantiate(prefabBoss, new Vector3(spawnX, 0f, 0f), Quaternion.identity);        
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

    // Instancia un enemigo en posición X fija, Y aleatoria (usado por kamikazes)
    GameObject SpawnEnemy(GameObject prefab)
    {
        if (prefab == null) return null;
        float y = Random.Range(spawnMinY, spawnMaxY);
        return Instantiate(prefab, new Vector3(spawnX, y, 0f), Quaternion.identity);
    }

    // Instancia un enemigo en posición X fija, Y exacta (usado por formaciones)
    GameObject SpawnAt(GameObject prefab, float y)
    {
        if (prefab == null) return null;
        return Instantiate(prefab, new Vector3(spawnX, y, 0f), Quaternion.identity);
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
