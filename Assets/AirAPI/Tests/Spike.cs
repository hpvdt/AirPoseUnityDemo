using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace AirAPI.Tests
{
    public class Spike
    {
        // A Test behaves as an ordinary method
        [Test]
        public void SpikeSimplePasses()
        {
            // Use the Assert class to test conditions
            
            // int code = AirAPI.AirPoseProvider.StartConnection();
        }

        // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
        // `yield return null;` to skip a frame.
        [UnityTest]
        public IEnumerator SpikeWithEnumeratorPasses()
        {
            // Use the Assert class to test conditions.
            // Use yield to skip a frame.
            yield return null;
        }
    }
}