using System;
using NUnit.Framework;
using UniGame.Runtime.ObjectPool;
using UniGame.Runtime.ObjectPool.Extensions;
using UniGame.Core.Runtime.ObjectPool;
using UnityEngine;

namespace UnioModules.UniGame.CoreModules.Tests
{
    public class ClassPoolTests : MonoBehaviour
    {
        public class PooledTestClass : IPoolable, IDisposable
        {
            public string Message = string.Empty;
            
            public void Release()
            {
                Message = string.Empty;
            }

            public void Dispose()
            {
                Message = string.Empty;
            }
        }

        public class PooledTestBehaviour : MonoBehaviour, IPoolable
        {
            public int ReleaseCount { get; private set; }

            public void Release() => ReleaseCount++;
        }

        [TearDown]
        public void TearDown()
        {
            ObjectPool.DestroyAllPools();
        }
        
        [Test(TestOf = typeof(ClassPoolTests))]
        public void MakePoolItemTest()
        {
            //info
            var messageData = "Demo Message";
            //action
            var classOne = ClassPool.Spawn<PooledTestClass>();
            classOne.Message = messageData;
            classOne.Despawn();
            
            var classTwo = ClassPool.Spawn<PooledTestClass>();
            
            //assert
            Assert.That(classOne == classTwo,"classOne != pooled classTwo");
            Assert.That(classTwo.Message == messageData,"message from pooled class != " + messageData);
        }
        
        [Test(TestOf = typeof(ClassPoolTests))]
        public void MakeReleasePoolItemTest()
        {
            //info
            var messageData = "Demo Message";
            //action
            var classOne = ClassPool.Spawn<PooledTestClass>();
            classOne.Message = messageData;
            classOne.DespawnWithRelease();
            
            var classTwo = ClassPool.Spawn<PooledTestClass>();
            
            //assert
            Assert.That(classOne == classTwo,"classOne != pooled classTwo");
            Assert.That(classTwo.Message == string.Empty,"message not empty");
        }

        [Test]
        public void DespawnComponent_ReleasesPoolableExactlyOnce()
        {
            var prefab = new GameObject(nameof(DespawnComponent_ReleasesPoolableExactlyOnce));
            prefab.SetActive(false);
            var prototype = prefab.AddComponent<PooledTestBehaviour>();
            var clone = ObjectPool.Spawn<PooledTestBehaviour>(prototype);

            ObjectPool.Despawn(clone);

            Assert.That(clone.ReleaseCount, Is.EqualTo(1));
            Object.DestroyImmediate(prefab);
        }

        [Test]
        public void DespawnGameObject_ReleasesRootPoolableExactlyOnce()
        {
            var prefab = new GameObject(nameof(DespawnGameObject_ReleasesRootPoolableExactlyOnce));
            prefab.SetActive(false);
            prefab.AddComponent<PooledTestBehaviour>();
            var clone = ObjectPool.Spawn(prefab);
            var poolable = clone.GetComponent<PooledTestBehaviour>();

            ObjectPool.Despawn(clone);

            Assert.That(poolable.ReleaseCount, Is.EqualTo(1));
            Object.DestroyImmediate(prefab);
        }

        [Test]
        public void DespawnUnlinkedObject_ReleasesBeforeDestroy()
        {
            var instance = new GameObject(nameof(DespawnUnlinkedObject_ReleasesBeforeDestroy));
            var poolable = instance.AddComponent<PooledTestBehaviour>();

            ObjectPool.Despawn(instance);

            Assert.That(poolable.ReleaseCount, Is.EqualTo(1));
            Object.DestroyImmediate(instance);
        }
    }
}
