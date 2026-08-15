using MediatR;

namespace BookingService.Domain.SeedWork;

public abstract class Entity
{
    int? _requestedHashCode;
    int _Id;

    // thuộc tính virtual để lớp con có thể ghi đè
    // Chỉ có thể ghi nếu là repository 
    public virtual int Id
    {
        get
        {
            return _Id;
        }
        protected set
        {
            _Id = value;
        }
    }

    //Danh sách domain event, chứa các event nội bộ của domain
    private List<INotification> _domainEvents;

    public IReadOnlyCollection<INotification> DomainEvents => _domainEvents?.AsReadOnly();

    // Khi thực hiện 1 nghiệp vụ, add vào bộ nhớ tạm để lưu lại
    public void AddDomainEvent(INotification eventItem)
    {
        _domainEvents = _domainEvents ?? new List<INotification>();
        _domainEvents.Add(eventItem);
    }

    public void RemoveDomainEvent(INotification eventItem)
    {
        _domainEvents?.Remove(eventItem);
    }

    public void ClearDomainEvents()
    {
        _domainEvents?.Clear();
    }

    // Nếu chưa có giá trị là transient (Tạm thời)
    public bool IsTransient()
    {
        return this.Id == default;
    }


    //Logic so sánh 2 thực thể, nếu bằng nhau thì chúng là 1
    //
    public override bool Equals(object obj)
    {
        if (obj == null || !(obj is Entity))
            return false;

        if (Object.ReferenceEquals(this, obj))
            return true;

        if (this.GetType() != obj.GetType())
            return false;

        Entity item = (Entity)obj;

        if (item.IsTransient() || this.IsTransient())
            return false;
        else
            return item.Id == this.Id;
    }

    // Kỹ thuật nâng có giúp tối ưu hiệu suất khi thực thể được lưu trong các tập hợp HashSet hay Dictionary
    // Xử dụng toán tử XOR với số 31 để tạo ra một mã bâm phân phối ngẫu nhiên tốt
    // Các hàm GetHashCode được cấu hình ngầm định trong HashSet, Dictionary nhưng nó thường dựa trên địa chỉ ô nhớ để lấy giá trị băm của đối tượng
    // 2 đối tượng được cho là như nhau trong DDD thì nó phải có tất cả thuộc tính bằng nhau hết
    // Hàm này ghi đè để HashSet so sánh dựa trên ID để phân biệt sự khác nhau
    public override int GetHashCode()
    {
        if (!IsTransient())
        {
            // Nếu là tạm thời kiểm tra có dữ liệu không
            if (!_requestedHashCode.HasValue)
                _requestedHashCode = this.Id.GetHashCode() ^ 31; // XOR for random distribution (http://blogs.msdn.com/b/ericlippert/archive/2011/02/28/guidelines-and-rules-for-gethashcode.aspx)

            return _requestedHashCode.Value;
        }
        else
            return base.GetHashCode();

    }

    // Nạp chồng toán tử ==
    public static bool operator == (Entity left, Entity right)
    {
        if (Object.Equals(left, null))
            return (Object.Equals(right, null)) ? true : false;
        else
            return left.Equals(right);
    }

    //Nạp chồng toán tử !=
    public static bool operator != (Entity left, Entity right)
    {
        return !(left == right);
    }
}
