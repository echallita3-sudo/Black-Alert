using System;
using System.IO;
using UnityEngine;

namespace BlackAlert
{
    [Serializable]
    public sealed class BlackAlertCommanderProfile
    {
        public string DisplayName = "Azure Commander";
        public int Experience;
        public int Wins;
        public int Losses;
        public int Battles;
        public string AvatarFile = "commander-avatar.png";
        [NonSerialized] public Texture2D Avatar;

        public int Level { get { return 1 + Mathf.FloorToInt(Mathf.Sqrt(Mathf.Max(0,Experience)/125f)); } }

        public static BlackAlertCommanderProfile Load()
        {
            var path = Path.Combine(Application.persistentDataPath,"commander-profile.json");
            BlackAlertCommanderProfile profile;
            try
            {
                profile = File.Exists(path)
                    ? JsonUtility.FromJson<BlackAlertCommanderProfile>(File.ReadAllText(path))
                    : new BlackAlertCommanderProfile();
            }
            catch(Exception ex)
            {
                Debug.LogWarning("[Black Alert Profile] Profile load failed: " + ex.Message);
                profile = new BlackAlertCommanderProfile();
            }
            if(profile == null) profile = new BlackAlertCommanderProfile();
            profile.DisplayName = SanitizeName(profile.DisplayName);
            profile.LoadLocalPortrait();
            return profile;
        }

        private static string SanitizeName(string candidate)
        {
            if(string.IsNullOrWhiteSpace(candidate)) return "Azure Commander";
            candidate = candidate.Trim();
            return candidate.Length > 26 ? candidate.Substring(0,26) : candidate;
        }

        public void SetName(string name)
        {
            DisplayName = SanitizeName(name);
            Save();
        }

        public void RecordVictory()
        {
            Wins++; Battles++; Experience+=120;
            Save();
        }
        public void RecordDefeat()
        {
            Losses++; Battles++; Experience+=30;
            Save();
        }

        public bool LoadLocalPortrait()
        {
            // Safe local PNG/JPEG import in app's private data directory.
            // Native Android photo picker UX has not yet been implemented.
            var path=Path.Combine(Application.persistentDataPath,AvatarFile);
            try
            {
                var f=new FileInfo(path);
                if(!f.Exists || f.Length < 12 || f.Length > 5*1024*1024)return false;
                var bytes=File.ReadAllBytes(path);
                var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);
                if(!ImageConversion.LoadImage(texture,bytes,true))
                {
                    UnityEngine.Object.Destroy(texture);
                    return false;
                }
                if(texture.width>2048 || texture.height>2048)
                {
                    UnityEngine.Object.Destroy(texture);
                    return false;
                }
                if(Avatar!=null) UnityEngine.Object.Destroy(Avatar);
                Avatar=texture;
                return true;
            }
            catch(Exception ex)
            {
                Debug.LogWarning("[Black Alert Profile] Unable to read local portrait: "+ex.Message);
                return false;
            }
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Application.persistentDataPath);
                var path=Path.Combine(Application.persistentDataPath,"commander-profile.json");
                File.WriteAllText(path,JsonUtility.ToJson(this,true));
            }
            catch(Exception ex) {Debug.LogWarning("[Black Alert Profile] Save failed: "+ex.Message);}
        }
    }
}
