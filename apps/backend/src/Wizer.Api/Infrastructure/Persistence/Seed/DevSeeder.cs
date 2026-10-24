using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Wizer.Api.Modules.Assignments.Entities;
using Wizer.Api.Modules.Campuses.Entities;
using Wizer.Api.Modules.Chat.Entities;
using Wizer.Api.Modules.Classes.Entities;
using Wizer.Api.Modules.Identity.Entities;
using Wizer.Api.Modules.Submissions.Entities;
using Wizer.Api.Modules.Teams.Entities;
using Wizer.Api.Modules.Users.Entities;
using Wizer.Api.Modules.Whiteboard.Entities;
using Wizer.Api.Shared.Auth;
using Wizer.Api.Shared.Ids;
using Wizer.Api.Shared.Time;

namespace Wizer.Api.Infrastructure.Persistence.Seed;

/// <summary>
/// Development data (<c>.ia/database.md</c> §Seed): 1 institution, 3 campuses, 1 administrator, 2 teachers,
/// 30 students and 3 classes with teams, assignments in every state, submissions, messages and a whiteboard.
/// Every account uses <c>Seed:Password</c>. Idempotent: it does nothing once an institution exists.
/// </summary>
public sealed class DevSeeder(AppDbContext db, IClock clock, IIdGenerator ids, ILogger<DevSeeder> logger)
{
    public const string EnabledKey = "Seed:Enabled";
    public const string PasswordKey = "Seed:Password";

    public const string AdminEmail = "admin@wizer.local";
    public const int StudentCount = 30;

    private const string WhiteboardScene = """
        [{"id":"seed-rect","type":"rectangle","x":120,"y":80,"width":260,"height":140,"angle":0,
          "strokeColor":"#1e1e1e","backgroundColor":"#a5d8ff","fillStyle":"solid","strokeWidth":2,
          "strokeStyle":"solid","roughness":1,"opacity":100,"groupIds":[],"frameId":null,"roundness":null,
          "seed":1,"version":1,"versionNonce":1,"isDeleted":false,"boundElements":null,"updated":1,
          "link":null,"locked":false},
         {"id":"seed-text","type":"text","x":150,"y":130,"width":200,"height":25,"angle":0,
          "strokeColor":"#1e1e1e","backgroundColor":"transparent","fillStyle":"solid","strokeWidth":2,
          "strokeStyle":"solid","roughness":1,"opacity":100,"groupIds":[],"frameId":null,"roundness":null,
          "seed":2,"version":1,"versionNonce":2,"isDeleted":false,"boundElements":null,"updated":1,
          "link":null,"locked":false,"text":"Ideas del proyecto","fontSize":20,"fontFamily":1,
          "textAlign":"left","verticalAlign":"top","containerId":null,"originalText":"Ideas del proyecto",
          "autoResize":true,"lineHeight":1.25}]
        """;

