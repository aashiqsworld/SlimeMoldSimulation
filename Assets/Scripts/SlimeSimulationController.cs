using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;


public class SlimeController : MonoBehaviour
{
    public ComputeShader slimeSimShader;
    public RawImage slimeRTDisplay;
    public float moveSpeed;
    public float evaporateSpeed;
    public float diffuseRate;
    public float turnSpeed;

    public float sensorOffsetDist;
    public float sensorSize;
    public float sensorAngleDegrees;
    
    private RenderTexture _TrailMap;
    private RenderTexture _ProcessedTrailMap;
    private ComputeBuffer _agentCB;
    private int _updateKernelHandle, _processTrailMapKernelHandle, _resetTrailMapKernelHandle;
    private static readonly int TrailMap = Shader.PropertyToID("TrailMap");

    private int _mapWidth, _mapHeight;
    
    public struct Agent
    {
        public Vector2 position;
        public float angle;
    }

    private int numAgents = 3200;
    

    void Start()
    {
        _mapWidth = 1920;
        _mapHeight = 1080;
        
        Debug.Log("Support Compute: " + SystemInfo.supportsComputeShaders);

        _TrailMap = new RenderTexture(_mapWidth, _mapHeight, 0)
        {
            enableRandomWrite = true
        };
        _TrailMap.Create();
        
        _ProcessedTrailMap = new RenderTexture(_mapWidth, _mapHeight, 0)
        {
            enableRandomWrite = true
        };
        _ProcessedTrailMap.Create();
        
        _updateKernelHandle = slimeSimShader.FindKernel("Update");
        _processTrailMapKernelHandle = slimeSimShader.FindKernel("ProcessTrailMap");
        _resetTrailMapKernelHandle = slimeSimShader.FindKernel("ResetTrailMap");
        slimeSimShader.SetTexture(_updateKernelHandle, TrailMap, _TrailMap);
        slimeSimShader.SetTexture(_processTrailMapKernelHandle, TrailMap, _TrailMap);
        slimeSimShader.SetTexture(_processTrailMapKernelHandle, "ProcessedTrailMap", _ProcessedTrailMap);
        slimeSimShader.SetTexture(_resetTrailMapKernelHandle, TrailMap, _TrailMap);
        slimeSimShader.SetTexture(_resetTrailMapKernelHandle, "ProcessedTrailMap", _ProcessedTrailMap);
        
        slimeRTDisplay.texture = _TrailMap;

        
        moveSpeed = 6.6f;
        evaporateSpeed = .14f;
        diffuseRate = 47.96f;

        turnSpeed = 100f;
        sensorOffsetDist = 5f;
        sensorSize = 5f;
        sensorAngleDegrees = 45f;
        
        SetAgents();
    }

    void SetAgents()
    {
        Agent[] agents = new Agent[numAgents];

        for (int i = 0; i < numAgents; i++)
        {
            float randomAngle = Random.value * Mathf.PI * 2;
            agents[i] = new Agent() { position = new Vector2(_mapWidth/2, _mapHeight/2), angle = randomAngle};
        }

        _agentCB = new ComputeBuffer(numAgents, sizeof(float)*3);
        _agentCB.SetData(agents);
        slimeSimShader.SetBuffer(_updateKernelHandle, "agents", _agentCB);
        
    }

    void Update()
    {
        slimeSimShader.SetFloat("deltaTime", Time.fixedDeltaTime);
        slimeSimShader.SetFloat("time", Time.fixedTime);
        
        slimeSimShader.SetFloat("moveSpeed", moveSpeed);
        slimeSimShader.SetFloat("evaporateSpeed", evaporateSpeed);
        slimeSimShader.SetFloat("diffuseRate", diffuseRate);
        slimeSimShader.SetFloat("turnSpeed", turnSpeed);

        slimeSimShader.SetFloat("sensorOffsetDist", sensorOffsetDist);
        slimeSimShader.SetFloat("sensorSize", sensorSize);
        slimeSimShader.SetFloat("sensorAngleDegrees", sensorAngleDegrees);
        
        slimeSimShader.SetInt("width", _mapWidth);
        slimeSimShader.SetInt("height", _mapHeight);
        slimeSimShader.SetInt("numAgents", numAgents);
        
        slimeSimShader.Dispatch(_updateKernelHandle, numAgents/32, 1, 1);
        slimeSimShader.Dispatch(_processTrailMapKernelHandle, _mapWidth/8, _mapHeight/8, 1);
        // slimeSimShader.Dispatch(_resetTrailMapKernelHandle, _mapWidth/8, _mapHeight/8, 1);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(_TrailMap, destination);
    }

    private void OnDestroy()
    {
        _TrailMap.Release();
    }
}
