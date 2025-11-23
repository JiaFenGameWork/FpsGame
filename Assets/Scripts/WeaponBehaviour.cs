using System.Collections;
using UnityEngine;

public class WeaponBehaviour : MonoBehaviour
{
    public Camera playerCamera;
    public bool isShooting, readyToShoot;
    bool allowReset = true;
    public float shootingDelay = 2f;
    public int bulletsPerBurst = 3;
    public int burstBulletLeft;
    public GameObject bulletPrefab;
    public Transform bulletSpawn;
    public float bulletVelocity = 30;
    public float bulletPrefabLifeTime = 3f;

    public enum ShootingMode
    {
        Single,
        Burst,
        Auto
    }

    public ShootingMode CurrentShootingMode;
    
    private void Awake()
    {
        readyToShoot = true;
        burstBulletLeft = bulletsPerBurst;
    }
    // Update is called once per frame
    void Update()
    {
        if (CurrentShootingMode == ShootingMode.Auto)
        {
            isShooting = Input.GetKey(KeyCode.Mouse0);
        }
        else if (CurrentShootingMode == ShootingMode.Single || CurrentShootingMode == ShootingMode.Burst)
        {
            isShooting = Input.GetKeyDown(KeyCode.Mouse0);
        }
        if (readyToShoot && isShooting)
        {
            burstBulletLeft = bulletsPerBurst;
            FireWeapon();
        }
    }

    private void FireWeapon()
    {
        readyToShoot = false;
        Vector3 shootingDirection = CalcuateDirectionAndSpread().normalized;
        GameObject bullet = Instantiate(bulletPrefab, bulletSpawn.position, Quaternion.identity);
        bullet.transform.forward = shootingDirection;
        bullet.GetComponent<Rigidbody>().AddForce(shootingDirection * bulletVelocity, ForceMode.Impulse);
        StartCoroutine(DestroyBulletAfterTime(bullet, bulletPrefabLifeTime));

        if (allowReset)
        {
            Invoke("ResetShot", shootingDelay);
            allowReset = false;
        }

        if (CurrentShootingMode == ShootingMode.Burst && burstBulletLeft > 1)
        {
            burstBulletLeft--;
            Invoke("FireWeapon", shootingDelay);
        }
    }

    private IEnumerator DestroyBulletAfterTime(GameObject bullet, float delay)
    {
        yield return new WaitForSeconds(1f);
        Destroy(bullet);
    }

    private void ResetShot()
    {
        readyToShoot = true;
        allowReset = true;
    }
    
    public Vector3 CalcuateDirectionAndSpread()
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;
        Vector3 targetpoint;
        if (Physics.Raycast(ray, out hit, 1000f))
        {
            targetpoint = hit.point;
        }
        else
        {
            targetpoint = ray.GetPoint(1000f);
        }
        Vector3 direction;
        direction = (targetpoint-bulletSpawn.position).normalized;
       // float x = UnityEngine.Random.Range(-spreadIntensity, spreadIntensity);
       // float y = UnityEngine.Random.Range(-spreadIntensity, spreadIntensity);
        return direction;
    }
}
