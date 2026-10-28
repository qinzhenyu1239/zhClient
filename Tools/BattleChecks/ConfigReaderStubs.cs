// Only the Unity/resource boundary is replaced; tests run the actual CSV decoder and config reader.
using System;
using System.IO;

namespace UnityEngine
{
    public sealed class TextAsset { public byte[] bytes; }
    public sealed class UnityException : Exception { public UnityException(string message) : base(message) { } }
    public static class Debug { public static void Log(object message) { } }
}

public abstract class Singleton<T> where T : new()
{
    public static T Instance { get; } = new T();
}

public sealed class ResMgr
{
    public static ResMgr Instance { get; } = new ResMgr();
    public T LoadAssetSync<T>(string path) where T : class
    {
        string root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        string file = Path.Combine(root, "Assets/Game/AssetsPacakge", path);
        if (!File.Exists(file)) return null;
        using var stream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var bytes = new MemoryStream();
        stream.CopyTo(bytes);
        return new UnityEngine.TextAsset { bytes = bytes.ToArray() } as T;
    }
}
