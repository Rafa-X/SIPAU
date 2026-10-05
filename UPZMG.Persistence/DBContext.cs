using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using UPZMG.Persistence.Models;

namespace UPZMG.Persistence;

public class AppDBContext(DbContextOptions<AppDBContext> options) : DbContext(options)
{
    // Security
    public DbSet<SystemUser> SystemUsers => Set<SystemUser>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<ReportsPermission> ReportsPermissions => Set<ReportsPermission>();
    public DbSet<Reports> Reports => Set<Reports>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<RoleReportPermission> RoleReportsPermissions => Set<RoleReportPermission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();

    // Academic
    public DbSet<Careers> Careers => Set<Careers>();
    public DbSet<ClassGroups> ClassGroups => Set<ClassGroups>();
    public DbSet<Courses> Courses => Set<Courses>();
    public DbSet<InstitutionalConfiguration> InstitutionalConfigurations => Set<InstitutionalConfiguration>();
    public DbSet<InstitutionalEvaluation> InstitutionalEvaluations => Set<InstitutionalEvaluation>();
    public DbSet<Kardex> Kardexes => Set<Kardex>();
    public DbSet<Periods> Periods => Set<Periods>();
    public DbSet<CourseEnrollment> CourseEnrollment => Set<CourseEnrollment>();
    public DbSet<ScolarshipAssigned> ScolarshipAssigneds => Set<ScolarshipAssigned>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<StudentDisabilities> StudentDisabilities => Set<StudentDisabilities>();
    public DbSet<TitulationProcess> TitulationProcesses => Set<TitulationProcess>();
    public DbSet<TutoringSessions> TutoringSessions => Set<TutoringSessions>();

    // // Extension
    // public DbSet<ContinuousEducation> ContinuousEducations => Set<ContinuousEducation>();

    // // Identification
    // public DbSet<School> Schools => Set<School>();

    // // Public
    // public DbSet<Disabilities> Disabilities => Set<Disabilities>();
    public DbSet<ScolarshipProgram> ScolarshipPrograms => Set<ScolarshipProgram>();
    // public DbSet<WithdrawReason> WithdrawReasons => Set<WithdrawReason>();
    public DbSet<IndigenousLanguages> IndigenousLanguages => Set<IndigenousLanguages>();
    public DbSet<IndigenousLanguagesSpeakers> IndigenousLanguagesSpeakers => Set<IndigenousLanguagesSpeakers>();

    // Resources
    public DbSet<SchoolFacility> SchoolFacilities => Set<SchoolFacility>();
    // // Pending areas of the 911 infrastructure questionnaire
    // public DbSet<AnnualTICInventory> AnnualTICInventories => Set<AnnualTICInventory>();
    // public DbSet<Infraestructure> Infraestructures => Set<Infraestructure>();
    // public DbSet<ParkingLots> ParkingLots => Set<ParkingLots>();

    // Extension
    public DbSet<OutreachProject> OutreachProjects => Set<OutreachProject>();

    // Finance
    public DbSet<AnnualFinance> AnnualFinances => Set<AnnualFinance>();

    // RRHH
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<InternalAreas> InternalAreas => Set<InternalAreas>();
    public DbSet<MaxDegrees> MaxDegrees => Set<MaxDegrees>();
    public DbSet<ContractTypes> ContractTypes => Set<ContractTypes>();
    // public DbSet<AcademicGoals> AcademicGoals => Set<AcademicGoals>();
    // public DbSet<AcademicStaff> AcademicStaffs => Set<AcademicStaff>();
    // public DbSet<ASTeacher> ASTeachers => Set<ASTeacher>();
    // public DbSet<Contracts> Contracts => Set<Contracts>();
    // public DbSet<InternalAreas> InternalAreas => Set<InternalAreas>();
    // public DbSet<PositionsMapping> PositionsMappings => Set<PositionsMapping>();
    // public DbSet<ResearchAreas> ResearchAreas => Set<ResearchAreas>();
    // public DbSet<TeacherContinuousEducation> TeacherContinuousEducations => Set<TeacherContinuousEducation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Table mappings, keys and relationships structure (if has any)

