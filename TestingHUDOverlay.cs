using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using static SnowyLib.Plugin;

namespace SnowyLib
{
    public class TestingHUDOverlay : MonoBehaviour
    {
        private static TestingHUDOverlay? _instance;
        private static TestingHUDOverlay Instance
        {
            get
            {
                if (_instance == null)
                    _instance = Init();

                return _instance;
            }
        }

        [SerializeField] Transform canvasTransform = null!;
        [SerializeField] GameObject textElementPrefab = null!;

        private static Dictionary<string, TestingHUDEntry> entries = new Dictionary<string, TestingHUDEntry>();

        private static TestingHUDOverlay Init()
        {
            GameObject prefab = (GameObject)ModAssets.LoadAsset("Assets/ModAssets/TestingHUDOverlay.prefab");
            return Instantiate(prefab, localPlayer.transform).GetComponent<TestingHUDOverlay>();
        }

        private void Update()
        {
            foreach (var pair in entries.ToList())
            {
                if (Time.unscaledTime >= pair.Value.ExpireTime)
                {
                    Destroy(pair.Value.Text.gameObject);
                    entries.Remove(pair.Key);
                }
            }
        }

        public static void SetValue(string key, string value, float expireTime = 5f)
        {
            _ = Instance;

            if (!entries.TryGetValue(key, out TestingHUDEntry entry))
            {
                entry = CreateEntry();
                entries.Add(key, entry);
            }

            entry.Text.text = $"{key}: {value}";
            entry.ExpireTime = Time.unscaledTime + expireTime;
        }

        private static TestingHUDEntry CreateEntry()
        {
            TestingHUDEntry entry = new TestingHUDEntry();
            entry.Text = Instantiate(Instance.textElementPrefab, Instance.canvasTransform).GetComponent<TMP_Text>();
            return entry;
        }
    }

    internal class TestingHUDEntry
    {
        public TMP_Text Text = null!;
        public float ExpireTime;
    }
}