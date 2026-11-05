#if UNITY_EDITOR && UNITY_ANDROID
using System;
using System.IO;
using System.Text;
using System.Xml;
using UnityEditor.Android;
using UnityEngine;

namespace DerbyArena.EditorTools
{
    /// <summary>
    /// 4.0.2: turns Android Auto Backup off. The save (JSON in PlayerPrefs, i.e. SharedPreferences) was copied to the
    /// player's Google account by Android and put back after an uninstall and reinstall, so a deleted game came back
    /// with its old progress. Until progress is tied to an optional Google sign-in, deleting the game deletes its
    /// progress. Patches the manifests of the Gradle project Unity generates (launcher = the app, unityLibrary):
    /// android:allowBackup="false", with tools:replace so a library manifest can never switch it back on.
    /// </summary>
    public sealed class DerbyAndroidNoBackup : IPostGenerateGradleAndroidProject
    {
        const string AndroidNs = "http://schemas.android.com/apk/res/android";
        const string ToolsNs = "http://schemas.android.com/tools";
        const string XmlnsNs = "http://www.w3.org/2000/xmlns/";

        public int callbackOrder { get { return 110; } }

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            Patch(Path.GetFullPath(Path.Combine(path, "../launcher/src/main/AndroidManifest.xml")));
            Patch(Path.GetFullPath(Path.Combine(path, "src/main/AndroidManifest.xml")));
        }

        static void Patch(string file)
        {
            try
            {
                if (!File.Exists(file))
                {
                    Debug.LogWarning("DERBY BACKUP: manifest missing: " + file);
                    return;
                }
                var doc = new XmlDocument();
                doc.PreserveWhitespace = true;
                doc.Load(file);
                var manifest = doc.DocumentElement;
                if (manifest == null || manifest.LocalName != "manifest") return;

                if (manifest.GetAttributeNode("xmlns:tools") == null)
                {
                    var ns = doc.CreateAttribute("xmlns", "tools", XmlnsNs);
                    ns.Value = ToolsNs;
                    manifest.Attributes.Append(ns);
                }

                XmlElement app = null;
                foreach (XmlNode n in manifest.ChildNodes)
                    if (n is XmlElement && n.LocalName == "application") { app = (XmlElement)n; break; }
                if (app == null)
                {
                    app = doc.CreateElement("application");
                    manifest.AppendChild(app);
                }

                Set(doc, app, "android", "allowBackup", AndroidNs, "false");
                string replace = app.GetAttribute("replace", ToolsNs);
                if (string.IsNullOrEmpty(replace)) replace = "android:allowBackup";
                else if (replace.IndexOf("android:allowBackup", StringComparison.Ordinal) < 0) replace += ",android:allowBackup";
                Set(doc, app, "tools", "replace", ToolsNs, replace);

                var settings = new XmlWriterSettings { Encoding = new UTF8Encoding(false), Indent = false };
                using (var w = XmlWriter.Create(file, settings)) doc.Save(w);
                Debug.Log("DERBY BACKUP: android:allowBackup=false in " + file);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("DERBY BACKUP: manifest not patched (" + file + "): " + ex.Message);
            }
        }

        static void Set(XmlDocument doc, XmlElement el, string prefix, string localName, string ns, string value)
        {
            var a = el.GetAttributeNode(localName, ns);
            if (a == null)
            {
                a = doc.CreateAttribute(prefix, localName, ns);
                el.Attributes.Append(a);
            }
            a.Value = value;
        }
    }
}
#endif
