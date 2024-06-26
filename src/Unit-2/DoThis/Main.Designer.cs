﻿namespace ChartApp
{
    partial class Main
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea1 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend1 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            System.Windows.Forms.DataVisualization.Charting.Series series1 = new System.Windows.Forms.DataVisualization.Charting.Series();
            
            this.sysChart = new System.Windows.Forms.DataVisualization.Charting.Chart();
            btnPause = new System.Windows.Forms.Button();
            btnCpu = new System.Windows.Forms.Button();
            btnMem = new System.Windows.Forms.Button();
            btnDisk = new System.Windows.Forms.Button();

            ((System.ComponentModel.ISupportInitialize)(this.sysChart)).BeginInit();
            this.SuspendLayout();

            // 
            // sysChart
            // 
            chartArea1.Name = "ChartArea1";
            this.sysChart.ChartAreas.Add(chartArea1);
            this.sysChart.Dock = System.Windows.Forms.DockStyle.Fill;
            legend1.Name = "Legend1";
            this.sysChart.Legends.Add(legend1);
            this.sysChart.Location = new System.Drawing.Point(0, 0);
            this.sysChart.Name = "sysChart";
            series1.ChartArea = "ChartArea1";
            series1.Legend = "Legend1";
            series1.Name = "Series1";
            this.sysChart.Series.Add(series1);
            this.sysChart.Size = new System.Drawing.Size(684, 446);
            this.sysChart.TabIndex = 0;
            this.sysChart.Text = "sysChart";
            // 
            // btnPause
            // 
            btnPause.Location = new System.Drawing.Point(580, 280);
            btnPause.Name = "btnPause";
            btnPause.Size = new System.Drawing.Size(90, 29);
            btnPause.TabIndex = 1;
            btnPause.Text = "PAUSE ||";
            btnPause.UseVisualStyleBackColor = true;
            btnPause.Click += btnPause_Click;
            // 
            // btnCpu
            // 
            btnCpu.Location = new System.Drawing.Point(580, 340);
            btnCpu.Name = "btnCpu";
            btnCpu.Size = new System.Drawing.Size(90, 29);
            btnCpu.TabIndex = 1;
            btnCpu.Text = "CPU (ON)";
            btnCpu.UseVisualStyleBackColor = true;
            btnCpu.Click += btnCpu_Click;
            // 
            // btnMem
            // 
            btnMem.Location = new System.Drawing.Point(580, 370);
            btnMem.Name = "btnMem";
            btnMem.Size = new System.Drawing.Size(90, 29);
            btnMem.TabIndex = 1;
            btnMem.Text = "MEMORY (OFF)";
            btnMem.UseVisualStyleBackColor = true;
            btnMem.Click += btnMem_Click;
            // 
            // btnDisk
            // 
            btnDisk.Location = new System.Drawing.Point(580, 400);
            btnDisk.Name = "btnCpu";
            btnDisk.Size = new System.Drawing.Size(90, 29);
            btnDisk.TabIndex = 1;
            btnDisk.Text = "DISK (OFF)";
            btnDisk.UseVisualStyleBackColor = true;
            btnDisk.Click += btnDisk_Click;
            // 
            // Main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(684, 446);
            Controls.Add(btnPause);
            Controls.Add(btnCpu);
            Controls.Add(btnMem);
            Controls.Add(btnDisk);
            this.Controls.Add(this.sysChart);
            this.Name = "Main";
            this.Text = "System Metrics";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Main_FormClosing);
            this.Load += new System.EventHandler(this.Main_Load);
            ((System.ComponentModel.ISupportInitialize)(this.sysChart)).EndInit();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.DataVisualization.Charting.Chart sysChart;
        private System.Windows.Forms.Button btnPause;
        private System.Windows.Forms.Button btnCpu;
        private System.Windows.Forms.Button btnMem;
        private System.Windows.Forms.Button btnDisk;
    }
}

