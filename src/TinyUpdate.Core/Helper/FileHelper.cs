using System.IO;

namespace TinyUpdate.Core.Helper
{
    public class FileHelper
    {
        public static FileStream OpenWrite(string path, long preallocationSize = 0)
        {
            return MakeFileStream(path, FileMode.OpenOrCreate, FileAccess.Write, preallocationSize);
        }
        
        public static FileStream MakeFileStream(string path, FileMode mode, FileAccess access, long preallocationSize = 0)
        {
#if NET6_0_OR_GREATER
            return File.Open(path, new FileStreamOptions { Mode = mode, Access = access, PreallocationSize = preallocationSize});
#else
            return File.Open(path, mode, access);
#endif
        }
    }
}