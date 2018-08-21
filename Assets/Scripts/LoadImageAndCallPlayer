using UnityEngine;

/// <summary>
/// 这只是个debug的脚本，重点在 LoadLocalImage 和 ImagePlayer 上
/// </summary>
public class LoadImageAndCallPlayer : MonoBehaviour
{
    [SerializeField]
    ImagePlayer _imagePlayer;


    /// <summary>
    /// 加载gif
    /// </summary>
    public void LoadGif()
    {
        string fullName = FilePath.picPath + "暗中观察.gif";

        GifData data = LoadLocalImage.LoadImage(fullName);

        _imagePlayer.SetImage(data);
    }


    /// <summary>
    /// 加载png
    /// </summary>
    public void LoadPng()
    {
        string fullName = FilePath.picPath + "magic.png";

        GifData data = LoadLocalImage.LoadImage(fullName);

        _imagePlayer.SetImage(data);
    }


    /// <summary>
    /// 路径错误的情况
    /// </summary>
    public void LoadFalse()
    {
        string fullName = FilePath.picPath + "这张图不存在的";

        GifData data = LoadLocalImage.LoadImage(fullName);

        _imagePlayer.SetImage(data);
    }
}
