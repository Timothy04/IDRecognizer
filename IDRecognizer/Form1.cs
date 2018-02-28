using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using AForge.Video;
using AForge.Video.DirectShow;
using Newtonsoft.Json;

namespace IDRecognizer
{
    public partial class Form1 : Form
    {
        private FilterInfoCollection VideoCaptureDevices;
        private VideoCaptureDevice FinalVideoSource;
        Bitmap image;
        Timer t;

        string _subscriptionKey = "3326bad869ce401994724aaf67b854ea";
        string _ocrEndpoint = "https://westcentralus.api.cognitive.microsoft.com/vision/v1.0/ocr";

        public Form1()
        {
            InitializeComponent();

            VideoCaptureDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            FinalVideoSource = new VideoCaptureDevice(VideoCaptureDevices[0].MonikerString);
            FinalVideoSource.NewFrame += new NewFrameEventHandler(FinalVideoSource_NewFrame);
            FinalVideoSource.Start();

            t = new Timer();
            t.Interval = 15 * 1000;
            t.Tick += T_Tick;
        }

        void FinalVideoSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            image = (Bitmap)eventArgs.Frame.Clone();
            pictureBox1.Image = image;
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            ReadWords();
        }

        public async void ReadWords()
        {
            HttpClient client = new HttpClient();

            // Request headers.
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _subscriptionKey);

            // Request parameters.
            string requestParameters = "language=unk&detectOrientation=true";

            // Assemble the URI for the REST API Call.
            string uri = _ocrEndpoint + "?" + requestParameters;

            HttpResponseMessage response;

            // Request body. Posts a locally stored JPEG image.
            byte[] byteData = (byte[])new ImageConverter().ConvertTo(image, typeof(byte[]));

            using (ByteArrayContent content = new ByteArrayContent(byteData))
            {
                // This example uses content type "application/octet-stream".
                // The other content types you can use are "application/json" and "multipart/form-data".
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                // Execute the REST API call.
                response = await client.PostAsync(uri, content);

                // Get the JSON response.
                string contentString = await response.Content.ReadAsStringAsync();

                // Handle json
                List<string> words = new List<string>();
                var myObject = JsonConvert.DeserializeObject<OcrResult>(contentString);
                foreach (var region in myObject.regions)
                    foreach (var line in region.lines)
                        foreach (var word in line.words)
                        {
                            words.Add(word.text);
                        }

                string textToRead = string.Join(" ", words.ToArray());
                if (myObject.language != "unk")
                {
                    txtInfo.Text = "(language=" + myObject.language + ")";
                }
                else
                {
                    txtInfo.Text = "language unknown.";
                }

                txtInfo.Text += "\n" + textToRead;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ReadWords();
            t.Start();
        }

        private void T_Tick(object sender, EventArgs e)
        {
            ReadWords();
        }
    }

    [Serializable]
    public class OcrResult
    {
        public string language;
        public float textAngle;
        public string orientation;
        public Region[] regions;
    }

    [Serializable]
    public class Region
    {
        public string boundingBox;
        public Line[] lines;
    }

    [Serializable]
    public class Line
    {
        public string boundingBox;
        public Word[] words;
    }

    [Serializable]
    public class Word
    {
        public string boundingBox;
        public string text;
    }
}
