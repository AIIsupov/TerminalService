using Leaf.xNet;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Usb.Events;

namespace TerminalService
{
    public class TerminalService
    {
        private BarDecoder.Decoder decoder;

        private readonly IUsbEventWatcher usbEventWatcher = new UsbEventWatcher();

        private static string nameService = "Service:> ";
        private static string os;
        private bool IsStart = false;
        private int currentCountPorts;

        static void Main(string[] args)
        {
            os = Environment.OSVersion.Platform.ToString();
            //Console.WriteLine(nameService + os);
            //Console.WriteLine(nameService+"Ожидание команды...");
            new TerminalService();
        }

        public TerminalService()
        {
            StartService();
        }

        private void StartService()
        {
            if (!IsStart)
            {
                Console.WriteLine(nameService + "Запуск сервиса...");
                startDecoder();
                IsStart = true;
                Console.WriteLine(nameService + "Сервис выполнил все запуски");
            }
            else
            {
                Console.WriteLine(nameService + "Уже в процессе");
            }
            waitCommand();
        }

        
        private void waitCommand()
        {
            Console.Write(nameService);
            string command = Console.ReadLine();
            switch (command)
            {
                case "start":
                    StartService();
                    break;
                case "start decoder":
                    startDecoder();
                    break;
                case "stop decoder":
                    stopDecoder();
                    break;
                case "restart decoder":
                    restartDecoder();
                    break;
                case "stop":
                    stopService();
                    break;
                case "clear":
                    Console.Clear();
                    break;
                default:
                    Console.WriteLine(nameService + "Команда {0} не найдена", command);
                    break;
            }
            waitCommand();
        }
        
        private void startDecoder()
        {
            if (decoder == null)
            {
                decoder = new BarDecoder.Decoder();
                decoder.StartDecode();
                getCurrentCountPorts();
                decoder.onBarcodeDecode += Decoder_onBarcodeDecode;

                usbEventWatcher.UsbDeviceAdded += UsbEventWatcher_UsbDeviceAdded;
                usbEventWatcher.UsbDeviceRemoved += UsbEventWatcher_UsbDeviceRemoved;
            }
            else
            {
                Console.WriteLine(nameService+"Декодер уже запущен!");
            }
        }

        private void Decoder_onBarcodeDecode(BarDecoder.Decoder decoder, BarDecoder.Decoder.BarcodeValue data)
        {
            var postData = JsonConvert.SerializeObject(data);
            var request = new HttpRequest();

            try
            {
                string json = "";
                string json_path = "";
                if (os.StartsWith("win"))
                {
                    json_path = Environment.CurrentDirectory + "\\connect\\Connection.json";
                }
                else
                {
                    json_path = Environment.CurrentDirectory + "/connect/Connection.json";
                }
                using (StreamReader sr = new StreamReader(json_path, Encoding.Default))
                {
                    json = sr.ReadToEnd();
                }
                using (StreamReader file = File.OpenText(json_path))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    json = (serializer.Deserialize(file, typeof(Dictionary<string, string>)) as Dictionary<string, string>)["connectionString"];
                }
                var res = request.Post(json, postData, "application/json").ToString();
            }
            catch
            {

            }
            Console.Write(nameService);
        }

        private void UsbEventWatcher_UsbDeviceAdded(object sender, UsbDevice e)
        {
            if (decoder.GetPortCount() > currentCountPorts)
            {
                Console.WriteLine(nameService + "Обнаружен доступный COM порт!");
                getCurrentCountPorts();                
                restartDecoder();
            }
        }

        private void UsbEventWatcher_UsbDeviceRemoved(object sender, UsbDevice e)
        {
            if (decoder.GetPortCount() < currentCountPorts)
            {
                Console.WriteLine(nameService + "Потерян доступ к одному из COM портов!");
                getCurrentCountPorts();
                restartDecoder();
            }
        }

        private void getCurrentCountPorts()
        {
            currentCountPorts = decoder.GetPortCount();
            Console.WriteLine(nameService + "Количество доступных COM портов: " + currentCountPorts);
        }

        private void restartDecoder()
        {
            if (decoder != null)
            {
                Console.WriteLine(nameService + "Перезапуск декодера!");
                stopDecoder();
                startDecoder();
            }
            else
            {
                Console.WriteLine(nameService + "Декодер выключен!");
            }
        }

        private void stopDecoder()
        {
            if (decoder != null)
            {
                decoder.StopDecode();
                decoder = null;
            }
            else
            {
                if (new Random().Next(0,100) == new Random().Next(0, 100))
                {
                    Console.WriteLine(nameService + "Чтобы что-то остановить, нужно сначала запустить");
                }
                else
                {
                    Console.WriteLine(nameService + "Декодер выключен!");
                }
                
            }
            
        }

        private void stopService()
        {
            Console.WriteLine("Остановка сервиса...");
            stopDecoder();
            Environment.Exit(0);
        }
    }
}
