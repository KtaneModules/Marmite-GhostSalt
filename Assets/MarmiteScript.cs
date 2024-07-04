using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;

public class MarmiteScript : MonoBehaviour
{
    static List<int> _marmiteAdditives = new List<int>();
    static int _marmiteHighestID = 0;
    static bool _marmiteSpecial = false;

    static int _moduleIdCounter = 1;
    int _moduleID = 0;

    public KMBombModule Module;
    public KMBombInfo Bomb;
    public KMAudio Audio;
    public KMBossModule Boss;
    public MeshRenderer ModuleBGRend;
    public KMSelectable Button;
    public SpriteRenderer MarmiteRend;
    public Sprite[] Sprites;
    public TextMesh MarmiteText;
    public TextMesh AdditionRend;
    public static string[] IgnoredModules = null;

    private KMAudio.KMAudioRef Sound;
    private Coroutine MarmiteTextAnimCoroutine;
    private int Addition, SolveCache;
    private bool Autosolve, Focused, Solved, Success;

    private bool IsPrime(int number)
    {
        if (number <= 1) return false;
        if (number == 2) return true;

        var limit = Mathf.CeilToInt(Mathf.Sqrt(number));

        for (int i = 2; i <= limit; ++i)
            if (number % i == 0)
                return false;

        return true;
    }

    void Awake()
    {
        _moduleID = _moduleIdCounter++;
        if (_moduleID > _marmiteHighestID)
            _marmiteHighestID = _moduleID;
        if (_marmiteAdditives.Count() > 0)
            _marmiteAdditives = new List<int>();
        _marmiteSpecial = false;

        Button.OnInteract += delegate { MarmitePress(); return false; };
        Button.OnHighlight += delegate { if (Focused) MarmiteRend.sprite = Sprites[1]; };
        Button.OnHighlightEnded += delegate { MarmiteRend.sprite = Sprites[0]; };

        Module.GetComponent<KMSelectable>().OnFocus += delegate { Focused = true; };
        Module.GetComponent<KMSelectable>().OnDefocus += delegate { Focused = false; };
        Module.OnActivate += delegate
        {
            ModuleBGRend.material.color = Color.white;
            MarmiteRend.transform.localScale = Vector3.one;
            AdditionRend.transform.localScale = Vector3.one;

            if (_moduleID == _marmiteHighestID)
                Audio.PlaySoundAtTransform("intro", Button.transform);
        };

        MarmiteText.transform.localScale = Vector3.zero;
        AdditionRend.transform.localScale = Vector3.zero;
        MarmiteRend.transform.localScale = Vector3.zero;

        ModuleBGRend.material.color = Color.black;

        StartCoroutine(SolveCheck());

        if (IgnoredModules == null)
            IgnoredModules = GetComponent<KMBossModule>().GetIgnoredModules("Marmite", new string[]{
                "14",
                "8",
                "Forget Enigma",
                "Forget Everything",
                "Forget It Not",
                "Forget Me Later",
                "Forget Me Not",
                "Forget Perspective",
                "Forget Them All",
                "Forget This",
                "Forget Us Not",
                "Marmite",
                "Organization",
                "Purgatory",
                "Simon's Stages",
                "Souvenir",
                "Tallordered Keys",
                "The Time Keeper",
                "Timing is Everything",
                "The Troll",
                "Turn The Key",
                "Übermodule",
                "Ültimate Custom Night",
                "The Very Annoying Button"
            });
    }

    // Use this for initialization
    void Start ()
    {
        if (Bomb.GetSolvableModuleNames().Where(x => x == "Marmite").Count() > 1)
        {
            var count = Bomb.GetSolvableModuleNames().Where(x => x == "Marmite").Count();
            Debug.LogFormat("[Marmite #{0}] There {1} other Marmite{2} on the bomb. Hello, fellow Marmite{2}!", _moduleID, count == 2 ? "is 1" : "are " + (count - 1).ToString(), count == 2 ? "" : "s");
            if (_moduleID == _marmiteHighestID)
            {
                _marmiteAdditives = Enumerable.Range(0, count).ToList().Shuffle().ToList();
                Addition = _marmiteAdditives.First();
                _marmiteAdditives.RemoveAt(0);

                if (Addition != 0)
                {
                    AdditionRend.text = "+" + Addition.ToString();
                    Debug.LogFormat("[Marmite #{0}] To spice things up a bit, my additive is {1}!", _moduleID, AdditionRend.text);
                }
                else
                {
                    AdditionRend.text = "±0";
                    Debug.LogFormat("[Marmite #{0}] Although I'm not the only Marmite, I have an additive of ±0.", _moduleID);
                }
            }
            else
                StartCoroutine(CheckIfAdditivesReady());
        }
        else
        {
            AdditionRend.text = "";
            Debug.LogFormat("[Marmite #{0}] There are no other Marmites on this bomb. So lonely. That means that my additive is ±0.", _moduleID);
        }
    }

