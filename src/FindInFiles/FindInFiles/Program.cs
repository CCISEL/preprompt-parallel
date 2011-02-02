using System;
using System.Windows.Forms;

namespace FindInFiles
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var form = new Form1(new SearchBehaviors.SequentialSearchBehavior());
            Application.Run(form);
        }
    }
}