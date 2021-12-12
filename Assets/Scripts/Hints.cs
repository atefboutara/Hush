using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Hints : MonoBehaviour
{
    public static Hints Instance;
    public string[] SlasherHint;
    public string[] SurvivorHint;
    Text _text;
    float curTime = 0f;
    float timer = 3f;
    private void Start()
    {
        if (Instance == null)
            Instance = this;
        _text = GetComponent<Text>();
        if (HushManager.Instance.Slasher)
        {
            int r = Random.Range(0, SlasherHint.Length - 1);
            _text.text = SlasherHint[r];
        }
        else
        {
            int r = Random.Range(0, SurvivorHint.Length - 1);
            _text.text = SurvivorHint[r];
        }
        transform.parent.GetComponent<Slider>().value = 0.2f;
        StartCoroutine(ReadyLoading());
    }

    IEnumerator ReadyLoading()
    {
        yield return new WaitForSeconds(2f);
        HushNetwork.Instance.SetReadyLoading();
    }

    private void Update()
    {
        curTime += Time.deltaTime;
        if(curTime >= timer)
        {
            if (HushManager.Instance.Slasher)
            {
                int r = Random.Range(0, SlasherHint.Length - 1);
                _text.text = SlasherHint[r];
            }
            else
            {
                int r = Random.Range(0, SurvivorHint.Length - 1);
                _text.text = SurvivorHint[r];
            }
            curTime = 0;
        }
    }

    public void AddLoadingPercentage()
    {
        transform.parent.GetComponent<Slider>().value += 0.2f;
        StartCoroutine(checkLoadingCompleted());
    }

    IEnumerator checkLoadingCompleted()
    {
        yield return new WaitForSeconds(0.4f);
        float value = transform.parent.GetComponent<Slider>().value;
        if (value >= 1.0f)
        {
            transform.parent.parent.gameObject.SetActive(false);
            HushNetwork.Instance.AskToSpawn();
        }
    }
}
