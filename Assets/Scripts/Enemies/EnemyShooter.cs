using UnityEngine;

public class EnemyShooter : EnemyBase
{
    [Header("Disparo")]
    public float fireRate       = 2f;
    public float firstShotDelay = 0.8f;

    float _fireTimer;

    protected override void OnEnable()
    {
        base.OnEnable();
        _fireTimer = -firstShotDelay;
    }

    protected override void Update()
    {
        transform.Translate(Vector2.left * moveSpeed * Time.deltaTime);

        _fireTimer += Time.deltaTime;
        if (_fireTimer >= fireRate)
        {
            _fireTimer = 0f;
            ShootAtPlayer();
        }

        base.Update();
    }
}
