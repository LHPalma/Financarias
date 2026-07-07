namespace Financarias.Application.Common.Messaging;

/// <summary>
/// Marca um objeto como comando de escrita e amarra, em tempo de compilação,
/// o tipo de resultado (<typeparamref name="TResult"/>) que ele produz.
/// </summary>
public interface ICommand<TResult>
{
}
