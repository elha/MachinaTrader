namespace MachinaTrader.Globals.Structure.Enums
{
    public class TradeAdvice
    {
        public TradeAdviceEnum Advice { get; set; }
        public TradeActionEnum Action { get; set; }
        public string Comment { get; set; }

        public SellType SellType { get; set; } = SellType.Strategy;
        public string Strategy { get; set; }

        // too lazy to convert all the old code
        public class Factory
        {
            public static TradeAdvice Sell
            {
                get
                {
                    return new TradeAdvice { Advice = TradeAdviceEnum.Sell };
                }
            }

            public static TradeAdvice Buy
            {
                get
                {
                    return new TradeAdvice { Advice = TradeAdviceEnum.Buy };
                }
            }


            public static TradeAdvice Hold
            {
                get
                {
                    return new TradeAdvice { Advice = TradeAdviceEnum.Hold };
                }
            }

        }
    }

    public enum TradeAdviceEnum
    {
        Sell = -1,
        Hold = 0,
        Buy=1
    }
    public enum TradeActionEnum
    {
        Short = -1,
        Long = 0
    }
}
