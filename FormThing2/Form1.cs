using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FormThing2
{
    public partial class Form1 : Form
    {
        static bool active = false;
        static uint prevState = 0;

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern uint SetThreadExecutionState(uint esFlags);
        public const uint ES_CONTINUOUS = 0x80000000;
        public const uint ES_SYSTEM_REQUIRED = 0x00000001;
        public const uint ES_DISPLAY_REQUIRED = 0x00000002;
        

        public Form1()
        {
            InitializeComponent();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            SetThreadExecutionState(prevState);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            active = !active;
            if (active)
            {
                button1.Text = "Active";
                new Thread(new ThreadStart(MakeActivity)).Start();
            }
            else
            {
                button1.Text = "Inactive";
                SetThreadExecutionState(prevState);
            }
        }

        private static void MakeActivity()
        {
            //Cursor Cursor = new Cursor(Cursor.Current.Handle);

            //Cursor.Clip = new Rectangle(this.Location, this.Size);
            prevState = SetThreadExecutionState(ES_CONTINUOUS | ES_SYSTEM_REQUIRED | ES_DISPLAY_REQUIRED);
            while (active)
            {
                if (SetThreadExecutionState(ES_CONTINUOUS | ES_SYSTEM_REQUIRED | ES_DISPLAY_REQUIRED) <= 0)
                    throw new Exception("ERROR");


                //if (Cursor.Position.X + 1 >= Screen.PrimaryScreen.Bounds.Width)
                //    Cursor.Position = new Point(0, Cursor.Position.Y);
                //else
                //    Cursor.Position = new Point(Cursor.Position.X + 1, Cursor.Position.Y);

                Thread.Sleep(10000);
            }
        }
    }
}
