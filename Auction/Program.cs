Car car1 = new(startPrice: 4000000, id:Guid.NewGuid(), name:"Hyondai Solaris", year: 2000);
Car car2 = new(startPrice: 3000000, id:Guid.NewGuid(), name:"Hyondai i40", year: 1995);
Painting painting1 = new(startPrice: 4000000, id:Guid.NewGuid(), name:"Крик", author: "Unknown");
Painting  painting2 = new(startPrice: 4000000, id:Guid.NewGuid(), name:"Мона Лиза", author: "");
Auction<Car> auction1 = new(car1, new AuctionSettings());
Auction<Car> auction2 = new(car2, new AuctionSettings());
Auction<Painting> auction3 = new(painting1, new AuctionSettings());
Auction<Painting> auction4 = new(painting2, new AuctionSettings());
Bidder tom = new Bidder(balance: 5000000, name: "Tom");
Bidder david = new Bidder(balance: 7000000, name: "David");
Bidder sam  = new Bidder(balance: 8000000, name: "Sam");
Bidder bob  = new Bidder(balance: 5000000, name: "Bob");
Bidder ivan  = new Bidder(balance: 1000000, name: "Ivan");
Bidder kirill  = new Bidder(balance: 0, name: "Kirill");
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
try
{
    auction1.PlaceBid(tom, 4500000);
}
catch (InvalidBidException ex)
{
    Console.WriteLine(ex.Message);
}
try

{
    auction1.PlaceBid(sam, 6000000);
}
catch (InvalidBidException ex)
{
    Console.WriteLine(ex.Message);
    
}

try
{
    auction1.PlaceBid(david, 6000000);
}
catch (InvalidBidException ex)
{
    Console.WriteLine(ex.Message);
}

auction1.CloseAuction();
Console.WriteLine(BaseAuction.TotalAuctions);

void NotifyEveryone(object sender, BidEventArgs args)
{
    foreach (IParticipant participant in ((dynamic)sender).Participants)
    {
        participant.ReceiveNotification($"Внесена ставка на сумму {args.BidAmount} участником {args.BidderName}");
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
    public void ReceiveNotification(string message);
}

public class Bidder : IParticipant
{
    public string Name { get; set; }
    public decimal Balance { get; set; }

    public Bidder(string name, decimal balance)
    {
        this.Name = name;
        this.Balance = balance;
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
    public void CalculateTax(ref decimal balance, out decimal taxAmount);
}
public class Car : BaseItem, ITaxable
{
    public int Year { get; set; }

    public void CalculateTax(ref decimal balance, out decimal taxAmount)
    {
        taxAmount = StartPrice * (decimal)0.1;
        balance -= taxAmount;
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
        if (IsActive && bidAmount<=participant.Balance && bidAmount>CurrentPrice && bidAmount-CurrentPrice>=_settings.MinBidStep)
        {
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
        IsActive = false;
        if (HighestBidder != null)
        {
            decimal currentBalance = HighestBidder.Balance;
            if (_Item is ITaxable taxableItem)
            {
                taxableItem.CalculateTax(ref currentBalance, out var taxAmount);
            }
            HighestBidder.Balance = currentBalance;
            HighestBidder.Balance -= CurrentPrice;
        }

        AuctionEndEventArgs inf = new();
        inf.FinalAmount = CurrentPrice;
        inf.WinnerName = HighestBidder?.Name ?? "Никто";
        OnAuctionEnded?.Invoke(this,inf);
    }
}
