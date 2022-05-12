namespace MachinaTrader.Globals.Structure.Models
{
    public class Ticker
    {
        public decimal Bid { get; set; }
        public decimal Ask { get; set; }
        public decimal Last { get; set; }
        public decimal Volume { get; set; }


        public decimal Mid()
        {
            return (Bid + Ask) / 2.0m; 
        }
    }
}
