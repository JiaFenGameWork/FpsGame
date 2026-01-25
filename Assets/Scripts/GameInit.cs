using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameInit : MonoBehaviour
{
    public PlayerState playerState;
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
        if (playerState.CurrentHealth <= 0)
        {
            Time.timeScale = 0f;
            Debug.Log("GameOver");
        }
    }
    void BossBattle()
    {
        Debug.Log("BossBattle");
        BossGate.SetActive(true);
        AudioClip audio = Resources.Load<AudioClip>("Sound/BossBattle");
        audioManager.PlayMusic(audio,1f,2f);

    }
    IEnumerator PlayMusic()
    {
        yield return new WaitForSeconds(1f);
        audioManager.PlayMusic(audio,0.4f,2f);
    }

}
