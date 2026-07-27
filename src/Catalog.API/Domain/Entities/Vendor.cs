namespace Catalog.API.Domain.Entities
{
    public class Vendor
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; } = default!;
        public string LogoUrl { get; set; } = default!;
        public bool IsActive { get; set; } = true;

        //Navigation Property
        public IReadOnlyCollection<Cinema> Cinemas => _cinemas.AsReadOnly();
        private readonly List<Cinema> _cinemas = new();

        public Vendor(string name)
        {
            Name = name;
        }
    }
}
