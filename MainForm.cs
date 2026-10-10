using System.Drawing.Drawing2D;
using System.Globalization;

namespace Image3D
{
    public class MainForm : Form
    {
        Bitmap img;
        double[,] heights;
        Color[,] colors;
        bool useColor = true;
        int cols = 60;
        int rows;

        double angleZ = -25 * Math.PI / 180;
        double angleX = 52 * Math.PI / 180;
        double zoom = 1;
        double heightK = 0.25;

        Point lastMouse;
        FlowLayoutPanel panel1;
        TrackBar trackBar1;
        TrackBar trackBar2;
        CheckBox checkBox1;
        Pen pen = new Pen(Color.Pink, 1);

        public MainForm()
        {
            Text = "пепе 3D";
            ClientSize = new Size(1100, 700);
            BackColor = Color.Black;
            DoubleBuffered = true;

            panel1 = new FlowLayoutPanel();
            panel1.Dock = DockStyle.Top;
            panel1.Height = 50;
            panel1.BackColor = Color.FromArgb(67, 67, 67);
            panel1.WrapContents = false;
            panel1.Padding = new Padding(6, 8, 6, 0);
            Controls.Add(panel1);

            Button button1 = new Button();
            button1.Text = "отткрыть";
            button1.AutoSize = true;
            button1.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            button1.Padding = new Padding(10, 2, 10, 2);
            button1.MinimumSize = new Size(0, 30);
            button1.ForeColor = Color.White;
            button1.FlatStyle = FlatStyle.System;
            button1.Click += button1_Click;
            panel1.Controls.Add(button1);

            Label label1 = new Label();
            label1.Text = "детализация";
            label1.AutoSize = true;
            label1.ForeColor = Color.White;
            label1.Margin = new Padding(14, 7, 4, 0);
            panel1.Controls.Add(label1);

            trackBar1 = new TrackBar();
            trackBar1.Width = 160;
            trackBar1.Minimum = 20;
            trackBar1.Maximum = 150;
            trackBar1.Value = cols;
            trackBar1.TickStyle = TickStyle.None;
            trackBar1.Scroll += trackBar1_Scroll;
            panel1.Controls.Add(trackBar1);

            Label label2 = new Label();
            label2.Text = "высота";
            label2.AutoSize = true;
            label2.ForeColor = Color.White;
            label2.Margin = new Padding(14, 7, 4, 0);
            panel1.Controls.Add(label2);

            trackBar2 = new TrackBar();
            trackBar2.Width = 160;
            trackBar2.Minimum = 0;
            trackBar2.Maximum = 100;
            trackBar2.Value = 25;
            trackBar2.TickStyle = TickStyle.None;
            trackBar2.Scroll += trackBar2_Scroll;
            panel1.Controls.Add(trackBar2);

            checkBox1 = new CheckBox();
            checkBox1.Text = "Цвет";
            checkBox1.Checked = true;
            checkBox1.AutoSize = true;
            checkBox1.ForeColor = Color.White;
            checkBox1.Margin = new Padding(14, 6, 4, 0);
            checkBox1.CheckedChanged += checkBox1_CheckedChanged;
            panel1.Controls.Add(checkBox1);

            MouseDown += MainForm_MouseDown;
            MouseMove += MainForm_MouseMove;
            MouseWheel += MainForm_MouseWheel;
            Resize += MainForm_Resize;
        }

        void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "Картинки и FDF|*.png;*.jpg;*.jpeg;*.bmp;*.fdf|Все файлы|*.*";
            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            if (Path.GetExtension(dlg.FileName).ToLower() == ".fdf")
                OpenFdf(dlg.FileName);
            else
                OpenImage(dlg.FileName);
        }

