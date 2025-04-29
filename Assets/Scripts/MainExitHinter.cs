using LMCore.AbstractClasses;
using LMCore.Crawler;
using LMCore.Extensions;
using LMCore.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MainExitHinter : Singleton<MainExitHinter, MainExitHinter>, IOnLoadSave
{
    [System.Serializable]
    public struct ExpectedRegion
    {
        public string regionId;
        public string groupId;
        public string humanizedName;
        public List<AudioClip> clips;
    }

    [SerializeField]
    AudioSource speaker;

    [SerializeField]
    List<ExpectedRegion> expectedRegions = new List<ExpectedRegion>();

    [SerializeField]
    List<AudioClip> multipleRegionsUnexplored = new List<AudioClip>();

    List<string> visitedRegions = new List<string>();

    private void OnEnable()
    {
        LevelRegion.OnEnterRegion += LevelRegion_OnEnterRegion;
    }

    private void OnDisable()
    {
        LevelRegion.OnEnterRegion -= LevelRegion_OnEnterRegion;
    }


    private void LevelRegion_OnEnterRegion(GridEntity entity, string regionId)
    {
        if (!visitedRegions.Contains(regionId))
        {
            visitedRegions.Add(regionId);
        }

        var myRegion = GetComponentInParent<LevelRegion>();
        if (myRegion == null)
        {
            Debug.LogError($"Main Exit hinter {name}: I'm not in a region");
        } else
        {
            if (myRegion.RegionId == regionId && speaker != null && !speaker.isPlaying)
            {
                CheckExitStatus();
            }
        }
    }

    void CheckExitStatus()
    {
        var unvisited = expectedRegions.Where(er => !visitedRegions.Contains(er.regionId)).ToList();

        var n = unvisited.Count;

        // We've done our due diligence and visited every room
        if (n == 0) return;

        if (n == 1)
        {
            PlayClipFrom(unvisited[0]);
            return;
        }

        var groups = unvisited.Select(er => er.groupId ?? er.regionId).GroupBy(id => id).Count();
        if (groups == 1)
        {
            PlayClipFrom(unvisited.GetRandomElement());
            return;
        }

        PlayDefaultClip();
    }

    void PlayClipFrom(ExpectedRegion region)
    {

        if (region.clips != null)
        {
            var clip = region.clips.GetRandomElementOrDefault();
            if (clip != null)
            {
                speaker.PlayOneShot(clip);
                return;
            }
        }
        PlayDefaultClip();
    }

    void PlayDefaultClip()
    {
        var clip = multipleRegionsUnexplored.GetRandomElementOrDefault();
        if (clip == null)
        {
            Debug.LogWarning($"Main exit hinter {name}: Lacks multi unexplored clips");
            return;
        }
        speaker.PlayOneShot(clip);
    }

    #region Save / Load
    public IEnumerable<string> Save() => visitedRegions;

    public int OnLoadPriority => throw new System.NotImplementedException();

    void OnLoad(WWSave save)
    {
        visitedRegions.Clear();
        visitedRegions.AddRange(save.visitedRegions);
    }

    public void OnLoad<T>(T save) where T : new()
    {
        if (save is WWSave)
        {
            OnLoad(save as WWSave);
        }
    }
    #endregion
}
