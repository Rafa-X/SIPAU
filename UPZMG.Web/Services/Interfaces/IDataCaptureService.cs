using UPZMG.Implementations.DataCapture;
using UPZMG.Persistence.Models;
using UPZMG.Web.Models;

namespace UPZMG.Web.Services;

public interface IDataCaptureService
{
    IReadOnlyList<string> Sectors { get; }
    IReadOnlyList<string> CompanySizes { get; }
    IReadOnlyList<string> ActivityTypes { get; }
    IReadOnlyList<SelectOption> BulkAreas { get; }

    Task<Student?> CreateStudent(CreateStudent request);
    Task<Employee?> CreateTeacher(CreateEmployee request);
    Task<ClassGroups?> CreateClassGroup(CreateClassGroup request);

    /// <summary>Checks a file of students or, with Commit, stores all of it or none.</summary>
    Task<ImportResult> ImportStudentsAsync(ImportStudents request);

    /// <summary>Checks a file of employees or, with Commit, stores all of it or none.</summary>
    Task<ImportResult> ImportEmployeesAsync(ImportEmployees request);

    /// <summary>The captured record, to fill the correction form; null when it no longer exists.</summary>
    Task<UpdateEmployee?> GetTeacherAsync(Guid id);
    Task<UpdateClassGroup?> GetClassGroupAsync(Guid id);

    Task<Employee?> UpdateTeacher(UpdateEmployee request);
    Task<ClassGroups?> UpdateClassGroup(UpdateClassGroup request);

    Task<IReadOnlyList<EmployeeListItem>?> GetEmployeesListAsync();
    Task<IReadOnlyList<ClassGroupListItem>?> GetClassGroupsListAsync();

    Task<SchoolFacility?> CreateFacility(CreateFacility request);
    Task<OutreachProject?> CreateOutreachProject(CreateOutreachProject request);
    Task<AnnualFinance?> SaveFinance(SaveAnnualFinance request);

    // These five answer null when the request failed, and an empty list when the area really
    // has nothing registered yet. The screens say which of the two it is.
    Task<IReadOnlyList<FacilityListItem>?> GetFacilitiesListAsync();
    Task<IReadOnlyList<OutreachListItem>?> GetOutreachListAsync();
    Task<IReadOnlyList<FinanceListItem>?> GetFinanceListAsync();

    Task<ScolarshipAssigned?> CreateScholarship(CreateScholarship request);
    Task<TitulationProcess?> CreateTitulation(CreateTitulation request);

    Task<IReadOnlyList<ScholarshipListItem>?> GetScholarshipsAsync();
    Task<IReadOnlyList<TitulationListItem>?> GetTitulationsAsync();
    Task<IReadOnlyList<LookupOption>> GetScholarshipProgramsAsync();
}
