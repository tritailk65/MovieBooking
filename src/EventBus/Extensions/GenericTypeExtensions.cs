namespace EventBus.Extensions;

public static class GenericTypeExtensions
{
    /// <summary>
    /// Hàm tiện ích làm đẹp tên kiểu dữ liệu của đối tượng, làm đẹp tên vì mặc định hệ thống trả về cái tên rất xấu
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static string GetGenericTypeName(this Type type)
    {
        string typeName;

        if (type.IsGenericType)
        {
            var genericTypes = string.Join(",", type.GetGenericArguments().Select(t => t.Name).ToArray());
            typeName = $"{type.Name.Remove(type.Name.IndexOf('`'))}<{genericTypes}>";
        }
        else
        {
            typeName = type.Name;
        }

        return typeName;
    }

    public static string GetGenericTypeName(this object @object)
    {
        return @object.GetType().GetGenericTypeName();
    }
}
