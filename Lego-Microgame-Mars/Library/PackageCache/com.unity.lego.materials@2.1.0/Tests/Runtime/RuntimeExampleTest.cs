// Copyright (C) LEGO System A/S - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited

using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;

namespace LEGOMaterials.Tests 
{
	
	class RuntimeExampleTest 
	{

		[Test]
		public void PlayModeSampleTestSimplePasses() 
		{
			// Use the Assert class to test conditions.
		}

		// A UnityTest behaves like a coroutine in PlayMode
		// and allows you to yield null to skip a frame in EditMode
		[UnityTest]
		public IEnumerator PlayModeSampleTestWithEnumeratorPasses() 
		{
			// Use the Assert class to test conditions.
			// yield to skip a frame
			yield return null;
		}
	}
}