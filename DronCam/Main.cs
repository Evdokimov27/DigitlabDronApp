using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using AForge.Video;
using AForge.Video.DirectShow;
using ZXing;

namespace DronCam
{
	public partial class Main : System.Windows.Forms.Form
    {
        private FilterInfoCollection videoDevices;
        private List<PictureBox> pictureBoxesList;
        private bool[] qrCodeDetectedStates; // Состояния для каждого QR-кода
        private bool allQRCodesDetected = false; // Переменная для общего состояния

        public Main()
        {
            pictureBoxesList = new List<PictureBox>();
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            GetVideoDevices();
            qrCodeDetectedStates = new bool[4];
            for (int i = 0; i < qrCodeDetectedStates.Length; i++)
            {
                qrCodeDetectedStates[i] = false;
            }
            for (int index = 0; index < videoDevices.Count; index++)
            {
                int localIndex = index;
                VideoCaptureDevice videoSource = new VideoCaptureDevice();
                videoSource = new VideoCaptureDevice(videoDevices[localIndex].MonikerString);
                videoSource.NewFrame += new NewFrameEventHandler((s, args) => videoSource_NewFrame(s, args, localIndex));
                videoSource.Start();
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
        private void DrawBoundingBox(PictureBox pictureBox, ResultPoint[] resultPoints)
        {
            Graphics graphics = Graphics.FromImage(pictureBox.Image);
            Pen pen = new Pen(Color.Red, 2);

            // Преобразование координат ResultPoint в координаты PictureBox
            PointF[] points = Array.ConvertAll(resultPoints, rp => new PointF(rp.X * pictureBox.Width, rp.Y * pictureBox.Height));

            // Отрисовка прямоугольника вокруг QR-кода
            graphics.DrawPolygon(pen, points);

            // Обновление PictureBox
            pictureBox.Invalidate();
        }

        private void DecodeQRCode(Bitmap bitmap, int index)
        {
            try
            {
                BarcodeReader reader = new BarcodeReader();
                Result[] results = reader.DecodeMultiple(bitmap);

                if (results != null && results.Length > 0)
                {
                    foreach (Result result in results)
                    {
                        string decodedData = result.Text;
                        Console.WriteLine($"{index} камера распознала QR код: {decodedData}");

                        if (decodedData == "верх")
                        {
                            qrCodeDetectedStates[0] = true;
                            DrawBoundingBox(pictureBoxesList[index], result.ResultPoints);
                        }
                        if (decodedData == "низ")
                        {
                            qrCodeDetectedStates[1] = true;
                            DrawBoundingBox(pictureBoxesList[index], result.ResultPoints);
                        }
                        if (decodedData == "лево")
                        {
                            qrCodeDetectedStates[2] = true;
                            DrawBoundingBox(pictureBoxesList[index], result.ResultPoints);
                        }
                        if (decodedData == "право")
                        {
                            qrCodeDetectedStates[3] = true;
                            DrawBoundingBox(pictureBoxesList[index], result.ResultPoints);
                        }
                    }
                }

                CheckAllQRCodesDetected();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error decoding QR Codes: " + ex.Message);
            }
        }

        private void CheckAllQRCodesDetected()
        {
            // Проверка, обнаружены ли все QR-коды
            allQRCodesDetected = true;
            for (int i = 0; i < qrCodeDetectedStates.Length; i++)
            {
                if (!qrCodeDetectedStates[i])
                {
                    allQRCodesDetected = false;
                    break;
                }
            }

            // Вывод соответствующего сообщения
            if (allQRCodesDetected)
            {
                Console.WriteLine("Найдено!");
            }
            Console.WriteLine($"{qrCodeDetectedStates[0]} {qrCodeDetectedStates[1]} {qrCodeDetectedStates[2]} {qrCodeDetectedStates[3]}");

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


        private void UpdatePictureBoxes(Bitmap frame, int index)
        {
            pictureBoxesList[index].Image = frame;
            DecodeQRCode(frame, index);
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
