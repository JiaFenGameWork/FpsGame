using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class PlayerState : MonoBehaviour
{
    public int max_hp = 10;
    public int current_hp;
    public List<Image> images;
    void Start()
    {
        this.GetComponents<Image>();
        current_hp = max_hp;
    }

    // Update is called once per frame
    void Update()
    {
        float h = (float)current_hp / max_hp;
        HealthReduce(images.Count-1,h);
    }

    void Gameover()
    {
        
    }
    public void HealthReduce(int index,float value)
    {
        if (value <= 0f)
        {
            Gameover();
            return;
        }
        if (index < 0)
        {
            return;
        }
        float a = (float)(index) /images.Count;
        if (value< a)
        {
            images[index].enabled = false;
            HealthReduce(index-1,value);
        }
        else
        {
            for (int i = 0; i < index; i++)
            {
                images[i].enabled = true;
            }

        }
    }
    
}
