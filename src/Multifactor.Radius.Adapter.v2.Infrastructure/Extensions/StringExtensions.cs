using System.Text;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Extensions;

public static class StringExtensions
{
    public static string EscapeCharacters(this string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;
    
        var sb = new StringBuilder();
        foreach (char c in value)
        {
            switch (c)
            {
                case '&': sb.Append("\\26"); break;
                case '(': sb.Append("\\28"); break;
                case ')': sb.Append("\\29"); break;
                case '*': sb.Append("\\2a"); break;
                case '\\': sb.Append("\\5c"); break;
                case '\0': sb.Append("\\00"); break;
                case '+':  sb.Append("\\2b"); break;
                case ',':  sb.Append("\\2c"); break;
                case '-':  sb.Append("\\2d"); break;
                case '.':  sb.Append("\\2e"); break;
                case '/':  sb.Append("\\2f"); break;

                case ':':  sb.Append("\\3a"); break;
                case ';':  sb.Append("\\3b"); break;
                case '<':  sb.Append("\\3c"); break;
                case '=':  sb.Append("\\3d"); break;
                case '>':  sb.Append("\\3e"); break;
                case '?':  sb.Append("\\3f"); break;
                case '@':  sb.Append("\\40"); break;

                case '[':  sb.Append("\\5b"); break;
                case ']':  sb.Append("\\5d"); break;
                case '^':  sb.Append("\\5e"); break;
                case '_':  sb.Append("\\5f"); break;
                case '`':  sb.Append("\\60"); break;

                case '{':  sb.Append("\\7b"); break;
                case '|':  sb.Append("\\7c"); break;
                case '}':  sb.Append("\\7d"); break;
                case '~':  sb.Append("\\7e"); break;
                default: sb.Append(c); break;
            }
        }
        return sb.ToString();
    }
}