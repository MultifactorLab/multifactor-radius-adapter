using System.Text;

namespace Multifactor.Radius.Adapter.v2.Shared.Extensions;

public static class HttpRequestMessageExtension
{
    public static HttpRequestMessage CloneHttpRequestMessage(this HttpRequestMessage original)
    {
        var clone = new HttpRequestMessage(original.Method, original.RequestUri);
        
        // Копируем содержимое
        if (original.Content != null)
        {
            // Для StreamContent и ByteArrayContent используем оригинал
            if (original.Content is StreamContent || 
                original.Content is ByteArrayContent ||
                original.Content is StringContent)
            {
                clone.Content = original.Content;
            }
            else
            {
                // Для других типов читаем и создаем новое содержимое
                var content = original.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                clone.Content = new StringContent(content, Encoding.UTF8, 
                    original.Content.Headers.ContentType?.MediaType ?? "application/json");
            }
        }
        
        // Копируем заголовки
        foreach (var header in original.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }
        
        // Копируем свойства запроса
        foreach (var prop in original.Options)
        {
            clone.Options.TryAdd(prop.Key, prop.Value);
        }
        
        // Копируем версию
        clone.Version = original.Version;
        
        return clone;
    }
}
