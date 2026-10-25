using Wizer.Api.Infrastructure.Email;
using Wizer.Api.Modules.Identity.Entities;

namespace Wizer.Api.Modules.Identity.Classes;

/// <summary>Email texts (user-facing, in Spanish).</summary>
public static class IdentityEmails
{
    public static EmailMessage VerificationCode(User user, VerificationPurpose purpose, string code) => purpose switch
    {
        VerificationPurpose.EmailVerification => new EmailMessage(
            user.Email,
            "Tu código de verificación de Wizer",
            $"""
            Hola, {user.FullName}:

            Tu código para verificar el correo en Wizer es {code}.
            Vence en 15 minutos. Si no creaste una cuenta, ignora este mensaje.
            """),
        VerificationPurpose.PasswordReset => new EmailMessage(
            user.Email,
            "Recupera tu contraseña de Wizer",
            $"""
            Hola, {user.FullName}:

            Tu código para crear una nueva contraseña en Wizer es {code}.
            Vence en 15 minutos. Si no lo pediste, ignora este mensaje: tu contraseña no cambió.
            """),
        _ => throw new ArgumentOutOfRangeException(nameof(purpose), purpose, null),
    };

    public static EmailMessage Invitation(User user, string activationLink) => new(
        user.Email,
        "Te invitaron a Wizer",
        $"""
        Hola, {user.FullName}:

        Te crearon una cuenta en Wizer. Para empezar, crea tu contraseña en este enlace:

        {activationLink}

        El enlace sirve una sola vez y vence en 72 horas.
        """);
}
