using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;

public class DummyScript : MonoBehaviour
{
    static int _moduleIdCounter = 1;
    int _moduleID = 0;

    public KMBombModule Module;
    public KMSelectable Button;

    void Awake()
    {
        _moduleID = _moduleIdCounter++;

        Button.OnInteract += delegate { Module.HandlePass(); return false; };
    }
}
