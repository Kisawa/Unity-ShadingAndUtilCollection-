using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

public class MaterialDataCache : ScriptableObject
{
    public Material material;
    public List<string> rampNames = new List<string>();
    public List<Gradient> rampGradients = new List<Gradient>();

    public Gradient GetRampGradient(MaterialProperty prop)
    {
        Gradient rampGradient;
        int index = rampNames.IndexOf(prop.name);
        if (index == -1)
        {
            rampNames.Add(prop.name);
            rampGradient = new Gradient();
            rampGradients.Add(rampGradient);
        }
        else
            rampGradient = rampGradients[index];
        return rampGradient;
    }

    public void SetRampGradient(MaterialProperty prop, Gradient gradient)
    {
        int index = rampNames.IndexOf(prop.name);
        if (index == -1)
        {
            rampNames.Add(prop.name);
            rampGradients.Add(gradient);
        }
        else
            rampGradients[index] = gradient;
    }
}