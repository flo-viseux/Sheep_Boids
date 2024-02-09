﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Implementation of http://www.csc.kth.se/utbildning/kth/kurser/DD143X/dkand13/Group9Petter/report/Martin.Barksten.David.Rydberg.report.pdf
/// </summary>
public class SheepBoid : MonoBehaviour
{
    /// <summary>
    /// Vector created by applying all the herding rules
    /// </summary>
    Vector3 targetVelocity;

    /// <summary>
    /// Actual velocity of the sheep
    /// </summary>
    Vector3 velocity;
    NavMeshAgent agent;
    Transform predator;

    [SerializeField] private float _flightZoneRadius = 7;
    [SerializeField] private float _weightCohesionBase = .5f;
    [SerializeField] private float _weightCohesionFear = 5;
    [SerializeField] private float _weightSeparationBase = 2;
    [SerializeField] private float _weightSeparationFear = 0;
    [SerializeField] private float _alignementZoneRadius = 3;
    [SerializeField] private float _weightAlignmentBase = 0.1f;
    [SerializeField] private float _weightAlignmentFear = 1;
    [SerializeField] private float _weightEscape = 6;
    [SerializeField] private float _weightEnclosement = 10;

    [SerializeField] private bool _debug = true;

    void Start()
    {
        predator = GameObject.FindGameObjectWithTag("Predator").transform;
        agent = GetComponent<NavMeshAgent>();   
    }

    /// <summary>
    /// 3.1
    /// Sigmoid function, used for impact of second multiplier
    /// </summary>
    /// <param name="x">Distance to the predator</param>
    /// <returns>Weight of the rule</returns>
    float P(float x)
    {
        return (1 / Mathf.PI * Mathf.Atan((_flightZoneRadius - x) / .3f) + .5f);
    }

    /// <summary>
    /// 3.2
    /// Combine the two weights affecting the rules
    /// </summary>
    /// <param name="mult1">first multiplier</param>
    /// <param name="mult2">second multipler</param>
    /// <param name="x">distance to the predator</param>
    /// <returns>Combined weights</returns>
    float CombineWeight(float mult1, float mult2, float x)
    {
        return mult1 * (1 + P(x) * mult2);
    }

    /// <summary>
    /// 3.3
    /// In two of the rules, Separation and Escape, nearby objects are prioritized higher than
    ///those further away. This prioritization is described by an inverse square function
    /// </summary>
    /// <param name="x">Distance to the predator</param>
    /// <param name="s">Softness factor</param>
    /// <returns></returns>
    float Inv(float x, float s)
    {
        float value = x / s + Mathf.Epsilon;

        return 1 / (value * value);
    }

    /// <summary>
    /// 3.4
    /// The Cohesion rule is calculated for each sheep s with position sp. The Cohesion vector
    ///coh(s) is directed towards the average position Sp.The rule vector is calculated
    ///with the function
    ///coh(s) = Sp − sp/|Sp − sp|
    /// </summary>
    /// <returns>coh(s) the cohesion vector</returns>
    Vector3 RuleCohesion()
    {
        Vector3 Sp = Vector3.zero;

        foreach (SheepBoid sheep in SheepHerd.Instance.sheeps)
        {
            Sp += sheep.transform.position;
        }

        Sp = Sp / SheepHerd.Instance.sheeps.Length;

        return (Sp - transform.position).normalized;
    }

    /// <summary>
    /// 3.5
    /// The Separation rule is calculated for each sheep s with position sp. The contribution
    ///of each nearby sheep si
    ///is determined by the inverse square function of the distance
    ///between the sheep with a softness factor of 1. This function can be seen in Formula
    ///(3.3). The rule vector is directed away from the sheep and calculated with the
    ///function
    ///sep(s) = sum(n,i)(sp − sip/|sp − sip| * inv(|sp − sip|, 1))
    /// </summary>
    /// <returns>sep(s) the separation vector</returns>
    Vector3 RuleSeparation()
    {
        Vector3 sep = Vector3.zero;

        foreach (SheepBoid sheep in SheepHerd.Instance.sheeps)
        {
            if(sheep != this)
                sep += (transform.position - sheep.transform.position).normalized * (1 / (transform.position - sheep.transform.position).magnitude + Mathf.Epsilon);
        }

        return sep;
    }

