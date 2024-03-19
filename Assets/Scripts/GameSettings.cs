using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using UnityEngine.Audio;
using UnityEngine.Rendering.Universal;

[CreateAssetMenu(fileName = "GameSettings", menuName = "Game Settings")]
public class GameSettings : ScriptableObject
{

    public int matchTime;
    public int startingTime;
    public bool playerCanTransform;

    public LayerMask weaponHitLayer;

    public int selectedHiderIndex;
    public GameObject[] hiders;
    public GameObject[] hidersPreview;

    public int selectedSeekerIndex;
    public GameObject[] seekers;
    public GameObject[] seekersPreview;

    public GameObject[] bulletHoles;

    [Header("VFX")]
    public GameObject blood;

    [Header("UI")]
    public GameObject KillStat;

    [Header("Graphics")]

    public bool postProcessing;
    public UniversalRenderPipelineAsset renderPipeline;

    [Header("Audio")]

    public AudioMixer mixer;

    public AudioClip[] concreteFootsteps;
    public AudioClip[] grassFootsteps;
    public AudioClip[] woodFootsteps;
    public AudioClip[] bulletImpact;
    public AudioClip tountSound;
    public AudioClip[] door_Open;
    public AudioClip[] door_Close;

    [Header("Default sensibility")]
    public AxisState xAxis;
    public AxisState xAxisCapped;
    public AxisState yAxis;

}
