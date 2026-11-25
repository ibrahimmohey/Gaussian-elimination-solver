using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace WindowsFormsApp2
{
    public partial class Form1 : Form
    {
        private NumericUpDown rowsInput, colsInput;
        private Button createFieldsBtn, solveBtn, prevStepBtn, nextStepBtn;
        private RadioButton refRadio, rrefRadio;
        private Panel equationsPanel, drawPanel;
        private Label stepLabel, statusLabel;

        private List<TextBox> textBoxes = new List<TextBox>();
        private List<StepData> history = new List<StepData>();
        private int currentStep = 0;
        private int n = 3, m = 4;

        public Form1()
        {
            SetupScreen();
            CreateEquationBoxes();
        }

        private void SetupScreen()
        {
            this.Text = "Gauss Elimination Visualizer (REF & RREF)";
            this.Size = new Size(1150, 720);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 10F);

            Panel topBar = new Panel { Dock = DockStyle.Top, Height = 65, BackColor = Color.FromArgb(240, 244, 248), Padding = new Padding(10) };

            Label labelN = new Label { Text = "الصفوف (n):", Location = new Point(15, 20), AutoSize = true };
            rowsInput = new NumericUpDown { Location = new Point(95, 18), Width = 55, Minimum = 1, Maximum = 15, Value = 3 };

            Label labelM = new Label { Text = "الأعمدة (m):", Location = new Point(165, 20), AutoSize = true };
            colsInput = new NumericUpDown { Location = new Point(245, 18), Width = 55, Minimum = 2, Maximum = 16, Value = 4 };

            createFieldsBtn = new Button { Text = "تجهيز الحقول", Location = new Point(315, 15), Width = 110, Height = 32, BackColor = Color.SteelBlue, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            createFieldsBtn.Click += (s, e) =>
            {
                n = (int)rowsInput.Value;
                m = (int)colsInput.Value;
                CreateEquationBoxes();
            };

            refRadio = new RadioButton { Text = "REF", Location = new Point(455, 19), AutoSize = true };
            rrefRadio = new RadioButton { Text = "RREF", Location = new Point(520, 19), AutoSize = true, Checked = true };

            solveBtn = new Button { Text = "رسم وحل الخطوات", Location = new Point(605, 15), Width = 150, Height = 32, BackColor = Color.SeaGreen, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            solveBtn.Click += SolveButtonClicked;

            topBar.Controls.AddRange(new Control[] { labelN, rowsInput, labelM, colsInput, createFieldsBtn, refRadio, rrefRadio, solveBtn });

            Panel leftSide = new Panel { Dock = DockStyle.Left, Width = 360, BackColor = Color.WhiteSmoke, Padding = new Padding(10) };
            Label guideLabel = new Label
            {
                Text = "أدخل المعادلات (مثال: 2x + 3y - z = 5)\nأو الأرقام مفصولة بمسافة (2 3 -1 5):",
                Dock = DockStyle.Top,
                Height = 45,
                ForeColor = Color.DimGray
            };

            statusLabel = new Label { Dock = DockStyle.Bottom, Height = 55, ForeColor = Color.Firebrick, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };

            equationsPanel = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            leftSide.Controls.Add(equationsPanel);
            leftSide.Controls.Add(guideLabel);
            leftSide.Controls.Add(statusLabel);

            Panel rightSide = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };

            Panel bottomNav = new Panel { Dock = DockStyle.Bottom, Height = 60, BackColor = Color.FromArgb(245, 245, 245) };
            prevStepBtn = new Button { Text = "◄ الخطوة السابقة", Location = new Point(20, 12), Width = 140, Height = 35, Enabled = false };
            prevStepBtn.Click += (s, e) =>
            {
                if (currentStep > 0)
                {
                    currentStep--;
                    RefreshDrawing();
                }
            };

            nextStepBtn = new Button { Text = "الخطوة التالية ►", Location = new Point(175, 12), Width = 140, Height = 35, Enabled = false };
            nextStepBtn.Click += (s, e) =>
            {
                if (currentStep < history.Count - 1)
                {
                    currentStep++;
                    RefreshDrawing();
                }
            };

            stepLabel = new Label { Text = "أدخل المعادلات واضغط على (رسم وحل الخطوات)", Location = new Point(335, 18), AutoSize = true, Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = Color.DarkBlue };

            bottomNav.Controls.AddRange(new Control[] { prevStepBtn, nextStepBtn, stepLabel });

            drawPanel = new SmoothPanel { Dock = DockStyle.Fill, BackColor = Color.White };
            drawPanel.Paint += DrawMatrixOnScreen;

            rightSide.Controls.Add(drawPanel);
            rightSide.Controls.Add(bottomNav);

            this.Controls.Add(rightSide);
            this.Controls.Add(leftSide);
            this.Controls.Add(topBar);
        }

        private void CreateEquationBoxes()
        {
            equationsPanel.Controls.Clear();
            textBoxes.Clear();
            statusLabel.Text = "";

            for (int i = 0; i < n; i++)
            {
                Label eqNum = new Label { Text = $"Eq {i + 1}:", Location = new Point(5, i * 40 + 10), Width = 45 };
                TextBox box = new TextBox { Location = new Point(55, i * 40 + 7), Width = 265, Tag = i };

                box.TextChanged += (s, e) =>
                {
                    TextBox currentBox = (TextBox)s;
                    currentBox.BackColor = Color.White;
                    statusLabel.Text = "";
                };

                box.KeyDown += (s, e) =>
                {
                    if (e.KeyCode == Keys.Enter)
                    {
                        e.SuppressKeyPress = true;
                        int index = (int)((TextBox)s).Tag;
                        double[] temp;
                        if (CheckEquation(index, out temp))
                        {
                            if (index + 1 < textBoxes.Count)
                                textBoxes[index + 1].Focus();
                            else
                                solveBtn.PerformClick();
                        }
                    }
                };

                equationsPanel.Controls.Add(eqNum);
                equationsPanel.Controls.Add(box);
                textBoxes.Add(box);
            }
        }

        private bool CheckEquation(int index, out double[] numbers)
        {
            numbers = new double[m];
            TextBox box = textBoxes[index];
            string text = box.Text.Trim();

            if (string.IsNullOrEmpty(text))
            {
                ShowInputError(box, $"المعادلة {index + 1} فارغة! اكتبها مرة أخرى.");
                return false;
            }

            string[] parts = text.Split(new[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
            double dummy;
            if (parts.Length == m && parts.All(p => double.TryParse(p, NumberStyles.Any, CultureInfo.InvariantCulture, out dummy)))
            {
                for (int j = 0; j < m; j++)
                    numbers[j] = double.Parse(parts[j], CultureInfo.InvariantCulture);

                box.BackColor = Color.Honeydew;
                return true;
            }

            if (!text.Contains("="))
            {
                ShowInputError(box, $"خطأ في المعادلة {index + 1}: يجب وضع علامة (=) أو كتابة {m} أرقام.");
                return false;
            }

            string[] sides = text.Split('=');
            double resultValue;
            if (sides.Length != 2 || !double.TryParse(sides[1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out resultValue))
            {
                ShowInputError(box, $"خطأ في المعادلة {index + 1}: الرقم بعد علامة (=) غير صحيح.");
                return false;
            }

            numbers[m - 1] = resultValue;
            string leftSide = sides[0].Replace(" ", "");

            var matches = Regex.Matches(leftSide, @"([+-]?\d*\.?\d*)([a-zA-Z]\d*)");
            if (matches.Count == 0 || matches.Count > m - 1)
            {
                ShowInputError(box, $"خطأ في المعادلة {index + 1}: عدد المتغيرات غير متطابق مع الأعمدة ({m - 1}).");
                return false;
            }

            string checkString = string.Concat(matches.Cast<Match>().Select(x => x.Value));
            if (checkString.Length != leftSide.Length)
            {
                ShowInputError(box, $"خطأ في صياغة المعادلة {index + 1}: راجع الإشارات والمتغيرات.");
                return false;
            }

            for (int k = 0; k < matches.Count; k++)
            {
                string signAndNum = matches[k].Groups[1].Value;
                double value = 1.0;

                if (signAndNum == "" || signAndNum == "+")
                    value = 1.0;
                else if (signAndNum == "-")
                    value = -1.0;
                else
                    value = double.Parse(signAndNum, CultureInfo.InvariantCulture);

                numbers[k] = value;
            }

            box.BackColor = Color.Honeydew;
            return true;
        }

        private void ShowInputError(TextBox box, string msg)
        {
            box.BackColor = Color.MistyRose;
            box.Focus();
            box.SelectAll();
            statusLabel.ForeColor = Color.Firebrick;
            statusLabel.Text = msg;
        }

        private void SolveButtonClicked(object sender, EventArgs e)
        {
            double[,] matrix = new double[n, m];

            for (int i = 0; i < n; i++)
            {
                double[] rowData;
                if (!CheckEquation(i, out rowData))
                    return;

                for (int j = 0; j < m; j++)
                    matrix[i, j] = rowData[j];
            }

            statusLabel.ForeColor = Color.SeaGreen;
            statusLabel.Text = "تم قراءة جميع المعادلات بنجاح!";

            history = RunGauss(matrix, rrefRadio.Checked);
            currentStep = 0;
            RefreshDrawing();
        }

        private List<StepData> RunGauss(double[,] startMatrix, bool isRref)
        {
            List<StepData> stepsList = new List<StepData>();
            double[,] grid = (double[,])startMatrix.Clone();
            int rowsCount = grid.GetLength(0);
            int colsCount = grid.GetLength(1);

            stepsList.Add(new StepData
            {
                Grid = (double[,])grid.Clone(),
                Title = "البداية: المصفوفة الموسعة قبل أي عمليات",
                PivotR = -1,
                PivotC = -1,
                ActiveR = -1
            });

            int lead = 0;
            for (int r = 0; r < rowsCount; r++)
            {
                if (lead >= colsCount - 1) break;

                int i = r;
                while (Math.Abs(grid[i, lead]) < 1e-9)
                {
                    i++;
                    if (i == rowsCount)
                    {
                        i = r;
                        lead++;
                        if (lead == colsCount - 1) return stepsList;
                    }
                }

                if (i != r)
                {
                    for (int j = 0; j < colsCount; j++)
                    {
                        double temp = grid[r, j];
                        grid[r, j] = grid[i, j];
                        grid[i, j] = temp;
                    }

                    stepsList.Add(new StepData
                    {
                        Grid = (double[,])grid.Clone(),
                        Title = $"تبديل الصف R{r + 1} مع الصف R{i + 1}",
                        PivotR = r,
                        PivotC = lead,
                        ActiveR = i,
                        Swapped = true
                    });
                }

                double pivot = grid[r, lead];
                if (isRref && Math.Abs(pivot - 1.0) > 1e-9)
                {
                    for (int j = 0; j < colsCount; j++)
                        grid[r, j] /= pivot;

                    stepsList.Add(new StepData
                    {
                        Grid = (double[,])grid.Clone(),
                        Title = $"قسمة الصف R{r + 1} على ({pivot:0.##}) ليصبح الـ Pivot = 1",
                        PivotR = r,
                        PivotC = lead,
                        ActiveR = -1
                    });
                }

                int startRow = isRref ? 0 : r + 1;
                for (int k = startRow; k < rowsCount; k++)
                {
                    if (k == r) continue;
                    double factor = grid[k, lead] / grid[r, lead];
                    if (Math.Abs(factor) > 1e-9)
                    {
                        for (int j = 0; j < colsCount; j++)
                            grid[k, j] -= factor * grid[r, j];

                        stepsList.Add(new StepData
                        {
                            Grid = (double[,])grid.Clone(),
                            Title = $"تصفير العمود {lead + 1}:  R{k + 1} ← R{k + 1} - ({factor:0.##}) × R{r + 1}",
                            PivotR = r,
                            PivotC = lead,
                            ActiveR = k
                        });
                    }
                }
                lead++;
            }

            stepsList.Add(new StepData
            {
                Grid = (double[,])grid.Clone(),
                Title = $"النتيجة النهائية بصيغة {(isRref ? "RREF" : "REF")}",
                PivotR = -1,
                PivotC = -1,
                ActiveR = -1
            });

            return stepsList;
        }

        private void RefreshDrawing()
        {
            if (history.Count == 0) return;
            prevStepBtn.Enabled = currentStep > 0;
            nextStepBtn.Enabled = currentStep < history.Count - 1;
            stepLabel.Text = $"[{currentStep + 1} / {history.Count}]  {history[currentStep].Title}";
            drawPanel.Invalidate();
        }

        private void DrawMatrixOnScreen(object sender, PaintEventArgs e)
        {
            if (history.Count == 0) return;

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            StepData current = history[currentStep];
            double[,] grid = current.Grid;
            int rowsCount = grid.GetLength(0);
            int colsCount = grid.GetLength(1);

            int boxW = 75;
            int boxH = 50;
            int startX = 110;
            int startY = 80;
            int fullW = colsCount * boxW;
            int fullH = rowsCount * boxH;

            if (current.PivotC >= 0)
            {
                using (SolidBrush colColor = new SolidBrush(Color.FromArgb(40, 100, 149, 237)))
                {
                    g.FillRectangle(colColor, startX + current.PivotC * boxW, startY, boxW, fullH);
                }
            }

            if (current.PivotR >= 0)
            {
                using (SolidBrush pivotRowColor = new SolidBrush(Color.FromArgb(70, 60, 179, 113)))
                {
                    g.FillRectangle(pivotRowColor, startX, startY + current.PivotR * boxH, fullW, boxH);
                }
            }

            if (current.ActiveR >= 0)
            {
                using (SolidBrush targetRowColor = new SolidBrush(Color.FromArgb(85, 255, 193, 7)))
                {
                    g.FillRectangle(targetRowColor, startX, startY + current.ActiveR * boxH, fullW, boxH);
                }
            }

            if (current.PivotR >= 0 && current.PivotC >= 0)
            {
                using (Pen boxPen = new Pen(Color.DarkGreen, 2.5f))
                {
                    g.DrawRectangle(boxPen, startX + current.PivotC * boxW + 4, startY + current.PivotR * boxH + 4, boxW - 8, boxH - 8);
                }
            }

            using (Font font = new Font("Consolas", 13F, FontStyle.Bold))
            {
                StringFormat alignCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

                for (int r = 0; r < rowsCount; r++)
                {
                    g.DrawString($"R{r + 1}", new Font("Segoe UI", 10F, FontStyle.Bold), Brushes.Gray, new RectangleF(startX - 55, startY + r * boxH, 40, boxH), alignCenter);

                    for (int c = 0; c < colsCount; c++)
                    {
                        double num = Math.Abs(grid[r, c]) < 1e-9 ? 0 : grid[r, c];
                        RectangleF rect = new RectangleF(startX + c * boxW, startY + r * boxH, boxW, boxH);
                        g.DrawString(num.ToString("0.##"), font, Brushes.Black, rect, alignCenter);
                    }
                }
            }

            using (Pen borderPen = new Pen(Color.Black, 3f))
            {
                int offset = 8;

                g.DrawLines(borderPen, new Point[] {
                    new Point(startX + offset, startY - offset),
                    new Point(startX - offset, startY - offset),
                    new Point(startX - offset, startY + fullH + offset),
                    new Point(startX + offset, startY + fullH + offset)
                });

                g.DrawLines(borderPen, new Point[] {
                    new Point(startX + fullW - offset, startY - offset),
                    new Point(startX + fullW + offset, startY - offset),
                    new Point(startX + fullW + offset, startY + fullH + offset),
                    new Point(startX + fullW - offset, startY + fullH + offset)
                });
            }

            using (Pen linePen = new Pen(Color.DarkGray, 2f) { DashStyle = DashStyle.Dash })
            {
                int lineX = startX + (colsCount - 1) * boxW;
                g.DrawLine(linePen, lineX, startY, lineX, startY + fullH);
            }

            if (current.PivotC >= 0)
            {
                int arrowX = startX + current.PivotC * boxW + boxW / 2;
                int yBottom = startY + fullH + 55;
                int yTop = startY + fullH + 15;

                using (Pen arrow = new Pen(Color.Crimson, 3.5f))
                {
                    arrow.CustomEndCap = new AdjustableArrowCap(6, 6);
                    g.DrawLine(arrow, arrowX, yBottom, arrowX, yTop);
                }

                g.DrawString($"Pivot Col {current.PivotC + 1}", new Font("Segoe UI", 9.5F, FontStyle.Bold), Brushes.Crimson, new PointF(arrowX - 38, yBottom + 5));
            }

            if (current.PivotR >= 0 && current.ActiveR >= 0)
            {
                int sideX = startX + fullW + 25;
                int y1 = startY + current.PivotR * boxH + boxH / 2;
                int y2 = startY + current.ActiveR * boxH + boxH / 2;

                using (Pen curveArrow = new Pen(current.Swapped ? Color.Purple : Color.DarkOrange, 2.5f))
                {
                    curveArrow.CustomEndCap = new AdjustableArrowCap(5, 5);
                    if (current.Swapped) curveArrow.CustomStartCap = new AdjustableArrowCap(5, 5);

                    g.DrawBezier(curveArrow, sideX, y1, sideX + 45, y1, sideX + 45, y2, sideX, y2);
                }
            }
        }
    }

    public class StepData
    {
        public double[,] Grid { get; set; }
        public string Title { get; set; }
        public int PivotR { get; set; }
        public int PivotC { get; set; }
        public int ActiveR { get; set; }
        public bool Swapped { get; set; }
    }

    public class SmoothPanel : Panel
    {
        public SmoothPanel()
        {
            this.DoubleBuffered = true;
        }
    }
}