        void OpenImage(string fileName)
        {
            Bitmap tmp;
            try
            {
                tmp = new Bitmap(fileName);
            }
            catch
            {
                MessageBox.Show("не получилось открыть файл");
                return;
            }

            if (img != null)
                img.Dispose();
            img = new Bitmap(tmp);
            tmp.Dispose();

            cols = trackBar1.Value;
            MakeGrid();
        }

       
        void OpenFdf(string fileName)
        {
            try
            {
                List<string[]> table = new List<string[]>();
                foreach (string line in File.ReadAllLines(fileName))
                {
                    if (line.Trim() == "")
                        continue;
                    table.Add(line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries));
                }

                int r = table.Count;
                int c = 0;
                for (int j = 0; j < r; j++)
                    if (table[j].Length > c)
                        c = table[j].Length;

                if (c < 2 || r < 2)
                {
                    MessageBox.Show("в файле мало точек");
                    return;
                }

                double[,] z = new double[c, r];
                Color[,] col = new Color[c, r];
                for (int j = 0; j < r; j++)
                {
                    for (int i = 0; i < c; i++)
                    {
                        col[i, j] = Color.Pink;
                        if (i >= table[j].Length)
                            continue;

                        string[] part = table[j][i].Split(',');
                        z[i, j] = double.Parse(part[0], CultureInfo.InvariantCulture);
                        if (part.Length > 1)
                        {
                            int rgb = Convert.ToInt32(part[1].Trim(), 16);
                            col[i, j] = Color.FromArgb((rgb >> 16) & 255, (rgb >> 8) & 255, rgb & 255);
                        }
                    }
                }

                double min = z[0, 0];
                double max = z[0, 0];
                for (int j = 0; j < r; j++)
                    for (int i = 0; i < c; i++)
                    {
                        if (z[i, j] < min) min = z[i, j];
                        if (z[i, j] > max) max = z[i, j];
                    }

                double[,] hh = new double[c, r];
                for (int j = 0; j < r; j++)
                    for (int i = 0; i < c; i++)
                    {
                        if (max > min)
                            hh[i, j] = (max - z[i, j]) / (max - min);
                        else
                            hh[i, j] = 0;
                    }

                if (img != null)
                {
                    img.Dispose();
                    img = null;
                }
                cols = c;
                rows = r;
                heights = hh;
                colors = col;
                Invalidate();
            }
            catch
            {
                MessageBox.Show("не получилось открыть fdf файл");
            }
        }

        void MakeGrid()
        {
            if (img == null)
                return;

            rows = cols * img.Height / img.Width;
            if (rows < 2)
                rows = 2;

            Bitmap small = new Bitmap(cols, rows);
            Graphics g = Graphics.FromImage(small);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.DrawImage(img, 0, 0, cols, rows);
            g.Dispose();

            heights = new double[cols, rows];
            colors = new Color[cols, rows];
            for (int i = 0; i < cols; i++)
            {
                for (int j = 0; j < rows; j++)
                {
                    Color c = small.GetPixel(i, j);
                    double y = 0.299 * c.R + 0.587 * c.G + 0.114 * c.B;
                    heights[i, j] = y / 255;
                    colors[i, j] = c;
                }
            }
            small.Dispose();
            Invalidate();
        }

        void trackBar1_Scroll(object sender, EventArgs e)
        {
            if (img == null && heights != null)
                return;
            cols = trackBar1.Value;
            MakeGrid();
        }

        void trackBar2_Scroll(object sender, EventArgs e)
        {
            heightK = trackBar2.Value / 100.0;
            Invalidate();
        }

        void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            useColor = checkBox1.Checked;
            Invalidate();
        }

        void MainForm_MouseDown(object sender, MouseEventArgs e)
        {
            lastMouse = e.Location;
        }

        void MainForm_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;

            angleZ += (e.X - lastMouse.X) * 0.01;
            angleX += (e.Y - lastMouse.Y) * 0.01;
            lastMouse = e.Location;
            Invalidate();
        }

        void MainForm_MouseWheel(object sender, MouseEventArgs e)
        {
            if (e.Delta > 0)
                zoom *= 1.1;
            else
                zoom /= 1.1;
            Invalidate();
        }

        void MainForm_Resize(object sender, EventArgs e)
        {
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (heights == null)
                return;

            e.Graphics.SmoothingMode = SmoothingMode.None;
            DrawGrid(e.Graphics, ClientSize.Width, ClientSize.Height - panel1.Height, panel1.Height);
        }

        void DrawGrid(Graphics g, int w, int h, int top)
        {
            int n = Math.Max(cols, rows);
            double scale = zoom * Math.Min(w, h) / (n * 1.5);
            double zMax = heightK * n;

            double cosA = Math.Cos(angleZ);
            double sinA = Math.Sin(angleZ);
            double cosB = Math.Cos(angleX);
            double sinB = Math.Sin(angleX);

            PointF[,] pts = new PointF[cols, rows];
            for (int i = 0; i < cols; i++)
            {
                for (int j = 0; j < rows; j++)
                {
                    double x = i - cols / 2.0;
                    double y = j - rows / 2.0;
                    double z = heights[i, j] * zMax;

                    double x1 = x * cosA - y * sinA;
                    double y1 = x * sinA + y * cosA;
                    double y2 = y1 * cosB - z * sinB;

                    float sx = (float)(w / 2.0 + x1 * scale);
                    float sy = (float)(top + h / 2.0 + y2 * scale);
                    pts[i, j] = new PointF(sx, sy);
                }
            }

            for (int j = 0; j < rows; j++)
                for (int i = 0; i < cols - 1; i++)
                    DrawLine(g, pts, i, j, i + 1, j);

            for (int i = 0; i < cols; i++)
                for (int j = 0; j < rows - 1; j++)
                    DrawLine(g, pts, i, j, i, j + 1);
        }

        void DrawLine(Graphics g, PointF[,] pts, int i1, int j1, int i2, int j2)
        {
            if (useColor)
            {
                Color a = colors[i1, j1];
                Color b = colors[i2, j2];
                pen.Color = Color.FromArgb((a.R + b.R) / 2, (a.G + b.G) / 2, (a.B + b.B) / 2);
            }
            else
                pen.Color = Color.Pink;

            g.DrawLine(pen, pts[i1, j1], pts[i2, j2]);
        }
    }
}
