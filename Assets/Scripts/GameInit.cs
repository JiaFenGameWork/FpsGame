using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

public class GameInit : MonoBehaviour
{
    public PlayerState playerState;
    AudioManager audioManager;
    public AudioClip audio;
    public GameObject BossGate;
    public EnemyBoss boss;
    public GameObject UI;
    public GameObject Heart;
    // Start is called before the first frame update
    void Start()
    {
        BossGate.SetActive(false);
        audioManager = AudioManager.Instance;
        StartCoroutine(PlayMusic());
        boss.OnBossStart += BossBattle;
        UI.SetActive(false);
    }
    void Update()
    {
        if (playerState.CurrentHealth <= 0)
        {
            Time.timeScale = 0f;
            Debug.Log("GameOver");
            UI.SetActive(true);
            UI.GetComponentInChildren<TextMeshProUGUI>().text = "You Lose";
            Heart.SetActive(false);
            audioManager.StopMusic();
        }
        else if (boss == null)
        {
            Time.timeScale = 0f;
            UI.SetActive(true);
            TextMeshProUGUI[] te = UI.GetComponentsInChildren<TextMeshProUGUI>();
            foreach (var a in te)
            {
               // if ( == "dd") ;
            }
            Heart.SetActive(false);
            audioManager.StopMusic();
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
