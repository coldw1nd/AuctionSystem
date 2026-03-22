using System.Linq;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

Car car1 = new(startPrice: 1000000, id:Guid.NewGuid(), name:"Hyondai Solaris", year: 2000);
Car car2 = new(startPrice: 4000000, id:Guid.NewGuid(), name:"Hyondai i40", year: 1995);
Painting painting1 = new(startPrice: 1000000, id:Guid.NewGuid(), name:"Крик", author: "Unknown");
Painting painting2 = new(startPrice: 5000000, id:Guid.NewGuid(), name:"Мона Лиза", author: "");

Auction<Car> auction1 = new(car1, new AuctionSettings());
Auction<Car> auction2 = new(car2, new AuctionSettings());
Auction<Painting> auction3 = new(painting1, new AuctionSettings());
Auction<Painting> auction4 = new(painting2, new AuctionSettings());

Bidder tom = new Bidder(balance: 7000000, name: "Tom");
Bidder david = new Bidder(balance: 11000000, name: "David");
Bidder sam  = new Bidder(balance: 1000000, name: "Sam");
Bidder bob  = new Bidder(balance: 7000000, name: "Bob");
Bidder ivan  = new Bidder(balance: 0, name: "Ivan");
Bidder kirill  = new Bidder(balance: 10500000, name: "Kirill");

auction1.RegisterParticipant(tom);
auction1.RegisterParticipant(david);
auction1.RegisterParticipant(sam);
auction2.RegisterParticipant(sam);
auction2.RegisterParticipant(david);
auction2.RegisterParticipant(bob);
auction2.RegisterParticipant(tom);
auction3.RegisterParticipant(david);
auction3.RegisterParticipant(sam);
auction3.RegisterParticipant(bob);
auction3.RegisterParticipant(ivan);
auction3.RegisterParticipant(kirill);
auction4.RegisterParticipant(kirill);

auction1.OnBidPlaced += NotifyEveryone;
auction2.OnBidPlaced += NotifyEveryone;
auction3.OnBidPlaced += NotifyEveryone;
auction4.OnBidPlaced += NotifyEveryone;

List<BidRecord> bidHistory = new();
List<IItem> allItems = new();
List<IParticipant> allBidders = new();
List<IParticipant> activeBidders = new();

allItems.Add(car1);
allItems.Add(car2);
allItems.Add(painting1);
allItems.Add(painting2);

allBidders.Add(tom);
allBidders.Add(david);
allBidders.Add(sam);
allBidders.Add(bob);
allBidders.Add(ivan);
allBidders.Add(kirill);

ProccessRegistration(allBidders);

activeBidders.Add(tom);
activeBidders.Add(david);
activeBidders.Add(sam);
activeBidders.Add(bob);
activeBidders.Add(kirill);

RunLiveBoard(tom, david, sam);

TryPlaceBid(auction1, tom, 4000000, bidHistory);
TryPlaceBid(auction1, sam, 5000000, bidHistory);
TryPlaceBid(auction1, david, 7000000, bidHistory);
TryPlaceBid(auction1, kirill, 3000000, bidHistory);
TryPlaceBid(auction1, david, 8000000, bidHistory);
TryPlaceBid(auction1, tom, 9000000, bidHistory);
TryPlaceBid(auction2, david, 3000000, bidHistory);
TryPlaceBid(auction2, ivan, 6000000, bidHistory);
TryPlaceBid(auction2, david, 4000000, bidHistory);
TryPlaceBid(auction2, kirill, 9000000, bidHistory);
TryPlaceBid(auction3, tom, 4000000, bidHistory);
TryPlaceBid(auction3, sam, 5000000, bidHistory);
TryPlaceBid(auction3, tom, 4010000, bidHistory);
TryPlaceBid(auction2, kirill, 8000000, bidHistory);
TryPlaceBid(auction2, ivan, 9000000, bidHistory);
TryPlaceBid(auction3, david, 9500000, bidHistory);
TryPlaceBid(auction1, kirill, 9500000, bidHistory);
TryPlaceBid(auction1, david, 10000000, bidHistory);
TryPlaceBid(auction1, kirill, 10100000, bidHistory);
TryPlaceBid(auction2, kirill, 10100000, bidHistory);

