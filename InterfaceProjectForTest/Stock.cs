using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterfaceProjectForTest
{
    public interface IStockFeed
    {
        int GetSharePrice(string company);
        string PriceUnit { get; set; }
        event EventHandler Changed;
        T GetValue<T>();
    }

    public class StockAnalyzer
    {
        private IStockFeed stockFeed;
        public StockAnalyzer(IStockFeed feed)
        {
            stockFeed = feed;
        }
        public int GetContosoPrice()
        {
            return stockFeed.GetSharePrice("COOO");
        }
    }






}
