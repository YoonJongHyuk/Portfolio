using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mediapipe_CreateBrush_Trigger : MonoBehaviour
{
    public GameObject brushPrefab;

    public MediapipeThirdManager mediapipeThirdManager;

    private void Start()
    {
        if (mediapipeThirdManager == null) mediapipeThirdManager = GameObject.FindAnyObjectByType<MediapipeThirdManager>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("HandImage") && !mediapipeThirdManager.createBrushTrue)
        {
            mediapipeThirdManager.createBrushTrue = true;
            GameObject hand = collision.gameObject;
            GameObject go = Instantiate(brushPrefab, hand.transform);
            mediapipeThirdManager.BrushObject = go;
            mediapipeThirdManager.brushTriggerEvent = go.GetComponent<BrushTriggerEvent>();
        }
    }
}
