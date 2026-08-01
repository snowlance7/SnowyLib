using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SnowyLib
{
    [CreateAssetMenu(fileName = "AudioLibrary", menuName = "SnowyLib/AudioLibrary", order = 0)]
    public class AudioLibrary : ScriptableObject
    {
        public AudioClipGroup[] Groups = [];
        public AudioClip[] Clips = [];

        public AudioClip? GetClip(string clipName)
        {
            return Clips.Where(x => x.name.ToLower() == clipName.ToLower()).FirstOrDefault();
        }

        public AudioClip[]? GetClips(string groupId)
        {
            var group = Groups.Where(x => x.Id.ToLower() == groupId.ToLower()).FirstOrDefault();
            return group != null ? group.Clips : null;
        }
    }

    [System.Serializable]
    public class AudioClipGroup
    {
        public string Id = "";
        public AudioClip[] Clips = [];
    }
}