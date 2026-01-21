using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameInit : MonoBehaviour
{
    AudioManager audioManager;
    public AudioClip audio;
    public GameObject BossGate;
    public EnemyBoss boss;
    // Start is called before the first frame update
    void Start()
    {
        BossGate.SetActive(false);
        audioManager = AudioManager.Instance;
        StartCoroutine(PlayMusic());
        boss.OnBossStart += BossBattle;

    }
    void Update()
    {
        
    }
    void BossBattle()
    {
        Debug.Log("BossBattle");
        BossGate.SetActive(true);
        AudioClip audio = Resources.Load<AudioClip>("Sound/BossBattle");
        audioManager.PlayMusic(audio,0.4f,2f);

    }
    IEnumerator PlayMusic()
    {
        yield return new WaitForSeconds(1f);
        audioManager.PlayMusic(audio,0.4f,2f);
    }

}
