namespace Catalog.API.Domain.Entities
{
    public class Cinema
    {
        public int Id { get; set; } 
        public int VendorId { get; set; }
        [Required]
        public string Name { get; set; } = default!;
        [Required]
        public string Address { get; set; } = default!;
        [Required]
        public string City { get; set; } = default!;

        public Cinema(string name)
        {
            Name = name;
        }
    }
}
