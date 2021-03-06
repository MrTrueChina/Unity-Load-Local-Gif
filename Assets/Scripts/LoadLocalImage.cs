using System.Collections.Generic;
using UnityEngine;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System;

public static class LoadLocalImage
{
    public static GifData LoadImage(string fullName)
    {
        if (fullName.Contains(".gif"))
            return LoadGif(fullName);
        else if(fullName.Contains(".png") || fullName.Contains(".jpg"))     //png和jpg似乎能共用一个方法
            return LoadPng(fullName);

        return new GifData();
    }


    //png   读取png的初版代码不知道扔哪了，只剩这个没多少注释的了，看到不认识的就百度，问题应该不大
    static GifData LoadPng(string fullName)
    {
        return new GifData() { frames = new GifFrame[] { new GifFrame { sprite = GetFileSprite(fullName), delay = 0 } } };
    }

    public static Sprite GetFileSprite(string fullName)
    {
        return GetFileBytes(fullName).ToTexture2D().ToSprite();
    }
    static byte[] GetFileBytes(string fullName)
    {
        using (FileStream fileStream = new FileStream(fullName, FileMode.Open, FileAccess.Read))  //路径，打开，只读
        {
            fileStream.Seek(0, SeekOrigin.Begin);                           //设置光标位置（读取位置）， Seek(偏移量, 起始点)

            byte[] bytes = new byte[fileStream.Length];                     //按照读取文件的长度创建缓冲区

            fileStream.Read(bytes, 0, Convert.ToInt32(fileStream.Length));  //把文件读进缓冲区里     Read(byte[] 读进去的缓冲区, int 偏转量, int 长度)   FileStream.Length 是 long，用Convert.ToInt32()正好能转成int32，Convert在System命名空间里

            return bytes;
        }
    }



    //gif   初版gif也找不到在哪了...
    static GifData LoadGif(string fullName)
    {
        Image gifImage = Image.FromFile(fullName);      //从文件读取出 Image对象，注意这个Image不是 UnityEngine.UI.Image，是 System.Drawing.Imaging.Image

        if (gifImage == null) return new GifData();



        List<GifFrame> gifData = new List<GifFrame>();

        FrameDimension dimension = new FrameDimension(gifImage.FrameDimensionsList[0]); //根据gif的第一个尺寸创建尺寸
        int framesCount = gifImage.GetFrameCount(dimension);                            //获取gif里这个尺寸的帧的总数
        for (int i = 0; i < framesCount; i++)
            gifData.Add(GetAFrameData(gifImage, dimension, i));

        return new GifData { frames = gifData.ToArray() };
    }

    static GifFrame GetAFrameData(Image gifImage, FrameDimension dimension, int frameIndex)
    {
        gifImage.SelectActiveFrame(dimension, frameIndex);                              //选择符合尺寸的，下标数量的帧，作为活跃帧

        GifFrame frameData = new GifFrame();

        frameData.sprite = GetActiveFrameSprite(gifImage);
        frameData.delay = GetActiveFrameSecondDelay(gifImage, frameIndex);

        return frameData;
    }

    static Sprite GetActiveFrameSprite(Image gifImage)
    {
        Bitmap bitmap = new Bitmap(gifImage.Width, gifImage.Height);                    //根据活跃帧的宽高创建 Bitmap

        System.Drawing.Graphics.FromImage(bitmap).DrawImage(gifImage, Point.Empty);     //匿名对象用完就丢，先创建Graphics对象，之后把gif活跃帧绘制进 bitmap，
        /*  
        上面一句的分解步骤：
        using (System.Drawing.Graphics newGraphics = System.Drawing.Graphics.FromImage(bitmap))     //这一步的参数 bitmap 是下一步绘制的目标位置     用using自动处理掉Graphice对象
        {
            newGraphics.DrawImage(gifImage, Point.Empty);   //这一步是绘制，将 gifImage 的活跃帧绘制到上一句设置的目标里，Point.Empty不明白什么用处
        }
        */

        byte[] bytes = bitmap.ToByteArray();

        Texture2D frame = new Texture2D(gifImage.Width, gifImage.Height);               //根据gif活跃帧宽高创建Texture2D对象

        frame.LoadImage(bytes);

        return frame.ToSprite();
    }

