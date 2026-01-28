using System;
using UnityEngine;

public class ChangeWeaponBehaviour : MonoBehaviour
{
    public GameObject ShouQiang;
    public GameObject PenZi;
    public GameObject ChongFenQiang;
    public WeaponBehaviour ShouQiangWeaponBehaviour;
    public WeaponBehaviour PenZiWeaponBehaviour;
    public WeaponBehaviour chongFenQiangWeaponBehaviour;

    void Start()
    {
        ShouQiang.SetActive(true);
        PenZi.SetActive(false);
        ChongFenQiang.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            ShouQiang.SetActive(true);
            PenZi.SetActive(false);
            ChongFenQiang.SetActive(false);
            ShouQiangWeaponBehaviour.CurrentShootingMode = WeaponBehaviour.ShootingMode.Single;
        }    
        // else if (Input.GetKeyDown(KeyCode.Alpha2))
        // {
        //     ShouQiang.SetActive(false);
        //     PenZi.SetActive(true);
        //     ChongFenQiang.SetActive(false);
        //     PenZiWeaponBehaviour.CurrentShootingMode = WeaponBehaviour.ShootingMode.Burst;
        // }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            ShouQiang.SetActive(false);
            PenZi.SetActive(false);
            ChongFenQiang.SetActive(true);
            chongFenQiangWeaponBehaviour.CurrentShootingMode = WeaponBehaviour.ShootingMode.Auto;
        }
    }
}
