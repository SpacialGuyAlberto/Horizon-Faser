using System;

namespace HorizonFaser.Models
{
    public enum UserRole
    {
        Obrero = 0, // Obrero / Operario de Campo
        Bauleiter = 1, // Bauleiter / Jefe de Obra
        ProjectManager = 2, // Project Manager / Planificador
        Admin = 3 // Administrador
    }

    [Serializable]
    public class UserInfo
    {
        public string Token;
        public string Email;
        public string FirstName;
        public string LastName;
        public UserRole Role = UserRole.Obrero;
        public long UserId = 0;
        public long OrganizationId = 0;
        public bool IsLoggedIn = false;

        public string GetFullName()
        {
            if (string.IsNullOrEmpty(FirstName) && string.IsNullOrEmpty(LastName))
            {
                return string.IsNullOrEmpty(Email) ? "Operario Anónimo" : Email;
            }
            return $"{FirstName} {LastName}".Trim();
        }

        public string GetRoleString()
        {
            switch (Role)
            {
                case UserRole.Obrero: return "Obrero de Campo";
                case UserRole.Bauleiter: return "Bauleiter";
                case UserRole.ProjectManager: return "Project Manager";
                case UserRole.Admin: return "Admin";
                default: return "Operario";
            }
        }
    }
}
