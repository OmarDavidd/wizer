using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wizer.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "institutions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_institutions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "campuses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    institution_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_campuses", x => x.id);
                    table.ForeignKey(
                        name: "fk_campuses_institution_institution_id",
                        column: x => x.institution_id,
                        principalTable: "institutions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "school_classes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    institution_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    schedule = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    join_code = table.Column<string>(type: "character(6)", fixedLength: true, maxLength: 6, nullable: false),
                    archived_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_school_classes", x => x.id);
                    table.CheckConstraint("ck_school_classes_join_code", "join_code ~ '^[23456789ABCDEFGHJKMNPQRSTUVWXYZ]{6}$'");
                    table.ForeignKey(
                        name: "fk_school_classes_institutions_institution_id",
                        column: x => x.institution_id,
                        principalTable: "institutions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "class_campuses",
                columns: table => new
                {
                    class_id = table.Column<Guid>(type: "uuid", nullable: false),
                    campus_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_class_campuses", x => new { x.class_id, x.campus_id });
                    table.ForeignKey(
                        name: "fk_class_campuses_campuses_campus_id",
                        column: x => x.campus_id,
                        principalTable: "campuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_class_campuses_school_class_class_id",
                        column: x => x.class_id,
                        principalTable: "school_classes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "teams",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    class_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    max_size = table.Column<short>(type: "smallint", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_teams", x => x.id);
                    table.UniqueConstraint("ak_teams_id_class_id", x => new { x.id, x.class_id });
                    table.CheckConstraint("ck_teams_max_size", "max_size BETWEEN 2 AND 10");
                    table.ForeignKey(
                        name: "fk_teams_school_classes_class_id",
                        column: x => x.class_id,
                        principalTable: "school_classes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "whiteboards",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scene = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'[]'::jsonb"),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_whiteboards", x => x.id);
                    table.CheckConstraint("ck_whiteboards_version", "version >= 0");
                    table.ForeignKey(
                        name: "fk_whiteboards_teams_team_id",
                        column: x => x.team_id,
                        principalTable: "teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "academic_profiles",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    career = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    semester = table.Column<short>(type: "smallint", nullable: true),
                    enrollment_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    professional_title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_academic_profiles", x => x.user_id);
                    table.CheckConstraint("ck_academic_profiles_semester", "semester BETWEEN 1 AND 20");
                });

            migrationBuilder.CreateTable(
                name: "assignments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    class_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    instructions = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: false),
                    due_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    mode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    grading_type = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    max_points = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    allow_late_submissions = table.Column<bool>(type: "boolean", nullable: false),
                    late_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    reminder_sent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_assignments", x => x.id);
                    table.CheckConstraint("ck_assignments_grading_type", "grading_type IN ('points', 'pass_fail')");
                    table.CheckConstraint("ck_assignments_late_until", "late_until IS NULL OR (allow_late_submissions AND late_until > due_at)");
                    table.CheckConstraint("ck_assignments_max_points", "(grading_type = 'points' AND max_points BETWEEN 1 AND 100) OR (grading_type = 'pass_fail' AND max_points IS NULL)");
                    table.CheckConstraint("ck_assignments_mode", "mode IN ('individual', 'team')");
                    table.CheckConstraint("ck_assignments_status", "status IN ('draft', 'published')");
                    table.ForeignKey(
                        name: "fk_assignments_school_class_class_id",
                        column: x => x.class_id,
                        principalTable: "school_classes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "chat_channels",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    class_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    team_id = table.Column<Guid>(type: "uuid", nullable: true),
                    assignment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_chat_channels", x => x.id);
                    table.CheckConstraint("ck_chat_channels_owner", "(type = 'class' AND team_id IS NULL AND assignment_id IS NULL) OR (type = 'team' AND team_id IS NOT NULL AND assignment_id IS NULL) OR (type = 'assignment' AND assignment_id IS NOT NULL AND team_id IS NULL)");
                    table.CheckConstraint("ck_chat_channels_type", "type IN ('class', 'team', 'assignment')");
                    table.ForeignKey(
                        name: "fk_chat_channels_assignments_assignment_id",
                        column: x => x.assignment_id,
                        principalTable: "assignments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_chat_channels_school_class_class_id",
                        column: x => x.class_id,
                        principalTable: "school_classes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_chat_channels_team_team_id",
                        column: x => x.team_id,
                        principalTable: "teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "chat_access_grants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    teacher_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    resolved_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    resolved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_chat_access_grants", x => x.id);
                    table.CheckConstraint("ck_chat_access_grants_status", "status IN ('pending', 'approved', 'rejected', 'revoked')");
                    table.ForeignKey(
                        name: "fk_chat_access_grants_team_team_id",
                        column: x => x.team_id,
                        principalTable: "teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "chat_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    channel_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sender_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    content = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    edited_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_chat_messages", x => x.id);
                    table.CheckConstraint("ck_chat_messages_content", "char_length(content) BETWEEN 1 AND 4000");
                    table.ForeignKey(
                        name: "fk_chat_messages_chat_channels_channel_id",
                        column: x => x.channel_id,
                        principalTable: "chat_channels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "class_memberships",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    class_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_class_memberships", x => x.id);
                    table.CheckConstraint("ck_class_memberships_role", "role IN ('teacher', 'student')");
                    table.ForeignKey(
                        name: "fk_class_memberships_school_class_class_id",
                        column: x => x.class_id,
                        principalTable: "school_classes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "external_logins",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false),
                    provider_key = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_external_logins", x => x.id);
                    table.CheckConstraint("ck_external_logins_provider", "provider IN ('google')");
                });

            migrationBuilder.CreateTable(
                name: "files",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    institution_id = table.Column<Guid>(type: "uuid", nullable: false),
                    class_id = table.Column<Guid>(type: "uuid", nullable: true),
                    context_type = table.Column<string>(type: "character varying(19)", maxLength: 19, nullable: false),
                    context_id = table.Column<Guid>(type: "uuid", nullable: false),
                    chat_message_id = table.Column<Guid>(type: "uuid", nullable: true),
                    original_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    content_type = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    object_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    status = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    scanned_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    uploaded_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_files", x => x.id);
                    table.CheckConstraint("ck_files_chat_message_id", "chat_message_id IS NULL OR context_type = 'chat_attachment'");
                    table.CheckConstraint("ck_files_class_id", "(context_type = 'avatar' AND class_id IS NULL) OR (context_type <> 'avatar' AND class_id IS NOT NULL)");
                    table.CheckConstraint("ck_files_context_type", "context_type IN ('class_material', 'assignment_material', 'submission', 'chat_attachment', 'avatar')");
                    table.CheckConstraint("ck_files_size_bytes", "size_bytes > 0");
                    table.CheckConstraint("ck_files_status", "status IN ('uploading', 'pending_scan', 'available', 'rejected', 'scan_failed')");
                    table.ForeignKey(
                        name: "fk_files_chat_messages_chat_message_id",
                        column: x => x.chat_message_id,
                        principalTable: "chat_messages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_files_institutions_institution_id",
                        column: x => x.institution_id,
                        principalTable: "institutions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_files_school_classes_class_id",
                        column: x => x.class_id,
                        principalTable: "school_classes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    institution_id = table.Column<Guid>(type: "uuid", nullable: false),
                    campus_id = table.Column<Guid>(type: "uuid", nullable: true),
                    full_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    platform_role = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    email_verified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    avatar_file_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                    table.CheckConstraint("ck_users_platform_role", "platform_role IN ('student', 'teacher', 'admin')");
                    table.ForeignKey(
                        name: "fk_users_campuses_campus_id",
                        column: x => x.campus_id,
                        principalTable: "campuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_users_files_avatar_file_id",
                        column: x => x.avatar_file_id,
                        principalTable: "files",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_users_institutions_institution_id",
                        column: x => x.institution_id,
                        principalTable: "institutions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "invitation_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    invited_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    used_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_invitation_tokens", x => x.id);
                    table.ForeignKey(
                        name: "fk_invitation_tokens_user_invited_by_id",
                        column: x => x.invited_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_invitation_tokens_user_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "message_mentions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mentioned_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_message_mentions", x => x.id);
                    table.ForeignKey(
                        name: "fk_message_mentions_chat_messages_message_id",
                        column: x => x.message_id,
                        principalTable: "chat_messages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_message_mentions_user_mentioned_user_id",
                        column: x => x.mentioned_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "message_reactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reaction = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_message_reactions", x => x.id);
                    table.CheckConstraint("ck_message_reactions_reaction", "reaction IN ('thumbs_up', 'heart', 'laugh', 'surprised', 'sad', 'party')");
                    table.ForeignKey(
                        name: "fk_message_reactions_chat_messages_message_id",
                        column: x => x.message_id,
                        principalTable: "chat_messages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_message_reactions_user_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(27)", maxLength: 27, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    body = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    data = table.Column<string>(type: "jsonb", nullable: true),
                    read_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notifications", x => x.id);
                    table.CheckConstraint("ck_notifications_type", "type IN ('assignment_published', 'assignment_due_date_changed', 'assignment_due_soon', 'submission_graded', 'team_assigned', 'chat_mention', 'chat_access_requested', 'chat_access_resolved', 'file_rejected')");
                    table.ForeignKey(
                        name: "fk_notifications_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    family_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    is_persistent = table.Column<bool>(type: "boolean", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    replaced_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refresh_tokens", x => x.id);
                    table.ForeignKey(
                        name: "fk_refresh_tokens_refresh_tokens_replaced_by_id",
                        column: x => x.replaced_by_id,
                        principalTable: "refresh_tokens",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_refresh_tokens_user_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "submissions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    assignment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: true),
                    team_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    is_late = table.Column<bool>(type: "boolean", nullable: false),
                    submitted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    submitted_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    points = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    result = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                    feedback = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    graded_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    graded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_submissions", x => x.id);
                    table.CheckConstraint("ck_submissions_grade", "points IS NULL OR result IS NULL");
                    table.CheckConstraint("ck_submissions_owner", "(student_id IS NULL) <> (team_id IS NULL)");
                    table.CheckConstraint("ck_submissions_points", "points IS NULL OR points >= 0");
                    table.CheckConstraint("ck_submissions_result", "result IN ('pass', 'fail')");
                    table.CheckConstraint("ck_submissions_status", "status IN ('draft', 'submitted', 'graded')");
                    table.ForeignKey(
                        name: "fk_submissions_assignments_assignment_id",
                        column: x => x.assignment_id,
                        principalTable: "assignments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_submissions_team_team_id",
                        column: x => x.team_id,
                        principalTable: "teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_submissions_users_graded_by_id",
                        column: x => x.graded_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_submissions_users_student_id",
                        column: x => x.student_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_submissions_users_submitted_by_id",
                        column: x => x.submitted_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "team_members",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    class_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_team_members", x => x.id);
                    table.ForeignKey(
                        name: "fk_team_members_teams_team_id_class_id",
                        columns: x => new { x.team_id, x.class_id },
                        principalTable: "teams",
                        principalColumns: new[] { "id", "class_id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_team_members_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "verification_codes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    purpose = table.Column<string>(type: "character varying(18)", maxLength: 18, nullable: false),
                    code_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    consumed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_verification_codes", x => x.id);
                    table.CheckConstraint("ck_verification_codes_attempts", "attempts >= 0");
                    table.CheckConstraint("ck_verification_codes_purpose", "purpose IN ('email_verification', 'password_reset')");
                    table.ForeignKey(
                        name: "fk_verification_codes_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "submission_links",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    submission_id = table.Column<Guid>(type: "uuid", nullable: false),
                    url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_submission_links", x => x.id);
                    table.ForeignKey(
                        name: "fk_submission_links_submissions_submission_id",
                        column: x => x.submission_id,
                        principalTable: "submissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "submission_member_grades",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    submission_id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    points = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    result = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                    graded_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    graded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_submission_member_grades", x => x.id);
                    table.CheckConstraint("ck_submission_member_grades_grade", "(points IS NOT NULL AND points >= 0 AND result IS NULL) OR (points IS NULL AND result IS NOT NULL)");
                    table.CheckConstraint("ck_submission_member_grades_result", "result IN ('pass', 'fail')");
                    table.ForeignKey(
                        name: "fk_submission_member_grades_submissions_submission_id",
                        column: x => x.submission_id,
                        principalTable: "submissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_submission_member_grades_users_graded_by_id",
                        column: x => x.graded_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_submission_member_grades_users_student_id",
                        column: x => x.student_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_assignments_class_id_due_at",
                table: "assignments",
                columns: new[] { "class_id", "due_at" });

            migrationBuilder.CreateIndex(
                name: "ix_assignments_created_by_id",
                table: "assignments",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "ux_campuses_institution_id_name",
                table: "campuses",
                columns: new[] { "institution_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_chat_access_grants_resolved_by_id",
                table: "chat_access_grants",
                column: "resolved_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_chat_access_grants_revoked_by_id",
                table: "chat_access_grants",
                column: "revoked_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_chat_access_grants_teacher_id",
                table: "chat_access_grants",
                column: "teacher_id");

            migrationBuilder.CreateIndex(
                name: "ix_chat_access_grants_team_id_status",
                table: "chat_access_grants",
                columns: new[] { "team_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ux_chat_access_grants_team_id_pending",
                table: "chat_access_grants",
                column: "team_id",
                unique: true,
                filter: "status = 'pending'");

            migrationBuilder.CreateIndex(
                name: "ux_chat_channels_assignment_id",
                table: "chat_channels",
                column: "assignment_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_chat_channels_class_id_general",
                table: "chat_channels",
                column: "class_id",
                unique: true,
                filter: "type = 'class'");

            migrationBuilder.CreateIndex(
                name: "ux_chat_channels_team_id",
                table: "chat_channels",
                column: "team_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_chat_messages_channel_id_created_at_id",
                table: "chat_messages",
                columns: new[] { "channel_id", "created_at", "id" },
                descending: new[] { false, true, true });

            migrationBuilder.CreateIndex(
                name: "ix_chat_messages_deleted_by_id",
                table: "chat_messages",
                column: "deleted_by_id");

            migrationBuilder.CreateIndex(
                name: "ux_chat_messages_sender_id_client_message_id",
                table: "chat_messages",
                columns: new[] { "sender_id", "client_message_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_class_campuses_campus_id",
                table: "class_campuses",
                column: "campus_id");

            migrationBuilder.CreateIndex(
                name: "ix_class_memberships_user_id",
                table: "class_memberships",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ux_class_memberships_class_id_teacher",
                table: "class_memberships",
                column: "class_id",
                unique: true,
                filter: "role = 'teacher'");

            migrationBuilder.CreateIndex(
                name: "ux_class_memberships_class_id_user_id",
                table: "class_memberships",
                columns: new[] { "class_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_external_logins_provider_provider_key",
                table: "external_logins",
                columns: new[] { "provider", "provider_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_external_logins_user_id_provider",
                table: "external_logins",
                columns: new[] { "user_id", "provider" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_files_chat_message_id",
                table: "files",
                column: "chat_message_id");

            migrationBuilder.CreateIndex(
                name: "ix_files_class_id",
                table: "files",
                column: "class_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_files_context_type_context_id",
                table: "files",
                columns: new[] { "context_type", "context_id" });

            migrationBuilder.CreateIndex(
                name: "ix_files_institution_id",
                table: "files",
                column: "institution_id");

            migrationBuilder.CreateIndex(
                name: "ix_files_status_pending",
                table: "files",
                column: "status",
                filter: "status IN ('uploading', 'pending_scan')");

            migrationBuilder.CreateIndex(
                name: "ix_files_uploaded_by_id",
                table: "files",
                column: "uploaded_by_id");

            migrationBuilder.CreateIndex(
                name: "ux_files_object_key",
                table: "files",
                column: "object_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_invitation_tokens_invited_by_id",
                table: "invitation_tokens",
                column: "invited_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_invitation_tokens_user_id",
                table: "invitation_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ux_invitation_tokens_token_hash",
                table: "invitation_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_message_mentions_mentioned_user_id",
                table: "message_mentions",
                column: "mentioned_user_id");

            migrationBuilder.CreateIndex(
                name: "ux_message_mentions_message_id_mentioned_user_id",
                table: "message_mentions",
                columns: new[] { "message_id", "mentioned_user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_message_reactions_user_id",
                table: "message_reactions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ux_message_reactions_message_id_user_id_reaction",
                table: "message_reactions",
                columns: new[] { "message_id", "user_id", "reaction" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_notifications_user_id_created_at",
                table: "notifications",
                columns: new[] { "user_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_notifications_user_id_unread",
                table: "notifications",
                column: "user_id",
                filter: "read_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_family_id",
                table: "refresh_tokens",
                column: "family_id");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_replaced_by_id",
                table: "refresh_tokens",
                column: "replaced_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_user_id",
                table: "refresh_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ux_refresh_tokens_token_hash",
                table: "refresh_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_school_classes_institution_id",
                table: "school_classes",
                column: "institution_id");

            migrationBuilder.CreateIndex(
                name: "ux_school_classes_join_code",
                table: "school_classes",
                column: "join_code",
                unique: true,
                filter: "archived_at IS NULL AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_submission_links_submission_id",
                table: "submission_links",
                column: "submission_id");

            migrationBuilder.CreateIndex(
                name: "ix_submission_member_grades_graded_by_id",
                table: "submission_member_grades",
                column: "graded_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_submission_member_grades_student_id",
                table: "submission_member_grades",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "ux_submission_member_grades_submission_id_student_id",
                table: "submission_member_grades",
                columns: new[] { "submission_id", "student_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_submissions_assignment_id_status",
                table: "submissions",
                columns: new[] { "assignment_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_submissions_graded_by_id",
                table: "submissions",
                column: "graded_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_submissions_student_id",
                table: "submissions",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "ix_submissions_submitted_by_id",
                table: "submissions",
                column: "submitted_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_submissions_team_id",
                table: "submissions",
                column: "team_id");

            migrationBuilder.CreateIndex(
                name: "ux_submissions_assignment_id_student_id",
                table: "submissions",
                columns: new[] { "assignment_id", "student_id" },
                unique: true,
                filter: "student_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_submissions_assignment_id_team_id",
                table: "submissions",
                columns: new[] { "assignment_id", "team_id" },
                unique: true,
                filter: "team_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_team_members_team_id_class_id",
                table: "team_members",
                columns: new[] { "team_id", "class_id" });

            migrationBuilder.CreateIndex(
                name: "ix_team_members_user_id",
                table: "team_members",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ux_team_members_class_id_user_id",
                table: "team_members",
                columns: new[] { "class_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_team_members_team_id_user_id",
                table: "team_members",
                columns: new[] { "team_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_teams_class_id_name",
                table: "teams",
                columns: new[] { "class_id", "name" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_users_avatar_file_id",
                table: "users",
                column: "avatar_file_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_campus_id",
                table: "users",
                column: "campus_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_institution_id",
                table: "users",
                column: "institution_id");

            migrationBuilder.CreateIndex(
                name: "ux_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_verification_codes_user_id_purpose",
                table: "verification_codes",
                columns: new[] { "user_id", "purpose" },
                unique: true,
                filter: "consumed_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_whiteboards_team_id",
                table: "whiteboards",
                column: "team_id",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_academic_profiles_users_user_id",
                table: "academic_profiles",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_assignments_user_created_by_id",
                table: "assignments",
                column: "created_by_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_chat_access_grants_user_resolved_by_id",
                table: "chat_access_grants",
                column: "resolved_by_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_chat_access_grants_user_revoked_by_id",
                table: "chat_access_grants",
                column: "revoked_by_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_chat_access_grants_user_teacher_id",
                table: "chat_access_grants",
                column: "teacher_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_chat_messages_user_deleted_by_id",
                table: "chat_messages",
                column: "deleted_by_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_chat_messages_user_sender_id",
                table: "chat_messages",
                column: "sender_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_class_memberships_user_user_id",
                table: "class_memberships",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_external_logins_user_user_id",
                table: "external_logins",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_files_user_uploaded_by_id",
                table: "files",
                column: "uploaded_by_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_assignments_user_created_by_id",
                table: "assignments");

            migrationBuilder.DropForeignKey(
                name: "fk_chat_messages_user_deleted_by_id",
                table: "chat_messages");

            migrationBuilder.DropForeignKey(
                name: "fk_chat_messages_user_sender_id",
                table: "chat_messages");

            migrationBuilder.DropForeignKey(
                name: "fk_files_user_uploaded_by_id",
                table: "files");

            migrationBuilder.DropTable(
                name: "academic_profiles");

            migrationBuilder.DropTable(
                name: "chat_access_grants");

            migrationBuilder.DropTable(
                name: "class_campuses");

            migrationBuilder.DropTable(
                name: "class_memberships");

            migrationBuilder.DropTable(
                name: "external_logins");

            migrationBuilder.DropTable(
                name: "invitation_tokens");

            migrationBuilder.DropTable(
                name: "message_mentions");

            migrationBuilder.DropTable(
                name: "message_reactions");

            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "submission_links");

            migrationBuilder.DropTable(
                name: "submission_member_grades");

            migrationBuilder.DropTable(
                name: "team_members");

            migrationBuilder.DropTable(
                name: "verification_codes");

            migrationBuilder.DropTable(
                name: "whiteboards");

            migrationBuilder.DropTable(
                name: "submissions");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "campuses");

            migrationBuilder.DropTable(
                name: "files");

            migrationBuilder.DropTable(
                name: "chat_messages");

            migrationBuilder.DropTable(
                name: "chat_channels");

            migrationBuilder.DropTable(
                name: "assignments");

            migrationBuilder.DropTable(
                name: "teams");

            migrationBuilder.DropTable(
                name: "school_classes");

            migrationBuilder.DropTable(
                name: "institutions");
        }
    }
}
