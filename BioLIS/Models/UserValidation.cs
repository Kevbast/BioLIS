namespace BioLIS.Models
{
    public class UserValidation
    {//Vista del usuario 
        public int UserID { get; set; }
        public string Username { get; set; }
        public string? Email { get; set; }
        public string Role { get; set; }
        public int? DoctorID { get; set; }
        public string Salt { get; set; }
        public byte[] PasswordHash { get; set; }
    }
}
