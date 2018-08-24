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
/// gif数据，所有的帧
/// </summary>
public struct GifData
{
    public GifFrame[] frames;
}


/// <summary>
/// 播放gif的组件，需要挂载的物体有Image组件，也可以直接存入单张Sprite
/// </summary>
[RequireComponent(typeof(Image))]
public class ImagePlayer : MonoBehaviour
{
    Image _imageComponent;
    GifData _gifData;
    RectTransform _rectTransform;
    float[] _nextFrameTime;             //每次改变图像的时间，根据这些时间和播放时间可以确认应该显示的帧
    float _playTime;

    [SerializeField]
    [Range(0, 1)]
    float _AdaptableToFill;             //适应或填充，存入图片时根据图片分辨率调整Image组件的宽高，设为0则调整后的宽高都不大于原宽高，设为1则调整后的宽高都不小于原宽高
    float _originalWidth;
    float _originalHeight;
    float _originalAspectRatio;


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
                    delay = 0           //间隔写0就行
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


    /*
    假设Image是正方形，有四种情况
        1.图片是扁的、适应大小    ->  保持宽度
        2.图片是扁的、填充        ->  保持高度
        3.图片是高的、适应大小    ->  保持高度
        4.图片是高的、填充        ->  保持宽度

    假设Image不是正方形，宽度 / 高度 = WH
        1.图片WH > 显示WH、适应    ->  保持宽度
        2.图片WH > 显示WH、填充    ->  保持高度
        3.图片WH < 显示WH、适应    ->  保持高度
        4.图片WH < 显示WH、填充    ->  保持宽度

    需要先求出适应和填充，之后插值，则
        1.图片WH > 显示WH、适应    ->  保持宽度
        2.图片WH < 显示WH、适应    ->  保持高度
        3.图片WH > 显示WH、填充    ->  保持高度
        4.图片WH < 显示WH、填充    ->  保持宽度
    */
    void AdjustmentHeightAndWidth(GifData imageData)
    {
        Vector2 imageSize = new Vector2(imageData.frames[0].sprite.texture.width, imageData.frames[0].sprite.texture.height);
        //如果用 imageData.frames[0].sprite.textureRect.size 就能直接获取到宽高，但不知道为什么有时候宽高会获取错误，只能这样分两次获取了

        Vector2 adaptableSize = GetAdaptableSizeDelta(imageSize);
        Vector2 fillSize = GetFillSizeDelta(imageSize);

        _rectTransform.sizeDelta = Vector2.Lerp(adaptableSize, fillSize, _AdaptableToFill);
    }
    Vector2 GetAdaptableSizeDelta(Vector2 imageSize)
    {
        float gifAspectRatio = imageSize.x / imageSize.y;

        if (gifAspectRatio > _originalAspectRatio)
            return GetNewSizeWithFixedWidth(imageSize);
        else
            return GetNewSizeWithFixedHeight(imageSize);
    }
    Vector2 GetFillSizeDelta(Vector2 imageSize)
    {
        float gifAspectRatio = imageSize.x / imageSize.y;

        if (gifAspectRatio > _originalAspectRatio)
            return GetNewSizeWithFixedHeight(imageSize);
        else
            return GetNewSizeWithFixedWidth(imageSize);
    }
    Vector2 GetNewSizeWithFixedHeight(Vector2 imageSize)
    {
        float newWidth = imageSize.x * (_originalHeight / imageSize.y);

        return new Vector2(newWidth, _originalHeight);
    }
    Vector2 GetNewSizeWithFixedWidth(Vector2 imageSize)
    {
        float newHeight = imageSize.y * (_originalWidth / imageSize.x);

        return new Vector2(_originalWidth, newHeight);
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




    //初始化和更新
    private void Awake()
    {
        _imageComponent = GetComponent<Image>();
        _rectTransform = GetComponent<RectTransform>();

        _originalWidth = _rectTransform.sizeDelta.x;
        _originalHeight = _rectTransform.sizeDelta.y;
        _originalAspectRatio = _originalWidth / _originalHeight;

        Stop();     //刚开始是没有数据的，自动Stop
    }

    private void Update()
    {
        //Debug.Log("15.2 % 2.5 = " + (15.2 % 2.5));    //输出是 0.5，这一步可以看出C#的取余规则不要求必须整数，取余只取到不能整除的部分
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
