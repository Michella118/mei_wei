using HalconDotNet;
using MvCamCtrl.NET;
using MvCameraControl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static MvCamCtrl.NET.MyCamera;

namespace BoardMeasure
{
    internal class CameraControl
    {
        int nRet;
        MV_CC_DEVICE_INFO_LIST m_stDevList;
        public int CameraEnumeration()  //枚举相机
        {
            try
            {
                nRet = MyCamera.MV_CC_EnumDevices_NET(MyCamera.MV_GIGE_DEVICE | MyCamera.MV_USB_DEVICE, ref m_stDevList);
                if (m_stDevList.nDeviceNum == 0)
                {
                    //没找到相机
                    return -1;
                }

                return nRet;
            } 
            catch 
            { return -2; }
        }
        public MyCamera CreateAndOpenDevice(int deviceIndex = 0)
        {
            MyCamera camera = new MyCamera();
            IntPtr pDeviceInfo = m_stDevList.pDeviceInfo[deviceIndex];
            MyCamera.MV_CC_DEVICE_INFO deviceInfo = (MyCamera.MV_CC_DEVICE_INFO)Marshal.PtrToStructure(
                pDeviceInfo,
                typeof(MyCamera.MV_CC_DEVICE_INFO));
            // 3. 创建设备句柄
            nRet = camera.MV_CC_CreateDevice_NET(ref deviceInfo);
            if (nRet != 0)
            {
                return null;
            }

            // 4. 打开设备（此时才真正获得可操作的句柄）
            nRet = camera.MV_CC_OpenDevice_NET();
            if (nRet != 0)
            {
                camera.MV_CC_DestroyDevice_NET();  // 清理资源
                return null;
            }

            return camera;  // 这个camera对象就包含了有效的设备句柄
        }
        public int CloseDevice(MyCamera camera)
        {
            if (camera == null)
            {
                return -1;
            }

            try
            {
                // 停止采集
                StopGrabbing(camera);

                // 关闭设备
                camera.MV_CC_CloseDevice_NET();
                camera.MV_CC_DestroyDevice_NET();

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"关闭设备异常: {ex.Message}");
                return -2;
            }
        }
        public int StartGrabbing(MyCamera camera)
        {
            if (camera == null)
            {
                return -1;
            }

            nRet = camera.MV_CC_StartGrabbing_NET();
            return nRet;
        }
        public int StopGrabbing(MyCamera camera)
        {
            if (camera == null)
            {
                return -1;
            }

            nRet = camera.MV_CC_StopGrabbing_NET();
            return nRet;
        }
        public string GetDeviceInfo(int deviceIndex) //获取设备信息
        {
            if (deviceIndex < 0 || deviceIndex >= m_stDevList.nDeviceNum)
            {
                return "无效的设备索引";
            }

            try
            {
                IntPtr pDeviceInfo = m_stDevList.pDeviceInfo[deviceIndex];
                MyCamera.MV_CC_DEVICE_INFO deviceInfo = new MyCamera.MV_CC_DEVICE_INFO();
                Marshal.PtrToStructure(pDeviceInfo, deviceInfo);

                if (deviceInfo.nTLayerType == MyCamera.MV_GIGE_DEVICE)
                {
                    MyCamera.MV_GIGE_DEVICE_INFO gigeInfo =
                        (MyCamera.MV_GIGE_DEVICE_INFO)MyCamera.ByteToStruct(
                            deviceInfo.SpecialInfo.stGigEInfo,
                            typeof(MyCamera.MV_GIGE_DEVICE_INFO));

                    return $"GigE设备: {gigeInfo.chManufacturerName} - {gigeInfo.chModelName}";
                }
                else if (deviceInfo.nTLayerType == MyCamera.MV_USB_DEVICE)
                {
                    MyCamera.MV_USB3_DEVICE_INFO usbInfo =
                        (MyCamera.MV_USB3_DEVICE_INFO)MyCamera.ByteToStruct(
                            deviceInfo.SpecialInfo.stUsb3VInfo,
                            typeof(MyCamera.MV_USB3_DEVICE_INFO));

                    return $"USB设备: {usbInfo.chManufacturerName} - {usbInfo.chModelName}";
                }

                return "未知设备类型";
            }
            catch
            {
                return "获取设备信息失败";
            }
        }
        public int SetFPS(MyCamera camera, float value)//设置帧率
        {
            if (camera == null)
            {
                return -1;
            }

            // 参数范围检查
            if (value >= 0.1f && value <= 100000.0f)
            {
                nRet = camera.MV_CC_SetFloatValue_NET("AcquisitionFrameRate", value);
                return nRet;
            }
            else
            {
                return -1; // 参数错误
            }
        }
        public int SetTriggerMode(MyCamera camera, string value)//设置触发模式
        {
            if (camera == null)
            {
                return -1;
            }

            uint enumValue = 0;
            switch (value)
            {
                case "关闭":
                    enumValue = 0; // Off
                    break;
                case "打开":
                    enumValue = 1; // On
                    break;
                default:
                    return -1; // 参数错误
            }

            nRet = camera.MV_CC_SetEnumValue_NET("TriggerMode", enumValue);
            return nRet;
        }
        public int SetTriggerSource(MyCamera camera, string value)//设置触发源
        {
            if (camera == null)
            {
                return -1;
            }

            uint enumValue = 0;
            switch (value)
            {
                case "软触发":
                    enumValue = 7; // Software
                    break;
                case "线路0":
                    enumValue = 0; // Line0
                    break;
                case "线路2":
                    enumValue = 2; // Line2
                    break;
                case "计数器0":
                    enumValue = 4; // Counter0
                    break;
                case "多路":
                    enumValue = 25; // Multi
                    break;
                default:
                    return -1; // 参数错误
            }

            nRet = camera.MV_CC_SetEnumValue_NET("TriggerSource", enumValue);
            return nRet;
        }
        public int SetTriggerActivation(MyCamera camera, string value)//设置触发激活方式
        {
            if (camera == null)
            {
                return -1;
            }

            uint enumValue = 0;
            switch (value)
            {
                case "上升沿":
                    enumValue = 0; // RisingEdge
                    break;
                case "下降沿":
                    enumValue = 1; // FallingEdge
                    break;
                case "高电平":
                    enumValue = 2; // LevelHigh
                    break;
                case "低电平":
                    enumValue = 3; // LevelLow
                    break;
                case "上升或下降沿":
                    enumValue = 4; // AnyEdge
                    break;
                default:
                    return -1; // 参数错误
            }

            nRet = camera.MV_CC_SetEnumValue_NET("TriggerActivation", enumValue);
            return nRet;
        }
        public int SetTriggerDelay(MyCamera camera, float value)//设置触发延迟
        {
            if (camera == null)
            {
                return -1;
            }

                nRet = camera.MV_CC_SetFloatValue_NET("TriggerDelay", value);
                return nRet;

        }
        public int SetExposureTime(MyCamera camera, float value)//设置曝光时间
        {
            if (camera == null)
            {
                return -1;
            }


                nRet = camera.MV_CC_SetFloatValue_NET("ExposureTime", value);
                return nRet;

        }
        public int SetGain(MyCamera camera, float value)//设置增益
        {
            if (camera == null)
            {
                return -1;
            }

            // 参数范围检查

                nRet = camera.MV_CC_SetFloatValue_NET("Gain", value);
                return nRet;
            

        }
        public int SetLineSelector(MyCamera camera, string value)//设置线路选择
        {
            if (camera == null)
            {
                return -1;
            }

            uint enumValue = 0;
            switch (value)
            {
                case "线路0":
                    enumValue = 0; // Line0
                    break;
                case "线路1":
                    enumValue = 1; // Line1
                    break;
                case "线路2":
                    enumValue = 2; // Line2
                    break;
                default:
                    return -1; // 参数错误
            }

            nRet = camera.MV_CC_SetEnumValue_NET("LineSelector", enumValue);
            return nRet;
        }
        public int SetLineMode(MyCamera camera, string value)//设置线路模式
        {
            if (camera == null)
            {
                return -1;
            }

            uint enumValue = 0;
            switch (value)
            {
                case "输入":
                    enumValue = 0; // Input
                    break;
                case "频闪输出":
                    enumValue = 8; // Strobe
                    break;
                default:
                    return -1; // 参数错误
            }

            nRet = camera.MV_CC_SetEnumValue_NET("LineMode", enumValue);
            return nRet;
        }
        public int TriggerSoftware(MyCamera camera)//软触发一次
        {
            if (camera == null)
            {
                return -1;
            }

            nRet = camera.MV_CC_SetCommandValue_NET("TriggerSoftware");
            return nRet;
        }

        internal void SetDisplayWindow(HSmartWindowControl hSmartWindowControl1)
        {
            throw new NotImplementedException();
        }
    };
    

}
