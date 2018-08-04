namespace MachinaTrader
{
    class Program
    {
        static void Main(string[] args)
        {
            RuntimeSettings.LoadSettings();
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
