using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameInit : MonoBehaviour
{
    public PlayerState playerState;
    AudioManager audioManager;
    public AudioClip audio;
    public GameObject BossGate;
    public EnemyBoss boss;
    public GameObject UI;
    public GameObject Heart;
    private TextMeshProUGUI[] text;
    // Start is called before the first frame update
    void Start()
    {
        BossGate.SetActive(false);
        audioManager = AudioManager.Instance;
        StartCoroutine(PlayMusic());
        boss.OnBossStart += BossBattle;
        UI.SetActive(false);
        text = UI.GetComponentsInChildren<TextMeshProUGUI>();
    }
    void Update()
    {
        if (playerState.CurrentHealth <= 0)
        {
            Time.timeScale = 0f;
            Debug.Log("GameOver");
            UI.SetActive(true);
            foreach (var val in text)
            {
                if (val.name == "text")
                {
                    val.text = "你输了";
                    val.color = Color.red;
                    break;
                }
            }
            Heart.SetActive(false);
            audioManager.StopMusic();
        }
        else if (boss == null)
        {
            Time.timeScale = 0f;
            UI.SetActive(true);
            foreach (var val in text)
            {
                if (val.name == "text")
                {
                    val.text = "你赢了";
                    val.color = Color.green;
                    break;
                }
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
