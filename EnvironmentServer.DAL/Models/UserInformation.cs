using System;

namespace EnvironmentServer.DAL.Models;

public class UserInformation
{
    public long ID { get; set; }
    public long UserID { get; set; }
    public string Name { get; set; }
    public string SlackID { get; set; }
    public long DepartmentID { get; set; }
    public string DepartmentName { get; set; }
    public DateTime? AbsenceDate { get; set; }
    public string AbsenceReason { get; set; } = "";
    public string AdminNote { get; set; } = "";

    public void PrepareForDB()
    {
        AdminNote ??= "";
        AbsenceReason ??= "";
    }
}