using UnityEngine;

public static class FilePath
{
    static string _exePath;
    static string exePath
    {
        get
        {
            if (_exePath != null)
            {
                return _exePath;
            }
            else
            {
                _exePath = GetExePath();
                return _exePath;
            }
        }
    }
    static string GetExePath()
    {
        string basePath = Application.dataPath;

        int lastSlashndex = FindLastSlashIndex(basePath);

        string exePath = basePath.Substring(0, lastSlashndex + 1);  //Substring 的第二个参数是 【截取长度】，因为是从 0 开始截取，所以可以用最后斜杆下标 +1 来截取到最后斜杠，如果不是从0开始则需要更多处理

        return exePath;
    }
    static int FindLastSlashIndex(string srcPath)
    {
        for (int i = srcPath.Length - 1; i >= 0; i--)
            if (srcPath[i] == '/')
                return i;
        return 0;
    }


    static string _contentPath;
    static string contentPath
    {
        get
        {
            if (_contentPath != null)
            {
                return _contentPath;
            }
            else
            {
                _contentPath = GetContentPath();
                return _contentPath;
            }
        }
    }
    static string GetContentPath()
    {
        return exePath + "content/";
    }


    static string _picPath;
    public static string picPath
    {
        get
        {
            if (_picPath != null)
            {
                return _picPath;
            }
            else
            {
                _picPath = GetPicPath();
                return _picPath;
            }
        }
    }
    static string GetPicPath()
    {
        return contentPath + "pic/";
    }
}
