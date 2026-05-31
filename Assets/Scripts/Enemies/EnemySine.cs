using UnityEngine;

public class EnemySine : EnemyBase
{
    [Header("Onda")]
    public float amplitude = 1f;
    public float frequency = 2f;

    float _startY;
    float _time;

    protected override void OnEnable()
    {
        base.OnEnable();
        // Clampea el centro de la onda para que en el peor caso (amplitud máxima)
        // el enemigo nunca sobrepase los límites del scroll/hull.
        _startY = Mathf.Clamp(transform.position.y, _minY + amplitude, _maxY - amplitude);
        _time   = 0f;
    }

    protected override void Update()
    {
        _time += Time.deltaTime;

        float x = transform.position.x - moveSpeed * Time.deltaTime;
        float y = _startY + Mathf.Sin(_time * frequency) * amplitude;

        transform.position = new Vector3(x, y, 0f);

        base.Update();
    }
}
