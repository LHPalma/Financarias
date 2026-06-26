namespace Financarias.Application.Common.Messaging;

/// <summary>
/// Marca um objeto como query de leitura e amarra, em tempo de compilação,
/// o tipo de resultado (<typeparamref name="TResult"/>) que ela produz.
/// </summary>
public interface IQuery<TResult>
{
}