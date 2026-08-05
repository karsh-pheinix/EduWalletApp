namespace EduWalletApp.DTOs
{
    public class RegisterStudentDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string StudentIdNumber { get; set; } = string.Empty;
        public string University { get; set; } = string.Empty;
        public int YearOfStudy { get; set; }
        public Models.Gender Gender { get; set; }
        public DateOnly DateOfBirth { get; set; }
        public string City { get; set; } = string.Empty;
    }
}