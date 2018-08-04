namespace MachinaTrader
{
    class Program
    {
        static void Main(string[] args)
        {
            GlobalSettings.LoadSettings();
            WebApplication.ProcessInit();
        }
    }

    public static class WebApplication
    {
        public static void ProcessInit()
        {
            Startup.RunWebHost();
        }
    }
}
