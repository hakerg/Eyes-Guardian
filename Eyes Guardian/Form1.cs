using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Eyes_Guardian
{
    public partial class Form1 : Form
    {
        private readonly LightAdjuster adjuster = new LightAdjuster();
        private Thread thread;
        private bool working = true;
        private TimeSpan sleepingPeriod = TimeSpan.FromSeconds(10.0);

        private void AdjustInfinitely()
        {
            while (working)
            {
                adjuster.Adjust();
                Thread.Sleep(sleepingPeriod);
            }
        }

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Hide();
            thread = new Thread(new ThreadStart(AdjustInfinitely));
            thread.Start();
        }

        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            working = false;
            Application.Exit();
        }
    }
}
