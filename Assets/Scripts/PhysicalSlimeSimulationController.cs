using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Random = UnityEngine.Random;


public class PhysicalSlimeSimulationController : MonoBehaviour
{
    public ComputeShader slimeSimShader;
    public RawImage slimeRTDisplay;
    public SpawnPattern spawnPattern = SpawnPattern.Center;
    public SpawnAngle spawnAngle = SpawnAngle.TowardsCenter;
    
    public int numAgents = 100000;
    
    
    public float moveSpeed;
    public float evaporateSpeed;
    public float diffuseRate;
    [Range(0f, 3f)]
    public float turnSpeed;

    public float sensorOffsetDist;
    public float sensorSize;
    public float sensorAngleDegrees;

    public Texture slimeGradient;

    private Material slimeSurfaceMat;

    
    private RenderTexture _TrailMap;
    private RenderTexture _ProcessedTrailMap;
    private ComputeBuffer _agentCB;
    private int _updateKernelHandle, _processTrailMapKernelHandle, _postProcessTrailMapKernelHandle;
    private static readonly int TrailMap = Shader.PropertyToID("TrailMap");

    private int _mapWidth, _mapHeight;
    
    public struct Agent
    {
        public Vector2 position;
        public float angle;
    }
    
    public enum SpawnPattern
    {
        Center,
        Random,
        RandomInCircle,
    }

    public enum SpawnAngle
    {
        Random,
        TowardsCenter,
        AwayFromCenter,
    }

    

    void Start()
    {
        
        _mapWidth = 2048;
        _mapHeight = 2048;
        
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
        _postProcessTrailMapKernelHandle = slimeSimShader.FindKernel("PostProcessTrailMap");
        
        slimeSimShader.SetTexture(_updateKernelHandle, TrailMap, _TrailMap);
        slimeSimShader.SetTexture(_updateKernelHandle, "ProcessedTrailMap", _ProcessedTrailMap);
        slimeSimShader.SetTexture(_processTrailMapKernelHandle, TrailMap, _TrailMap);
        slimeSimShader.SetTexture(_processTrailMapKernelHandle, "ProcessedTrailMap", _ProcessedTrailMap);
        slimeSimShader.SetTexture(_postProcessTrailMapKernelHandle, TrailMap, _TrailMap);
        slimeSimShader.SetTexture(_postProcessTrailMapKernelHandle, "ProcessedTrailMap", _ProcessedTrailMap);
        
        
        slimeRTDisplay.texture = _TrailMap;

        
        moveSpeed = 6.6f;
        evaporateSpeed = .14f;
        diffuseRate = 47.96f;

        turnSpeed = 0.23f;
        sensorOffsetDist = 6f;
        sensorSize = 1f;
        sensorAngleDegrees = 45f;
        
        slimeSurfaceMat = GetComponent<Renderer>().sharedMaterial;
        
        ResetSimulation();
    }

    void ResetSimulation()
    {
        slimeSimShader.SetTexture(_postProcessTrailMapKernelHandle, "Gradient", slimeGradient);
        slimeSimShader.SetInt("gradientWidth", slimeGradient.width);
        
        Agent[] agents = new Agent[numAgents];
        Vector2 mapCenter = new Vector2(_mapWidth / 2, _mapHeight / 2);
        
        // set the position of the particles
        if (spawnPattern == SpawnPattern.Center)
        {
            for (int i = 0; i < numAgents; i++)
            {
                agents[i] = new Agent() { position = mapCenter};
            }
        }
        else if (spawnPattern == SpawnPattern.RandomInCircle)
        {
            float radius = Mathf.Min(_mapHeight, _mapWidth) / 2f * 0.85f;
            
            for (int i = 0; i < numAgents; i++)  {
                agents[i] = new Agent()
                {
                    position = (radius * Random.insideUnitCircle) + mapCenter
                };
            }
        }
        else if (spawnPattern == SpawnPattern.Random)
        {
            for (int i = 0; i < numAgents; i++)  {
                agents[i] = new Agent()
                {
                    position = new Vector2(Random.value * _mapWidth, Random.value * _mapHeight)
                };
            }
        }
        
        // set the angle of the particles
        if (spawnAngle == SpawnAngle.TowardsCenter)
        {
            for (int i = 0; i < numAgents; i++)
            {
                agents[i].angle = Mathf.Atan2((mapCenter - agents[i].position).normalized.y, (mapCenter - agents[i].position).normalized.x);
            }
        }
        else if (spawnAngle == SpawnAngle.AwayFromCenter)
        {
            for (int i = 0; i < numAgents; i++)
            {
                agents[i].angle = Mathf.Atan2((agents[i].position - mapCenter).normalized.y, (agents[i].position - mapCenter).normalized.x);
            }
        }
        else if (spawnAngle == SpawnAngle.Random)
        {
            for (int i = 0; i < numAgents; i++)
            {
                agents[i].angle = Random.value * Mathf.PI * 2;
            }
        }
        
        _agentCB = new ComputeBuffer(numAgents, sizeof(float)*3);
        _agentCB.SetData(agents);
        slimeSimShader.SetBuffer(_updateKernelHandle, "agents", _agentCB);
        
    }

    void FixedUpdate()
    {
        slimeSurfaceMat.SetTexture("_ColorSlimeTex", _ProcessedTrailMap);
        slimeSurfaceMat.SetTexture("_ValueSlimeTex", _TrailMap);
        
        slimeSimShader.SetFloat("deltaTime", Time.fixedDeltaTime);
        slimeSimShader.SetFloat("time", Time.fixedTime);
        
        slimeSimShader.SetFloat("moveSpeed", moveSpeed);
        slimeSimShader.SetFloat("evaporateSpeed", evaporateSpeed);
        slimeSimShader.SetFloat("diffuseRate", diffuseRate);
        slimeSimShader.SetFloat("slimeTurnSpeed", turnSpeed);

        slimeSimShader.SetFloat("sensorOffsetDist", sensorOffsetDist);
        slimeSimShader.SetFloat("sensorSize", sensorSize);
        slimeSimShader.SetFloat("sensorAngleDegrees", sensorAngleDegrees);
        
        slimeSimShader.SetInt("width", _mapWidth);
        slimeSimShader.SetInt("height", _mapHeight);
        slimeSimShader.SetInt("numAgents", numAgents);
        
        slimeSimShader.Dispatch(_updateKernelHandle, numAgents/32, 1, 1);
        slimeSimShader.Dispatch(_processTrailMapKernelHandle, _mapWidth/8, _mapHeight/8, 1);
        slimeSimShader.Dispatch(_postProcessTrailMapKernelHandle, _mapWidth/8, _mapHeight/8, 1);

        if (Input.GetKeyDown(KeyCode.Space))
        {
            ResetSimulation();
        }
        RotateSelf();
    }

    void RotateSelf()
    {
        transform.Rotate(Vector3.up, Time.deltaTime * 10f);
    }
    
    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        // Graphics.Blit(_ProcessedTrailMap, destination);
    }

    private void OnDestroy()
    {
        _TrailMap.Release();
    }
}
