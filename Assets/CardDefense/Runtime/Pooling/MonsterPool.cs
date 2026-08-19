using System.Collections.Generic;
using CardDefense.Enemies;
using UnityEngine;

namespace CardDefense.Pooling
{
    public sealed class MonsterPool : MonoBehaviour
    {
        private readonly Queue<Monster> available = new Queue<Monster>(128);
        private Monster prefab;
        private Transform poolRoot;

        public void Configure(Monster monsterPrefab, int prewarmCount)
        {
            prefab = monsterPrefab;
            if (poolRoot == null)
            {
                GameObject root = new GameObject("MonsterPool_Inactive");
                poolRoot = root.transform;
                poolRoot.SetParent(transform, false);
            }

            while (available.Count < prewarmCount) available.Enqueue(Create());
        }

        public Monster Get()
        {
            return available.Count > 0 ? available.Dequeue() : Create();
        }

        public void Release(Monster monster)
        {
            monster.transform.SetParent(poolRoot, false);
            monster.gameObject.SetActive(false);
            available.Enqueue(monster);
        }

        private Monster Create()
        {
            Monster instance = Instantiate(prefab, poolRoot);
            instance.name = "Monster_Pooled";
            instance.gameObject.SetActive(false);
            return instance;
        }
    }
}
