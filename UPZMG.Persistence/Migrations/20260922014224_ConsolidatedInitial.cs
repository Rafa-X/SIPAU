using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UPZMG.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ConsolidatedInitial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "careers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Alias = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SEPCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    EducationalLevel = table.Column<int>(type: "int", nullable: false),
                    Modality = table.Column<int>(type: "int", nullable: false),
                    PeriodType = table.Column<int>(type: "int", nullable: false),
                    DurationInPeriods = table.Column<int>(type: "int", nullable: false),
                    DurationInYears = table.Column<int>(type: "int", nullable: false),
                    TotalCredits = table.Column<int>(type: "int", nullable: false),
                    PlanCreadtedDate = table.Column<DateOnly>(type: "date", nullable: false),
                    LastUpdatedDate = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_careers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "contractTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contractTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Disabilities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Disabilities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "employees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeNumber = table.Column<int>(type: "int", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Rfc = table.Column<string>(type: "nvarchar(13)", maxLength: 13, nullable: false),
                    Curp = table.Column<string>(type: "nvarchar(18)", maxLength: 18, nullable: false),
                    Gender = table.Column<string>(type: "char(1)", nullable: false),
                    BirthDate = table.Column<DateOnly>(type: "date", nullable: false),
                    HighestEducationLevel = table.Column<int>(type: "int", nullable: false),
                    TitleStatus = table.Column<int>(type: "int", nullable: false),
                    IndigenousLanguages = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HasDisability = table.Column<bool>(type: "bit", nullable: false),
                    ExternalXPYears = table.Column<int>(type: "int", nullable: false),
                    FirstHiredDate = table.Column<DateOnly>(type: "date", nullable: false),
                    IsTeacher = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employees", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IndigenousLanguages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Language = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IndigenousLanguages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IndigenousLanguagesSpeakers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SpeakerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LanguageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IndigenousLanguagesSpeakers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "institutionalConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PeriodId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsEvaluated = table.Column<bool>(type: "bit", nullable: false),
                    PublicResults = table.Column<bool>(type: "bit", nullable: false),
                    URLResults = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SEAES_Framework = table.Column<bool>(type: "bit", nullable: false),
                    SEAES_Feedback = table.Column<bool>(type: "bit", nullable: false),
                    Observations = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_institutionalConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "internalAreas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ParentAreaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Type = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_internalAreas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_internalAreas_internalAreas_ParentAreaId",
                        column: x => x.ParentAreaId,
                        principalTable: "internalAreas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "maxDegrees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_maxDegrees", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "periods",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SchoolCycle = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: false),
                    SEP_Period = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_periods", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "permissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "reports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReportType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    TemplateVersion = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AnswersJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "reportsPermissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reportsPermissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SchoolFacility",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Capacity = table.Column<int>(type: "int", nullable: false),
                    InUse = table.Column<bool>(type: "bit", nullable: false),
                    HasInternet = table.Column<bool>(type: "bit", nullable: false),
                    HasComputers = table.Column<bool>(type: "bit", nullable: false),
                    SupportDisabilities = table.Column<bool>(type: "bit", nullable: false),
                    Problems = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchoolFacility", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScolarshipProgram",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OfficialName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    SourceType = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScolarshipProgram", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "students",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EnrollmentNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CURP = table.Column<string>(type: "nvarchar(18)", maxLength: 18, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MiddleName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    BirthDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Gender = table.Column<string>(type: "char(1)", nullable: false),
                    CareerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PeriodIdStart = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InscriptionType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IndigenousLanguages = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PostalCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    EnglishLevel = table.Column<int>(type: "int", nullable: false),
                    MaritalStatus = table.Column<int>(type: "int", nullable: false),
                    NumberOfChildren = table.Column<int>(type: "int", nullable: false),
                    Works = table.Column<bool>(type: "bit", nullable: false),
                    BirthCountry = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BirthState = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_students", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "systemUser",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Password = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LastLogin = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_systemUser", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WithdrawReason",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WithdrawReason", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "courses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CareerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseCode = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Credits = table.Column<int>(type: "int", nullable: false),
                    Cuatrimestre = table.Column<int>(type: "int", nullable: false),
                    IsMandatory = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_courses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_courses_careers_CareerId",
                        column: x => x.CareerId,
                        principalTable: "careers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "institutionalEvaluations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PeriodId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsEvaluated = table.Column<bool>(type: "bit", nullable: false),
                    PublicResults = table.Column<bool>(type: "bit", nullable: false),
                    URLResults = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SEAES_Framework = table.Column<bool>(type: "bit", nullable: false),
                    SEAES_Feedback = table.Column<bool>(type: "bit", nullable: false),
                    Observations = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_institutionalEvaluations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_institutionalEvaluations_periods_PeriodId",
                        column: x => x.PeriodId,
                        principalTable: "periods",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "rolePermissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PermissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rolePermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_rolePermissions_permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "permissions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_rolePermissions_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "roles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "roleReportsPermissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportsPermissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CanEdit = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CanSign = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roleReportsPermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_roleReportsPermissions_reportsPermissions_ReportsPermissionId",
                        column: x => x.ReportsPermissionId,
                        principalTable: "reportsPermissions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_roleReportsPermissions_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "roles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "scolarshipAssigneds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScolarshipProgramId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PeriodId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScolarshipObjective = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FinantialSource = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<float>(type: "real", nullable: false),
                    AssignedDate = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scolarshipAssigneds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_scolarshipAssigneds_ScolarshipProgram_ScolarshipProgramId",
                        column: x => x.ScolarshipProgramId,
                        principalTable: "ScolarshipProgram",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_scolarshipAssigneds_periods_PeriodId",
                        column: x => x.PeriodId,
                        principalTable: "periods",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_scolarshipAssigneds_students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "students",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "studentDisabilities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DisabilityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_studentDisabilities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_studentDisabilities_Disabilities_DisabilityId",
                        column: x => x.DisabilityId,
                        principalTable: "Disabilities",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_studentDisabilities_students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "students",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "titulationProcesses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CareerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TitulationAssessorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreditsEgressDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CertificateTitulationDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TitleExpeditionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    HasProfessionalLicence = table.Column<bool>(type: "bit", nullable: false),
                    ProfessionalLicenceNumber = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_titulationProcesses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_titulationProcesses_careers_CareerId",
                        column: x => x.CareerId,
                        principalTable: "careers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_titulationProcesses_employees_TitulationAssessorId",
                        column: x => x.TitulationAssessorId,
                        principalTable: "employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_titulationProcesses_students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "students",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "userRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_userRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_userRoles_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "roles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_userRoles_systemUser_UserId",
                        column: x => x.UserId,
                        principalTable: "systemUser",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "classGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FacilityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Schedule = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Capacity = table.Column<int>(type: "int", nullable: false),
                    Enrolled = table.Column<int>(type: "int", nullable: false),
                    Weekhours = table.Column<double>(type: "float", nullable: false),
                    PeriodId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CareerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeacherId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsTheory = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_classGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_classGroups_SchoolFacility_FacilityId",
                        column: x => x.FacilityId,
                        principalTable: "SchoolFacility",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_classGroups_careers_CareerId",
                        column: x => x.CareerId,
                        principalTable: "careers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_classGroups_courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "courses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_classGroups_employees_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_classGroups_periods_PeriodId",
                        column: x => x.PeriodId,
                        principalTable: "periods",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "kardex",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PeriodId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Grade = table.Column<double>(type: "float", nullable: false),
                    IsPassed = table.Column<bool>(type: "bit", nullable: false),
                    Opportunity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_kardex", x => x.Id);
                    table.ForeignKey(
                        name: "FK_kardex_courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "courses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_kardex_periods_PeriodId",
                        column: x => x.PeriodId,
                        principalTable: "periods",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_kardex_students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "students",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "courseEnrollment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PeriodId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InscriptionType = table.Column<int>(type: "int", nullable: false),
                    FinalStatus = table.Column<int>(type: "int", nullable: false),
                    WithdrawReasonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_courseEnrollment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_courseEnrollment_WithdrawReason_WithdrawReasonId",
                        column: x => x.WithdrawReasonId,
                        principalTable: "WithdrawReason",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_courseEnrollment_classGroups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "classGroups",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_courseEnrollment_periods_PeriodId",
                        column: x => x.PeriodId,
                        principalTable: "periods",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_courseEnrollment_students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "students",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "tutoringSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TopicDiscussion = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tutoringSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tutoringSessions_classGroups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "classGroups",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tutoringSessions_students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "students",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_classGroups_CareerId",
                table: "classGroups",
                column: "CareerId");

            migrationBuilder.CreateIndex(
                name: "IX_classGroups_CourseId",
                table: "classGroups",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_classGroups_FacilityId_PeriodId",
                table: "classGroups",
                columns: new[] { "FacilityId", "PeriodId" });

            migrationBuilder.CreateIndex(
                name: "IX_classGroups_PeriodId",
                table: "classGroups",
                column: "PeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_classGroups_TeacherId",
                table: "classGroups",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_contractTypes_Name",
                table: "contractTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_courseEnrollment_GroupId",
                table: "courseEnrollment",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_courseEnrollment_PeriodId",
                table: "courseEnrollment",
                column: "PeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_courseEnrollment_StudentId",
                table: "courseEnrollment",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_courseEnrollment_WithdrawReasonId",
                table: "courseEnrollment",
                column: "WithdrawReasonId");

            migrationBuilder.CreateIndex(
                name: "IX_courses_CareerId",
                table: "courses",
                column: "CareerId");

            migrationBuilder.CreateIndex(
                name: "IX_IndigenousLanguagesSpeakers_LanguageId",
                table: "IndigenousLanguagesSpeakers",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_IndigenousLanguagesSpeakers_SpeakerId_LanguageId",
                table: "IndigenousLanguagesSpeakers",
                columns: new[] { "SpeakerId", "LanguageId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_institutionalEvaluations_PeriodId",
                table: "institutionalEvaluations",
                column: "PeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_internalAreas_Name",
                table: "internalAreas",
                column: "Name",
                unique: true,
                filter: "[Name] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_internalAreas_ParentAreaId",
                table: "internalAreas",
                column: "ParentAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_kardex_CourseId",
                table: "kardex",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_kardex_PeriodId",
                table: "kardex",
                column: "PeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_kardex_StudentId",
                table: "kardex",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_maxDegrees_Name",
                table: "maxDegrees",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_permissions_Name",
                table: "permissions",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_reports_ReportType_TemplateVersion",
                table: "reports",
                columns: new[] { "ReportType", "TemplateVersion" });

            migrationBuilder.CreateIndex(
                name: "IX_reportsPermissions_ReportId",
                table: "reportsPermissions",
                column: "ReportId");

            migrationBuilder.CreateIndex(
                name: "IX_rolePermissions_PermissionId",
                table: "rolePermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_rolePermissions_RoleId_PermissionId",
                table: "rolePermissions",
                columns: new[] { "RoleId", "PermissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_roleReportsPermissions_ReportsPermissionId",
                table: "roleReportsPermissions",
                column: "ReportsPermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_roleReportsPermissions_RoleId_ReportsPermissionId",
                table: "roleReportsPermissions",
                columns: new[] { "RoleId", "ReportsPermissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_scolarshipAssigneds_PeriodId",
                table: "scolarshipAssigneds",
                column: "PeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_scolarshipAssigneds_ScolarshipProgramId",
                table: "scolarshipAssigneds",
                column: "ScolarshipProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_scolarshipAssigneds_StudentId",
                table: "scolarshipAssigneds",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_studentDisabilities_DisabilityId",
                table: "studentDisabilities",
                column: "DisabilityId");

            migrationBuilder.CreateIndex(
                name: "IX_studentDisabilities_StudentId",
                table: "studentDisabilities",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_titulationProcesses_CareerId",
                table: "titulationProcesses",
                column: "CareerId");

            migrationBuilder.CreateIndex(
                name: "IX_titulationProcesses_StudentId",
                table: "titulationProcesses",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_titulationProcesses_TitulationAssessorId",
                table: "titulationProcesses",
                column: "TitulationAssessorId");

            migrationBuilder.CreateIndex(
                name: "IX_tutoringSessions_GroupId",
                table: "tutoringSessions",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_tutoringSessions_StudentId",
                table: "tutoringSessions",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_userRoles_RoleId",
                table: "userRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_userRoles_UserId",
                table: "userRoles",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "contractTypes");

            migrationBuilder.DropTable(
                name: "courseEnrollment");

            migrationBuilder.DropTable(
                name: "IndigenousLanguages");

            migrationBuilder.DropTable(
                name: "IndigenousLanguagesSpeakers");

            migrationBuilder.DropTable(
                name: "institutionalConfigurations");

            migrationBuilder.DropTable(
                name: "institutionalEvaluations");

            migrationBuilder.DropTable(
                name: "internalAreas");

            migrationBuilder.DropTable(
                name: "kardex");

            migrationBuilder.DropTable(
                name: "maxDegrees");

            migrationBuilder.DropTable(
                name: "reports");

            migrationBuilder.DropTable(
                name: "rolePermissions");

            migrationBuilder.DropTable(
                name: "roleReportsPermissions");

            migrationBuilder.DropTable(
                name: "scolarshipAssigneds");

            migrationBuilder.DropTable(
                name: "studentDisabilities");

            migrationBuilder.DropTable(
                name: "titulationProcesses");

            migrationBuilder.DropTable(
                name: "tutoringSessions");

            migrationBuilder.DropTable(
                name: "userRoles");

            migrationBuilder.DropTable(
                name: "WithdrawReason");

            migrationBuilder.DropTable(
                name: "permissions");

            migrationBuilder.DropTable(
                name: "reportsPermissions");

            migrationBuilder.DropTable(
                name: "ScolarshipProgram");

            migrationBuilder.DropTable(
                name: "Disabilities");

            migrationBuilder.DropTable(
                name: "classGroups");

            migrationBuilder.DropTable(
                name: "students");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "systemUser");

            migrationBuilder.DropTable(
                name: "SchoolFacility");

            migrationBuilder.DropTable(
                name: "courses");

            migrationBuilder.DropTable(
                name: "employees");

            migrationBuilder.DropTable(
                name: "periods");

            migrationBuilder.DropTable(
                name: "careers");
        }
    }
}
