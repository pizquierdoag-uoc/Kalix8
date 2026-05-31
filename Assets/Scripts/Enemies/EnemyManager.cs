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

    [Tooltip("Distancia mínima entre enemigos al aparecer. Si otro enemigo está más cerca que esto del punto de spawn, el nuevo se empuja a la derecha para que entren espaciados.")]
    public float spawnMinSpacing = 1.8f;

    [Header("Power-ups")]
    public GameObject[] powerUpPrefabs;

    [Tooltip("Posición fija donde aparecen los power-ups")]
    public Vector3 powerUpSpawnPos = new Vector3(4f, 0f, 0f);

    [Tooltip("Separación vertical entre power-ups de una misma oleada")]
    public float powerUpSpacing = 1.4f;

    [Header("Texto de aviso de fase")]
    public TextMeshProUGUI txtPhaseWarning;

    bool _bossSpawned;
    bool _paused;              // flag para bloquear Update sin tocar enabled ni el coroutine
    Coroutine _stageCoroutine;
    // Índice de la fase actual (0..4). Se conserva entre Game Over y Continue
    // para poder retomar la partida desde la misma fase en la que el jugador murió.
    int _currentPhase;
    public int CurrentPhase => _currentPhase;
    const int LAST_PHASE = 4;

    // Escala un intervalo de tiempo según la dificultad actual
    float S(float t) => t * GameSettings.SpawnIntervalMult;

    // Alternativa a WaitForSeconds que sobrevive correctamente al ciclo timeScale=0→1.
    // WaitForSeconds puede no reanudarse en Unity 6 tras volver de timeScale=0;
    // este helper cuenta el tiempo con Time.deltaTime (que es 0 cuando timeScale=0)
    // y usa yield return null (que siempre dispara cada frame).
    IEnumerator WaitGame(float seconds)
    {
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    void Awake()
    {
        // Recalcula el rango de spawn desde la cámara usando el mismo padding que EnemyBase
        Camera cam = Camera.main;
        if (cam != null)
        {
            float halfH   = cam.orthographicSize;
            float padding = 1.0f;   // debe coincidir con EnemyBase.boundsPaddingY
            spawnMinY = cam.transform.position.y - halfH + padding;
            spawnMaxY = cam.transform.position.y + halfH - padding;
        }
    }

    void Start()
    {
        if (txtPhaseWarning != null) txtPhaseWarning.text = "";
        _currentPhase   = 0;
        _stageCoroutine = StartCoroutine(InitialDelayAndStart());
    }

    void Update()
    {
        if (_paused) return;
        if (Keyboard.current != null && Keyboard.current.f2Key.wasPressedThisFrame) SkipToBoss();
    }

    /// <summary>
    /// Congela el spawn sin matar el coroutine: Time.timeScale=0 pausa los
    /// WaitForSeconds, así que al restaurar el tiempo el coroutine retoma
    /// exactamente donde estaba (mismo punto de oleada, mismo boss si estaba vivo).
    /// Usamos un flag en lugar de enabled=false para no cancelar los WaitForSeconds
    /// en coroutines activas (comportamiento de Unity 6).
    /// </summary>
    public void PauseStage()
    {
        _paused = true;
    }

    /// <summary>Reactiva el spawn tras un Continue.</summary>
    public void UnpauseStage()
    {
        _paused = false;
    }

    /// <summary>Reinicio total: vuelve a la fase 0 con la pausa inicial. (Uso: nueva partida.)</summary>
    public void RestartStage()
    {
        if (_stageCoroutine != null) StopCoroutine(_stageCoroutine);
        _bossSpawned    = false;
        _currentPhase   = 0;
        _stageCoroutine = StartCoroutine(InitialDelayAndStart());
    }

    /// <summary>Retoma la partida desde la fase en la que estaba al perder. (Uso: Continue.)</summary>
    public void ResumeStage()
    {
        if (_stageCoroutine != null) StopCoroutine(_stageCoroutine);
        _bossSpawned    = false;   // el boss fue destruido en el Game Over: re-spawnea si toca
        int phase       = Mathf.Clamp(_currentPhase, 0, LAST_PHASE);
        _stageCoroutine = StartCoroutine(RunPhase(phase));
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

        _currentPhase   = LAST_PHASE;
        _stageCoroutine = StartCoroutine(RunPhase(LAST_PHASE));
    }

    IEnumerator InitialDelayAndStart()
    {
        // Pausa inicial para que el jugador se oriente (solo en partida nueva, no en Continue)
        yield return WaitGame(3f);
        _stageCoroutine = StartCoroutine(RunPhase(0));
    }

    /// <summary>
    /// Ejecuta una fase concreta y, al terminar, encadena automáticamente la siguiente.
    /// Cada fase es atómica: si el jugador muere y pulsa Continue, ResumeStage llama
    /// a RunPhase con el índice guardado, replanteando la fase desde su inicio.
    /// </summary>
    IEnumerator RunPhase(int phase)
    {
        _currentPhase = phase;

        switch (phase)
        {
            case 0:
                // Fase 1: enemigos normales + power-ups de fase
                yield return ShowWarning("FASE 1", 2f);
                yield return Phase1_NormalEnemies();
                yield return SpawnPhaseEndPowerUps();
                yield return WaitGame(2f);
                break;

            case 1:
                // Fase 2: oleada de kamikazes + power-ups de fase
                yield return KamikazeWarningSequence();
                yield return PhaseKamikazes(total: 60, groupSize: 9,
                                            intraDelay: 0.35f,
                                            pauseFirst: S(3.5f), pauseSecond: S(2f),
                                            halfAt: 30, speedMult: GameSettings.KamikazeSpeedMult);
                yield return SpawnPhaseEndPowerUps();
                yield return WaitGame(2f);
                break;

            case 2:
                // Fase 3: enemigos normales agresivos + power-ups de fase
                yield return ShowWarning("FASE 3", 2f);
                yield return Phase3_NormalEnemiesAggressive();
                yield return SpawnPhaseEndPowerUps();
                yield return WaitGame(2f);
                break;

            case 3:
                // Fase 4: segunda oleada de kamikazes + power-ups de fase
                yield return KamikazeWarningSequence();
                yield return PhaseKamikazes(total: 45, groupSize: 9,
                                            intraDelay: 0.25f,
                                            pauseFirst: S(1.5f), pauseSecond: S(1.5f),
                                            halfAt: 999, speedMult: GameSettings.KamikazeSpeedMult);
                yield return SpawnPhaseEndPowerUps();
                yield return WaitGame(2f);
                break;

            case 4:
                // Fase 5: Boss final (última fase, no encadena)
                yield return BossWarningSequence();
                yield return Phase5_Boss();
                yield break;
        }

        // Encadena la siguiente fase automáticamente
        _stageCoroutine = StartCoroutine(RunPhase(phase + 1));
    }

    IEnumerator Phase1_NormalEnemies()
    {
        // --- OLA 1: Tren de exploración — 5 Linears en cadena central ---
        for (int i = 0; i < 5; i++)
        {
            SpawnAt(prefabLinear, 0f);
            yield return WaitGame(S(0.35f));
        }
        yield return WaitGame(S(3f));

        // --- OLA 2: Pinza vertical — Linears que se cierran desde bordes ---
        SpawnAt(prefabLinear,  3.0f);
        SpawnAt(prefabLinear, -3.0f);
        yield return WaitGame(S(0.5f));
        SpawnAt(prefabLinear,  1.5f);
        SpawnAt(prefabLinear, -1.5f);
        yield return WaitGame(S(3f));

        // --- OLA 3: Doble onda — 2 Sines simultáneos en sentidos opuestos ---
        SpawnAt(prefabSine,  2.0f);
        SpawnAt(prefabSine, -2.0f);
        yield return WaitGame(S(3.5f));

        // --- OLA 4: Cortina diagonal — 5 Sines en cascada arriba→abajo ---
        SpawnAt(prefabSine,  3.5f);
        yield return WaitGame(S(0.4f));
        SpawnAt(prefabSine,  1.75f);
        yield return WaitGame(S(0.4f));
        SpawnAt(prefabSine,  0f);
        yield return WaitGame(S(0.4f));
        SpawnAt(prefabSine, -1.75f);
        yield return WaitGame(S(0.4f));
        SpawnAt(prefabSine, -3.5f);
        yield return WaitGame(S(3.5f));

        // --- OLA 5: Francotirador con escolta — Shooter central + Linears flancos ---
        SpawnAt(prefabShooter, 0f);
        yield return WaitGame(S(0.8f));
        SpawnAt(prefabLinear,  2.5f);
        SpawnAt(prefabLinear, -2.5f);
        yield return WaitGame(S(4f));

        // --- OLA 6: Flecha inversa — V de 5 Linears que se abre de dentro a fuera ---
        SpawnAt(prefabLinear, 0f);
        yield return WaitGame(S(0.3f));
        SpawnAt(prefabLinear,  1.5f);
        SpawnAt(prefabLinear, -1.5f);
        yield return WaitGame(S(0.3f));
        SpawnAt(prefabLinear,  3.0f);
        SpawnAt(prefabLinear, -3.0f);
        yield return WaitGame(S(3.5f));

        // --- OLA 7: Doble Shooter con cobertura de Sines ---
        SpawnAt(prefabShooter,  2.5f);
        SpawnAt(prefabShooter, -2.5f);
        yield return WaitGame(S(0.6f));
        SpawnAt(prefabSine,  3.2f);
        SpawnAt(prefabSine, -3.2f);
        yield return WaitGame(S(3.5f));

        // --- OLA 8: Presión cruzada — Shooters bombardean + Sines ondean desde extremos ---
        SpawnAt(prefabShooter,  1.5f);
        SpawnAt(prefabShooter, -1.5f);
        yield return WaitGame(S(1.0f));
        SpawnAt(prefabSine,  3.0f);
        SpawnAt(prefabSine, -3.0f);
        yield return WaitGame(S(4f));

        // --- OLA 9: Muro escalonado — Linears exteriores + Shooter + Sine central ---
        SpawnAt(prefabLinear,  3.2f);
        SpawnAt(prefabLinear, -3.2f);
        yield return WaitGame(S(0.4f));
        SpawnAt(prefabShooter,  1.5f);
        SpawnAt(prefabShooter, -1.5f);
        yield return WaitGame(S(0.4f));
        SpawnAt(prefabSine, 0f);
        yield return WaitGame(S(3.5f));

        // --- OLA 10: Gran formación final — los 3 tipos en secuencia escalonada ---
        SpawnAt(prefabShooter, 0f);                         // ancla central con disparo
        yield return WaitGame(S(0.5f));
        SpawnAt(prefabSine,  3.0f);                         // flancos ondulantes
        SpawnAt(prefabSine, -3.0f);
        yield return WaitGame(S(0.5f));
        SpawnAt(prefabLinear,  2.5f);                       // muro que cierra
        SpawnAt(prefabLinear, -2.5f);
        yield return WaitGame(S(0.35f));
        SpawnAt(prefabLinear,  1.0f);
        SpawnAt(prefabLinear, -1.0f);
        yield return WaitGame(S(0.35f));
        SpawnAt(prefabShooter, 3.2f);                       // remate con Shooters extremos
        SpawnAt(prefabShooter, -3.2f);
        yield return WaitGame(S(3f));
    }

    IEnumerator Phase3_NormalEnemiesAggressive()
    {
        // Fase 3
        
        // Ola 1: Triple diagonal rápida de Linears (top → centro → bottom)
        SpawnAt(prefabLinear,  3f);
        yield return WaitGame(0.3f);
        SpawnAt(prefabLinear,  0f);
        yield return WaitGame(0.3f);
        SpawnAt(prefabLinear, -3f);
        yield return WaitGame(S(2f));

        // Ola 2: Cuatro Linears en 2 pares rápidos (exterior luego interior)
        SpawnAt(prefabLinear,  3f);
        SpawnAt(prefabLinear, -3f);
        yield return WaitGame(0.35f);
        SpawnAt(prefabLinear,  1.2f);
        SpawnAt(prefabLinear, -1.2f);
        yield return WaitGame(S(2.5f));

        // Ola 3: Triple Sine simétrico (exteriores + centro)
        SpawnAt(prefabSine,  3f);
        SpawnAt(prefabSine, -3f);
        yield return WaitGame(0.5f);
        SpawnAt(prefabSine, 0f);
        yield return WaitGame(S(3f));

        // Ola 4: Cuatro Shooters en cuadrado — máxima presión de fuego
        SpawnAt(prefabShooter,  2.5f);
        SpawnAt(prefabShooter, -2.5f);
        yield return WaitGame(0.4f);
        SpawnAt(prefabShooter,  0.8f);
        SpawnAt(prefabShooter, -0.8f);
        yield return WaitGame(S(3.5f));

        // Ola 5: Flanqueo cruzado — Linears y Sines intercalados en lados opuestos
        SpawnAt(prefabLinear,  2.5f);
        SpawnAt(prefabSine,   -2.5f);
        yield return WaitGame(0.5f);
        SpawnAt(prefabSine,    2.5f);
        SpawnAt(prefabLinear, -2.5f);
        yield return WaitGame(S(2.5f));

        // Ola 6: Diagonal de Sines + Shooters cubriendo huecos
        SpawnAt(prefabSine,     3f);
        yield return WaitGame(0.35f);
        SpawnAt(prefabSine,     0f);
        SpawnAt(prefabShooter, -1.5f);
        yield return WaitGame(0.35f);
        SpawnAt(prefabSine,    -3f);
        SpawnAt(prefabShooter,  1.5f);
        yield return WaitGame(S(3f));

        // Ola 7: Muro de 5 Sines en abanico completo
        SpawnAt(prefabSine,  3.2f);
        yield return WaitGame(0.25f);
        SpawnAt(prefabSine,  1.6f);
        yield return WaitGame(0.25f);
        SpawnAt(prefabSine,  0f);
        yield return WaitGame(0.25f);
        SpawnAt(prefabSine, -1.6f);
        yield return WaitGame(0.25f);
        SpawnAt(prefabSine, -3.2f);
        yield return WaitGame(S(3f));

        // Ola 8 (cierre fase 3): Embestida total con los 3 tipos en flancos alternos
        SpawnAt(prefabLinear,   3f);
        SpawnAt(prefabShooter, -3f);
        yield return WaitGame(0.3f);
        SpawnAt(prefabSine,    1.5f);
        SpawnAt(prefabLinear, -1.5f);
        yield return WaitGame(0.3f);
        SpawnAt(prefabShooter,  0f);
        yield return WaitGame(0.3f);
        SpawnAt(prefabLinear,   3f);
        SpawnAt(prefabLinear,  -3f);
        yield return WaitGame(S(3f));
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
            yield return WaitGame(pause);
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
            yield return WaitGame(intraDelay);
        }
    }

    IEnumerator KamikazeWarningSequence()
    {
        float sfxLen = AudioManager.Instance != null ? AudioManager.Instance.WarningSFXLength : 2f;
        HUDController.Instance?.ShowKamikazeWarning();   // muestra panel + 1er sonido
        yield return WaitGame(sfxLen);
        AudioManager.Instance?.PlayWarningSFXOneShot();  // 2o sonido
        float remaining = 4f - sfxLen;
        if (remaining > 0f) yield return WaitGame(remaining);
        HUDController.Instance?.HideKamikazeWarning();
    }

    IEnumerator BossWarningSequence()
    {
        AudioManager.Instance?.PlaySFX("warning");
        HUDController.Instance?.StartBossWarning();
        yield return WaitGame(6f);
        HUDController.Instance?.StopBossWarning();
    }

    IEnumerator Phase5_Boss()
    {
        if (prefabBoss == null || _bossSpawned) yield break;
        _bossSpawned = true;
        Instantiate(prefabBoss, new Vector3(spawnX, 0f, 0f), Quaternion.identity);        
    }


    // Instancia un enemigo en posición X fija, Y aleatoria (usado por kamikazes)
    GameObject SpawnEnemy(GameObject prefab)
    {
        if (prefab == null) return null;
        float y = Random.Range(spawnMinY, spawnMaxY);
        return Instantiate(prefab, SafeSpawnPos(y), Quaternion.identity);
    }

    // Instancia un enemigo en posición X fija, Y exacta (usado por formaciones)
    GameObject SpawnAt(GameObject prefab, float y)
    {
        if (prefab == null) return null;
        return Instantiate(prefab, SafeSpawnPos(y), Quaternion.identity);
    }

    // Devuelve un punto de spawn que respete la distancia mínima con cualquier enemigo activo.
    // Si hay alguien cerca, empuja la X hacia la derecha (fuera de pantalla) para que entren espaciados.
    Vector3 SafeSpawnPos(float y)
    {
        float x = spawnX;
        var existing = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
        foreach (var eb in existing)
        {
            if (eb == null || !eb.gameObject.activeInHierarchy) continue;
            Vector3 p = eb.transform.position;
            // Solo nos importan los que están a una altura similar y a la derecha del jugador
            if (Mathf.Abs(p.y - y) < spawnMinSpacing && p.x > x - spawnMinSpacing)
            {
                x = p.x + spawnMinSpacing;
            }
        }
        return new Vector3(x, y, 0f);
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

            yield return WaitGame(0.3f);   // pequeño retraso entre ítems
        }
    }

    /// <summary>
    /// Spawna la oleada de power-ups al final de cada fase:
    ///   · Un arma de cada tipo (Normal, Spread, Laser, Homing)
    ///   · Una bomba
    ///   · Un ítem aleatorio de los "bonus" (ExtraLife, Shield, SpeedBoost, OrbitDrones)
    /// Total: 6 power-ups separados verticalmente, centrados en pantalla.
    /// </summary>
    IEnumerator SpawnPhaseEndPowerUps()
    {
        if (powerUpPrefabs == null || powerUpPrefabs.Length == 0)
        {
            Debug.LogWarning("[EnemyManager] No hay power-up prefabs asignados en el Inspector.");
            yield break;
        }

        // Tipos fijos que siempre aparecen
        PowerUpType[] fixed_types = new PowerUpType[]
        {
            PowerUpType.WeaponNormal,
            PowerUpType.WeaponSpread,
            PowerUpType.WeaponLaser,
            PowerUpType.WeaponHoming,
            PowerUpType.Bomb,
        };

        // Tipos bonus: se elige 1 aleatorio
        PowerUpType[] bonus_types = new PowerUpType[]
        {
            PowerUpType.ExtraLife,
            PowerUpType.Shield,
            PowerUpType.SpeedBoost,
            PowerUpType.OrbitDrones,
        };
        PowerUpType randomBonus = bonus_types[Random.Range(0, bonus_types.Length)];

        // Lista completa: 5 fijos + 1 bonus aleatorio = 6 ítems
        PowerUpType[] spawnOrder = new PowerUpType[fixed_types.Length + 1];
        fixed_types.CopyTo(spawnOrder, 0);
        spawnOrder[fixed_types.Length] = randomBonus;

        int count         = spawnOrder.Length;   // 6
        float totalHeight = (count - 1) * powerUpSpacing;
        float startY      = powerUpSpawnPos.y + totalHeight * 0.5f;

        for (int i = 0; i < count; i++)
        {
            GameObject prefab = GetPrefabByType(spawnOrder[i]);
            if (prefab == null)
            {
                Debug.LogWarning($"[EnemyManager] Prefab no encontrado para tipo {spawnOrder[i]}. ¿Está asignado en el Inspector?");
                continue;
            }

            Vector3 pos = new Vector3(
                powerUpSpawnPos.x,
                startY - i * powerUpSpacing,
                0f);

            Instantiate(prefab, pos, Quaternion.identity);
            yield return WaitGame(0.3f);
        }
    }

    /// <summary>Busca en powerUpPrefabs el primer prefab cuyo PowerUpItem.type coincida con el solicitado.</summary>
    GameObject GetPrefabByType(PowerUpType type)
    {
        foreach (var prefab in powerUpPrefabs)
        {
            if (prefab == null) continue;
            var item = prefab.GetComponent<PowerUpItem>();
            if (item != null && item.type == type)
                return prefab;
        }
        return null;
    }

    IEnumerator ShowWarning(string message, float duration)
    {
        if (txtPhaseWarning != null)
        {
            txtPhaseWarning.text  = message;
            txtPhaseWarning.alpha = 1f;
        }

        yield return WaitGame(duration);

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