var topBidders = allBidders.OrderByDescending(b => b.Balance).Take(2);
var sum = allBidders.Aggregate(0m, (sum, bidder)=>sum+bidder.Balance);
var priceGroups = allItems.GroupBy(item => item.StartPrice < 2000000);
var notActiveBidders = allBidders.Except(activeBidders);
var bidsWithBalance = allBidders.Join(bidHistory, b => b.Name, rec => rec.bidderName, (b, rec) => new{b.Name, b.Balance, Amount=rec.amount});
var bidInfo = allItems
    .GroupJoin(bidHistory, item => item.Name, bid => bid.itemName, (item, bids) => new { ItemName = item.Name, Bidders = bids
        .Select(bid => bid.bidderName) });

ViewTracker tracker = new ViewTracker();
tracker.ViewItem(car1);
tracker.ViewItem(car2);
tracker.ViewItem(painting1);
tracker.ViewItem(painting2);

AnalyzeHistory(bidHistory);

auction1.CloseAuction();
auction2.CloseAuction();
auction3.CloseAuction();
auction4.CloseAuction();

foreach (var group in priceGroups)
{
    Console.WriteLine(group.Key ? "Дешевле 2 миллионов:" : "2 миллиона или дороже:");
    foreach (var item in group)
    {
        Console.WriteLine(item.Name);
    }
    Console.WriteLine();
}

foreach (var bid in bidsWithBalance)
{
    Console.WriteLine($"Участник {bid.Name} имеет баланс {bid.Balance} и сделал ставку на сумму {bid.Amount}");
}

Console.WriteLine($"Всего проведено аукционов: {BaseAuction.TotalAuctions}");

void AnalyzeHistory(List<BidRecord> history)
{
    List<BidRecord> validBids = new List<BidRecord>(100);
    validBids.AddRange(history);
    validBids.RemoveAll(record => record.isSuccess == false);
    validBids.Sort(new BidRecordComparer());
    int topCount = Math.Min(3, validBids.Count);
    List<BidRecord> topBids = validBids.GetRange(0, topCount);
    BidRecord[] topBidsArray = new BidRecord[topCount];
    topBids.CopyTo(topBidsArray);

    foreach (var bid in topBidsArray)
    {
        Console.WriteLine($"Лот: {bid.itemName}, Участник: {bid.bidderName}, Сумма: {bid.amount}");
    }

}

void NotifyEveryone(object sender, BidEventArgs args)
{
    foreach (IParticipant participant in ((dynamic)sender).Participants)
    {
        participant.ReceiveNotification($"Внесена ставка на сумму {args.BidAmount} участником {args.BidderName}");
    }
}

void TryPlaceBid<T>(Auction<T> auction, IParticipant bidder, decimal amount, List<BidRecord> history) where T : IItem
{
    bool success=false;
    try
    {
        auction.PlaceBid(bidder, amount);
        success = true;
    }
    catch(InvalidBidException ex)
    {
        success = false;
        Console.WriteLine($"Ошибка: {ex.Message}");
    }
    finally
    {
        history.Add(new(bidder.Name, amount, success, auction.Item.Name));
    }
}

void ProccessRegistration(List<IParticipant> bidders)
{
    Queue<IParticipant> queue = new Queue<IParticipant>(bidders);
    Stack<IParticipant> rejectedBidders = new Stack<IParticipant>();
    Dictionary<string, List<IParticipant>> approvedCategories = new Dictionary<string, List<IParticipant>>();
    while (queue.TryDequeue(out var pariticipant))
    {
        if (pariticipant.Balance <= 0)
        {
            rejectedBidders.Push(pariticipant);
        }
        else
        {
            string category = pariticipant.Balance > 5000000 ? "VIP" : "Standard";
            if (!approvedCategories.ContainsKey(category))
            {
                approvedCategories.Add(category, new List<IParticipant>());
            }
            approvedCategories[category].Add(pariticipant);
        }
    }
    Console.WriteLine("Отклоненные участники:");
    while (rejectedBidders.TryPop(out var rejectedBidder))
    {
        Console.WriteLine($"Охрана выводит: {rejectedBidder.Name}");
    }
}

