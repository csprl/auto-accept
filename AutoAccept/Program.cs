﻿using System;
using System.Windows.Forms;

namespace AutoAccept
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Application.Run(new App());
        }
    }
}
