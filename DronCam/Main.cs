using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Xml.XPath;
using AForge.Video;
using AForge.Video.DirectShow;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.UI;
using Emgu.Util;
using Emgu.Util;
using ZXing;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace DronCam
{
    public partial class Main : System.Windows.Forms.Form
    {
        private FilterInfoCollection videoDevices;
        private List<PictureBox> pictureBoxesList;
        private List<string> deviceList;
        private BarcodeReader _barcodeReader;
        private VideoCapture _videoCapture;
        public Main()
        {
            pictureBoxesList = new List<PictureBox>();
            _barcodeReader = new BarcodeReader();

            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            GetVideoDevices();
            for (int index = 0; index < videoDevices.Count; index++)
            {
                int localIndex = index;
                VideoCaptureDevice videoSource = new VideoCaptureDevice();
                videoSource = new VideoCaptureDevice(videoDevices[localIndex].MonikerString);
                videoSource.NewFrame += new NewFrameEventHandler((s, args) => videoSource_NewFrame(s, args, localIndex));
                videoSource.Start();
            }
            _videoCapture = new VideoCapture();
            Application.Idle += VideoCaptureOnFrameGrabbed;
        }
        private void VideoCaptureOnFrameGrabbed(object sender, EventArgs e)
        {
            Mat frame = new Mat();
            _videoCapture.Retrieve(frame, 0);

            if (frame != null)
            {
                FindAndDecodeBarcode(frame);
                imageBox1.Image = frame;
            }
        }
        private void GetVideoDevices()
        {
            videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            tableLayoutPanel1.ColumnCount = Math.Min(videoDevices.Count, 2);
            tableLayoutPanel1.RowCount = (int)Math.Ceiling((double)videoDevices.Count / tableLayoutPanel1.ColumnCount);
            //if (videoDevices.Count < 4) tableLayoutPanel1.ColumnCount = videoDevices.Count;
            //else if (videoDevices.Count > 3)
            //{
            //    tableLayoutPanel1.ColumnCount = 2;
            //    tableLayoutPanel1.RowCount = 2;
            //}

            for (int i = 0; i < tableLayoutPanel1.ColumnCount; i++)
            {
                tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            }

            for (int i = 0; i < tableLayoutPanel1.RowCount; i++)
            {
                tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            }

            AddPictureBoxes();
        }
        private void InitializeTableLayoutPanel()
        {
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            this.Controls.Add(tableLayoutPanel1);
        }
        private void AddPictureBoxes()
        {
            for (int row = 0; row < tableLayoutPanel1.RowCount; row++)
            {
                for (int col = 0; col < tableLayoutPanel1.ColumnCount; col++)
                {
                    PictureBox pictureBox = new PictureBox();
                    pictureBox.SizeMode = PictureBoxSizeMode.StretchImage;
                    pictureBoxesList.Add(pictureBox);
                    pictureBox.Dock = DockStyle.Fill;

                    tableLayoutPanel1.Controls.Add(pictureBox, col, row);
                }
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
           
        }


        private void videoSource_NewFrame(object sender, NewFrameEventArgs eventArgs, int index)
        {
            if (InvokeRequired)
            {
              Invoke(new Action(() => UpdatePictureBoxes(eventArgs.Frame.Clone() as Bitmap, index)));
            }
            else
            {
                UpdatePictureBoxes(eventArgs.Frame.Clone() as Bitmap, index);
            }
        }
        private void FindAndDecodeBarcode(Mat frame)
        {
            if (frame == null)
                return;

            Image<Bgr, byte> img = frame.ToImage<Bgr, byte>();

            // Преобразование кадра в изображение Gray для улучшения производительности
            UMat uimage = new UMat();
            CvInvoke.CvtColor(img, uimage, ColorConversion.Bgr2Gray);
            Bitmap grayBitmap = uimage.ToBitmap();
            // Декодирование штрих-кода
            var result = _barcodeReader.Decode(grayBitmap);

            if (result != null)
            {
                ShowBarcodeResult(result.Text);
            }
        }


        private void ShowBarcodeResult(string barcodeText)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => ShowBarcodeResult(barcodeText)));
            }
            else
            {
                // Обработка результатов штрих-кода
                textBox1.Text = barcodeText;
            }
        }

        private void UpdatePictureBoxes(Bitmap frame, int index)
        {
            pictureBoxesList[index].Image = frame;
        }

        private void Main_SizeChanged(object sender, EventArgs e)
        {
            AdjustTableLayoutPanelSize();
        }
        private void AdjustTableLayoutPanelSize()
        {
            // Устанавливаем новый размер для TableLayoutPanel
            tableLayoutPanel1.Size = new Size(this.ClientRectangle.Width - 20, this.ClientRectangle.Height - 100);

            // Рассчитываем количество столбцов и строк
            int columns = Math.Min(videoDevices.Count, 2);
            int rows = (int)Math.Ceiling((double)videoDevices.Count / columns);

            // Очищаем старые столбцы и строки
            tableLayoutPanel1.ColumnStyles.Clear();
            tableLayoutPanel1.RowStyles.Clear();

            // Устанавливаем новые размеры столбцов и строк
            for (int i = 0; i < columns; i++)
            {
                tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            }

            for (int i = 0; i < rows; i++)
            {
                tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            }
            Debug.WriteLine(tableLayoutPanel1.Size.Width + " " + tableLayoutPanel1.Size.Height);
            foreach (PictureBox pictureBox in pictureBoxesList)
            {
                tableLayoutPanel1.Controls.Remove(pictureBox);
            }
            pictureBoxesList.Clear();
            AddPictureBoxes();


        }
    }
}
