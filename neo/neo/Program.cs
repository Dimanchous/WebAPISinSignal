using System;
using System.Drawing;
using System.Net;
using System.Drawing.Imaging;

namespace neo
{
    class Program
    {
        static void Main(string[] args)
        {
            //прослушиваем локальный адрес
            Network Network = new Network("http://localhost:8080/");
            while (true)

            //получаем данные через HTTP-запрос
            {
                Dictionary<string, string> Data = Network.GetData();
                SignalController.Create(double.Parse(Data["A"]), double.Parse(Data["Fd"]), double.Parse(Data["Fs"]), int.Parse(Data["N"]));
                Network.UploadFile("1.png");
            }
        }

        //класс - HTTP-сервер
        public class Network 
        {
            private HttpListener listener;
            private HttpListenerContext context;
            private HttpListenerRequest request;
            private HttpListenerResponse response;
            public Network(string url)
            {
                listener = new HttpListener();
                listener.Prefixes.Add(url);
                listener.Start();
            }

            //принимаем данные из HTTP-запроса
            public Dictionary<string, string> GetData() 
            {
                context = listener.GetContext();
                request = context.Request;
                response = context.Response;
                return DataParse(request.RawUrl);
            }

            //отправляем файл клиенту
            public void UploadFile(string FilePath)
            {
                byte[] fileBytes = File.ReadAllBytes(FilePath);

                response.ContentType = "application/octet-stream";
                response.ContentLength64 = fileBytes.Length;
                response.AddHeader("Content-Disposition", $"attachment; filename=\"{Path.GetFileName(FilePath)}\"");

                Stream outputStream = response.OutputStream;
                outputStream.Write(fileBytes, 0, fileBytes.Length);
                outputStream.Close();
            }

            //разбираем строку запроса, извлекам ключи и значения
            private Dictionary<string, string> DataParse(string queryString)
            {
                Dictionary<string, string> parameters = new Dictionary<string, string>();

                if (queryString.StartsWith("/?"))
                {
                    queryString = queryString.Substring(2);
                }

                string[] keyValuePairs = queryString.Split('&');

                foreach (string keyValue in keyValuePairs)
                {
                    string[] parts = keyValue.Split('=');
                    if (parts.Length == 2)
                    {
                        string key = Uri.UnescapeDataString(parts[0]);
                        string value = Uri.UnescapeDataString(parts[1]);
                        parameters[key] = value;
                    }
                }
                return parameters;
            }
        }

        //создаем синусоидальный сигнал и сохраняем его как PNG
        static public class SignalController
        {

            static public void Create(double amplitude, double sampleRate, double frequency, int periods)
            {
                FileStream fileStream = File.Create("1.png");

                int width = 800;
                int height = 400;

                using (Bitmap bitmap = new Bitmap(width, height))
                using (Graphics graphics = Graphics.FromImage(bitmap))
                {
                    Pen pen = new Pen(Color.Blue, 2);

                    for (int x = 0; x < width; x++)
                    {
                        double t = x / sampleRate;
                        double y = amplitude * Math.Sin(2 * Math.PI * frequency * t);

                        int yOffset = height / 2 - (int)(y * (height / 2));
                        graphics.DrawRectangle(pen, x, yOffset, 1, 1);
                    }

                    MemoryStream stream = new MemoryStream();
                    bitmap.Save(stream, ImageFormat.Png);
                    stream.Seek(0, SeekOrigin.Begin);
                    stream.CopyTo(fileStream);
                    fileStream.Close();

                }
            }
        }
    }
}