using System;
using System.IO;
using System.Reflection;
using UnityEngine;

/*
Settings.cs 동작 개요 (단일 소스 방식)

- 단일 소스: SettingId(enum)에 각 설정의 메타데이터를 Attribute로 선언합니다.
  - [SettingFloat(min,max,default)] / [SettingBool(default)]
  - (선택) [MixerParam(name, affectsListener)] 로 기본 오디오 파라미터를 선언
- 저장소: _data는 (id,value) 엔트리 리스트를 직렬화하여 보관합니다.
- Load: 파일을 읽고, 누락된 id는 Attribute의 기본값으로 채웁니다.
- Get/Set: enum id로 접근 → Attribute 기반으로 범위/기본값 처리 → 저장/이벤트 발행
- 확장: SettingId에 멤버 + Attribute만 추가하면 자동 반영되며, Data 구조나 코드 수정이 필요 없습니다.
*/

namespace Nonsliep.Core.Settings
{
    // ===== Metadata attributes =====
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class SettingFloatAttribute : Attribute
    {
        public readonly float Min;
        public readonly float Max;
        public readonly float Default;
        public SettingFloatAttribute(float min = 0f, float max = 1f, float @default = 1f)
        {
            Min = min;
            Max = max;
            Default = @default;
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class SettingBoolAttribute : Attribute
    {
        public readonly bool Default;
        public SettingBoolAttribute(bool @default = true)
        {
            Default = @default;
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class MixerParamAttribute : Attribute
    {
        public readonly string Name;
        public readonly bool AffectsListener;
        public MixerParamAttribute(string name = null, bool affectsListener = false)
        {
            Name = name;
            AffectsListener = affectsListener;
        }
    }

    // Strongly-typed setting identifiers with metadata
    public enum SettingId
    {
        [SettingFloat(0f, 1f, 1f), MixerParam("MasterVolume", true)] MasterVolume = 0,
        [SettingFloat(0f, 1f, 1f), MixerParam("BGMVolume", false)] BGMVolume = 1, 
        [SettingFloat(0f, 1f, 1f), MixerParam("SFXVolume", false)] SFXVolume = 2,
        [SettingBool(true)] Haptics = 100,
    }

    // Single, simple settings hub. Enum-based get/set + change events.
    public static class Settings
    {
        public static event Action<SettingId, float> OnFloatChangedId;
        public static event Action<SettingId, bool> OnBoolChangedId;

        private static readonly string FilePath = Path.Combine(Application.persistentDataPath, "user_settings.json");
        private static Data _data;

        [Serializable]
        private class Data
        {
            [Serializable]
            public class FloatEntry { public SettingId id; public float value; }
            [Serializable]
            public class BoolEntry  { public SettingId id; public bool  value; }

            public System.Collections.Generic.List<FloatEntry> floats = new();
            public System.Collections.Generic.List<BoolEntry>  bools  = new();
        }

        // Public load/save
        public static void Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    var json = File.ReadAllText(FilePath);
                    _data = JsonUtility.FromJson<Data>(json) ?? new Data();
                    EnsureAllIdsExist();
                }
                else
                {
                    _data = new Data();
                    EnsureAllIdsExist();
                    Save();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Settings load failed: {ex.Message}");
                _data = new Data();
                EnsureAllIdsExist();
                Save();
            }
        }

        public static void Save()
        {
            EnsureLoaded();
            try
            {
                var json = JsonUtility.ToJson(_data, true);
                File.WriteAllText(FilePath, json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Settings save failed: {e.Message}");
            }
        }

        public static float GetFloat(SettingId id)
        {
            EnsureLoaded();
            if (TryGetFloatEntry(id, out var entry))
            {
                return entry.value;
            }
            return GetFloatDefault(id);
        }

        public static void SetFloat(SettingId id, float v, bool silent = false)
        {
            EnsureLoaded();
            var (min, max) = GetFloatRange(id);
            float clamped = Mathf.Clamp(v, min, max);
            if (!TryGetFloatEntry(id, out var entry))
            {
                entry = new Data.FloatEntry { id = id, value = clamped };
                _data.floats.Add(entry);
            }
            else
            {
                entry.value = clamped;
            }
            Save();
            if (!silent)
            {
                OnFloatChangedId?.Invoke(id, clamped);
            }
        }

        public static bool GetBool(SettingId id)
        {
            EnsureLoaded();
            if (TryGetBoolEntry(id, out var entry))
            {
                return entry.value;
            }
            return GetBoolDefault(id);
        }

        public static void SetBool(SettingId id, bool v, bool silent = false)
        {
            EnsureLoaded();
            if (!TryGetBoolEntry(id, out var entry))
            {
                entry = new Data.BoolEntry { id = id, value = v };
                _data.bools.Add(entry);
            }
            else
            {
                entry.value = v;
            }
            Save();
            if (!silent)
            {
                OnBoolChangedId?.Invoke(id, v);
            }
        }

        public static void ForEach(Action<SettingId, float> floatVisitor, Action<SettingId, bool> boolVisitor)
        {
            EnsureLoaded();
            foreach (SettingId id in Enum.GetValues(typeof(SettingId)))
            {
                if (HasFloat(id))
                {
                    floatVisitor?.Invoke(id, GetFloat(id));
                }
                else if (HasBool(id))
                {
                    boolVisitor?.Invoke(id, GetBool(id));
                }
            }
        }

        private static void EnsureLoaded()
        {
            if (_data == null)
            {
                Load();
            }
        }

        // ===== Helpers: metadata and storage =====
        private static void EnsureAllIdsExist()
        {
            foreach (SettingId id in Enum.GetValues(typeof(SettingId)))
            {
                if (HasFloat(id))
                {
                    if (!TryGetFloatEntry(id, out _))
                    {
                        _data.floats.Add(new Data.FloatEntry { id = id, value = GetFloatDefault(id) });
                    }
                }
                else if (HasBool(id))
                {
                    if (!TryGetBoolEntry(id, out _))
                    {
                        _data.bools.Add(new Data.BoolEntry { id = id, value = GetBoolDefault(id) });
                    }
                }
            }
        }

        private static bool TryGetFloatEntry(SettingId id, out Data.FloatEntry entry)
        {
            entry = _data.floats.Find(e => e.id.Equals(id));
            return entry != null;
        }

        private static bool TryGetBoolEntry(SettingId id, out Data.BoolEntry entry)
        {
            entry = _data.bools.Find(e => e.id.Equals(id));
            return entry != null;
        }

        private static (float min, float max) GetFloatRange(SettingId id)
        {
            var mem = typeof(SettingId).GetMember(id.ToString());
            if (mem != null && mem.Length > 0)
            {
                var attr = mem[0].GetCustomAttribute<SettingFloatAttribute>();
                if (attr != null)
                {
                    return (attr.Min, attr.Max);
                }
            }
            return (0f, 1f);
        }

        private static float GetFloatDefault(SettingId id)
        {
            var mem = typeof(SettingId).GetMember(id.ToString());
            if (mem != null && mem.Length > 0)
            {
                var attr = mem[0].GetCustomAttribute<SettingFloatAttribute>();
                if (attr != null)
                {
                    return attr.Default;
                }
            }
            return 0f;
        }

        private static bool GetBoolDefault(SettingId id)
        {
            var mem = typeof(SettingId).GetMember(id.ToString());
            if (mem != null && mem.Length > 0)
            {
                var attr = mem[0].GetCustomAttribute<SettingBoolAttribute>();
                if (attr != null)
                {
                    return attr.Default;
                }
            }
            return false;
        }

        private static bool HasFloat(SettingId id)
        {
            var mem = typeof(SettingId).GetMember(id.ToString());
            if (mem != null && mem.Length > 0)
            {
                return mem[0].GetCustomAttribute<SettingFloatAttribute>() != null;
            }
            return false;
        }

        private static bool HasBool(SettingId id)
        {
            var mem = typeof(SettingId).GetMember(id.ToString());
            if (mem != null && mem.Length > 0)
            {
                return mem[0].GetCustomAttribute<SettingBoolAttribute>() != null;
            }
            return false;
        }
    }
}