    public async Task SeedAsync(string password, CancellationToken cancellationToken = default)
    {
        if (await db.Set<Institution>().AnyAsync(cancellationToken))
        {
            logger.LogInformation("Development seed skipped: data already exists");
            return;
        }

        var now = clock.UtcNow;
        var institution = new Institution { Id = ids.NewId(), Name = "Instituto de Demostración" };
        var campuses = new[] { "Campus Centro", "Campus Norte", "Campus Sur" }
            .Select(name => new Campus { Id = ids.NewId(), InstitutionId = institution.Id, Name = name })
            .ToArray();
        db.Add(institution);
        db.AddRange(campuses);

        // One hash for every account: development data only. The default hasher ignores the user argument.
        var passwordHash = new PasswordHasher<User>().HashPassword(default!, password);
        User NewUser(string fullName, string email, PlatformRole role, Campus campus) => new()
        {
            Id = ids.NewId(),
            InstitutionId = institution.Id,
            CampusId = campus.Id,
            FullName = fullName,
            Email = email,
            PasswordHash = passwordHash,
            PlatformRole = role,
            IsActive = true,
            EmailVerifiedAt = now,
        };

        var admin = NewUser("Ana Administradora", AdminEmail, PlatformRole.Admin, campuses[0]);
        var teachers = new[]
        {
            NewUser("Laura Docente", "docente1@wizer.local", PlatformRole.Teacher, campuses[0]),
            NewUser("Mario Docente", "docente2@wizer.local", PlatformRole.Teacher, campuses[1]),
        };
        var students = Enumerable.Range(1, StudentCount)
            .Select(n => NewUser($"Alumno {n:00}", $"alumno{n:00}@wizer.local", PlatformRole.Student, campuses[n % campuses.Length]))
            .ToArray();
        db.Add(admin);
        db.AddRange(teachers);
        db.AddRange(students);

        db.AddRange(teachers.Select(teacher => new AcademicProfile { UserId = teacher.Id, ProfessionalTitle = "Licenciatura en Sistemas" }));
        db.AddRange(students.Select((student, index) => new AcademicProfile
        {
            UserId = student.Id,
            Career = "Ingeniería en Sistemas",
            Semester = (short)(index % 8 + 1),
            EnrollmentNumber = $"A{2026000 + index + 1}",
        }));

        // Overlapping rosters, so some students are in two classes.
        SeedClass("Programación Web", "WZR2A4", teachers[0], campuses, students[..12], now);
        SeedClass("Bases de Datos", "WZR3B5", teachers[0], campuses[..2], students[10..22], now);
        SeedClass("Ingeniería de Software", "WZR4C6", teachers[1], campuses[1..], students[20..], now);

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Development seed created {CampusCount} campuses, {UserCount} users and {ClassCount} classes",
            campuses.Length,
            1 + teachers.Length + students.Length,
            3);
    }

    private void SeedClass(string name, string joinCode, User teacher, Campus[] campuses, User[] roster, DateTimeOffset now)
    {
        var schoolClass = new SchoolClass
        {
            Id = ids.NewId(),
            InstitutionId = teacher.InstitutionId,
            Name = name,
            Description = $"Clase de demostración de {name}.",
            Schedule = "Martes y jueves 18:00–20:00",
            JoinCode = joinCode,
        };
        db.Add(schoolClass);
        db.AddRange(campuses.Select(campus => new ClassCampus { ClassId = schoolClass.Id, CampusId = campus.Id }));
        db.Add(new ClassMembership { ClassId = schoolClass.Id, UserId = teacher.Id, Role = ClassRole.Teacher });
        db.AddRange(roster.Select(student => new ClassMembership { ClassId = schoolClass.Id, UserId = student.Id, Role = ClassRole.Student }));

        var general = new ChatChannel { Id = ids.NewId(), ClassId = schoolClass.Id, Type = ChatChannelType.Class };
        db.Add(general);
        AddMessage(general, teacher, "¡Bienvenidos a la clase! Revisen la primera tarea.", now.AddDays(-6));
        AddMessage(general, roster[0], "Gracias, profe. ¿La entrega es en PDF?", now.AddDays(-6).AddMinutes(5));
        AddMessage(general, teacher, "Sí, en PDF o con un enlace.", now.AddDays(-6).AddMinutes(9));

        // Three teams of three; the rest of the roster stays without a team.
        var teams = new List<Team>();
        for (var t = 0; t < 3; t++)
        {
            var team = new Team { Id = ids.NewId(), ClassId = schoolClass.Id, Name = $"Equipo {t + 1}", MaxSize = 5 };
            teams.Add(team);
            db.Add(team);
            var members = roster.Skip(t * 3).Take(3).ToArray();
            db.AddRange(members.Select(member => new TeamMember { TeamId = team.Id, ClassId = schoolClass.Id, UserId = member.Id }));

            var channel = new ChatChannel { Id = ids.NewId(), ClassId = schoolClass.Id, Type = ChatChannelType.Team, TeamId = team.Id };
            db.Add(channel);
            AddMessage(channel, members[0], "¿Nos repartimos las partes del trabajo?", now.AddDays(-3));
            AddMessage(channel, members[1], "De acuerdo, yo hago la introducción.", now.AddDays(-3).AddMinutes(2));

            db.Add(new TeamWhiteboard { TeamId = team.Id, Scene = t == 0 ? WhiteboardScene : "[]", Version = t == 0 ? 1 : 0 });
        }

        SeedAssignments(schoolClass, teacher, roster, teams, now);
    }

    private void SeedAssignments(SchoolClass schoolClass, User teacher, User[] roster, List<Team> teams, DateTimeOffset now)
    {
        Assignment NewAssignment(string title, AssignmentMode mode, GradingType grading, DateTimeOffset dueAt, AssignmentStatus status) => new()
        {
            Id = ids.NewId(),
            ClassId = schoolClass.Id,
            Title = title,
            Instructions = $"Consigna de demostración: {title}.",
            DueAt = dueAt,
            Mode = mode,
            GradingType = grading,
            MaxPoints = grading == GradingType.Points ? 10 : null,
            Status = status,
            PublishedAt = status == AssignmentStatus.Published ? now.AddDays(-7) : null,
            CreatedById = teacher.Id,
            CreatedAt = now.AddDays(-7),
        };

        var draft = NewAssignment("Trabajo final", AssignmentMode.Team, GradingType.Points, now.AddDays(30), AssignmentStatus.Draft);
        var open = NewAssignment("Ensayo individual", AssignmentMode.Individual, GradingType.Points, now.AddDays(3), AssignmentStatus.Published);
        var teamLate = NewAssignment("Prototipo en equipo", AssignmentMode.Team, GradingType.PassFail, now.AddDays(-1), AssignmentStatus.Published);
        teamLate.AllowLateSubmissions = true;
        teamLate.LateUntil = now.AddDays(2);
        var closed = NewAssignment("Cuestionario inicial", AssignmentMode.Individual, GradingType.Points, now.AddDays(-5), AssignmentStatus.Published);
        db.AddRange(draft, open, teamLate, closed);

        // Open assignment: one submitted, one draft.
        var submitted = new Submission
        {
            Id = ids.NewId(),
            AssignmentId = open.Id,
            StudentId = roster[0].Id,
            Status = SubmissionStatus.Submitted,
            SubmittedAt = now.AddHours(-5),
            SubmittedById = roster[0].Id,
        };
        db.Add(submitted);
        db.Add(new SubmissionLink { SubmissionId = submitted.Id, Url = "https://example.com/ensayo", Title = "Borrador del ensayo" });
        db.Add(new Submission { AssignmentId = open.Id, StudentId = roster[1].Id, Status = SubmissionStatus.Draft });

        // Team assignment past its due date: one team on time, one late.
        db.Add(new Submission { AssignmentId = teamLate.Id, TeamId = teams[0].Id, Status = SubmissionStatus.Submitted, SubmittedAt = now.AddDays(-2), SubmittedById = roster[0].Id });
        db.Add(new Submission { AssignmentId = teamLate.Id, TeamId = teams[1].Id, Status = SubmissionStatus.Submitted, IsLate = true, SubmittedAt = now.AddHours(-3), SubmittedById = roster[3].Id });

        // Closed assignment: graded, plus a 0 for a student who never submitted (BR-GRD-03).
        db.Add(new Submission
        {
            AssignmentId = closed.Id,
            StudentId = roster[0].Id,
            Status = SubmissionStatus.Graded,
            SubmittedAt = now.AddDays(-6),
            SubmittedById = roster[0].Id,
            Points = 8.5m,
            Feedback = "Buen trabajo. Revisa la conclusión.",
            GradedById = teacher.Id,
            GradedAt = now.AddDays(-4),
        });
        db.Add(new Submission
        {
            AssignmentId = closed.Id,
            StudentId = roster[2].Id,
            Status = SubmissionStatus.Graded,
            Points = 0,
            Feedback = "Sin entrega.",
            GradedById = teacher.Id,
            GradedAt = now.AddDays(-4),
        });
    }

    private void AddMessage(ChatChannel channel, User sender, string content, DateTimeOffset sentAt) =>
        db.Add(new ChatMessage
        {
            ChannelId = channel.Id,
            SenderId = sender.Id,
            ClientMessageId = ids.NewId(),
            Content = content,
            CreatedAt = sentAt,
        });
}
