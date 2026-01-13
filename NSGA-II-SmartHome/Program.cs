using System;
using System.Windows.Forms;
using NSGA_II_SmartHome.UI;

namespace NSGA_II_SmartHome
{
    internal static class Program
    {
        
        [STAThread] 
        static void Main()
        {
            
            ApplicationConfiguration.Initialize();

            
            Application.Run(new MainForm());
        }
    }
}