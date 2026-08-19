using System.Collections;
using CardDefense.Combat;
using CardDefense.Core;
using CardDefense.Enemies;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CardDefense.Tests
{
    public sealed class PrototypeSceneSmokeTests
    {
        [UnityTest]
        public IEnumerator PrototypeSceneStartsCoreSystems()
        {
            AsyncOperation load = SceneManager.LoadSceneAsync("CardDefensePrototype", LoadSceneMode.Single);
            while (!load.isDone) yield return null;
            yield return null;

            Assert.IsNotNull(Object.FindObjectOfType<GameComposition>());
            Assert.IsNotNull(Object.FindObjectOfType<WaveDirector>());
            Assert.IsNotNull(Object.FindObjectOfType<MonsterSystem>());
            Assert.IsNotNull(Object.FindObjectOfType<CardSummonController>());

            WaveDirector waves = Object.FindObjectOfType<WaveDirector>();
            Assert.GreaterOrEqual(waves.CurrentRound, 1);
        }
    }
}
