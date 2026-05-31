using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Lista de iconos de enemigos con sus puntuaciones (al matarlos).
/// Diseñado para el PausePanel — debajo de los controles.
/// Mismo patrón que PauseMenuPowerUpList: lee EnemyHealth.scoreValue del prefab.
/// </summary>
public class PauseMenuEnemyScores : MonoBehaviour
{
    [Header("Prefabs de enemigos")]
    public GameObject[] enemyPrefabs;

    [Header("Estilo")]
    public float  iconSize    = 48f;
    public float  rowHeight   = 56f;
    public float  rowSpacing  = 6f;
    public float  fontSize    = 22f;
    public Color  labelColor  = Color.white;
    public Color  iconColor   = Color.white;
    public string suffix      = " PTS";

    void Start()
    {
        BuildList();
    }

    void BuildList()
    {
        // Limpia hijos previos
        foreach (Transform child in transform)
            Destroy(child.gameObject);

        if (enemyPrefabs == null) return;

        // Layout vertical centrado
        var vlg = GetComponent<VerticalLayoutGroup>() ?? gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.spacing                = rowSpacing;
        vlg.childAlignment         = TextAnchor.MiddleCenter;
        vlg.childControlWidth      = true;
        vlg.childControlHeight     = false;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.padding                = new RectOffset(8, 8, 8, 8);

        var csf = GetComponent<ContentSizeFitter>() ?? gameObject.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        foreach (var prefab in enemyPrefabs)
        {
            if (prefab == null) continue;
            int score = GetScoreValue(prefab);
            if (score < 0) continue;
            AddRow(prefab, score);
        }
    }

    int GetScoreValue(GameObject prefab)
    {
        var eh = prefab.GetComponent<EnemyHealth>();
        if (eh != null) return eh.scoreValue;
        var boss = prefab.GetComponent<BossController>();
        if (boss != null) return boss.scoreValue;
        return -1;
    }

    void AddRow(GameObject prefab, int score)
    {
        var row   = new GameObject(prefab.name);
        var rowRT = row.AddComponent<RectTransform>();
        rowRT.SetParent(transform, false);
        rowRT.sizeDelta = new Vector2(0f, rowHeight);

        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing                = 16f;
        hlg.childAlignment         = TextAnchor.MiddleCenter;
        hlg.childControlWidth      = false;
        hlg.childControlHeight     = false;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = false;
        hlg.padding                = new RectOffset(0, 0, 0, 0);

        // Icono
        var iconGO = new GameObject("Icon");
        var iconRT = iconGO.AddComponent<RectTransform>();
        iconRT.SetParent(row.transform, false);
        iconRT.sizeDelta = new Vector2(iconSize, iconSize);

        var img = iconGO.AddComponent<Image>();
        var sr  = prefab.GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null) img.sprite = sr.sprite;
        img.color          = iconColor;
        img.preserveAspect = true;

        // Score
        var labelGO = new GameObject("Score");
        var labelRT = labelGO.AddComponent<RectTransform>();
        labelRT.SetParent(row.transform, false);
        labelRT.sizeDelta = new Vector2(140f, rowHeight);

        var tmp = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text             = score.ToString() + suffix;
        tmp.fontSize         = fontSize;
        tmp.color            = labelColor;
        tmp.alignment        = TextAlignmentOptions.MidlineLeft;
        tmp.fontStyle        = FontStyles.Bold;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode     = TextOverflowModes.Overflow;
    }
}
