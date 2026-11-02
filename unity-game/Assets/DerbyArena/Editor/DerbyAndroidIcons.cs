#if UNITY_EDITOR && UNITY_ANDROID
using System;
using System.IO;
using UnityEditor.Android;
using UnityEngine;

namespace DerbyArena.EditorTools
{
    /// <summary>
    /// Writes the DERBY ARENA launcher icons (legacy, round and adaptive, mdpi..xxxhdpi) straight into the
    /// Gradle project Unity generates, so the APK/AAB always carries them. Does not depend on the
    /// PlayerSettings icon API or on texture re-imports during the build. Source: unity-game/AndroidIcons/res.
    /// </summary>
    public sealed class DerbyAndroidIcons : IPostGenerateGradleAndroidProject
    {
        public int callbackOrder { get { return 100; } }

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            try
            {
                string src = Path.GetFullPath(Path.Combine(Application.dataPath, "../AndroidIcons/res"));
                string dst = Path.GetFullPath(Path.Combine(path, "../launcher/src/main/res"));
                if (!Directory.Exists(src))
                {
                    Debug.LogWarning("DERBY ICONS: source folder missing: " + src);
                    return;
                }
                if (!Directory.Exists(dst))
                {
                    Debug.LogWarning("DERBY ICONS: launcher res folder missing: " + dst);
                    return;
                }
                int copied = 0;
                foreach (string dir in Directory.GetDirectories(src))
                {
                    string target = Path.Combine(dst, Path.GetFileName(dir));
                    Directory.CreateDirectory(target);
                    foreach (string file in Directory.GetFiles(dir))
                    {
                        // remove same-named resources with another extension (png/webp/xml) to avoid duplicates
                        string name = Path.GetFileNameWithoutExtension(file);
                        foreach (string old in Directory.GetFiles(target))
                            if (Path.GetFileNameWithoutExtension(old) == name) File.Delete(old);
                        File.Copy(file, Path.Combine(target, Path.GetFileName(file)), true);
                        copied++;
                    }
                }
                Debug.Log("DERBY ICONS: " + copied + " launcher icon files written to " + dst);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("DERBY ICONS: launcher icons not written: " + ex);
            }
        }
    }
}
#endif
