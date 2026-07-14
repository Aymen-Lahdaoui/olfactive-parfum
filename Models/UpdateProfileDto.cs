namespace OlfactiveParfum.Backend.Models
{
    public class UpdateProfileDto
    {
        public string Nom { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string AncienEmail { get; set; } = string.Empty;
    }
}