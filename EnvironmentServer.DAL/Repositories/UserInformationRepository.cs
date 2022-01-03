using Dapper;
using EnvironmentServer.DAL.Models;

namespace EnvironmentServer.DAL.Repositories;

public class UserInformationRepository
{
    private Database DB;

    public UserInformationRepository(Database db)
    {
        DB = db;
    }

    public UserInformation Get(long uid)
    {
        using var connection = DB.GetConnection();
        return connection.QuerySingleOrDefault<UserInformation>("select * from users_information where UserID = @id;", new
        {
            id = uid
        });
    }

    public void Insert(UserInformation ui)
    {
        using var connection = DB.GetConnection();
        connection.Execute("INSERT INTO `users_information` (`ID`, `UserID`, `Name`, `SlackID`, " +
            "`DepartmentID`, `AbsenceDate`, `AbsenceReason`, `AdminNote`) " +
            "VALUES (NULL, @userID, @name, @slackID, @departmentID, NULL, '', '');", new
            {
                userID = ui.UserID,
                name = ui.Name,
                slackID = ui.SlackID,
                departmentID = ui.DepartmentID
            });
    }

    public void Update(UserInformation ui)
    {
        using var connection = DB.GetConnection();
        connection.Execute("UPDATE `users_information` SET " +
            "`Name` = @name, " +
            "`SlackID` = @slackID, " +
            "`DepartmentID` = @departmentID, " +
            "`AbsenceDate` = @absenceDate, " +
            "`AbsenceReason` = @absenceReason, " +
            "`AdminNote` = @adminNote " +
            " WHERE `users_information`.`ID` = @id;", new
            {
                id = ui.ID,
                name = ui.Name,
                slackID = ui.SlackID,
                departmentID = ui.DepartmentID,
                absenceDate = ui.AbsenceDate,
                absenceReason = ui.AbsenceReason,
                adminNote = ui.AdminNote
            });
    }
}
