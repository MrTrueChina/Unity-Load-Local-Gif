using UnityEngine;
using UnityEngine.UI;
using System.Linq;

/// <summary>
/// gif图一帧的数据，图像和延迟时间
/// </summary>
public struct GifFrame
{
    public Sprite sprite;
    /// <summary>
    /// 以秒为单位
    /// </summary>
    public float delay;
}


/// <summary>
/// gif数据，所有的图像和延迟时间
/// </summary>
public struct GifData
{
    public GifFrame[] frames;
}


/// <summary>
/// 播放gif的组件，需要挂载的物体有Image组件，也可以存入单图
/// </summary>
[RequireComponent(typeof(Image))]
public class ImagePlayer : MonoBehaviour
{
    Image _imageComponent;
    GifData _gifData;
    float[] _nextFrameTime;             //每次改变图像的时间，根据这些时间和播放时间可以确认应该显示的帧
    float _playTime;

    [SerializeField]
    [Range(0,1)]
    float _fixedHeight;         //存入图片时Image组件宽高以高为准变化的权重，0的话是以宽为准，1的话是以高为准，在中间的用插值
    float _originalWidth;
    float _originalHeight;


    //存入数据
    public void SetImage(GifData gif)
    {
        if (gif.frames == null || gif.frames.Length == 0 || gif.frames[0].sprite == null)
            NoFrame();
        else if (gif.frames.Length == 1)
            OneFrame(gif);
        else
            MultipleFrames(gif);
    }
    public void SetImage(Sprite sprite)
    {
        SetImage(new GifData { frames = new GifFrame[] { new GifFrame { sprite = sprite, delay = 0 } } });
        /*
        * 上面那行拆开是这样的
        new GifData                     //创建 GifData
        {
            frames = new GifFrame[]     //给这个 GifData 的 frames 赋一个 GifFrame数组
            {
                new GifFrame            //给这个 GifFrame数组 创建一个 GifFrame对象
                {
                    sprite = sprite,    //把参数 Sprite 赋给这个 GifFrame对象 的 sprite 属性
                    delay = 0           //间隔写0就行，单图用不到这个属性
                }
            }
        };
        */
    }

    void NoFrame()
    {
        _imageComponent.sprite = null;

        Stop();
    }

    void OneFrame(GifData gif)
    {
        _imageComponent.sprite = gif.frames[0].sprite;

        AdjustmentHeightAndWidth(gif);

        Stop();
    }

    void MultipleFrames(GifData gif)
    {
        _gifData = gif;

        AdjustmentHeightAndWidth(gif);

        ComputeNextFrameTimeArray(gif);

        PlayFromBeginning();
    }
    void ComputeNextFrameTimeArray(GifData gif)
    {
        _nextFrameTime = new float[gif.frames.Length];
        float nextFrameTime = 0;

        int frameCount = gif.frames.Length;
        for (int i = 0; i < frameCount; i++)
        {
            nextFrameTime += gif.frames[i].delay;
            _nextFrameTime[i] = nextFrameTime;
        }
    }

    void AdjustmentHeightAndWidth(GifData gif)
    {
        float gifWidth = gif.frames[0].sprite.texture.width;
        float gifHeight = gif.frames[0].sprite.texture.height;

        float newWidth = gifWidth * (_originalHeight / gifHeight);
        float newHeight = gifHeight * (_originalWidth / gifWidth);

        _rectTransform.sizeDelta = Vector2.Lerp(new Vector2(_originalWidth, newHeight), new Vector2(newWidth, _originalHeight), _fixedHeight);
    }




    //播放暂停等功能
    public void PlayFromBeginning()
    {
        ToBeginning();
        Play();
    }

    public void Play()
    {
        enabled = true;
    }

    public void Pause()
    {
        enabled = false;
    }

    public void Stop()
    {
        enabled = false;

        ToBeginning();
    }

    public void CleanData()
    {
        _gifData.frames = null;
        _nextFrameTime = null;
        ToBeginning();
    }

    void ToBeginning()
    {
        _playTime = 0;
    }




    //初始化和更新图片
    private void Awake()
    {
        _imageComponent = GetComponent<Image>();

        _originalWidth = _rectTransform.sizeDelta.x;
        _originalHeight = _rectTransform.sizeDelta.y;

        Stop();     //刚开始是没有数据的，自动Stop
    }

    private void Update()
    {
        _playTime += Time.deltaTime;
        UpdateGifFrame();
    }

    void UpdateGifFrame()
    {
        _imageComponent.sprite = GetCurrentSprite();
    }
    Sprite GetCurrentSprite()
    {
        float frameTime = (_playTime) % _nextFrameTime.Last();   //下一帧时间的最后一个数就是全部播放完的时间

        int framesCount = _gifData.frames.Length;
        for (int i = 0; i < framesCount; i++)
            if (frameTime < _nextFrameTime[i])          //时间小于下一帧时间，也就是说就在这一帧了
                return _gifData.frames[i].sprite;

        return _gifData.frames[0].sprite;       //一般来说应该到不了这，但如果真的到了，应该是时间超过最后一帧了，返回第一帧
    }
}
