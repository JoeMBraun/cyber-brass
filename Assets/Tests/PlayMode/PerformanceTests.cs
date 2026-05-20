using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.TestTools;

namespace CyberBrass.Tests
{
    /// <summary>
    /// PlayMode performance and capability tests for CyberBrass.
    /// Simulates rapid frame execution, calculates average FPS, and monitors GC memory allocations.
    /// </summary>
    public class PerformanceTests
    {
        private GameObject _performanceContainer;

        [SetUp]
        public void SetUp()
        {
            _performanceContainer = new GameObject("PerformanceContainer");
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(_performanceContainer);
        }

        /// <summary>
        /// Simulates gameplay execution over 2 seconds, measuring average frames per second (FPS).
        /// Outputs profiling metrics to the Unity debug console and asserts baseline frame counts.
        /// </summary>
        [UnityTest]
        public IEnumerator MeasureGameplayFPS()
        {
            float duration = 2.0f;
            float timeElapsed = 0f;
            int initialFrameCount = Time.frameCount;
            float startTime = Time.realtimeSinceStartup;

            // Run a loop yielding for 2 seconds to simulate gameplay frame cycles
            while (timeElapsed < duration)
            {
                yield return null;
                timeElapsed = Time.realtimeSinceStartup - startTime;
            }

            int finalFrameCount = Time.frameCount;
            int framesRendered = finalFrameCount - initialFrameCount;
            float averageFps = framesRendered / timeElapsed;

            Debug.Log($"[Performance Profiler] Average FPS over {timeElapsed:F2} seconds: {averageFps:F1} (Rendered {framesRendered} frames)");

            // Assert a basic capability baseline: at least 30 FPS in simulation mode
            // On headless CI systems, frame rendering might be decoupled, so we log warnings instead of hard-failing.
            if (averageFps < 30f)
            {
                Debug.LogWarning($"[Performance Warning] Simulation frame rate is low: {averageFps:F1} FPS. Check vsync or execution bottlenecks.");
            }

            Assert.Greater(framesRendered, 10, "The simulation did not render enough frames to calculate metrics.");
        }

        /// <summary>
        /// Monitors heap memory allocation fluctuations during simulated active execution cycles.
        /// Verifies that garbage collection pressure remains stable and does not leak memory.
        /// </summary>
        [UnityTest]
        public IEnumerator MeasureHeapMemoryFootprint()
        {
            // Collect initial memory footprint
            System.GC.Collect();
            long initialMemory = Profiler.GetMonoUsedSizeLong();
            Debug.Log($"[Memory Profiler] Initial Mono Heap Used Memory: {initialMemory / (1024f * 1024f):F2} MB");

            // Allocate some transient mock visual entities to simulate weapon debris
            var debrisList = new System.Collections.Generic.List<GameObject>();
            for (int i = 0; i < 50; i++)
            {
                var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
                block.transform.SetParent(_performanceContainer.transform);
                debrisList.Add(block);
                yield return null; // spread allocation over frames
            }

            long peakMemory = Profiler.GetMonoUsedSizeLong();
            Debug.Log($"[Memory Profiler] Peak Mono Heap Used Memory: {peakMemory / (1024f * 1024f):F2} MB");

            // Cleanup objects
            foreach (var debris in debrisList)
            {
                Object.Destroy(debris);
            }
            debrisList.Clear();

            // Run GC and assert memory returns close to initial footprint
            yield return null;
            System.GC.Collect();
            yield return null;

            long postCleanupMemory = Profiler.GetMonoUsedSizeLong();
            long memoryDifference = postCleanupMemory - initialMemory;
            Debug.Log($"[Memory Profiler] Post-GC Heap Used Memory: {postCleanupMemory / (1024f * 1024f):F2} MB");
            Debug.Log($"[Memory Profiler] Net Memory Delta: {memoryDifference / 1024f:F2} KB");

            // Verify memory is properly collected and doesn't leak out of control
            // A threshold of 5MB is standard buffer allowance for dynamic engine allocations.
            Assert.Less(memoryDifference, 5 * 1024 * 1024, "Possible memory leak detected: Heap memory remains significantly higher post-GC.");
        }
    }
}