void RunLiveBoard(Bidder tom, Bidder david, Bidder sam)
{
    ObservableCollection<IParticipant> vipBoard = new ObservableCollection<IParticipant>() {tom, david};
    vipBoard.CollectionChanged += VipBoard_CollectionChanged;
    vipBoard.Add(sam);
    vipBoard[0] = new Bidder("Elon Musk", 999999999);
    vipBoard.Move(1, 0);
    vipBoard.RemoveAt(2);
    vipBoard.Clear();
}

void VipBoard_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
{
    switch (e.Action)
    {
        case NotifyCollectionChangedAction.Add:
            if (e.NewItems?[0] is IParticipant addedParticipant)
            {
                Console.WriteLine($"$[ADD] {addedParticipant.Name} появился на табло!");
            }

            break;
        
        case NotifyCollectionChangedAction.Remove:
            if (e.OldItems?[0] is IParticipant removedParticipant)
            {
                Console.WriteLine($"[REMOVE] {removedParticipant.Name} покинул табло!");
            }
            break;
        
        case NotifyCollectionChangedAction.Replace:
            if (e.OldItems?[0] is IParticipant oldP && e.NewItems?[0] is IParticipant newP)
            {
                Console.WriteLine($"[REPLACE] {oldP.Name} был заменен на {newP.Name}");
            }
            break;
        
        case NotifyCollectionChangedAction.Move:
            if (e.NewItems?[0] is IParticipant movedP)
            {
                Console.WriteLine($"[MOVE] {movedP.Name} был перемещен на позицию {e.NewStartingIndex} с позиции {e.OldStartingIndex}");
            }
            break;
        
        case NotifyCollectionChangedAction.Reset:
            Console.WriteLine("Табло сброшено");
            break;
    }
}

public record class BidRecord(string bidderName, decimal amount, bool isSuccess, string itemName);

class ViewTracker
{
    private readonly int _limit = 3;
    private readonly LinkedList<IItem> _historyList = new LinkedList<IItem>();
    private readonly Dictionary<Guid, LinkedListNode<IItem>> _cache = new Dictionary<Guid, LinkedListNode<IItem>>();

    public void ViewItem(IItem item)
    {
        Console.WriteLine($"Просматриваем лот {item.Name}");
        if (_cache.ContainsKey(item.Id))
        {
            var node = _cache[item.Id];
            _historyList.Remove(node);
            _historyList.AddFirst(node);
        }
        else
        {
            if (_historyList.Count >= _limit)
            {
                var oldestNode = _historyList.Last;
                if (oldestNode != null)
                {
                    _cache.Remove(oldestNode.Value.Id);
                    _historyList.RemoveLast();
                }
            }

            var newNode = new LinkedListNode<IItem>(item);
            _historyList.AddFirst(newNode);
            _cache.Add(item.Id, newNode);
        }
    }
}

class BidRecordComparer : IComparer<BidRecord>
{
    public int Compare(BidRecord? x, BidRecord? y)
    {
        if (x == null || y == null)
        {
            return 0;
        }

        int amountComparison = y.amount.CompareTo(x.amount);
        if (amountComparison != 0)
        {
            return amountComparison;
        }

        return x.bidderName.CompareTo(y.bidderName);
    }
}

public interface IItem
{
    public Guid Id { get; init; }
    public string Name { get; set; }
    public decimal StartPrice { get; set; }
}

public interface IParticipant
{
    public string Name { get; set; }
    public decimal Balance { get; set; }
    public decimal ReservedFunds { get; set; }
    public void ReceiveNotification(string message);
}

public class Bidder : IParticipant
{
    public string Name { get; set; }
    public decimal Balance { get; set; }

    public decimal ReservedFunds { get; set; } = 0;

    public Bidder(string name, decimal balance)
    {
        Name = name;
        Balance = balance;
    }

    public void ReceiveNotification(string message)
    {
        Console.WriteLine($"Участник {Name} получил уведомление {message}");
    }
}

public abstract class BaseItem : IItem
{
    public Guid Id { get; init; }
    public string Name { get; set; }
    public decimal StartPrice { get; set; }

    public virtual string GetDescription()
    {
        return $"Предмет: {Name}";
    }

