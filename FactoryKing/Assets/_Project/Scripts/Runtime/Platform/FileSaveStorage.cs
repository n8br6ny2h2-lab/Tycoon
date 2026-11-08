using System.IO;
using FactoryKing.Core.Persistence;
using UnityEngine;

namespace FactoryKing.Runtime.Platform
{
    /// <summary>Saves under Application.persistentDataPath (backed up by iOS) using write-temp-then-replace.</summary>
    public sealed class FileSaveStorage : ISaveStorage
    {
        private readonly string _dir;

        public FileSaveStorage(string directory = null)
        {
            _dir = directory ?? Application.persistentDataPath;
            Directory.CreateDirectory(_dir);
        }

        private string PathOf(string name) => Path.Combine(_dir, name);

        public bool TryRead(string name, out string contents)
        {
            contents = null;
            try
            {
                string p = PathOf(name);
                if (!File.Exists(p)) return false;
                contents = File.ReadAllText(p, System.Text.Encoding.UTF8);
                return true;
            }
            catch (IOException e)
            {
                Debug.LogWarning("[FactoryKing] Save read failed: " + e.Message);
                return false;
            }
        }

        public void WriteAtomic(string name, string contents)
        {
            string target = PathOf(name);
            string tmp = target + ".tmp";
            File.WriteAllText(tmp, contents, new System.Text.UTF8Encoding(false));
            if (File.Exists(target)) File.Replace(tmp, target, null);
            else File.Move(tmp, target);
        }

        public bool Exists(string name) => File.Exists(PathOf(name));

        public void Delete(string name)
        {
            if (File.Exists(PathOf(name))) File.Delete(PathOf(name));
        }
    }
}
