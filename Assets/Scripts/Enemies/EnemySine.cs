using UnityEngine;

public class EnemySine : EnemyBase
{
    [Header("Onda")]
    public float amplitude = 2f;
    public float frequency = 2f;

    float _startY;
    float _time;

    protected override void OnEnable()
    {
        base.OnEnable();
        _startY = transform.position.y;
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
