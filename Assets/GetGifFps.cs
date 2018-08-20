using System;
using System.Drawing;
using System.Drawing.Imaging;

public static class GetGifFps
{
    static float GetGifSpeed(string fullName)
    {
        Image gifImage = Image.FromFile(fullName);
        FrameDimension dimension = new FrameDimension(gifImage.FrameDimensionsList[0]);
        int frameCount = gifImage.GetFrameCount(dimension);

        if (frameCount <= 1)
            return 0;
        else
        {
            for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
            {
                gifImage.SelectActiveFrame(dimension, frameIndex);              //根据传入的尺寸和索引选择活跃帧

                float delay = GetActiveFrameSecondDelay(gifImage, frameIndex);

                if (delay != 0) return 1.0f / delay;    //1秒 / 1秒延迟时间 = 每秒帧数
            }
        }
        return 0;
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
        //关于这个4，首先这次用到的属性是帧延迟，这个属性的值是一个数组，长度是【gif帧数的 4 倍】，之所以是4倍，是因为一个byte范围是 0-255，如果某一帧时长超过2.55秒就会出错，用4个byte可以达到32位，可以应对大部分gif

        delayArray[0] = property.Value[frameIndex * 4];         //获取这一帧的延迟时间的第一个byte，对应int32的第1-8位    PropertyItem.Value：属性的值的数组
        delayArray[1] = property.Value[frameIndex * 4 + 1];     //第二个byte，对应int32的9-16位
        delayArray[2] = property.Value[frameIndex * 4 + 2];     //第三个，17-24位
        delayArray[3] = property.Value[frameIndex * 4 + 3];     //第四个，25-32位
                                                                //一定要按照这个对应方式，否则在转为int32时会出错

        int delay = BitConverter.ToInt32(delayArray, 0);        //BitConverter.ToInt32(byte[] value, int startIndex)：将byte数组转为int32，需要注意参数数组要【从低位到高位】排列

        return delay / 100f;    //转化成以秒为单位，返回
    }
}
