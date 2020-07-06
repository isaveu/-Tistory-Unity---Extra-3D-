﻿using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class Enemy : LivingEntity
{
    // 상태 (기본, 추격, 공격)
    public enum State{Idle, Chasing, Attacking};
    State currentState; // 현재 상태

    NavMeshAgent pathfinder;
    Transform target;
    LivingEntity targetEntity;
    Material skinMaterial;
    Color originColor;

    float attackDistanceThreshold = 0.5f; // 공격 사정거리
    float timeBetweenAttacks = 1; // 공격 딜레이
    float damage = 1; // 공격 데미지

    float nextAttackTime; // 다음 공격이 가능한 시간
    float myCollisionRadius; // 자신의 충돌 범위
    float targetCollisionRadius; // 목표의 충돌 범위

    bool hasTarget; // 공격 타겟의 여부

    protected override void Start()
    {
        base.Start();
        pathfinder = GetComponent<NavMeshAgent>();
        skinMaterial = GetComponent<Renderer>().material;
        originColor = skinMaterial.color;

        if (GameObject.FindGameObjectWithTag("Player") != null)
        {
            currentState = State.Chasing;
            hasTarget = true;
            target = GameObject.FindGameObjectWithTag("Player").transform;
            targetEntity = target.GetComponent<LivingEntity>();
            targetEntity.OnDeath += OnTargetDeath;

            myCollisionRadius = GetComponent<CapsuleCollider>().radius;
            targetCollisionRadius = target.GetComponent<CapsuleCollider>().radius;
            StartCoroutine(UpdatePath());
        }
    }
    // 타겟 사망시 정지
    void OnTargetDeath()
    {
        hasTarget = false;
        currentState = State.Idle;
    }
    void Update()
    {
        if (hasTarget)
        {
            if (Time.time > nextAttackTime)
            {
                // (목표 위치 - 자신의 위치) 제곱을 한 수
                float sqrDstToTarget = (target.position - transform.position).sqrMagnitude;

                if (sqrDstToTarget < Mathf.Pow(attackDistanceThreshold + myCollisionRadius + targetCollisionRadius, 2))
                {
                    nextAttackTime = Time.time + timeBetweenAttacks;
                    StartCoroutine(Attack());
                }
            }
        }
    }
    // 적 공격
    IEnumerator Attack()
    {
        currentState = State.Attacking;
        pathfinder.enabled = false; // 네비게이션 추적 종료

        Vector3 originalPosition = transform.position;
        Vector3 dirToTarget = (target.position - transform.position).normalized;
        Vector3 attackPosition = target.position - dirToTarget * (myCollisionRadius);

        float attackSpeed = 3;
        float percent = 0;

        skinMaterial.color = Color.red;
        bool hasAppliedDamage = false; // 데미지를 적용하는 도중인가

        while (percent <= 1)
        {
            if(percent >= 0.5f && !hasAppliedDamage)
            {
                hasAppliedDamage = true;
                targetEntity.TakeDamage(damage);
            }
            percent += Time.deltaTime * attackSpeed;
            float interpolation = (-Mathf.Pow(percent, 2) + percent) * 4;
            transform.position = Vector3.Lerp(originalPosition, attackPosition, interpolation);

            yield return null;
        }
        skinMaterial.color = originColor;
        currentState = State.Chasing;
        pathfinder.enabled = true; // 네비게이션 추적 시작
    }
    // 적 추적
    IEnumerator UpdatePath()
    {
        float refreshRate = 0.25f;

        while (hasTarget)
        {
            if (currentState == State.Chasing)
            {
                Vector3 dirToTarget = (target.position - transform.position).normalized;
                Vector3 targetPosition = target.position - dirToTarget * (myCollisionRadius + targetCollisionRadius + attackDistanceThreshold/2);
                if (!dead)
                {
                    pathfinder.SetDestination(targetPosition); // 네비게이션 목표 설정
                }
            }
            yield return new WaitForSeconds(refreshRate);
        }
    }
}
