using System;
using System.Collections.Generic;

namespace HorizonFaser.Models
{
    /// <summary>
    /// Representa la entidad empresarial u organización principal (ej. Quarkbau).
    /// </summary>
    [Serializable]
    public class Organization
    {
        public long Id = 0;
        public string Name = "";
        public string Address = "";
        public string TaxId = ""; // CIF / VAT
        
        // Relación con empleados directos
        public List<InternalEmployee> Employees = new List<InternalEmployee>();
    }

    /// <summary>
    /// Representa una empresa subcontratada o asociada (Subcontratista).
    /// Usada habitualmente para obras civiles (Tiefbau), asfaltado, soplado de fibra, etc.
    /// </summary>
    [Serializable]
    public class Subunternehmen
    {
        public long Id = 0;
        public string Name = "";
        public string Specialization = ""; // Ej. "Tiefbau", "Spülbohrung", "Montage"
        public string ContactEmail = "";
        public string ContactPhone = "";
        
        // Foreign Key hacia la organización principal que los contrató
        public long OrganizationId = 0; 
        
        // Trabajadores externos asociados a este subcontratista
        public List<ExternalEmployee> Employees = new List<ExternalEmployee>();
    }

    /// <summary>
    /// Representa una cuadrilla o equipo de trabajo (Kolonne/Trupp) desplegado en el terreno.
    /// Puede ser una cuadrilla interna o de un subcontratista.
    /// </summary>
    [Serializable]
    public class Crew
    {
        public long Id = 0;
        public string Name = ""; // Ej. "Tiefbau Kolonne 1"
        
        // Empleado responsable de esta cuadrilla (TeamLeiter / Vorarbeiter)
        public long LeaderId = 0; 
        
        // Si pertenece a un subcontratista, se enlaza aquí (null si es interno)
        public long? SubunternehmenId = null; 
        
        // Lista de IDs de los empleados que forman parte de la cuadrilla
        public List<long> MemberIds = new List<long>(); 
        
        // Estado actual de la cuadrilla (Activa, Descanso, Inactiva)
        public string Status = "Active"; 
        
        // Última ubicación registrada (opcional para rastreo en mapa)
        public double? LastLatitude = null;
        public double? LastLongitude = null;

        // Nuevos campos traídos de monolith
        public string ForemanName = "";
        public int Size = 0;
        public string VehiclePlate = "";
        public List<Machine> AssignedMachines = new List<Machine>();
    }
}
