﻿using DG.Tweening;
using KammBase;
using ScriptableObjectArchitecture;
using Shapes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BSplineDrawer : MonoBehaviour
{
    [SerializeField] private PlayerMover playerMover;

    // doesn't include last part of curve
    [SerializeField] private int numSamplePointsPerCurve = 10;

    [SerializeField] private Polyline polyLine;

    [SerializeField] private float initPLThickness;

    [SerializeField] private float onPlayerHitPLThickness;

    [SerializeField] private float onPlayerHitTweenTime;

    private int numTotalPoints;

    [SerializeField] private List<Vector2> sPoints;
    [SerializeField] private List<Vector2> pPoints;
    private List<Vector2> prevFrameSPoints;
    private List<Vector2> prevFramePPoints;
    [SerializeField] private BSplinePointGenerator bsPointGen;

    [SerializeField] private GameEvent bSplineCompletedEvent;

    [SerializeField] private GameEvent splineThickeningStartedEvent;

    [SerializeField] private GameEvent splineThickeningEndedEvent;

    [SerializeField] private GameEvent splineAlreadyFineEvent;



    [Range(0, 1)]
    [SerializeField] private float startingTrailOpacity;

    [SerializeField] private ColorPalette colorPalette;
    
    private bool pointBeingDragged;

    private Option<int> curveRegenStartOpt = Option<int>.None;
    private Option<int> sPointRegenStartOpt = Option<int>.None;
    private bool currentlyThickening;

    // Start is called before the first frame update
    void Awake()
    {
        polyLine.Thickness = initPLThickness;

        sPoints = new List<Vector2>();
        pPoints = new List<Vector2>();

        prevFrameSPoints = new List<Vector2>();
        prevFramePPoints = new List<Vector2>();

    }

    private void GeneratePolyline()
    {
        for (var i = 0; i < sPoints.Count; i++)
        {
            SetCurvePointsAt(i);
        }
    }

    private void GenerateSPoints()
    {
        for (var i = 0; i < sPoints.Count; i++)
        {
            GenerateSPointAt(i);
        }
    }

    private void GenerateSPointAt(int i)
    {
        var p0 = i == 0 ? pPoints.Count - 1 : 2 * i - 1;
        var p1 = 2 * i;

        prevFrameSPoints[i] = sPoints[i];

        sPoints[i] = Vector2.Lerp(pPoints[p0], pPoints[p1], 0.5f);
    }

    private void GeneratePPoints(List<BSplinePoint> bsPoints)
    {
        for (var i = 0; i < bsPoints.Count; i++)
        {
            GeneratePPointsAt(i, bsPoints);
        }
    }

    private void GeneratePPointsAt(int i, List<BSplinePoint> bsPoints)
    {
        var bCur = bsPoints[i].transform.position;
        var bNext = i == bsPoints.Count - 1
            ? bsPoints[0].transform.position
            : bsPoints[i + 1].transform.position;

        prevFramePPoints[2 * i] = pPoints[2 * i];
        prevFramePPoints[2 * i + 1] = pPoints[2 * i + 1];


        pPoints[2 * i] = Vector2.Lerp(bCur, bNext, 1f / 3);
        pPoints[2 * i + 1] = Vector2.Lerp(bCur, bNext, 2f / 3);
    }

    public void OnBSPointsGenerated()
    {
        GenerateBSpline(bsPointGen.bSplinePoints);
    }

    public void OnBSPointIDragStarted(int idx)
    {
        curveRegenStartOpt = MathUtil.mod(idx - 2, bsPointGen.bSplinePoints.Count);


        sPointRegenStartOpt = MathUtil.mod(idx - 1, bsPointGen.bSplinePoints.Count);

        pointBeingDragged = true;
    }

    public void OnBSPointIDragEnded(int idx)
    {
        pointBeingDragged = false;

        curveRegenStartOpt = Option<int>.None;
        sPointRegenStartOpt = Option<int>.None;
    }

    private void GenerateBSpline(List<BSplinePoint> bSplinePoints)
    {
        polyLine.SetPoints(new List<Vector3>());

        //numTotalPoints = (numSamplePointsPerCurve * bSplinePoints.Count);
        numTotalPoints = (numSamplePointsPerCurve * bSplinePoints.Count) + 1;

        for (var i = 0; i < numTotalPoints; i++)
        {
            polyLine.AddPoint(new Vector3(0, 0));

            /*
            var color = Color.Lerp(
                colorPalette.TrailStart,
                colorPalette.TrailEnd,
                ((float)i) / numTotalPoints);*/

            var t = ((float)i) / numTotalPoints;

            polyLine.SetPointColor(i, colorPalette.GetColorAtT(t));

            //polyLine.SetPointColor(i, color);

            //polyLine.SetPointColor(i, new Color(1, 1, 1, (1 - ((float)i) / numTotalPoints)) * startingTrailOpacity);
        }

        sPoints = new List<Vector2>();
        pPoints = new List<Vector2>();

        prevFrameSPoints = new List<Vector2>();
        prevFramePPoints = new List<Vector2>();
        foreach (var _ in bSplinePoints)
        {
            sPoints.Add(Vector2.zero);
            pPoints.Add(Vector2.zero);
            pPoints.Add(Vector2.zero);

            prevFrameSPoints.Add(Vector2.zero);
            prevFramePPoints.Add(Vector2.zero);
            prevFramePPoints.Add(Vector2.zero);
        }

        GeneratePPoints(bSplinePoints);
        GenerateSPoints();

        GeneratePolyline();

        MakePlayerAndPolyThick();
        bSplineCompletedEvent.Raise();
    }

    // Update is called once per frame
    void Update()
    {
        UpdatePrevFramePoints();


        if (pointBeingDragged)
        {
            UpdatePointRange();
        }


    }

    private void UpdatePrevFramePoints()
    {
        if (pPoints.Count != prevFramePPoints.Count
            || sPoints.Count != prevFrameSPoints.Count)
        {
            throw new Exception("prevframe and curframe point lists are unsynced wtf");
        }

        for (var i = 0; i < pPoints.Count; i++)
        {
            prevFramePPoints[i] = pPoints[i];
        }

        for (var i = 0; i < sPoints.Count; i++)
        {
            prevFrameSPoints[i] = sPoints[i];
        }
    }

    private void UpdatePointRange()
    {
        if (!curveRegenStartOpt.HasValue || !sPointRegenStartOpt.HasValue)
        {
            throw new Exception("we should have values while point is being dragged");
        }

        var curveRegenStart = curveRegenStartOpt.Value;

        var sPointStart = sPointRegenStartOpt.Value;

        // update previous, current and next PPoints
        for (var i = 0; i < 3; i++)
        {
            var curPPointIdx = MathUtil.mod(sPointStart + i, bsPointGen.bSplinePoints.Count);

            GeneratePPointsAt(curPPointIdx, bsPointGen.bSplinePoints);
        }

        // update previous, current and next SPoints
        for (var i = 0; i < 3; i++)
        {
            var curSPointIdx = MathUtil.mod(sPointStart + i, bsPointGen.bSplinePoints.Count);

            GenerateSPointAt(curSPointIdx);
        }


        // update viz curves
        for (var ci = 0; ci < 5; ci++)
        {
            var curCurveIdx = MathUtil.mod(curveRegenStart + ci, bsPointGen.bSplinePoints.Count);
            SetCurvePointsAt(curCurveIdx);
        }
    }

    private void SetCurvePointsAt(int idx)
    {

        var pInitial = sPoints[idx];
        var pFinal = idx == sPoints.Count - 1
            ? sPoints[0]
            : sPoints[idx + 1];

        var pControl1 = pPoints[2 * idx];
        var pControl2 = pPoints[2 * idx + 1];

        for (var s = 0; s < numSamplePointsPerCurve; s++)
        {
            var t = ((float)s) / numSamplePointsPerCurve;


            var bT = Mathf.Pow(1 - t, 3) * pInitial
                + 3 * Mathf.Pow(1 - t, 2) * t * pControl1
                + 3 * (1 - t) * t * t * pControl2
                + t * t * t * pFinal;

            polyLine.SetPointPosition(numSamplePointsPerCurve * idx + s, bT);
        }

        // close da loop yo
        if (idx == sPoints.Count - 1)
        {
            polyLine.SetPointPosition(numTotalPoints - 1, pFinal);
        }
    }

    private Vector2 GetPointAtTForSpline(
        float t, List<Vector2> sPointsToUse, List<Vector2> pPointsToUse)
    {
        t = Mathf.Clamp(t, 0, 1);

        var curveToUse = (int)(bsPointGen.bSplinePoints.Count * t);

        var curveTStart = (curveToUse + 0f) / bsPointGen.bSplinePoints.Count;
        var curveTEnd = (curveToUse + 1f) / bsPointGen.bSplinePoints.Count;

        var localT = (t - curveTStart) / (curveTEnd - curveTStart);

        // duplicated code... consider not doing that...
        var pInitial = sPointsToUse[curveToUse];
        var pFinal = curveToUse == sPointsToUse.Count - 1
            ? sPointsToUse[0]
            : sPointsToUse[curveToUse + 1];

        var pControl1 = pPointsToUse[2 * curveToUse];
        var pControl2 = pPointsToUse[2 * curveToUse + 1];

        var point = Mathf.Pow(1 - localT, 3) * pInitial
                + 3 * Mathf.Pow(1 - localT, 2) * localT * pControl1
                + 3 * (1 - localT) * localT * localT * pControl2
                + localT * localT * localT * pFinal;

        return point;
    }

    public Vector2 GetPointAtTPrevFrame(float t)
    {
        return GetPointAtTForSpline(t, prevFrameSPoints, prevFramePPoints);
    }

    public Vector2 GetPointAtT(float t)
    {
        return GetPointAtTForSpline(t, sPoints, pPoints);
    }

    /*
    public Vector2 GetPointAtT(float t)
    {
        t = Mathf.Clamp(t, 0, 1);

        var curveToUse = (int)(bsPointGen.bSplinePoints.Count * t);

        var curveTStart = (curveToUse + 0f) / bsPointGen.bSplinePoints.Count;
        var curveTEnd = (curveToUse + 1f) / bsPointGen.bSplinePoints.Count;

        var localT = (t - curveTStart) / (curveTEnd - curveTStart);
        

        // duplicated code... consider not doing that...
        var pInitial = sPoints[curveToUse];
        var pFinal = curveToUse == sPoints.Count - 1
            ? sPoints[0]
            : sPoints[curveToUse + 1];

        var pControl1 = pPoints[2 * curveToUse];
        var pControl2 = pPoints[2 * curveToUse + 1];

        var point = Mathf.Pow(1 - localT, 3) * pInitial
                + 3 * Mathf.Pow(1 - localT, 2) * localT * pControl1
                + 3 * (1 - localT) * localT * localT * pControl2
                + localT * localT * localT * pFinal;

        return point;
    }
    */

    public void OnPlayerHitTarget()
    {
        if (currentlyThickening)
        {
            return;
        }

        currentlyThickening = true;

        polyLine.Thickness = initPLThickness;
        DOTween.To(
                () => polyLine.Thickness,
                x => polyLine.Thickness = x,
                onPlayerHitPLThickness,
                onPlayerHitTweenTime)
            .SetLoops(2, LoopType.Yoyo)
            .SetEase(Ease.OutQuad)
            .OnComplete(() => currentlyThickening = false);
    }


    public void OnNoUpdateNecessary()
    {
        splineAlreadyFineEvent.Raise();

        //MakePlayerAndPolyThick();
    }

    private void MakePlayerAndPolyThick()
    {
        // polylinebegun stuff

        splineThickeningStartedEvent.Raise();

        polyLine.Thickness = 0f;
        DOTween.To(
                () => polyLine.Thickness,
                x => polyLine.Thickness = x,
                initPLThickness,
                .75f)
            .OnComplete(() => splineThickeningEndedEvent.Raise());

        playerMover.MakeThick(0.75f);
    }

    public void MakePlayerAndPolySkinny()
    {
        DOTween.To(
                () => polyLine.Thickness,
                x => polyLine.Thickness = x,
                0,
                .75f);

        playerMover.MakeSkinny(0.75f);
    }
}
