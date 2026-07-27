namespace BookingService.Domain.SeedWork;

public abstract class ValueObject
{
    protected static bool EqualOperator(ValueObject left, ValueObject right)
    {
        // Nếu một bên null thì không cần phải so sánh nữa
        if (ReferenceEquals(left, null) ^ ReferenceEquals(right, null))
        {
            return false;
        }
        return ReferenceEquals(left, null) || left.Equals(right);
    }

    protected static bool NotEqualOperator(ValueObject left, ValueObject right)
    {
        return !(EqualOperator(left, right));
    }

    /// <summary>
    /// Hàm nào kế thừa phải khai báo class này để liệt kê các thuộc tính tạo nên giá trị của nó
    /// </summary>
    /// <returns></returns>
    protected abstract IEnumerable<object> GetEqualityComponents();


    public override bool Equals(object obj)
    {
        // Kiểm tra đối tượng truyền vào có bị null hoặc khác kiểu dữ liệu GetType không
        if (obj == null || obj.GetType() != GetType())
        {
            return false;
        }

        var other = (ValueObject)obj;

        //Kiểm tra xem từng thành phần có giống nhau không, trả về true/false
        return this.GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    // Trong .NET khi ghi đè Equal phải ghi đè GetHashCode để đảm bảo đối tượng bằng nhau phải có chung mã băm kết hợp lại bằng toán tử XOR ^
    public override int GetHashCode()
    {
        return GetEqualityComponents()
            .Select(x => x != null ? x.GetHashCode() : 0)
            .Aggregate((x, y) => x ^ y);
    }

    // Tạo bản sao để thay đổi vì bản ghi của ValueObject là bất biến
    public ValueObject GetCopy()
    {
        // Tạo bản sao nông của đối tượng
        return this.MemberwiseClone() as ValueObject;
    }
}