    /// <summary>
    /// 3.6
    /// The Alignment rule is calculated for each sheep s. Each sheep si within a radius of
    ///50 pixels has a velocity siv that contributes equally to the final rule vector.The size
    ///of the rule vector is determined by the velocity of all nearby sheep N.The vector is
    ///directed in the average direction of the nearby sheep.The rule vector is calculated
    ///with the function
    ///ali(s) = sum(Siv,N)
    ///where
    ///N = {si: si ∈ S ∩ |sip − sp| ≤ 50}
    /// </summary>
    /// <returns>ali(s) the alignement vector</returns>
    Vector3 RuleAlignment()
    {
        Vector3 p = Vector3.zero;
        int num = 0;
        foreach(var sheep in SheepHerd.Instance.sheeps)
        {
            if(Vector3.Distance(sheep.transform.position, transform.position) <= 3)
            {
                num++;
                p +=  sheep.velocity;
            }
        }
        p /= num;
        return p;
    }

    /// <summary>
    /// 3.8
    /// The Escape rule is calculated for each sheep s with a position sp. The size of the
    ///rule vector is determined by inverse square function(3.3) of the distance between
    ///the sheep and predator p with a softness factor of 10. The rule vector is directed
    ///away from the predator and is calculated with the function
    ///esc(s) = sp − pp / |sp − pp| * inv(|sp − pp|, 10)
    /// </summary>
    /// <returns>esc(s) the escape vector</returns>
    Vector3 RuleEscape() => (transform.position - predator.position).normalized * Inv((transform.position - predator.position).magnitude, 4);

    /// <summary>
    /// 3.9
    /// Get the intended velocity of the sheep by applying all the herding rules
    /// </summary>
    /// <returns>The resulting vector of all the rules</returns>
    Vector3 ApplyRules()
    {
        float x = Vector3.Distance(transform.position, predator.position);
        Vector3 cohesion = CombineWeight(_weightCohesionBase, _weightCohesionFear, x) * RuleCohesion();
        Vector3 separation = CombineWeight(_weightSeparationBase, _weightSeparationFear, x) * RuleSeparation();
        Vector3 alignement = CombineWeight(_weightAlignmentBase, _weightAlignmentFear, x) * RuleAlignment();
        Vector3 enclosement = _weightEnclosement * Pen.Instance.RuleEnclosed(transform.position);
        Vector3 escape = _weightEscape * RuleEscape();
        Vector3 v = cohesion + separation + alignement + escape + enclosement ;
        if(_debug && SheepHerd.Instance._debug)
        {
            Debug.DrawRay(transform.position, cohesion, Color.green);
            Debug.DrawRay(transform.position, separation, Color.white);
            Debug.DrawRay(transform.position, alignement, Color.yellow);
            Debug.DrawRay(transform.position, enclosement, Color.black);
            Debug.DrawRay(transform.position, escape, Color.red);
        }
        return v;
    }

    void Update()
    {
        targetVelocity = ApplyRules();
    }

    #region Move

    /// <summary>
    /// Move the sheep based on the result of the rules
    /// </summary>
    void Move()
    {
        float minVelocity = 0.1f;
        //Max velocity of the sheep
        float maxVelocityBase = 1;
        //Max velocity of the sheep when a predator is close
        float maxVelocityFear = 4;

        float distanceToPredator = (transform.position - predator.position).magnitude;

        //Clamp the velocity to a maximum that depends on the distance to the predator
        float currentMaxVelocity = Mathf.Lerp(maxVelocityBase, maxVelocityFear, 1 - (distanceToPredator / _flightZoneRadius));

        targetVelocity = Vector3.ClampMagnitude(targetVelocity, currentMaxVelocity);

        //Ignore the velocity if it's too small
        if (targetVelocity.magnitude < minVelocity)
            targetVelocity = Vector3.zero;

        //Draw the velocity as a blue line coming from the sheep in the scene view
        if(_debug && SheepHerd.Instance._debug)
        Debug.DrawRay(transform.position, targetVelocity, Color.blue);

        velocity = targetVelocity;

        //Make sure we don't move the sheep verticaly by misstake
        velocity.y = 0;

        //agent.SetDestination(velocity /** Time.deltaTime*/ + transform.position);
        //Move the sheep
        transform.Translate(velocity * Time.deltaTime, Space.World);
    }

    void LateUpdate()
    {
        Move();
    }
    #endregion
}
