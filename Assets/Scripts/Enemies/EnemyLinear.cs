using UnityEngine;

public class EnemyLinear : EnemyBase
{
    protected override void Update()
    {
        transform.Translate(Vector2.left * moveSpeed * Time.deltaTime);
        base.Update();
    }
}