    public BaseItem(Guid id, string name, decimal startPrice)
    {
        Id = id;
        Name = name;
        StartPrice = startPrice;
    }
}

public interface ITaxable
{
    public decimal CalculateTax();
}
public class Car : BaseItem, ITaxable
{
    public int Year { get; set; }
    public decimal CalculateTax()
    {
        decimal taxAmount = StartPrice * (decimal)0.1;
        return taxAmount;
    }
    public override string GetDescription()
    {
        return $"Автомобиль:{Name}";
    }

    public Car(Guid id, string name, decimal startPrice, int year) : base(id, name, startPrice)
    {
        Year = year;
    }
}

public class Painting : BaseItem
{
    public string Author { get; set; }

    public new string GetDescription()
    {
        return $"Картина {Name} от автора {Author}";
    }

public Painting(Guid id, string name, decimal startPrice, string author) : base(id, name, startPrice)
    {
        Author = author;
    }
}

public class BidEventArgs : EventArgs
{
    public string BidderName { get; set; }
    public decimal BidAmount { get; set; }
}

public class AuctionEndEventArgs : EventArgs
{
    public string WinnerName { get; set; }
    public decimal FinalAmount { get; set; }
}

public class InvalidBidException : Exception
{
    public InvalidBidException(string message) : base(message)
    {
        
    }
}

public struct AuctionSettings
{
    public decimal MinBidStep { get; init; } = 100000;

    public AuctionSettings(decimal minBidStep)
    {
        MinBidStep = minBidStep;
    }
}
public abstract class BaseAuction
{
    public static int TotalAuctions { get; protected set; }
}

public delegate void Notify(object sender, BidEventArgs args);
public class Auction<T> : BaseAuction where T : IItem
{
    private readonly T _Item;
    private readonly AuctionSettings _settings;
    public List<IParticipant> Participants { get; private set; } = new List<IParticipant>();
    public decimal CurrentPrice { get; private set; }
    public IParticipant HighestBidder { get; private set; }
    public bool IsActive { get; private set; } = true;
    
    public T Item
    {
        get { return _Item;}
    }

    private Notify OnBidNotify;
    public event Notify OnBidPlaced
    {
        add
        {
            OnBidNotify += value;
            Console.WriteLine("Ктото подписался на событие ставки");
        }
        remove
        {
            OnBidNotify -= value;
            Console.WriteLine("Ктото отписался от события ставки");
        }
    }
    public event EventHandler<AuctionEndEventArgs> OnAuctionEnded;

    public Auction(T item, in AuctionSettings settings)
    {
        _Item = item;
        CurrentPrice = item.StartPrice;
        _settings = settings;
        TotalAuctions++;
    }
    
    public void RegisterParticipant(IParticipant participant)
    {
        Participants.Add(participant);
    }
    
    public void PlaceBid(IParticipant participant, decimal bidAmount)
    {
        if (IsActive && bidAmount<=participant.Balance && bidAmount>CurrentPrice && bidAmount-CurrentPrice>=_settings.MinBidStep && (participant.Balance-participant.ReservedFunds)>=bidAmount)
        {
            participant.ReservedFunds += bidAmount;
            if (HighestBidder!=null)
            {
                HighestBidder.ReservedFunds -= CurrentPrice;
            }
            CurrentPrice = bidAmount;
            HighestBidder = participant;
            BidEventArgs info = new();
            info.BidAmount = bidAmount;
            info.BidderName = participant.Name;
            OnBidNotify?.Invoke(this, info);
        }
        else
        {
            throw new InvalidBidException($"Ставка участника {participant.Name} на сумму {bidAmount} не удалась");
        }
    }

    public void CloseAuction()
    {
        if (!IsActive) return;
        IsActive = false;
        if (HighestBidder != null)
        {
            decimal currentBalance = HighestBidder.Balance;
            HighestBidder.Balance = currentBalance;
            HighestBidder.Balance -= CurrentPrice;
            HighestBidder.ReservedFunds -= CurrentPrice;
        }
        AuctionEndEventArgs inf = new();
        inf.FinalAmount = CurrentPrice;
        inf.WinnerName = HighestBidder?.Name ?? "Никто";
        OnAuctionEnded?.Invoke(this,inf);
    }
}