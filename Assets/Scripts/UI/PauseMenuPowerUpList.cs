using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PauseMenuPowerUpList : MonoBehaviour
{
    [Header("Prefabs de powerups")]
    public GameObject[] powerUpPrefabs;

    [Header("Estilo de filas")]
    public float iconSize   = 44f;
    public float rowHeight  = 52f;
    public float rowSpacing = 6f;
    public float fontSize   = 18f;
    public Color labelColor = Color.white;

    readonly List<Transform> _icons       = new List<Transform>();
    readonly List<float>     _pulseSpeeds  = new List<float>();
    readonly List<float>     _pulseOffsets = new List<float>();
    readonly List<bool>      _rotates      = new List<bool>();

    void Start()
    {
        BuildList();
    }

    [Header("Rotación")]
    public float rotationSpeed = 90f; // Movimiento de grados por segundo

    void Update()
    {
        for (int i = 0; i < _icons.Count; i++)
        {
            if (_icons[i] == null) continue;

            // Respiración. Animación de agrandarse empequeñecerse
            float s = 1f + Mathf.Sin(Time.unscaledTime * _pulseSpeeds[i] + _pulseOffsets[i]) * 0.09f;
            _icons[i].localScale = Vector3.one * s;

            // Rotación continua
            if (_rotates[i])
                _icons[i].Rotate(0f, 0f, rotationSpeed * Time.unscaledDeltaTime);
        }
    }

    void BuildList()
    {
        foreach (Transform child in transform)
            Destroy(child.gameObject);
        _icons.Clear();
        _pulseSpeeds.Clear();
        _pulseOffsets.Clear();
        _rotates.Clear();

        if (powerUpPrefabs == null) return;

        // Layout vertical en este mismo objeto
        var vlg = GetComponent<VerticalLayoutGroup>() ?? gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.spacing                = rowSpacing;
        vlg.childAlignment         = TextAnchor.UpperLeft;
        vlg.childControlWidth      = true;
        vlg.childControlHeight     = false;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.padding                = new RectOffset(12, 12, 6, 6);

        var csf = GetComponent<ContentSizeFitter>() ?? gameObject.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        foreach (var prefab in powerUpPrefabs)
        {
            if (prefab == null) continue;
            var item = prefab.GetComponent<PowerUpItem>();
            if (item == null) continue;
            AddRow(item);
        }
    }

    void AddRow(PowerUpItem item)
    {
        // — Fila —
        var row   = new GameObject(item.type.ToString());
        var rowRT = row.AddComponent<RectTransform>();
        rowRT.SetParent(transform, false);
        rowRT.sizeDelta = new Vector2(0f, rowHeight);

        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing                = 14f;
        hlg.childAlignment         = TextAnchor.MiddleLeft;
        hlg.childControlWidth      = false;
        hlg.childControlHeight     = false;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = false;
        hlg.padding                = new RectOffset(0, 0, 0, 0);

        // — Icono —
        var iconGO = new GameObject("Icon");
        var iconRT = iconGO.AddComponent<RectTransform>();
        iconRT.SetParent(row.transform, false);
        iconRT.sizeDelta = new Vector2(iconSize, iconSize);

        var img = iconGO.AddComponent<Image>();
        var sr  = item.GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
            img.sprite = sr.sprite;
        img.color          = ColorForType(item.type);
        img.preserveAspect = true;

        _icons.Add(iconGO.transform);
        float baseSpeed = Random.Range(1.1f, 1.9f);
        _pulseSpeeds.Add(item.type == PowerUpType.ExtraLife ? baseSpeed * 10f : baseSpeed);
        _pulseOffsets.Add(Random.Range(0f, Mathf.PI * 2f));
        _rotates.Add(item.type != PowerUpType.ExtraLife);

        // Nombre 
        var labelGO = new GameObject("Label");
        var labelRT = labelGO.AddComponent<RectTransform>();
        labelRT.SetParent(row.transform, false);
        labelRT.sizeDelta = new Vector2(180f, rowHeight);

        var tmp       = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = NameForType(item.type);
        tmp.fontSize  = fontSize;
        tmp.color     = labelColor;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.fontStyle = FontStyles.Bold;
    }

    static string NameForType(PowerUpType t)
    {
        switch (t)
        {
            case PowerUpType.WeaponNormal:  return "NORMAL";
            case PowerUpType.WeaponSpread:  return "SPREAD";
            case PowerUpType.WeaponLaser:   return "LASER";
            case PowerUpType.WeaponHoming:  return "HOMING";
            case PowerUpType.ExtraLife:     return "EXTRA LIFE";
            case PowerUpType.Shield:        return "SHIELD";
            case PowerUpType.SpeedBoost:    return "SPEED BOOST";
            case PowerUpType.Bomb:          return "BOMB";
            case PowerUpType.OrbitDrones:   return "ORB DRONES";
            default:                        return t.ToString().ToUpper();
        }
    }

    static Color ColorForType(PowerUpType t)
    {
        switch (t)
        {
            case PowerUpType.WeaponNormal:  return new Color(1f, 0.85f, 0.1f);
            case PowerUpType.WeaponSpread:  return new Color(0.2f, 1f, 0.3f);
            case PowerUpType.WeaponLaser:   return new Color(0.3f, 0.85f, 1f);
            case PowerUpType.WeaponHoming:  return new Color(1f,   0.35f, 0.25f);
            case PowerUpType.ExtraLife:     return Color.white;
            case PowerUpType.Shield:        return new Color(0.4f, 0.8f, 1f);
            case PowerUpType.SpeedBoost:    return Color.white;
            case PowerUpType.Bomb:          return new Color(1f,   0.3f, 0.3f);
            case PowerUpType.OrbitDrones:   return Color.white;
            default:                        return Color.white;
        }
    }
}