        // Security
        modelBuilder.Entity<SystemUser>(entity => {
            entity.ToTable("systemUser");
            // Sign-in looks a user up by e-mail: two rows with the same one make it ambiguous.
            entity.HasIndex(x => x.Email).IsUnique();
            entity.HasKey(x => x.Id);
        });
        modelBuilder.Entity<Role>(entity => {
            entity.ToTable("roles");
            entity.HasKey(x => x.Id);
        });
        modelBuilder.Entity<Permission>(entity => {
            entity.ToTable("permissions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(50);
            entity.Property(x => x.DisplayName).HasMaxLength(80);
            entity.Property(x => x.Description).HasMaxLength(200);
            entity.HasIndex(x => x.Name).IsUnique();
        });
        modelBuilder.Entity<ReportsPermission>(entity => {
            entity.ToTable("reportsPermissions");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.ReportId);
        });
        modelBuilder.Entity<Reports>(entity => {
            entity.ToTable("reports");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ReportType).HasMaxLength(40);
            entity.Property(x => x.TemplateVersion).HasMaxLength(20);
            entity.Property(x => x.Status).HasMaxLength(20);
            entity.Property(x => x.AnswersJson).HasColumnType("nvarchar(max)");
            entity.Property(x => x.DateCreated).HasColumnType("datetime2");
            entity.Property(x => x.LastModified).HasColumnType("datetime2");
            // SQL Server keeps this column up to date; EF uses it as the concurrency token.
            entity.Property(x => x.RowVersion).IsRowVersion();
            entity.HasIndex(x => new { x.ReportType, x.TemplateVersion });
        });
        modelBuilder.Entity<RolePermission>(entity => {
            entity.ToTable("rolePermissions");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.RoleId, x.PermissionId }).IsUnique();
            entity.HasOne<Role>()
                .WithMany()
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Permission>()
                .WithMany()
                .HasForeignKey(x => x.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<RoleReportPermission>(entity => {
            entity.ToTable("roleReportsPermissions");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.RoleId, x.ReportsPermissionId }).IsUnique();
            entity.Property(x => x.CanEdit).HasDefaultValue(false);
            entity.Property(x => x.CanSign).HasDefaultValue(false);
            entity.HasOne<Role>()
                .WithMany()
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<ReportsPermission>()
                .WithMany()
                .HasForeignKey(x => x.ReportsPermissionId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<UserRole>(entity => {
            entity.ToTable("userRoles");
            entity.HasKey(x => x.Id);
            entity.HasOne<SystemUser>()
                .WithMany()
                .HasForeignKey(x => x.UserId);
            entity.HasOne<Role>()
                .WithMany()
                .HasForeignKey(x => x.RoleId);
        });

        // Academic
        modelBuilder.Entity<Careers>(entity => {
            entity.ToTable("careers");
            entity.HasKey(x => x.Id);
        });
        modelBuilder.Entity<ClassGroups>(entity => {
            entity.ToTable("classGroups");
            entity.HasKey(x => x.Id);
            entity.HasOne<Periods>()
                .WithMany()
                .HasForeignKey(x => x.PeriodId);
            entity.HasOne<Careers>()
                .WithMany()
                .HasForeignKey(x => x.CareerId);
            entity.HasOne<Courses>()
                .WithMany()
                .HasForeignKey(x => x.CourseId);
            entity.HasOne<Employee>()
                .WithMany()
                .HasForeignKey(x => x.TeacherId);
            entity.HasOne<SchoolFacility>()
                .WithMany()
                .HasForeignKey(x => x.FacilityId);
            entity.HasIndex(x => new { x.FacilityId, x.PeriodId }); // speeds up the schedule-overlap lookup on create
            entity.Property(x => x.Schedule)
                .HasColumnType("nvarchar(max)")
                .HasConversion(
                    schedule => JsonSerializer.Serialize(schedule, (JsonSerializerOptions?)null),
                    schedule => JsonSerializer.Deserialize<List<Schedule>>(schedule) ?? new List<Schedule>());
        });
        modelBuilder.Entity<Courses>(entity => {
            entity.ToTable("courses");
            entity.HasKey(x => x.Id);
            entity.HasOne<Careers>()
                .WithMany()
                .HasForeignKey(x => x.CareerId);
        });
        modelBuilder.Entity<InstitutionalConfiguration>(entity => {
            entity.ToTable("institutionalConfigurations");
            entity.HasKey(x => x.Id);
        });
        modelBuilder.Entity<InstitutionalEvaluation>(entity => {
            entity.ToTable("institutionalEvaluations");
            entity.HasKey(x => x.Id);
            entity.HasOne<Periods>()
                .WithMany()
                .HasForeignKey(x => x.PeriodId);
        });
        modelBuilder.Entity<Kardex>(entity => {
            entity.ToTable("kardex");
            entity.HasKey(x => x.Id);
            entity.HasOne<Student>()
                .WithMany()
                .HasForeignKey(x => x.StudentId);
            entity.HasOne<Courses>()
                .WithMany()
                .HasForeignKey(x => x.CourseId);
            entity.HasOne<Periods>()
                .WithMany()
                .HasForeignKey(x => x.PeriodId);
        });
        modelBuilder.Entity<Periods>(entity => {
            entity.ToTable("periods");
            entity.HasKey(x => x.Id);
        });
        modelBuilder.Entity<CourseEnrollment>(entity =>{
            entity.ToTable("courseEnrollment");
            entity.HasKey(x => x.Id);
            entity.HasOne<Student>()
                .WithMany()
                .HasForeignKey(x => x.StudentId);
            entity.HasOne<ClassGroups>()
                .WithMany()
                .HasForeignKey(x => x.GroupId);
            entity.HasOne<Periods>()
                .WithMany()
                .HasForeignKey(x => x.PeriodId);
            entity.HasOne<WithdrawReason>()
                .WithMany()
                .HasForeignKey(x => x.WithdrawReasonId);
        });
        modelBuilder.Entity<ScolarshipAssigned>(entity => {
            entity.ToTable("scolarshipAssigneds");
            entity.HasKey(x => x.Id);
            entity.HasOne<Student>()
                .WithMany()
                .HasForeignKey(x => x.StudentId);
            entity.HasOne<Periods>()
                .WithMany()
                .HasForeignKey(x => x.PeriodId);
             entity.HasOne<ScolarshipProgram>()
                .WithMany()
                .HasForeignKey(x => x.ScolarshipProgramId);
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            // The same student cannot hold the same grant twice in one period.
            entity.HasIndex(x => new { x.StudentId, x.PeriodId, x.ScolarshipProgramId }).IsUnique();
        });
        modelBuilder.Entity<Student>(entity => {
            entity.ToTable("students");
            entity.HasKey(x => x.Id);
            // Two simultaneous captures cannot both win: the database decides, not a prior read.
            entity.HasIndex(x => x.EnrollmentNumber).IsUnique();
            // CURP is optional in the capture, so only the ones actually given have to be unique.
            entity.HasIndex(x => x.CURP).IsUnique().HasFilter("[CURP] <> ''");
            entity.Property(x => x.Gender)
                .HasConversion<string>()
                .HasColumnType("char(1)");
        });
        modelBuilder.Entity<IndigenousLanguages>(entity => {
            entity.ToTable("IndigenousLanguages");
            entity.HasKey(x => x.Id);
        });
        modelBuilder.Entity<IndigenousLanguagesSpeakers>(entity => {
            entity.ToTable("IndigenousLanguagesSpeakers");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.SpeakerId, x.LanguageId }).IsUnique();
            entity.HasIndex(x => x.LanguageId);
        });
        modelBuilder.Entity<StudentDisabilities>(entity => {
            entity.ToTable("studentDisabilities");
            entity.HasKey(x => x.Id);
            entity.HasOne<Student>()
                .WithMany()
                .HasForeignKey(x => x.StudentId);
            entity.HasOne<Disabilities>()
                .WithMany()
                .HasForeignKey(x => x.DisabilityId);
        });
        modelBuilder.Entity<TitulationProcess>(entity => {
            entity.ToTable("titulationProcesses");
            entity.HasKey(x => x.Id);
            entity.HasOne<Student>()
                .WithMany()
                .HasForeignKey(x => x.StudentId);
            entity.HasOne<Careers>()
                .WithMany()
                .HasForeignKey(x => x.CareerId);
            entity.HasOne<Employee>()
                .WithMany()
                .HasForeignKey(x => x.TitulationAssessorId);
            // A student graduates from a programme once.
            entity.HasIndex(x => x.StudentId).IsUnique();
        });
        modelBuilder.Entity<TutoringSessions>(entity => {
            entity.ToTable("tutoringSessions");
            entity.HasKey(x => x.Id);
            entity.HasOne<Student>()
                .WithMany()
                .HasForeignKey(x => x.StudentId);
             entity.HasOne<ClassGroups>()
                .WithMany()
                .HasForeignKey(x => x.GroupId);
        });

        // // Extension
        // modelBuilder.Entity<ContinuousEducation>(entity => {
        //     entity.ToTable("continuousEducations");
        //     entity.HasKey(x => x.Id);
        // });

        // // Identification
        // modelBuilder.Entity<School>(entity => {
        //     entity.ToTable("schools");
        //     entity.HasKey(x => x.Id);
        // });

        // // Public
        // modelBuilder.Entity<Disabilities>(entity => {
        //     entity.ToTable("disabilities");
        //     entity.HasKey(x => x.Id);
        // });
        // The catalogue the official formats read, and now also the scholarship capture.
        modelBuilder.Entity<ScolarshipProgram>(entity => {
            entity.ToTable("ScolarshipProgram");
            entity.HasKey(x => x.Id);
        });
        // modelBuilder.Entity<WithdrawReason>(entity => {
        //     entity.ToTable("withdrawReasons");
        //     entity.HasKey(x => x.Id);
        // });

        // // Resources
        // modelBuilder.Entity<AnnualTICInventory>(entity => {
        //     entity.ToTable("annualTICInventories");
        //     entity.HasKey(x => x.Id);
        // });
        // modelBuilder.Entity<Infraestructure>(entity => {
        //     entity.ToTable("infraestructures");
        //     entity.HasKey(x => x.Id);
        // });
        // modelBuilder.Entity<ParkingLots>(entity => {
        //     entity.ToTable("parkingLots");
        //     entity.HasKey(x => x.Id);
        // });
        // The table already existed because class groups point at it; now the area owns it.
        modelBuilder.Entity<SchoolFacility>(entity => {
            entity.ToTable("SchoolFacility");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Problems)
                .HasColumnType("nvarchar(max)")
                .HasConversion(
                    problems => JsonSerializer.Serialize(problems, (JsonSerializerOptions?)null),
                    problems => JsonSerializer.Deserialize<List<string>>(problems, (JsonSerializerOptions?)null) ?? new List<string>());
        });

        // Extension
        modelBuilder.Entity<OutreachProject>(entity => {
            entity.ToTable("outreachProjects");
            entity.HasKey(x => x.Id);
            entity.HasOne<Periods>()
                .WithMany()
                .HasForeignKey(x => x.PeriodId);
            entity.HasOne<Student>()
                .WithMany()
                .HasForeignKey(x => x.StudentId);
            entity.Property(x => x.OrganizationName).HasMaxLength(200);
            entity.Property(x => x.ProjectName).HasMaxLength(200);
            entity.Property(x => x.Sector).HasMaxLength(50);
            entity.Property(x => x.CompanySize).HasMaxLength(50);
            entity.Property(x => x.ActivityType).HasMaxLength(50);
            entity.Property(x => x.StudentEnrollmentNumber).HasMaxLength(20);
            entity.HasIndex(x => x.PeriodId);
        });

        // Finance
        modelBuilder.Entity<AnnualFinance>(entity => {
            entity.ToTable("annualFinances");
            entity.HasKey(x => x.Id);
            // One summary per fiscal year: two rows for the same year would be two budgets.
            entity.HasIndex(x => x.FiscalYear).IsUnique();
            entity.Ignore(x => x.TotalIncome);
            entity.Ignore(x => x.TotalExpense);
            foreach (var amount in new[] { nameof(AnnualFinance.FederalIncome), nameof(AnnualFinance.StateIncome),
                                           nameof(AnnualFinance.OwnIncomeEnrollment), nameof(AnnualFinance.OwnIncomeServices),
                                           nameof(AnnualFinance.PayrollExpense), nameof(AnnualFinance.MaterialsExpense),
                                           nameof(AnnualFinance.ServicesExpense), nameof(AnnualFinance.InvestmentExpense) })
            {
                entity.Property(amount).HasPrecision(18, 2);
            }
            entity.Property(x => x.Notes).HasMaxLength(1000);
        });

        // RRHH
        modelBuilder.Entity<Employee>(entity => {
            entity.ToTable("employees");
            entity.HasIndex(x => x.EmployeeNumber).IsUnique();
            entity.HasIndex(x => x.Email).IsUnique();
            entity.HasIndex(x => x.Rfc).IsUnique();
            entity.HasIndex(x => x.Curp).IsUnique();
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Gender)
                .HasConversion<string>()
                .HasColumnType("char(1)");
        });
        modelBuilder.Entity<InternalAreas>(entity => {
            entity.ToTable("internalAreas");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Name).IsUnique();
            entity.HasIndex(x => x.ParentAreaId);
            entity.HasOne<InternalAreas>()
                .WithMany()
                .HasForeignKey(x => x.ParentAreaId)
                .OnDelete(DeleteBehavior.Restrict);
        });
            modelBuilder.Entity<MaxDegrees>(entity => {
                entity.ToTable("maxDegrees");
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => x.Name).IsUnique();
            });
            modelBuilder.Entity<ContractTypes>(entity => {
                entity.ToTable("contractTypes");
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => x.Name).IsUnique();
            });
        // modelBuilder.Entity<AcademicGoals>(entity => {
        //     entity.ToTable("academicGoals");
        //     entity.HasKey(x => x.Id);
        // });
        // modelBuilder.Entity<AcademicStaff>(entity => {
        //     entity.ToTable("academicStaffs");
        //     entity.HasKey(x => x.Id);
        // });
        // modelBuilder.Entity<ASTeacher>(entity => {
        //     entity.ToTable("asTeachers");
        //     entity.HasKey(x => x.Id);
        // });
        // modelBuilder.Entity<Contracts>(entity => {
        //     entity.ToTable("contracts");
        //     entity.HasKey(x => x.Id);
        // });
        // modelBuilder.Entity<PositionsMapping>(entity => {
        //     entity.ToTable("positionsMappings");
        //     entity.HasKey(x => x.Id);
        // });
        // modelBuilder.Entity<ResearchAreas>(entity => {
        //     entity.ToTable("researchAreas");
        //     entity.HasKey(x => x.Id);
        // });
        // modelBuilder.Entity<TeacherContinuousEducation>(entity => {
        //     entity.ToTable("teacherContinuousEducations");
        //     entity.HasKey(x => x.Id);
        // });

        // SQL Server does not allow multiple cascade paths; use NO ACTION for inferred cascade FKs.
        foreach (var foreignKey in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
        {
            if (foreignKey.DeleteBehavior == DeleteBehavior.Cascade)
            {
                foreignKey.DeleteBehavior = DeleteBehavior.NoAction;
            }
        }
    }
}

/*
HasOne<T>()	        “belongs to one”
WithMany()	        “one has many”
HasForeignKey()	    FOREIGN KEY column
HasKey()	        PRIMARY KEY
*/