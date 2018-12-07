using System;
using System.Windows.Forms;
using ChangeFromCommaToDot;

class InterceptKeys
{

    public static void Main()
    {
        DSChanger changer = null;
        try
        {
            changer = new DSChanger();
            changer.SetHook();
            Application.Run();
            changer.UnhookWindowsHookEx();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   {ex.Message}");
            Console.WriteLine("\n\nPress key to exit . . .");
            Console.ReadKey();
        }
    }
}