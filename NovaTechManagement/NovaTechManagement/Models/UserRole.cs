namespace NovaTechManagement.Models
{
    public class UserRole
    {
        public int UserId { get; set; }
        public User User { get; set; } = null!; // Navigation property

        public int RoleId { get; set; }
        public Role Role { get; set; } = null!; // Navigation property
    }
}
