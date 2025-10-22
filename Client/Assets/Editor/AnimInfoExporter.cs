using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

[Serializable]
public class AnimClipInfo
{
    public string clip;
    public double length;
    public float frameRate;
    public int samples;
    public string animName; // "Layer/SubState/State" 식 경로(있으면)
}

[Serializable]
public class CharacterAnimDump
{
    public string character;
    public string controller; // asset path
    public List<AnimClipInfo> clips = new();
}

[Serializable]
public class AnimDumpRoot
{
    public int version = 1;
    public List<CharacterAnimDump> characters = new();
}

public static class AnimInfoExporter
{
    [MenuItem("Tools/Export/Animation Info JSON")]
    public static void ExportAll()
    {
        // 1) 선택한 파일/폴더에서 AnimatorController 수집
        var controllerPaths = Selection.objects
            .Select(o => AssetDatabase.GetAssetPath(o))
            .Where(p => p.EndsWith(".controller", StringComparison.OrdinalIgnoreCase) || AssetDatabase.IsValidFolder(p))
            .SelectMany(p => CollectControllers(p))
            .Distinct()
            .ToList();

        if (controllerPaths.Count == 0)
        {
            EditorUtility.DisplayDialog("Anim Export", "선택한 항목에 AnimatorController가 없습니다.", "OK");
            return;
        }

        var dump = new AnimDumpRoot();
        foreach (var path in controllerPaths)
        {
            var ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(path);
            if (ctrl == null)
                continue;

            var item = new CharacterAnimDump
            {
                character = GuessCharacterNameFromPath(path),
                controller = path,
            };

            // 2) 컨트롤러의 모든 클립 수집(오버라이드 포함)
            var clips = new HashSet<AnimationClip>();
            foreach (var c in ctrl.animationClips)
                if (c != null)
                    clips.Add(c);

            // 3) 상태 트리에서 statePath 정보도 추출 (AnimatorController일 때만)
            var statePaths = new Dictionary<AnimationClip, List<string>>();
            if (ctrl is AnimatorController ac)
            {
                foreach (var layer in ac.layers)
                    TraverseStates(layer.stateMachine, statePaths);
            }

            // 4) JSON용으로 매핑
            foreach (var clip in clips)
            {
                item.clips.Add(new AnimClipInfo
                {
                    clip = clip.name,
                    length = Math.Round(clip.length, 6),
                    frameRate = clip.frameRate,
                    samples = Mathf.RoundToInt((float)(clip.length * clip.frameRate)),
                    animName = statePaths.TryGetValue(clip, out var paths) ? string.Join(" | ", paths.Distinct()) : ""
                });
            }

            // 이름순 정렬(가독성)
            item.clips = item.clips.OrderBy(c => c.clip).ToList();
            dump.characters.Add(item);
        }

        // 5) 저장
        var savePath = EditorUtility.SaveFilePanel("Save Animation Info JSON", "Assets", "AnimationInfos.json", "json");
        if (!string.IsNullOrEmpty(savePath))
        {
            var json = JsonUtility.ToJson(dump, prettyPrint: true);
            File.WriteAllText(savePath, json);
            AssetDatabase.Refresh();
            Debug.Log($"[AnimInfoExporter] Saved: {savePath}");
        }

        // 로컬 함수들
        static IEnumerable<string> CollectControllers(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                var guids = AssetDatabase.FindAssets("t:AnimatorController", new[] { path });
                foreach (var g in guids)
                    yield return AssetDatabase.GUIDToAssetPath(g);
            }
            else if (path.EndsWith(".controller", StringComparison.OrdinalIgnoreCase))
            {
                yield return path;
            }
        }

        static void TraverseStates(AnimatorStateMachine sm, /*string prefix,*/ Dictionary<AnimationClip, List<string>> map)
        {
            // 현 상태머신의 직접 상태
            foreach (var st in sm.states)
            {
                var state = st.state;
                var clip = state?.motion as AnimationClip;
                if (clip != null)
                {
                    var path = $"{state.name}";
                    if (!map.TryGetValue(clip, out var list))
                        map[clip] = list = new List<string>();
                    list.Add(path);
                }
            }
            // 서브 스테이트머신
            foreach (var sub in sm.stateMachines)
                TraverseStates(sub.stateMachine, map);
        }

        static string GuessCharacterNameFromPath(string path)
        {
            // 프로젝트 구조에 맞게 수정: 폴더명/파일명으로 대충 유추
            var file = Path.GetFileNameWithoutExtension(path);
            return file; // 예: "Rozzi"
        }
    }
}
