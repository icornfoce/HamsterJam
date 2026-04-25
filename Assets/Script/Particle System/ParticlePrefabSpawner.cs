using UnityEngine;
using System.Collections.Generic;

public class ParticlePrefabSpawner : MonoBehaviour
{
    [Header("Settings")]
    public ParticleSystem partSystem;
    public GameObject prefabToSpawn;
    public float lifeTime = 2f;
    public float velocityMultiplier = 1f;
    public bool useParticleVelocity = true;
    public bool useParticleRotation = true;

    private ParticleSystem.Particle[] particles;
    private HashSet<uint> trackedParticles = new HashSet<uint>();
    private List<uint> seedsToRemove = new List<uint>();

    void Start()
    {
        if (partSystem == null) partSystem = GetComponent<ParticleSystem>();
        
        // Initialize particle array with a reasonable size
        if (partSystem != null)
        {
            particles = new ParticleSystem.Particle[partSystem.main.maxParticles];
        }
    }

    void Update()
    {
        if (partSystem == null || prefabToSpawn == null) return;

        int currentCount = partSystem.particleCount;
        if (currentCount == 0)
        {
            trackedParticles.Clear();
            return;
        }

        // Ensure array is big enough
        if (particles == null || particles.Length < partSystem.main.maxParticles)
        {
            particles = new ParticleSystem.Particle[partSystem.main.maxParticles];
        }

        int numParticlesAlive = partSystem.GetParticles(particles);
        
        // HashSet to keep track of particles currently alive in this frame
        HashSet<uint> currentFrameSeeds = new HashSet<uint>();

        for (int i = 0; i < numParticlesAlive; i++)
        {
            uint seed = particles[i].randomSeed;
            currentFrameSeeds.Add(seed);

            // If this is a new particle we haven't seen before
            if (!trackedParticles.Contains(seed))
            {
                SpawnPrefabForParticle(particles[i]);
                trackedParticles.Add(seed);
            }
        }

        // Cleanup: Remove seeds of particles that are no longer alive
        seedsToRemove.Clear();
        foreach (uint seed in trackedParticles)
        {
            if (!currentFrameSeeds.Contains(seed))
            {
                seedsToRemove.Add(seed);
            }
        }

        foreach (uint seed in seedsToRemove)
        {
            trackedParticles.Remove(seed);
        }
    }

    private void SpawnPrefabForParticle(ParticleSystem.Particle particle)
    {
        // 1. Calculate Position (Handling Local vs World Space)
        Vector3 spawnPos;
        if (partSystem.main.simulationSpace == ParticleSystemSimulationSpace.Local)
        {
            spawnPos = partSystem.transform.TransformPoint(particle.position);
        }
        else
        {
            spawnPos = particle.position;
        }

        // 2. Instantiate Prefab
        GameObject spawned = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);

        // --- เพิ่มส่วนการทำลายตามเวลา ---
        if (lifeTime > 0)
        {
            Destroy(spawned, lifeTime);
        }

        // 3. Handle Physics/Velocity
        Rigidbody rb = spawned.GetComponent<Rigidbody>();
        Rigidbody2D rb2d = spawned.GetComponent<Rigidbody2D>();
        Vector3 worldVelocity;

        if (partSystem.main.simulationSpace == ParticleSystemSimulationSpace.Local)
        {
            worldVelocity = partSystem.transform.TransformDirection(particle.velocity);
        }
        else
        {
            worldVelocity = particle.velocity;
        }

        // Apply Multiplier
        worldVelocity *= velocityMultiplier;

        // Fallback: If velocity is zero, use emitter's forward
        if (worldVelocity == Vector3.zero)
        {
            worldVelocity = partSystem.transform.forward * velocityMultiplier;
        }

        // 3.1 Handle Physics (3D)
        if (rb != null && useParticleVelocity)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
#if UNITY_2023_1_OR_NEWER
            rb.linearVelocity = worldVelocity;
#else
            rb.velocity = worldVelocity;
#endif
        }

        // 3.2 Handle Physics (2D)
        if (rb2d != null && useParticleVelocity)
        {
            rb2d.bodyType = RigidbodyType2D.Dynamic;
            rb2d.linearVelocity = (Vector2)worldVelocity;
        }


        // 4. Handle Orientation
        if (useParticleRotation && worldVelocity != Vector3.zero)
        {
            spawned.transform.forward = worldVelocity;
        }
    }
}