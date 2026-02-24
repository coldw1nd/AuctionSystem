Car car = new(startPrice: 4000000, id:Guid.NewGuid(), name:"Hyondai Solaris", year: 2000);
Auction<Car> auction = new(car);
Bidder tom = new Bidder(balance: 5000000, name: "Tom");
Bidder david = new Bidder(balance: 7000000, name: "David");
Bidder sam  = new Bidder(balance: 3000000, name: "Sam");
auction.RegisterParticipant(tom);
auction.RegisterParticipant(david);
auction.RegisterParticipant(sam);
auction.OnBidPlaced += NotifyEveryone;
try
{
    auction.PlaceBid(tom, 4500000);
}
catch (InvalidBidException ex)
{
    Console.WriteLine(ex.Message);
}
try

{
    auction.PlaceBid(sam, 6000000);
}
catch (InvalidBidException ex)
{
    Console.WriteLine(ex.Message);
    
}

try
{
    auction.PlaceBid(david, 6000000);
}
catch (InvalidBidException ex)
{
    Console.WriteLine(ex.Message);
}

auction.CloseAuction();
Console.WriteLine(BaseAuction._TotalAuctions);

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
    public decimal CalculateTax(decimal price);
}
public class Car : BaseItem, ITaxable
{
    public int Year { get; set; }

    public decimal CalculateTax(decimal price)
    {
        return price * (decimal)0.1;
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

public abstract class BaseAuction
{
    public static int _TotalAuctions { get; protected set; }
}

public class Auction<T> : BaseAuction where T : IItem
{
    private readonly T _Item;
    public List<IParticipant> Participants { get; private set; } = new List<IParticipant>();
    public decimal CurrentPrice { get; private set; }
    public IParticipant HighestBidder { get; private set; }
    public bool IsActive { get; private set; } = true;
    
    public T Item
    {
        get { return _Item;}
    }
    

    public event EventHandler<BidEventArgs> OnBidPlaced;
    public event EventHandler<AuctionEndEventArgs> OnAuctionEnded;

    public Auction(T item)
    {
        _Item = item;
        CurrentPrice = item.StartPrice;
        _TotalAuctions++;
    }
    
    public void RegisterParticipant(IParticipant participant)
    {
        Participants.Add(participant);
    }
    
    public void PlaceBid(IParticipant participant, decimal bidAmount)
    {
        if (IsActive && bidAmount<=participant.Balance && bidAmount>CurrentPrice)
        {
            CurrentPrice = bidAmount;
            HighestBidder = participant;
            BidEventArgs inf = new();
            inf.BidAmount = bidAmount;
            inf.BidderName = participant.Name;
            OnBidPlaced?.Invoke(this, inf);
        }
        else
        {
            throw new InvalidBidException($"Ставка участника {participant.Name} на сумму {bidAmount} не удалась");
        }
    }

    public void CloseAuction()
    {
        IsActive = false;
        AuctionEndEventArgs inf = new();
        if (_Item is ITaxable itemTaxable)
        {
            CurrentPrice += itemTaxable.CalculateTax(CurrentPrice);
        }
        inf.FinalAmount = CurrentPrice;
        inf.WinnerName = HighestBidder?.Name ?? "Никто";
        if (HighestBidder!=null)
        {
            HighestBidder.Balance -= CurrentPrice;
        }
        OnAuctionEnded?.Invoke(this,inf);
    }
}