    static float GetActiveFrameSecondDelay(Image gifImage, int frameIndex)
    {
        for (int propertyIndex = 0; propertyIndex < gifImage.PropertyIdList.Length; propertyIndex++)
        {

            bool isFrameDelay = (int)gifImage.PropertyIdList.GetValue(propertyIndex) == 0x5100;
            /*
             * System.Drawing.Image.PropertyIdList.GetValue(int index)：从Image的属性表里获取指定索引的属性的ID，返回类型是object，需要配合类型转换使用
             * 0x5100，0x是十六进制开头，0x5100就是“十六进制的 5100”
             * ID 0x5100 属性：帧延迟，就是帧间隔时间，以 百分之一秒 为单位
             * 属性表：https://docs.microsoft.com/en-us/dotnet/api/system.drawing.imaging.propertyitem.id?redirectedfrom=MSDN&view=netframework-4.7.2#System_Drawing_Imaging_PropertyItem_Id
             * 
             * 单写一个bool是为了加注释，整段复制进if更好
              */

            if (isFrameDelay)
            {
                PropertyItem property = (PropertyItem)gifImage.PropertyItems.GetValue(propertyIndex);   //Image.PropertyItems.GetValue(int index)：从Image属性表里获取指定索引的属性，返回值同样是object，要配合类型转换使用

                return GetAFrameSecondDelay(frameIndex, property);
            }
        }
        return 0;
    }
    static float GetAFrameSecondDelay(int frameIndex, PropertyItem property)
    {
        byte[] delayArray = new byte[4];
        //用byte是因为从 PropertyItem 里获取到的值就是byte
        //关于这个4，首先这次用到的属性是帧延迟，这个属性的值是一个数组，长度是【gif帧数的 4 倍】，之所以是4倍，是因为一个byte范围是 0-255，如果某一帧时长超过2.55秒就会出错，用4个byte可以达到32位，可以应对绝大部分gif

        delayArray[0] = property.Value[frameIndex * 4];         //获取这一帧的延迟时间的第一个byte，对应int32的第1-8位    PropertyItem.Value：属性的值的数组
        delayArray[1] = property.Value[frameIndex * 4 + 1];     //第二个byte，对应int32的9-16位
        delayArray[2] = property.Value[frameIndex * 4 + 2];     //第三个，17-24位
        delayArray[3] = property.Value[frameIndex * 4 + 3];     //第四个，25-32位
                                                                //一定要按照这个对应方式，否则在转为int32时会出错

        int delay = BitConverter.ToInt32(delayArray, 0);        //BitConverter.ToInt32(byte[] value, int startIndex)：将byte数组转为int32，需要注意参数数组要【从低位到高位】排列

        return delay / 100f;    //转化成以秒为单位，返回
    }
    /*
     * 如果想要了解4个byte的实用表现可以尝试加载附带的 第一帧12345毫秒，第二帧1000毫秒.gif
     * 
     * 这个gif第一帧的帧延迟分别是   210      4        0        0
     * 按照二进制是                 11010010 00000100 00000000 00000000
     * 按照从低位到高位排列是        00000000 00000000 00000100 11010010
     * 
     * 转换为十进制是 1234，即以百分之一秒为单位的帧间隔
     */



    //扩展方法
    public static byte[] ToByteArray(this Bitmap bitmap)
    {
        using (MemoryStream stream = new MemoryStream())    //MemoryStream：内存流
        {
            bitmap.Save(stream, ImageFormat.Png);           //将 bitmap 以png格式存入内存流
            stream.Seek(0, SeekOrigin.Begin);               //将光标移动到流起点

            byte[] bytes = new byte[stream.Length];
            stream.Read(bytes, 0, Convert.ToInt32(stream.Length));      //System.Convert.ToInt32()：转为int32，流长度是long，转成int32正好

            return bytes;
        }
    }
    public static Texture2D ToTexture2D(this byte[] bytes)
    {
        Texture2D tex = new Texture2D(0, 0);
        tex.LoadImage(bytes);
        return tex;
    }
    public static Sprite ToSprite(this Texture2D texture)      //以扩展方法的方式给Texture2D增加转Sprite的方法
    {
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        //Sprite.Creat(Texture2D 源图像, Rect 截取范围, Vector2 轴心位置)
        //截取范围 = (x轴偏移, y轴偏移, 截取宽度, 截取高度)
        //轴心位置 = [0,1]范围的xy坐标，(0.5, 0.5)是正中心
    }
}
