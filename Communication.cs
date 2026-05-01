using HslCommunication;
using HslCommunication.ModBus;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BoardMeasure
{
    internal class Communication
    {
        // Modbus TCP 服务器实例
        HslCommunication.ModBus.ModbusTcpServer modbusServer
            = new HslCommunication.ModBus.ModbusTcpServer();

        // 后台读取线程
        private Thread _readThread;
        private bool _stopReading = false;

        // 定义事件委托
        public delegate void RegisterValueUpdatedHandler(long registerValue);
        public event RegisterValueUpdatedHandler Register41ValueUpdated;

        // 启动服务器
        public void StartServer()
        {
            
            //modbusServer.EnableWrite = true;
            //modbusServer.EnableIPv6 = false;
            modbusServer.Station = 1;
            //modbusServer.StationDataIsolation = false;
            //modbusServer.UseModbusRtuOverTcp = false;
            modbusServer.IsStringReverse = false;
            //modbusServer.EnableWriteMaskCode = true;
            modbusServer.DataFormat = HslCommunication.Core.DataFormat.CDAB;
            //modbusServer.ActiveTimeSpan = TimeSpan.Parse("01:00:00");
            //modbusServer.LocalAddress = System.Net.IPAddress.Parse("192.168.1.199");
            //modbusServer.EnableIPv6 = false;
            modbusServer.ServerStart(502);
            //modbusServer.RegisteredAddressMapping(null);
            
            Console.WriteLine("Modbus TCP 服务器已启动，监听 192.168.1.199:502");

            // 启动后台读取线程
            StartReadingThread();
        }

        // 启动后台读取线程
        private void StartReadingThread()
        {
            _stopReading = false;
            _readThread = new Thread(ReadRegisterLoop);
            _readThread.IsBackground = true; // 设置为后台线程
            _readThread.Start();
            Console.WriteLine("寄存器读取线程已启动");
        }

        // 寄存器读取循环
        private void ReadRegisterLoop()
        {
            long lastValue = 0;

            while (!_stopReading)
            {
                try
                {
                    // 读取地址 41 的 64 位整数
                    OperateResult<long> read = modbusServer.ReadInt64("41");

                    if (read.IsSuccess)
                    {
                        long currentValue = read.Content;

                        // 只有值变化时才触发事件
                        if (currentValue != lastValue)
                        {
                            lastValue = currentValue;
                            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 读取到寄存器41值: {currentValue}");

                            // 触发事件，通知 Form1 更新 UI
                            Register41ValueUpdated?.Invoke(currentValue);
                        }
                    }
                    else
                    {
                        Console.WriteLine($"读取寄存器41失败: {read.Message}");
                    }

                    // 每 100 毫秒读取一次
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"读取寄存器异常: {ex.Message}");
                    Thread.Sleep(1000); // 出错后等待1秒再重试
                }
            }

            Console.WriteLine("寄存器读取线程已停止");
        }

        // 停止读取线程
        public void StopReading()
        {
            _stopReading = true;
            if (_readThread != null && _readThread.IsAlive)
            {
                _readThread.Join(1000); // 等待线程结束，最多等待1秒
            }
        }

        // 停止服务器
        public void StopServer()
        {
            // 先停止读取线程
            StopReading();

            // 停止 Modbus 服务器
            if (modbusServer != null)
            {
                modbusServer.ServerClose();
                Console.WriteLine("Modbus TCP 服务器已停止");
            }
        }

    }
}