using System.Collections.Generic;
using UnityEngine;

public class WheelChairRenderer : MonoBehaviour
{
    public List<Renderer> renderers;

    public void OnOffRenderer(bool isTrue)
    {
        for (int i = 0; i < renderers.Count; i++)
        {
            renderers[i].enabled = isTrue;
        }
    }
}
