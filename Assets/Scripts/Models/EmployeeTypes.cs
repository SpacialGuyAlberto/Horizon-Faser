using System;

namespace HorizonFaser.Models
{
    public enum CompanyRole
    {
        Admin = 0,
        Bauleiter = 1,
        Projektleiter = 2,
        TeamLeiter = 3,
        ProjektManager = 4,
        SubcontractorOwner = 5,
        SubcontractorFieldExpert = 6,
        SubcontractorAsphaltExpert = 7
    }

    [Serializable]
    public class Employee
    {
        public long Id = 0;
        public string Email = "";
        public string FirstName = "";
        public string LastName = "";
        public CompanyRole Role = CompanyRole.Bauleiter;
        public bool Enabled = true;
    }

    [Serializable]
    public class InternalEmployee : Employee
    {
        public long OrganizationId = 0;
    }

    [Serializable]
    public class ExternalEmployee : Employee
    {
        public long SubcontractorId = 0;
    }
}