    private IEnumerator CheckIfAdditivesReady() //This whole complicated thing is just in case some Start()s are called before others. Wanna be sure this doesn't fall on its face. It's Marmite, after all.
    {
        while (_marmiteAdditives.Count() == 0)
            yield return null;
        Addition = _marmiteAdditives.First();
        _marmiteAdditives.RemoveAt(0);
        if (Addition != 0)
        {
            AdditionRend.text = "+" + Addition.ToString();
            Debug.LogFormat("[Marmite #{0}] To spice things up a bit, my additive is {1}!", _moduleID, AdditionRend.text);
        }
        else
        {
            AdditionRend.text = "±0";
            Debug.LogFormat("[Marmite #{0}] Although I'm not the only Marmite, I have an additive of ±0.", _moduleID);
        }
    }
	
	// Update is called once per frame
	void Update () {
		
	}

    void MarmitePress()
    {
        Button.AddInteractionPunch();
        if (Sound != null)
            Sound.StopSound();

        if (Bomb.GetSolvedModuleIDs().Where(x => !IgnoredModules.Contains(x)).Count() >= Bomb.GetSolvableModuleNames().Where(x => !IgnoredModules.Contains(x)).Count() && !Solved)
        {
            Success = true;
            Module.HandlePass();
            if (Bomb.GetSolvableModuleNames().Where(x => x == "Marmite").Count() == Bomb.GetSolvedModuleNames().Where(x => x == "Marmite").Count())
            {
                Sound = Audio.HandlePlaySoundAtTransformWithRef("solve", Button.transform, false);
                _marmiteSpecial = true;

                if (Autosolve)
                    Debug.LogFormat("[Marmite #{0}] Autosolved.", _moduleID);
                else if (Bomb.GetSolvableModuleNames().Where(x => x == "Marmite").Count() > 1)
                    Debug.LogFormat("[Marmite #{0}] Aaaaaand that's all of us Marmites solved! Woohoo!! :D", _moduleID);
                else
                    Debug.LogFormat("[Marmite #{0}] Aaaaaand solved! Good job! :D", _moduleID);
            }
            else
            {
                if (Autosolve)
                    Debug.LogFormat("[Marmite #{0}] Autosolved.", _moduleID);
                else
                    Debug.LogFormat("[Marmite #{0}] Awesome! I'm now solved. Just gonna wait for the others. Good job! :D", _moduleID);
            }
            Solved = true;
        }
        else if (_marmiteSpecial)
        {
            Sound = Audio.HandlePlaySoundAtTransformWithRef("special", Button.transform, false);
            if (MarmiteTextAnimCoroutine != null)
                StopCoroutine(MarmiteTextAnimCoroutine);
            MarmiteTextAnimCoroutine = StartCoroutine(MarmiteTextAnim());
            _marmiteSpecial = false;
        }
        else if (IsPrime(Bomb.GetSolvedModuleIDs().Count() + Addition) || Solved)
        {
            Success = true;
            Sound = Audio.HandlePlaySoundAtTransformWithRef("press", Button.transform, false);
            if (MarmiteTextAnimCoroutine != null)
                StopCoroutine(MarmiteTextAnimCoroutine);
            MarmiteTextAnimCoroutine = StartCoroutine(MarmiteTextAnim());
        }
        else
        {
            Module.HandleStrike();
            Sound = Audio.HandlePlaySoundAtTransformWithRef("strike", Button.transform, false);
            Debug.LogFormat("[Marmite #{0}] You pressed me when there were {1} solves! Nooooooooo! Strike!", _moduleID, SolveCache);
        }
    }

    private IEnumerator SolveCheck()
    {
        while (true)
        {
            yield return null;
            if (SolveCache != Bomb.GetSolvedModuleNames().Where(x => !IgnoredModules.Contains(x)).Count())
            {
                if (IsPrime(SolveCache + Addition) && !Success && !Solved && !Autosolve)
                {
                    Module.HandleStrike();
                    Sound = Audio.HandlePlaySoundAtTransformWithRef("strike", Button.transform, false);
                    Debug.LogFormat("[Marmite #{0}] You forgot to press me when there were {1} solves! Nooooooooo! Strike!", _moduleID, SolveCache);
                }
                Success = false;
                SolveCache = Bomb.GetSolvedModuleNames().Where(x => !IgnoredModules.Contains(x)).Count();
            }
        }
    }

    private IEnumerator MarmiteTextAnim(float duration = .75f)
    {
        float timer = 0;
        MarmiteText.transform.localScale = Vector3.one;
        while (timer < duration)
        {
            yield return null;
            timer += Time.deltaTime;
        }
        MarmiteText.transform.localScale = Vector3.zero;
    }

#pragma warning disable 414
    private string TwitchHelpMessage = "Use '!{0} press' to press the Marmite.";
#pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.ToLowerInvariant();
        if (command != "press")
        {
            yield return "sendtochaterror Invalid command.";
            yield break;
        }
        yield return null;
        Button.OnInteract();
    }
    IEnumerator TwitchHandleForcedSolve()
    {
        Autosolve = true;
        Debug.LogFormat("[Marmite #{0}] Autosolving...", _moduleID);
        while (Bomb.GetSolvedModuleIDs().Where(x => !IgnoredModules.Contains(x)).Count() < Bomb.GetSolvableModuleNames().Where(x => !IgnoredModules.Contains(x)).Count())
            yield return true;
        Button.OnInteract();
    }
}
