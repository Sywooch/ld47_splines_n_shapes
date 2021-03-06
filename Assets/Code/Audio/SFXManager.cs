﻿using KammBase;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms;

public class SFXManager : MonoBehaviour
{
    [SerializeField] private AudioSource aSrcCpMouseDown;
    [SerializeField] private AudioSource aSrcCpMouseUp;

    [SerializeField] private AudioSource aSrcLoopComplete;

    [SerializeField] private AudioClip cpMouseClip;

    [SerializeField] private AudioClip loopValidClip;

    [SerializeField] private AudioClip loopInvalidClip;


    //ugh
    [SerializeField] private BSplinePointGenerator bSplinePointGenerator;
    private int totalNumControlPoints => bSplinePointGenerator.bSplinePoints.Count;

    [SerializeField]
    private List<int> cpSemitones = new List<int> { 0, 2, 4, 5, 7 };
    private int semitoneDiffCpMouseDown = -1;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnCpMouseDown(int index)
    {
        var indexToUse = Mathf.Clamp(index, 0, totalNumControlPoints);
        if (indexToUse != index)
        {
            Debug.Log("wtf mate. index is out of bounds. OnCpMouseDown");
        }

        //var semitoneDiff = GetCPSemitoneDiff(indexToUse, totalNumControlPoints);
        semitoneDiffCpMouseDown = GetCPSemitoneDiff(semitoneDiffCpMouseDown);


        aSrcCpMouseDown.pitch = Mathf.Pow(1.05946f, semitoneDiffCpMouseDown);

        aSrcCpMouseDown.PlayOneShot(cpMouseClip);
    }

    public void OnCpMouseUp(int index)
    {
        var indexToUse = Mathf.Clamp(index, 0, totalNumControlPoints);
        if (indexToUse != index)
        {
            Debug.Log("wtf mate. index is out of bounds. OnCpMouseUp");
        }

        // lets make a P5
        var semitonDiffMouseUp = semitoneDiffCpMouseDown - 7;
        //var semitoneDiff = GetCPSemitoneDiff(indexToUse, totalNumControlPoints) - 7;

        aSrcCpMouseUp.pitch = Mathf.Pow(1.05946f, semitonDiffMouseUp);

        aSrcCpMouseUp.PlayOneShot(cpMouseClip);
    }

    private int GetCPSemitoneDiff(int prevSemitoneDiff)
    {
        var idxToUse = UnityEngine.Random.Range(0, cpSemitones.Count);
        
        
        if (cpSemitones[idxToUse] == prevSemitoneDiff)
        {
            var idxDiff = UnityEngine.Random.value > 0.5f ? 1 : -1;

            idxToUse = MathUtil.mod(idxToUse + idxDiff, cpSemitones.Count);
                //(idxToUse + 1) % cpSemitones.Count;
        }

        return cpSemitones[idxToUse];
        // low stuff betta

        /*
        switch (totalNumPoints)
        {
            case 0:
                throw new Exception("there should be at least one point cmoonnnn");
            case 1:
                return 0;
            case 2:
                return cpTwoPointSemis[index];
            case 3:
                return cpThreePointSemis[index];
            case 4:
                return cpFourPointSemis[index];
            case 5:
                return cpFivePointSemis[index];
            default:
                var offset = (index >= cpFivePointSemis.Count) ? 12 : 0;
                return cpFivePointSemis[index % cpFivePointSemis.Count] + offset;
        }
        */
    }

    public void PlayLoopValidAt(int semiToneDiff)
    {
        aSrcLoopComplete.pitch = Mathf.Pow(1.05946f, semiToneDiff);

        aSrcLoopComplete.PlayOneShot(loopValidClip);
    }

    public void PlayEnoughLoopsPassed()
    {
        aSrcLoopComplete.pitch = Mathf.Pow(1.05946f, 0);

        aSrcLoopComplete.PlayOneShot(loopInvalidClip);
    }
}
