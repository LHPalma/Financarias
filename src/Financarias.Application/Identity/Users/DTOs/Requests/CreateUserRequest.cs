namespace Financarias.Application.Identity.Users.DTOs.Requests;

/// <summary>Dados para criar um usuário.</summary>
/// <param name="Name">Nome de exibição. Obrigatório; espaços nas pontas são removidos.</param>
/// <param name="Email">E-mail de acesso, único no sistema. É normalizado para minúsculas.</param>
/// <param name="Password">Senha em texto puro, que precisa cumprir a política: de 8 a 128 caracteres, com letra minúscula, letra maiúscula, dígito e caractere especial. Nunca é devolvida pela API.</param>
public sealed record CreateUserRequest(
    string Name,
    string Email,
    string Password);
