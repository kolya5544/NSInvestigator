using dotNS;
using dotNS.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace NSInvestigator
{
    public class TCont
    {
        public Trade trade;
        public TradingCard tc;
    }

    public class Smuggle
    {
        public List<TCont> allTrades = new List<TCont>();
        public List<TCont> suspiciousTrades = new List<TCont>();
        public PublicNationInfo pni;
    }

    class Program
    {
        public static DotNS api = new DotNS();
        static void Main(string[] args)
        {
            api.UserAgent = "NSInvestigator by nk.ax";
            Console.WriteLine("=[ NSInvestigator v1.0 by kolya5544 ]=");
            Console.WriteLine("=[ 4 step pupper reveal utility     ]=");
            Console.Write("Step 1) Enter the name of nation to investigate:");
            string nname = Console.ReadLine().ToLower();
            Console.WriteLine("Step 2) Estimation of time the investigation will take... (you'll be given updates on investigation as it goes)");

            var deck = api.GetDeck(nname);
            var publicInfo = api.GetNationInfo(nname);

            int estimate = deck.Count * 2;

            Console.WriteLine($"Step 3) Deck investigation in progress. Estimated time: up to {Math.Round(estimate / (double)60, 1)} minutes");

            Dictionary<string, List<TCont>> senders = new Dictionary<string, List<TCont>>();
            List<long> duplicates = new List<long>();

            Thread.Sleep(1000);
            for (int i = 0; i<deck.Count; i++)
            {
                var tc = deck[i];
                if (!duplicates.Contains(tc.ID)) { duplicates.Add(tc.ID); } else { continue; }
                TradingCard card = api.GetCard(tc.ID, tc.Season);
                foreach (Trade trade in card.Trades)
                {
                    if (trade.Buyer == nname)
                    {
                        var tcont = new TCont() { trade = trade, tc = card };
                        if (senders.ContainsKey(trade.Seller))
                        {
                            senders[trade.Seller].Add(tcont);

                        } else
                        {
                            senders.Add(trade.Seller, new List<TCont>() { tcont });
                        }
                    }
                }
                Thread.Sleep(1000);

                if (i % 10 == 0)
                {
                    Console.WriteLine($"In progress... {Math.Round(i / (double)deck.Count * 100, 2)}%");
                }
            }
            Console.WriteLine("Final processing... Please allow up to 2 minutes to complete.");
            Dictionary<string, Smuggle> suspiciousCountries = new Dictionary<string, Smuggle>();
            foreach (KeyValuePair<string, List<TCont>> kvp in senders)
            {
                Smuggle s = new Smuggle();
                s.allTrades = kvp.Value;
                foreach (TCont tc in kvp.Value)
                {
                    if ((tc.trade.Price == 0 || tc.tc.Category == CardCategory.legendary || tc.tc.Category == CardCategory.epic) && tc.trade.Seller == kvp.Key && tc.trade.Buyer == nname)
                    {
                        s.suspiciousTrades.Add(tc);
                    }
                }
                double totalMV = 0; s.allTrades.ForEach(z => { if (z.trade.Seller == kvp.Key && z.trade.Buyer == nname) { totalMV += z.tc.MarketValue; } });
                if (s.suspiciousTrades.Count > 0 && totalMV > 0.5)
                {
                    try
                    {
                        s.pni = api.GetNationInfo(kvp.Key);
                    } catch { continue; }
                    Thread.Sleep(1000);
                    suspiciousCountries.Add(kvp.Key, s);
                }
            }
            var smugglers = suspiciousCountries.ToList().OrderBy(z => z.Value.suspiciousTrades.Count).Reverse().ToList();
            while (true)
            {
                Console.Clear();
                Console.WriteLine($"Step 4) INVESTIGATION REPORT ON {nname}...");
                Console.WriteLine($"===");
                for (int i = 0; i<smugglers.Count; i++)
                {
                    var kvp = smugglers[i];
                    double totalMV = 0; kvp.Value.allTrades.ForEach(z => { if (z.trade.Seller == kvp.Key && z.trade.Buyer == nname) { totalMV += z.tc.MarketValue; } });
                    double moneySpent = 0; kvp.Value.allTrades.ForEach(z => { if (z.trade.Seller == kvp.Key && z.trade.Buyer == nname) { moneySpent += z.trade.Price; } });
                    Console.WriteLine($"[id{i}] '{kvp.Key}' from '{kvp.Value.pni.Region}' has {kvp.Value.suspiciousTrades.Count} suspicious trades (out of {kvp.Value.allTrades.Count} total) with the suspect. Total MV of trades: {Math.Round(totalMV, 2)}. The Suspect spent: {Math.Round(moneySpent, 2)}");
                }
                if (smugglers.Count == 0)
                {
                    Console.WriteLine("So empty :( We couldn't find any suspicious trading");
                }
                Console.WriteLine($"===");
                Console.WriteLine($"More info on suspected activity? Enter ID!");
                Console.Write("ID:");
                string resp = Console.ReadLine();
                if (!string.IsNullOrEmpty(resp))
                {
                    int id = int.Parse(resp);
                    var kvp = smugglers[id];
                    
                    Console.WriteLine($"===");
                    foreach (TCont sus in kvp.Value.suspiciousTrades)
                    {
                        Console.WriteLine($"Card #{sus.tc.ID} was bought by The Suspect for {sus.trade.Price}. It was a {sus.tc.Category} card.");
                    }
                    Console.WriteLine($"===");
                }
                Console.WriteLine("Press Enter to continue.");
                Console.ReadLine();
            }
        }
    }
}
